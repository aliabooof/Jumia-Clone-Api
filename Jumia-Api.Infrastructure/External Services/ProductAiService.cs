using AutoMapper;
using Jumia_Api.Application.Dtos.ProductDtos;
using Jumia_Api.Application.Dtos.ProductDtos.Get;
using Jumia_Api.Application.Interfaces;
using Jumia_Api.Domain.Interfaces.UnitOfWork;
using Jumia_Api.Domain.Models;
using Microsoft.Extensions.AI;
using Newtonsoft.Json;
using OllamaSharp;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SymSpell;

namespace Jumia_Api.Application.Services
{
    public class ProductAiService : IProductAiService
    {
        // set up the client
        private readonly Uri uri;
        private readonly IMapper _mapper;
        private readonly SymSpell _symSpell;


        // select a model which should be used for further operations

        private readonly IUnitOfWork _unitOfWork;

        private readonly QdrantClient _qdrantClient;
        public ProductAiService(
                                IUnitOfWork unitOfWork, QdrantClient qdrantClient, IMapper mapper)
        {
            uri = new Uri("http://localhost:11434");

            _unitOfWork = unitOfWork;
            _qdrantClient = qdrantClient;
            _mapper = mapper;
            _symSpell = new SymSpell(maxDictionaryEditDistance:4,prefixLength:7);
            string dictionaryPath = "D:\\Repos\\Jumia-Api\\Jumia-Api.Api\\wwwroot\\symSpell\\frequency_dictionary_en_82_765.txt";
            const int termIndex = 0;    // Column index of the word in the dictionary
            const int countIndex = 1;   // Column index of the frequency

            if (!_symSpell.LoadDictionary(dictionaryPath, termIndex, countIndex))
            {
                throw new Exception("Failed to load SymSpell dictionary.");
            }
            Console.WriteLine(_symSpell);
        }

        //public async Task<ProductAttributeValueDto> ParseQueryToFilterAsync(string query)
        //{
        //    var categories = await _unitOfWork.CategoryRepo.GetAllAsync();
        //    var prompt = BuildPrompt(query, categories);

        //    var response = await _openAiClient.CompleteAsync(prompt);
        //    return JsonConvert.DeserializeObject<ProductAttributeValueDto>(response);
        //}

        //public async Task<ProductFilterRequestDto> GetSimilarProductsFilterAsync(int productId)
        //{
        //    var product = await _unitOfWork.ProductRepo.GetByIdAsync(productId);
        //    if (product == null) throw new Exception("Product not found");

        //    var prompt = $"""
        //    You are an AI assistant. Create a product filter JSON to find items similar to:
        //    - Name: "{product.Name}"
        //    - Attributes: {JsonConvert.SerializeObject(product.productAttributeValues)}
        //    - Category: {product.Category.Name}
        //    """;

        //    var response = await _openAiClient.CompleteAsync(prompt);
        //    return JsonConvert.DeserializeObject<ProductAttributeValueDto>(response);
        //}

        //private string BuildPrompt(string query, IEnumerable<Category> categories)
        //{
        //    var categoryList = JsonConvert.SerializeObject(categories.Select(c => c.Name));
        //    return $@"
        //    You are a smart e-commerce assistant.
        //    Available categories: {categoryList}

        //    User Query: ""{query}""

        //    Extract category, attributes, price, and sorting in JSON:
        //    {{
        //        ""categoryIds"": [int],
        //        ""attributes"": {{""Color"": ""Red"", ""Size"": ""M""}},
        //        ""minPrice"": decimal?,
        //        ""maxPrice"": decimal?,
        //        ""onlyAvailable"": bool,
        //        ""sortBy"": ""priceAsc|priceDesc|newest|popularity""
        //    }}";
        //}


        public async Task<string> AnswerProductQuestionAsync(string question, int? productId = null)
        {
            IChatClient chatClient = CreateChatClient(uri, "qwen3:0.6b");

            string context;

            if (productId.HasValue)
            {
                var product = await _unitOfWork.ProductRepo.GetWithVariantsAndAttributesAsync(productId.Value);
                Console.WriteLine(product.ProductVariants.Count);
                if (product == null) throw new Exception("Product not found");

                context = $"""
            Product Details:
            Name: {product.Name}
            Description: {product.Description}
            Price: {product.BasePrice:C}
            Attributes: {JsonConvert.SerializeObject(product.productAttributeValues.Select(a => new { a.ProductAttribute.Name, a.Value }))}
            Variants: {JsonConvert.SerializeObject(product.ProductVariants.Select(v => new { v.VariantName, v.Price, v.StockQuantity }))}
            """;
            }
            else
            {
                var allProducts = await _unitOfWork.ProductRepo.GetAllAsync();
                context = $"""
            Catalog Summary:
            {string.Join("\n", allProducts.Select(p => $"- {p.Name}: {p.Description} (${p.BasePrice})"))}
            """;
            }

            var prompt = $"""
            You are a helpful e-commerce assistant.
            Context:
            {context}

            User Question: "{question}"

            Answer concisely and use only the provided context.
         """;

            var response = await chatClient.GetResponseAsync(prompt);
            return response.Messages.FirstOrDefault()?.Text;
        }

