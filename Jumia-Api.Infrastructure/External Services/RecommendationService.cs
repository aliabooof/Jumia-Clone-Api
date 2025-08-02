using AutoMapper;
using Jumia_Api.Application.Dtos.ProductDtos.Get;
using Jumia_Api.Application.Interfaces;
using Jumia_Api.Domain.Interfaces.UnitOfWork;
using Jumia_Api.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jumia_Api.Infrastructure.External_Services
{
    public class RecommendationOptions
    {
        public int BasicCandidatePoolSize { get; set; } = 50;
        public int SimilarPerInteraction { get; set; } = 8;
        public int FinalRecommendationCount { get; set; } = 6;
        public TimeSpan AiTimeout { get; set; } = TimeSpan.FromSeconds(20);
        public TimeSpan BasicTimeout { get; set; } = TimeSpan.FromSeconds(15);
        public string AiEndpoint { get; set; } = "http://localhost:11434/api/generate";
        public string AiModel { get; set; } = "qwen3:0.6b";
        public int AiMinimumValidPicks { get; set; } = 4;
        public int MinimumInteractionsThreshold { get; set; } = 3; // Minimum interactions to use personalized recommendations
        public decimal PriceRangeTolerancePercentage { get; set; } = 30m; // ±30% price tolerance
    }

    public class UserPreferenceProfile
    {
        public Dictionary<int, int> CategoryPreferences { get; set; } = new();
        public Dictionary<int, int> SellerPreferences { get; set; } = new();
        public decimal AveragePriceRange { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public int TotalInteractions { get; set; }
        public DateTime LastInteractionDate { get; set; }
    }

    public class RecommendationService : IRecommendationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly HttpClient _httpClient;
        private readonly ILogger<RecommendationService> _logger;
        private readonly RecommendationOptions _options;

        public RecommendationService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IHttpClientFactory httpClientFactory,
            IOptions<RecommendationOptions> options,
            ILogger<RecommendationService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpClient = httpClientFactory.CreateClient("AIClient");
            _logger = logger;
            _options = options.Value;

            _httpClient.Timeout = _options.AiTimeout;
        }

        public async Task<IEnumerable<ProductDetailsDto>> GetRecommendationsAsync(int userId)
        {
            try
            {
                using var basicCts = new CancellationTokenSource(_options.BasicTimeout);

                // Step 1: Analyze user interaction history and build preference profile
                var userProfile = await BuildUserPreferenceProfileAsync(userId);
                _logger.LogInformation("User {UserId}: interaction count = {Count}, avg price = ${AvgPrice:F2}",
                    userId, userProfile.TotalInteractions, userProfile.AveragePriceRange);

                // Step 2: Check if user has enough interactions for personalized recommendations
                if (userProfile.TotalInteractions < _options.MinimumInteractionsThreshold)
                {
                    _logger.LogInformation("User {UserId}: insufficient interactions ({Count}), using top-selling products",
                        userId, userProfile.TotalInteractions);
                    return await GetTopSellingProductsAsync(_options.FinalRecommendationCount);
                }

                // Step 3: Get intelligently filtered and ranked candidates
                var rankedCandidates = await GetIntelligentlyRankedCandidatesAsync(userId, userProfile, basicCts.Token);

                if (!rankedCandidates.Any())
                {
                    _logger.LogWarning("User {UserId}: no suitable candidates found, falling back to top sellers", userId);
                    return await GetTopSellingProductsAsync(_options.FinalRecommendationCount);
                }

                // Step 4: Send top candidates to AI with timeout handling
                var aiRecommendations = await GetAIRecommendationsWithTimeoutAsync(userId, rankedCandidates, userProfile, basicCts.Token);

                if (aiRecommendations != null && aiRecommendations.Any())
                {
                    _logger.LogInformation("User {UserId}: AI recommendations successful ({Count} products)",
                        userId, aiRecommendations.Count());
                    return aiRecommendations;
                }

                // Step 5: AI timeout/failure fallback - return top ranked candidates
                _logger.LogWarning("User {UserId}: AI timeout or failure, using top ranked candidates", userId);
                return _mapper.Map<IEnumerable<ProductDetailsDto>>(
                    rankedCandidates.Take(_options.FinalRecommendationCount));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in recommendation pipeline for user {UserId}", userId);
                return await GetTopSellingProductsAsync(_options.FinalRecommendationCount);
            }
        }

        #region User Preference Analysis

        private async Task<UserPreferenceProfile> BuildUserPreferenceProfileAsync(int userId)
        {
            var profile = new UserPreferenceProfile();

            try
            {
                // Fetch all user interaction data
                var cartTask = _unitOfWork.CartRepo.GetCustomerCartAsync(userId);
                var ordersTask = _unitOfWork.OrderRepo.GetByCustomerIdAsync(userId);
                var wishlistTask = _unitOfWork.WishlistRepo.GetCustomerWishlistAsync(userId);

                await Task.WhenAll(cartTask, ordersTask, wishlistTask);

                var cart = await cartTask;
                var orders = await ordersTask;
                var wishlist = await wishlistTask;

                var allInteractions = new List<(Product Product, int Weight, DateTime Date)>();

                // Process cart items (weight: 1)
                if (cart?.CartItems?.Any() == true)
                {
                    foreach (var item in cart.CartItems.Where(i => i.Product != null))
                    {
                        allInteractions.Add((item.Product, 1, cart.CreatedAt));
                    }
                }

                // Process order items (weight: 3 - highest as they actually purchased)
                if (orders?.Any() == true)
                {
                    foreach (var order in orders)
                    {
                        foreach (var subOrder in order.SubOrders)
                        {
                            foreach (var item in subOrder.OrderItems.Where(i => i.Product != null))
                            {
                                allInteractions.Add((item.Product, 3 * item.Quantity, order.CreatedAt));
                            }
                        }
                    }
                }

                // Process wishlist items (weight: 2)
                if (wishlist?.WishlistItems?.Any() == true)
                {
                    foreach (var item in wishlist.WishlistItems.Where(i => i.Product != null))
                    {
                        allInteractions.Add((item.Product, 2, item.AddedAt));
                    }
                }

                if (!allInteractions.Any())
                    return profile;

                // Analyze preferences
                profile.TotalInteractions = allInteractions.Count;
                profile.LastInteractionDate = allInteractions.Max(i => i.Date);

                // Category preferences (weighted)
                profile.CategoryPreferences = allInteractions
                    .GroupBy(i => i.Product.CategoryId)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Weight));

                // Seller preferences (weighted)
                profile.SellerPreferences = allInteractions
                    .GroupBy(i => i.Product.SellerId)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Weight));

                // Price analysis
                var prices = allInteractions.Select(i => i.Product.BasePrice).ToList();
                profile.AveragePriceRange = prices.Average();
                profile.MinPrice = prices.Min();
                profile.MaxPrice = prices.Max();

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building user preference profile for user {UserId}", userId);
                return profile;
            }
        }

        #endregion

        #region Intelligent Filtering and Ranking

        private async Task<List<Product>> GetIntelligentlyRankedCandidatesAsync(int userId, UserPreferenceProfile profile, CancellationToken ct)
        {
            try
            {
                // Get all available products
                var allProducts = (await _unitOfWork.ProductRepo.GetAllAsync())
                    .Where(p => p.StockQuantity > 0 && p.IsAvailable && p.ApprovalStatus == "approved")
                    .ToList();

                // Get products user has already interacted with to exclude them
                var interactedProductIds = await GetUserInteractedProductIdsAsync(userId);

                // Filter out already interacted products
                var candidateProducts = allProducts
                    .Where(p => !interactedProductIds.Contains(p.ProductId))
                    .ToList();

                // Calculate smart ranking scores
                var scoredProducts = candidateProducts
                    .Select(product => new
                    {
                        Product = product,
                        Score = CalculateProductRecommendationScore(product, profile)
                    })
                    .Where(sp => sp.Score > 0) // Only include products with positive scores
                    .OrderByDescending(sp => sp.Score)
                    .ThenByDescending(sp => sp.Product.AverageRating) // Secondary sort by rating
                    .Take(_options.BasicCandidatePoolSize)
                    .Select(sp => sp.Product)
                    .ToList();

                _logger.LogInformation("User {UserId}: filtered {FilteredCount} candidates from {TotalCount} available products",
                    userId, scoredProducts.Count, candidateProducts.Count);

                return scoredProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in intelligent candidate ranking for user {UserId}", userId);
                return new List<Product>();
            }
        }

        private double CalculateProductRecommendationScore(Product product, UserPreferenceProfile profile)
        {
            double score = 0;

            // Category preference score (40% weight)
            if (profile.CategoryPreferences.TryGetValue(product.CategoryId, out var categoryWeight))
            {
                score += (categoryWeight / (double)profile.TotalInteractions) * 40;
            }

            // Seller preference score (20% weight)
            if (profile.SellerPreferences.TryGetValue(product.SellerId, out var sellerWeight))
            {
                score += (sellerWeight / (double)profile.TotalInteractions) * 20;
            }

            // Price similarity score (30% weight)
            var priceScore = CalculatePriceAffinityScore(product.BasePrice, profile);
            score += priceScore * 30;

            // Product quality indicators (10% weight)
            var qualityScore = Math.Min(product.AverageRating / 5.0, 1.0); // Normalize to 0-1
            score += qualityScore * 10;

            // Boost for discounted items
            if (product.DiscountPercentage > 0)
            {
                score += Math.Min((double)product.DiscountPercentage / 100.0 * 5, 5); // Up to 5 bonus points
            }

            return score;
        }

        private double CalculatePriceAffinityScore(decimal productPrice, UserPreferenceProfile profile)
        {
            if (profile.AveragePriceRange <= 0) return 0.5; // Neutral score if no price history

            var tolerance = profile.AveragePriceRange * (_options.PriceRangeTolerancePercentage / 100m);
            var lowerBound = profile.AveragePriceRange - tolerance;
            var upperBound = profile.AveragePriceRange + tolerance;

            // Perfect score if within preferred range
            if (productPrice >= lowerBound && productPrice <= upperBound)
                return 1.0;

            // Calculate score based on distance from preferred range
            var distance = productPrice < lowerBound
                ? lowerBound - productPrice
                : productPrice - upperBound;

            var maxAcceptableDistance = profile.AveragePriceRange; // Max distance = avg price
            var distanceRatio = (double)(distance / maxAcceptableDistance);

            return Math.Max(0, 1.0 - distanceRatio);
        }

        private async Task<HashSet<int>> GetUserInteractedProductIdsAsync(int userId)
        {
            var cartTask = _unitOfWork.CartRepo.GetCustomerCartAsync(userId);
            var ordersTask = _unitOfWork.OrderRepo.GetByCustomerIdAsync(userId);
            var wishlistTask = _unitOfWork.WishlistRepo.GetCustomerWishlistAsync(userId);

            await Task.WhenAll(cartTask, ordersTask, wishlistTask);

            var productIds = new HashSet<int>();

            var cart = await cartTask;
            if (cart?.CartItems?.Any() == true)
                productIds.UnionWith(cart.CartItems.Select(i => i.ProductId));

            var orders = await ordersTask;
            if (orders?.Any() == true)
            {
                var orderProductIds = orders
                    .SelectMany(o => o.SubOrders)
                    .SelectMany(so => so.OrderItems)
                    .Select(oi => oi.ProductId);
                productIds.UnionWith(orderProductIds);
            }

            var wishlist = await wishlistTask;
            if (wishlist?.WishlistItems?.Any() == true)
                productIds.UnionWith(wishlist.WishlistItems.Select(i => i.ProductId));

            return productIds;
        }

        #endregion

        #region AI Processing with Timeout

        private async Task<IEnumerable<ProductDetailsDto>?> GetAIRecommendationsWithTimeoutAsync(
            int userId,
            IEnumerable<Product> candidateProducts,
            UserPreferenceProfile profile,
            CancellationToken parentToken)
        {
            using var aiCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
            aiCts.CancelAfter(_options.AiTimeout);

            try
            {
                _logger.LogInformation("User {UserId}: sending {Count} candidates to AI for final selection",
                    userId, candidateProducts.Count());

                var prompt = ConstructEnhancedPromptForCandidates(profile, candidateProducts);
                var aiResponseText = await GetAIResponseAsync(prompt, aiCts.Token);

                if (string.IsNullOrWhiteSpace(aiResponseText))
                {
                    _logger.LogWarning("User {UserId}: AI returned empty response", userId);
                    return null;
                }

                var recommendedProducts = ParseAIResponse(aiResponseText, candidateProducts).ToList();

                if (recommendedProducts.Count < _options.AiMinimumValidPicks)
                {
                    _logger.LogWarning("User {UserId}: AI returned insufficient picks ({Count} < {Threshold})",
                        userId, recommendedProducts.Count, _options.AiMinimumValidPicks);
                    return null;
                }

                var finalRecommendations = recommendedProducts
                    .Where(p => p.StockQuantity > 0)
                    .Take(_options.FinalRecommendationCount)
                    .ToList();

                return _mapper.Map<IEnumerable<ProductDetailsDto>>(finalRecommendations);
            }
            catch (OperationCanceledException) when (aiCts.Token.IsCancellationRequested)
            {
                _logger.LogWarning("User {UserId}: AI request timed out after {Timeout}s", userId, _options.AiTimeout.TotalSeconds);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {UserId}: AI processing failed", userId);
                return null;
            }
        }

        private string ConstructEnhancedPromptForCandidates(UserPreferenceProfile profile, IEnumerable<Product> candidateProducts)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are selecting the best product recommendations based on user preferences and behavior patterns.");
            sb.AppendLine();

            sb.AppendLine("USER PREFERENCE PROFILE:");
            sb.AppendLine($"- Total interactions: {profile.TotalInteractions}");
            sb.AppendLine($"- Average preferred price: ${profile.AveragePriceRange:F2}");
            sb.AppendLine($"- Price range: ${profile.MinPrice:F2} - ${profile.MaxPrice:F2}");

            if (profile.CategoryPreferences.Any())
            {
                sb.AppendLine("- Preferred categories (by interaction weight):");
                foreach (var cat in profile.CategoryPreferences.OrderByDescending(x => x.Value).Take(3))
                {
                    sb.AppendLine($"  * Category {cat.Key}: {cat.Value} interactions");
                }
            }

            if (profile.SellerPreferences.Any())
            {
                sb.AppendLine("- Preferred sellers (by interaction weight):");
                foreach (var seller in profile.SellerPreferences.OrderByDescending(x => x.Value).Take(3))
                {
                    sb.AppendLine($"  * Seller {seller.Key}: {seller.Value} interactions");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"CANDIDATE PRODUCTS (pre-filtered and ranked by relevance):");
            foreach (var product in candidateProducts)
            {
                var discountInfo = product.DiscountPercentage > 0 ? $" (DISCOUNT: {product.DiscountPercentage}%)" : "";
                sb.AppendLine($"{product.ProductId}: {product.Name} | Cat: {product.CategoryId} | Seller: {product.SellerId} | ${product.BasePrice} | Rating: {product.AverageRating:F1}/5{discountInfo}");
            }

            sb.AppendLine();
            sb.AppendLine("SELECTION CRITERIA:");
            sb.AppendLine("- Choose products that best match the user's demonstrated preferences");
            sb.AppendLine("- Consider price compatibility with user's history");
            sb.AppendLine("- Prioritize highly-rated products");
            sb.AppendLine("- Include variety across different product types if possible");
            sb.AppendLine("- Favor discounted items when preferences are similar");
            sb.AppendLine();
            sb.AppendLine($"Return EXACTLY {_options.FinalRecommendationCount} product IDs as a comma-separated list (no explanations):");
            sb.AppendLine("Format: 123,456,789,101,112,131");

            return sb.ToString();
        }

        private async Task<string> GetAIResponseAsync(string prompt, CancellationToken ct)
        {
            try
            {
                var payload = new
                {
                    model = _options.AiModel,
                    prompt,
                    stream = false,
                    options = new { temperature = 0.3, top_p = 0.8 } // Lower temperature for more consistent results
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, _options.AiEndpoint)
                {
                    Content = JsonContent.Create(payload)
                };

                using var response = await _httpClient.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<AIResponse>(cancellationToken: ct);
                return result?.Response ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get AI response");
                return string.Empty;
            }
        }

        private IEnumerable<Product> ParseAIResponse(string response, IEnumerable<Product> candidates)
        {
            try
            {
                var candidateDict = candidates.ToDictionary(p => p.ProductId);

                var productIds = response
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.TryParse(id.Trim(), out var res) ? res : -1)
                    .Where(id => id > 0 && candidateDict.ContainsKey(id))
                    .Distinct()
                    .Take(_options.FinalRecommendationCount)
                    .ToArray();

                return productIds.Select(id => candidateDict[id]).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse AI response: {Response}", response);
                return Enumerable.Empty<Product>();
            }
        }

        #endregion

        #region Fallback Methods

        private async Task<IEnumerable<ProductDetailsDto>> GetTopSellingProductsAsync(int count)
        {
            try
            {
                var allOrders = await _unitOfWork.OrderRepo.GetAllWithDetailsAsync();

                var productSales = allOrders
                    .SelectMany(o => o.SubOrders)
                    .SelectMany(so => so.OrderItems)
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        TotalQuantity = g.Sum(oi => oi.Quantity),
                        TotalRevenue = g.Sum(oi => oi.TotalPrice)
                    })
                    .OrderByDescending(ps => ps.TotalQuantity)
                    .ThenByDescending(ps => ps.TotalRevenue)
                    .Take(count * 2) // Get more to account for out-of-stock items
                    .ToList();

                var productIds = productSales.Select(ps => ps.ProductId).ToList();
                var products = (await _unitOfWork.ProductRepo.GetbyIdsWithVariantsAndAttributesAsync(productIds))
                    .Where(p => p.StockQuantity > 0 && p.IsAvailable)
                    .OrderByDescending(p => productSales.FirstOrDefault(ps => ps.ProductId == p.ProductId)?.TotalQuantity ?? 0)
                    .Take(count)
                    .ToList();

                _logger.LogInformation("Returning {Count} top-selling products as fallback", products.Count);
                return _mapper.Map<IEnumerable<ProductDetailsDto>>(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top selling products");
                return await GetRandomProductsAsync(count);
            }
        }

        private async Task<IEnumerable<ProductDetailsDto>> GetRandomProductsAsync(int count)
        {
            var allProducts = await _unitOfWork.ProductRepo.GetAllAsync();
            var available = allProducts
                .Where(p => p.StockQuantity > 0 && p.IsAvailable && p.ApprovalStatus == "approved")
                .ToList();

            var randomProducts = available
                .OrderBy(_ => Random.Shared.Next())
                .Take(count);

            return _mapper.Map<IEnumerable<ProductDetailsDto>>(randomProducts);
        }

        #endregion

        private class AIResponse
        {
            [JsonProperty("response")]
            public string Response { get; set; }
        }
    }
}