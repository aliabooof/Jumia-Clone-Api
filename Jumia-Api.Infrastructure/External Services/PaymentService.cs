using Jumia_Api.Domain.Models;
using Jumia_Api.Domain.Interfaces.UnitOfWork;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Jumia_Api.Application.Interfaces;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Jumia_Api.Application.Dtos.PaymentDtos;
using EllipticCurve.Utils;
using Jumia_Api.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Identity;
using Jumia_Api.Application.Dtos.OrderDtos;
using AutoMapper;
using Qdrant.Client.Grpc;

namespace Jumia_Api.Services.Implementation
{
    public class PaymentService : IPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _apiKey;
        private readonly string _integrationId;
        private readonly string _iframeId;
        private readonly UserManager<AppUser> _userManager;
        private IMapper _mapper;
        private IOrderService _orderService;

        public PaymentService(IMapper mapper, HttpClient httpClient, IConfiguration config, IUnitOfWork unitOfWork, UserManager<AppUser> userManager, IOrderService orderService)
        {
            _httpClient = httpClient;
            _mapper = mapper;
            _config = config;
            _unitOfWork = unitOfWork;
            _apiKey = _config["Paymob:ApiKey"];
            _integrationId = _config["Paymob:IntegrationId"];
            _iframeId = _config["Paymob:CardIframeId"];
            _userManager = userManager;
            _orderService = orderService;
        }

        public async Task<PaymentResponseDto> InitiatePaymentAsync(CreateOrderDTO orderDto)
        {
            bool sucsses = false;
            var orderId =0;
            try
            {
                var orderResult = await _orderService.CreateOrderAsync(orderDto);
                // 1. Map and save order with suborders + items
                if (!orderResult.Success)
                {
                    return new PaymentResponseDto
                    {
                        Success = false,
                        Message = orderResult.ErrorMessage,
                        ErrorDetails = string.Join("; ", orderResult.ErrorDetails.Select(kv => $"{kv.Key}: {kv.Value}")),
                    };
                }
                orderId = orderResult.Order.OrderId;
                sucsses = orderResult.Success;
                

               
                var paymentRequest = new PaymentRequetsDto
                {

                    Order = orderResult.Order,
                    OrderId = orderResult.Order.OrderId,
                    Amount = orderResult.Order.FinalAmount,
                    Currency = "EGP",
                    PaymentMethod = orderResult.Order.PaymentMethod

                };

                // 3. Continue Paymob flow
                var token = await GetAuthTokenAsync();

                var paymobOrderId = await RegisterOrderAsync(token, paymentRequest);

                var paymentKey = await GeneratePaymentKeyAsync(token, paymobOrderId, paymentRequest);

                var iframeUrl = GetPaymentUrl(paymentKey, paymentRequest.PaymentMethod);

                return new PaymentResponseDto
                {
                    Success = true,
                    PaymentUrl = iframeUrl,
                    TransactionId = paymobOrderId,
                    Message = "Payment initiated successfully"
                };
            }
            catch (Exception ex)
            {
                if (sucsses)
                {
                    var cancellationSuccess = await _orderService.CancelOrderTransactionAsync(orderId);
                    await _unitOfWork.OrderRepo.Delete(orderId);
                    await _unitOfWork.SaveChangesAsync();
                }
                return new PaymentResponseDto
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }


        private async Task<string> GetAuthTokenAsync()
        {
            var response = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/auth/tokens", new { api_key = _apiKey });
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return json.RootElement.GetProperty("token").GetString();
        }

        private async Task<string> RegisterOrderAsync(string token, PaymentRequetsDto request)
        {
            var orderRequest = new
            {
                auth_token = token,
                delivery_needed = "false",
                amount_cents = (int)(request.Amount * 100),
                currency = request.Currency,
                merchant_order_id = $"{request.OrderId}_{request.Amount}_{request.Currency}_{request.PaymentMethod}",


                items = new[] {
            new {
                name = $"Order #{request.OrderId}",
                amount_cents = (int)(request.Amount * 100),
                description = "Jumia Clone Order",
                quantity = 1
            }
        }
            };

            var response = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/ecommerce/orders", orderRequest);
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();

            Console.WriteLine("fffffffffffffffff", json);
            Console.WriteLine(await response.Content.ReadAsStringAsync());

            if (!json.RootElement.TryGetProperty("id", out var idElement))
                throw new Exception("Paymob order registration response did not contain 'id'.");

            return idElement.ToString();
        }

        private async Task<string> GeneratePaymentKeyAsync(string token, string paymobOrderId, PaymentRequetsDto request)
        {
            var order = await _unitOfWork.OrderRepo.GetByIdAsync(request.OrderId);
            if (order == null)
                throw new Exception("Order not found.");

            var customer = await _unitOfWork.CustomerRepo.GetByIdAsync(order.CustomerId);
            if (customer == null)
                throw new Exception("Customer not found.");

            var user = await _userManager.FindByIdAsync(customer.UserId);

            if (user == null)
                throw new Exception("User not found.");

            var address = await _unitOfWork.AddressRepo.GetByIdAsync(order.AddressId);
            if (address == null)
                throw new Exception("Billing address not found.");

            var billing = new
            {
                first_name = user.FirstName ?? "N/A",              // Ensure FirstName exists in AppUser
                last_name = user.LastName ?? "N/A",                // Same here
                email = user.Email ?? "email@example.com",
                phone_number = address.PhoneNumber ?? "+201000000000",
                street = address.StreetAddress ?? "N/A",
                building = "N/A",                                   // Add building if available
                floor = "N/A",                                      // Add floor if stored
                apartment = "N/A",                                  // Add apartment if stored
                city = address.City ?? "N/A",
                state = address.State ?? address.City ?? "N/A",
                country = "EGYPT",
                postal_code = address.PostalCode ?? "00000"
            };

            var keyRequest = new
            {
                auth_token = token,
                amount_cents = (int)(request.Amount * 100),
                expiration = 3600,
                order_id = paymobOrderId,
                billing_data = billing,
                currency = request.Currency,
                integration_id = int.Parse(_integrationId)
            };

            var response = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/acceptance/payment_keys", keyRequest);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                // You can log the entire response here for debugging
                throw new Exception($"Payment Key Generation Failed ({response.StatusCode}): {content}");
            }
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return json.RootElement.GetProperty("token").GetString();
        }


