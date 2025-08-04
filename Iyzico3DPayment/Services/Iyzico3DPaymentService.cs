using Flurl.Http;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Iyzico3DPayment.Services
{
    public class Iyzico3DPaymentService
    {
        public class Settings
        {
            public string ApiKey { get; set; }
            public string SecretKey { get; set; }
            public string BaseUrl { get; set; }
        }

        private readonly Settings _settings;
        private readonly JsonSerializerOptions _jsonOptions;

        public Iyzico3DPaymentService(string apiKey, string secretKey, string baseUrl)
        {
            _settings = new Settings
            {
                ApiKey = apiKey,
                SecretKey = secretKey,
                BaseUrl = baseUrl
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        #region Helpers

        public string GenerateAuthorizationHeaderV2(string uriPath, string requestBody = "")
        {
            // 1. RandomKey (timestamp + sabit)
            string randomKey = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "123456789";
            //randomKey = "1754297297348123456789";
            // 2. Payload = randomKey + uriPath + requestBody
            string payload = string.IsNullOrEmpty(requestBody)
                ? randomKey + uriPath
                : randomKey + uriPath + requestBody;

            // 3. Signature (HMACSHA256(payload, secretKey))
            string signature = ComputeHmacSha256(payload, _settings.SecretKey);

            // 4. Authorization String
            string authorizationString = $"apiKey:{_settings.ApiKey}&randomKey:{randomKey}&signature:{signature}";

            // 5. Base64 Encode
            string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(authorizationString));

            // 6. Return full header
            return $"IYZWSv2 {base64Encoded}";
        }

        private string ComputeHmacSha256(string message, string secret)
        {
            using (var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hashBytes = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(message));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower(); // hex format
            }
        }
        #endregion

        #region Request Models
        public class Init3DPaymentRequest
        {
            public string locale { get; set; } = "tr";
            public string conversationId { get; set; }
            public decimal price { get; set; }
            public decimal paidPrice { get; set; }
            public string currency { get; set; } = "TRY";
            public string basketId { get; set; }
            public string paymentGroup { get; set; } = "PRODUCT";
            public string paymentChannel { get; set; } = "WEB";
            public string callbackUrl { get; set; }
            public int installment { get; set; } = 1;
            public PaymentCard paymentCard { get; set; }
            public Buyer buyer { get; set; }
            public Address shippingAddress { get; set; }
            public Address billingAddress { get; set; }
            public BasketItem[] basketItems { get; set; }
        }

        public class Complete3DPaymentRequest
        {
            public string locale { get; set; } = "tr";
            public string conversationId { get; set; }
            public string paymentId { get; set; }
            public string conversationData { get; set; }
        }

        public class PaymentCard
        {
            public string cardHolderName { get; set; }
            public string cardNumber { get; set; }
            public string expireMonth { get; set; }
            public string expireYear { get; set; }
            public string cvc { get; set; }
            public int registerCard { get; set; } = 0;
        }

        public class Buyer
        {
            public string id { get; set; }
            public string name { get; set; }
            public string surname { get; set; }
            public string gsmNumber { get; set; }
            public string email { get; set; }
            public string identityNumber { get; set; }
            public string lastLoginDate { get; set; }
            public string registrationDate { get; set; }
            public string registrationAddress { get; set; }
            public string ip { get; set; }
            public string city { get; set; }
            public string country { get; set; }
            public string zipCode { get; set; }
        }

        public class Address
        {
            public string contactName { get; set; }
            public string city { get; set; }
            public string country { get; set; }
            public string address { get; set; }
            public string zipCode { get; set; }
        }

        public class BasketItem
        {
            public string id { get; set; }
            public string name { get; set; }
            public string category1 { get; set; }
            public string category2 { get; set; } // ← Eklendi
            public string itemType { get; set; } = "PHYSICAL";
            public decimal price { get; set; }
        }
        #endregion

        #region Response Models
        public class Init3DResponse
        {
            public string status { get; set; }
            public string locale { get; set; }
            public long systemTime { get; set; }
            public string conversationId { get; set; }
            public string threeDSHtmlContent { get; set; }
            public string errorCode { get; set; }
            public string errorMessage { get; set; }
            public string errorGroup { get; set; }
        }

        public class Complete3DResponse
        {
            public string status { get; set; }
            public string locale { get; set; }
            public long systemTime { get; set; }
            public decimal price { get; set; }
            public decimal paidPrice { get; set; }
            public int installment { get; set; }
            public string paymentId { get; set; }
            public int fraudStatus { get; set; }
            public string basketId { get; set; }
            public string currency { get; set; }
            public string authCode { get; set; }
            public string phase { get; set; }
            public int mdStatus { get; set; }
            public string errorCode { get; set; }
            public string errorMessage { get; set; }
            public string errorGroup { get; set; }
        }
        #endregion

        #region Operations
        public async Task<Init3DResponse> Init3DPaymentAsync(Init3DPaymentRequest request)
        {
            try
            {
                string jsonBody = JsonSerializer.Serialize(request, _jsonOptions);
                // Request'i debug için loglayın
                System.Diagnostics.Debug.WriteLine("İyzico Request JSON:");
                System.Diagnostics.Debug.WriteLine(jsonBody);

                string uriPath = "/payment/initialize3ds";
                string url = _settings.BaseUrl + uriPath;


                // Authorization header'ı da loglayın
                string authHeader = GenerateAuthorizationHeaderV2(uriPath, jsonBody);
                System.Diagnostics.Debug.WriteLine($"Authorization Header: {authHeader}");


                var response = await url
                    .WithHeader("Authorization", GenerateAuthorizationHeaderV2(uriPath, jsonBody))
                    .WithHeader("Content-Type", "application/json")
                    .WithHeader("Accept", "application/json")
                    .PostStringAsync(jsonBody);

                string responseContent = await response.ResponseMessage.Content.ReadAsStringAsync();
                // Response'u da loglayın
                System.Diagnostics.Debug.WriteLine("İyzico Response:");
                System.Diagnostics.Debug.WriteLine(responseContent);

                return JsonSerializer.Deserialize<Init3DResponse>(responseContent, _jsonOptions);
            }
            catch (FlurlHttpException ex)
            {
                string error = await ex.GetResponseStringAsync();
                throw new Exception($"İyzico API Error: {error}", ex);
            }
        }

        public async Task<Complete3DResponse> Complete3DPaymentAsync(Complete3DPaymentRequest request)
        {
            try
            {
                string jsonBody = JsonSerializer.Serialize(request, _jsonOptions);
                string uriPath = "/payment/auth3ds";
                string url = _settings.BaseUrl + uriPath;

                var response = await url
                    .WithHeader("Authorization", GenerateAuthorizationHeaderV2(uriPath, jsonBody))
                    .WithHeader("Content-Type", "application/json")
                    .WithHeader("Accept", "application/json")
                    .PostStringAsync(jsonBody);

                string responseContent = await response.ResponseMessage.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Complete3DResponse>(responseContent, _jsonOptions);
            }
            catch (FlurlHttpException ex)
            {
                string error = await ex.GetResponseStringAsync();
                throw new Exception($"İyzico API Error: {error}", ex);
            }
        }
        #endregion

        #region Validation
        public void ValidateInit3DRequest(Init3DPaymentRequest request)
        {
            if (string.IsNullOrEmpty(request.conversationId))
                throw new ArgumentException("conversationId is required");

            if (request.price <= 0)
                throw new ArgumentException("price must be greater than 0");

            if (request.paidPrice <= 0)
                throw new ArgumentException("paidPrice must be greater than 0");

            if (string.IsNullOrEmpty(request.basketId))
                throw new ArgumentException("basketId is required");

            if (string.IsNullOrEmpty(request.callbackUrl))
                throw new ArgumentException("callbackUrl is required");

            if (request.paymentCard == null)
                throw new ArgumentException("paymentCard is required");

            if (request.buyer == null)
                throw new ArgumentException("buyer is required");

            if (request.basketItems == null || request.basketItems.Length == 0)
                throw new ArgumentException("basketItems is required and cannot be empty");

            // PaymentCard validation
            if (string.IsNullOrEmpty(request.paymentCard.cardHolderName))
                throw new ArgumentException("cardHolderName is required");

            if (string.IsNullOrEmpty(request.paymentCard.cardNumber))
                throw new ArgumentException("cardNumber is required");

            // Buyer validation
            if (string.IsNullOrEmpty(request.buyer.id))
                throw new ArgumentException("buyer.id is required");

            if (string.IsNullOrEmpty(request.buyer.name))
                throw new ArgumentException("buyer.name is required");

            if (string.IsNullOrEmpty(request.buyer.surname))
                throw new ArgumentException("buyer.surname is required");

            if (string.IsNullOrEmpty(request.buyer.email))
                throw new ArgumentException("buyer.email is required");
        }
        #endregion
    }
}