        public async Task<IEnumerable<ProductsUIDto>> SemanticSearch(string query, int pageNumber = 1, int pageSize = 20)
        {
            query = NormalizeQuery(query);
            query = CorrectQueryTypos(query);
            Console.WriteLine(query);

            IEmbeddingGenerator<string, Embedding<float>> embeddingClient = CreateEmbeddingClient(uri, "nomic-embed-text");
            var embeddingResponse = await  embeddingClient.GenerateAsync(query);


            float[] vector = embeddingResponse.Vector.ToArray();
            vector = NormalizeVector(vector);

            var searchResult = await _qdrantClient.SearchAsync(
                collectionName: "products",
                vector: vector,
                limit: (ulong)pageSize,
                offset: (ulong) ((pageNumber - 1) * pageSize),
                scoreThreshold: 0.5f
            );


            var productIds = searchResult.Select(r => (int)r.Id.Num).ToList();
            var products = await _unitOfWork.ProductRepo.GetbyIdsWithVariantsAndAttributesAsync(productIds);
            products = products.Where(p => p.IsAvailable&& p.ApprovalStatus.ToLower()=="approved") // Filter only available products
                .OrderByDescending(p => searchResult.FirstOrDefault(r => r.Id.Num == (ulong)p.ProductId)?.Score ?? 0)
                .ToList();
            var productsDto = _mapper.Map<IEnumerable<ProductsUIDto>>(products);
            return productsDto;



        }

        public Task<ProductAttributeDto> ParseQueryToFilterAsync(string query)
        {
            throw new NotImplementedException();
        }

        public Task<ProductAttributeDto> GetSimilarProductsFilterAsync(int productId)
        {
            throw new NotImplementedException();
        }

        private static IChatClient CreateChatClient(Uri uri, string model)
        {

            return new OllamaApiClient(uri, model);

        }



        private static IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingClient(Uri uri, string model)
        {
            return new OllamaApiClient(uri, model);
        }




        public async Task IndexAllProductsAsync()
        {
            IEmbeddingGenerator<string, Embedding<float>> embeddingClient = CreateEmbeddingClient(uri, "nomic-embed-text");
            var products = await _unitOfWork.ProductRepo.GetAllAsync();
            if (!products.Any()) throw new Exception("No products found in DB.");

            if (!await _qdrantClient.CollectionExistsAsync("products"))

                await _qdrantClient.CreateCollectionAsync(
                collectionName: "products",
                vectorsConfig: new VectorParams { Size = 768, Distance = Distance.Cosine },
                hnswConfig: new HnswConfigDiff
                {
                    M= 16,
                    EfConstruct=256,
                    
                }
                );




            var points = new List<PointStruct>();

            foreach (var product in products)
            {
                string fullText = $"{product.Name} {product.Description}";
                var embeddingResponse = await embeddingClient.GenerateAsync(fullText);
                float[] vector = NormalizeVector(embeddingResponse.Vector.ToArray());

                var point = new PointStruct
                {
                    Id = new PointId { Num = (ulong)product.ProductId },
                    Vectors = vector,
                    Payload =
                            {
                                ["Name"] = product.Name,
                                ["description"] = product.Description,
                                ["category"] = product.CategoryId
                            }
                };

                points.Add(point);
            }

            // Batch upsert for performance
            await _qdrantClient.UpsertAsync("products", points);

            Console.WriteLine($"{points.Count} products indexed in Qdrant.");
        }


        private float[] NormalizeVector(float[] vector)
        {
            var magnitude = MathF.Sqrt(vector.Sum(x => x * x));
            return vector.Select(x => x / magnitude).ToArray();
        }

        private string NormalizeQuery(string query)
        {
            return query
                .ToLowerInvariant()          // Lowercase
                .Replace("-", " ")           // Replace hyphens
                .Replace("_", " ")           // Replace underscores
                .Replace(".", "")            // Remove dots
                .Replace(",", "")            // Remove commas
                .Replace("  ", " ")          // Collapse spaces
                .Trim();
        }

        private string CorrectQueryTypos(string query)
        {
            if (!IsSymSpellDictionaryLoaded())
            {
                Console.WriteLine("⚠️ SymSpell dictionary not loaded. Skipping correction.");
                return query;
            }

            var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var correctedWords = words.Select(word =>
            {
                var suggestions = _symSpell.Lookup(
                    word,
                    Verbosity.Closest,      // Best match only
                    maxEditDistance: 1  // Allow up to 2 edits
                );

                if (suggestions.Any())
                {
                    Console.WriteLine($"🔄 Corrected '{word}' → '{suggestions.First().term}'");
                    return suggestions.First().term;
                }

                // If no correction found, keep original word
                Console.WriteLine($"✅ No correction for '{word}'");
                return word;
            });

            return string.Join(" ", correctedWords);
        }
        private bool IsSymSpellDictionaryLoaded()
        {
            return _symSpell != null && _symSpell.WordCount> 0;
        }
    }
}