        private string GetPaymentUrl(string paymentKey, string method)
        {
            return method.ToLower() switch
            {
                "card" => $"https://accept.paymob.com/api/acceptance/iframes/{_iframeId}?payment_token={paymentKey}",
                "vodafone" => $"https://accept.paymob.com/api/acceptance/payments/pay?payment_token={paymentKey}&source=mobile_wallet",
                "paypal" => $"https://accept.paymob.com/api/acceptance/payments/pay?payment_token={paymentKey}&source=paypal",
                _ => throw new ArgumentException("Unsupported payment method.")
            };
        }

       

        public async Task<bool> ValidatePaymentCallback(string payload)
        {
            try
            {
                Console.WriteLine("Validating callback payload: " + payload);
                var json = JsonDocument.Parse(payload);
                var root = json.RootElement;

               

                bool success = root.TryGetProperty("success", out var successElement)
                               && bool.TryParse(successElement.GetString(), out var isSuccess)
                               && isSuccess;

                if (!root.TryGetProperty("merchant_order_id", out var merchantIdElement))
                {
                    Console.WriteLine("Callback: merchant_order_id not found.");
                    return false;
                }

                var merchantIdStr = merchantIdElement.GetString();
                var parts = merchantIdStr?.Split('_');
                if (parts == null || parts.Length == 0 || !int.TryParse(parts[0], out int orderId))
                {
                    Console.WriteLine($"Callback: Could not parse orderId from merchant_order_id: {merchantIdStr}");
                    return false;
                }

                
                var orderForCartClear = await _unitOfWork.OrderRepo.GetByIdAsync(orderId);
                if (orderForCartClear == null)
                {
                    Console.WriteLine($"Callback: Order with ID {orderId} not found in DB, cannot clear cart.");
                    return false; 
                }


                if (success)
                {
                    
                   
                    orderForCartClear.PaymentStatus = "Paid";
                    Console.WriteLine($"Order {orderId} successfully paid. Status updated.");
                    await _unitOfWork.SaveChangesAsync(); 
                }
                else
                {
                    
                    Console.WriteLine($"Payment for Order {orderId} was unsuccessful. Initiating order cancellation and stock restoration.");
                    var cancellationSuccess = await _orderService.CancelOrderTransactionAsync(orderId);
                    await _unitOfWork.OrderRepo.Delete(orderId);

                    if (!cancellationSuccess)
                    {
                        Console.WriteLine($"CRITICAL ERROR: Failed to cancel order {orderId} and/or restore stock after unsuccessful payment. Manual intervention may be required.");
                        
                    }
                    else
                    {
                        Console.WriteLine($"Order {orderId} successfully cancelled and stock restored due to failed payment.");
                    }
                  
                }

                await _unitOfWork.CartRepo.ClearCartAsync(orderForCartClear.CustomerId);
               
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Callback validation failed: " + ex.Message);
                                return false; 
            }
        }
    }



}


