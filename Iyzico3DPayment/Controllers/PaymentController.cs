using Iyzico3DPayment.Services;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Iyzico3DPayment.Controllers
{
    public class PaymentController : Controller



    {
        private readonly Iyzico3DPaymentService _paymentService;

        public PaymentController()
        {



            _paymentService = new Iyzico3DPaymentService(
                "sandbox-4VEsEh5oSCDi13KPNSzeKIIPst6MmbWc",
                "JVksQYgQuWW3skNE8gQy8WQ8Nm5KBDe7",
                "https://sandbox-api.iyzipay.com"
            );
        }

        public ActionResult Form()
        {
            var model = new Iyzico3DPaymentService.Init3DPaymentRequest
            {
                paymentCard = new Iyzico3DPaymentService.PaymentCard()
            };

            return View("PaymentForm", model);
        }

        public async Task<ActionResult> Init()
        {
            try
            {
                var conversationId = Guid.NewGuid().ToString();
                Session["ConversationId"] = conversationId;


                var request = new Iyzico3DPaymentService.Init3DPaymentRequest
                {
                    conversationId = conversationId,
                    price = 100,
                    paidPrice = 100,
                    currency = "TRY",
                    basketId = "B00001234",
                    paymentGroup = "PRODUCT",
                    paymentChannel = "WEB",
                    callbackUrl = "https://192.168.1.40:45455/Payment/Complete",
                    installment = 1,
                    paymentCard = new Iyzico3DPaymentService.PaymentCard
                    {
                        cardHolderName = "YAVUZ ALİ",
                        cardNumber = "5528790000000008",
                        expireMonth = "12",
                        expireYear = "2030",
                        cvc = "123"
                    },
                    buyer = new Iyzico3DPaymentService.Buyer
                    {
                        id = "BY0000012",
                        name = "Yavuz",
                        surname = "Ali",
                        email = "yavuz@example.com",
                        identityNumber = "74300864791",
                        registrationAddress = "Yıldız Mah. Test Sok. No:1",
                        ip = GetClientIP(),
                        city = "Elazığ",
                        country = "Turkey"
                    },
                    shippingAddress = new Iyzico3DPaymentService.Address
                    {
                        contactName = "Yavuz Ali",
                        city = "Elazığ",
                        country = "Turkey",
                        address = "Yıldız Mah. Test Sok. No:1"
                    },
                    billingAddress = new Iyzico3DPaymentService.Address
                    {
                        contactName = "Yavuz Ali",
                        city = "Elazığ",
                        country = "Turkey",
                        address = "Yıldız Mah. Test Sok. No:1"
                    },
                    basketItems = new[]
                    {
                        new Iyzico3DPaymentService.BasketItem
                        {
            id = "BI101",
            name = "Ürün 1",
            category1 = "Kategori",
            itemType = "PHYSICAL",
            price = 100
        }
    }
                };



                // Validation kontrolü
                _paymentService.ValidateInit3DRequest(request);

                var response = await _paymentService.Init3DPaymentAsync(request);

                // Hata kontrolü
                if (response.status != "success")
                {
                    // Hata detaylarını loglayın
                    System.Diagnostics.Debug.WriteLine($"İyzico Error: {response.errorMessage}");
                    System.Diagnostics.Debug.WriteLine($"Error Code: {response.errorCode}");
                    System.Diagnostics.Debug.WriteLine($"Error Group: {response.errorGroup}");

                    ViewBag.Error = $"Hata: {response.errorMessage} (Kod: {response.errorCode})";
                    ViewBag.ErrorDetails = $"Group: {response.errorGroup}";
                    return View("Error");
                }

           
                // threeDSHtmlContent'i decode et ve render et
                if (!string.IsNullOrEmpty(response.threeDSHtmlContent))
                {
                    var htmlContent = System.Text.Encoding.UTF8.GetString(
                        Convert.FromBase64String(response.threeDSHtmlContent)
                    );
                    return Content(htmlContent, "text/html");
                }
                else
                {
                    ViewBag.Error = "3DS HTML içeriği alınamadı";
                    return View("Error");
                }
            }
            catch (Exception ex)
            {
                // Exception detaylarını da loglayın
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                ViewBag.Error = ex.Message;
                ViewBag.ExceptionDetails = ex.ToString();
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<ActionResult> Complete()
        {
            try
            {
                // İyzico'dan gelen callback parametreleri
                var status = Request.Form["status"];
                var paymentId = Request.Form["paymentId"];
                var conversationData = Request.Form["conversationData"];
                var conversationId = Request.Form["conversationId"];
                var mdStatus = Request.Form["mdStatus"];

                // Hata kontrolü
                if (status != "success" || mdStatus != "1")
                {
                    ViewBag.Error = "3DS doğrulama başarısız";
                    ViewBag.Status = status;
                    ViewBag.MdStatus = mdStatus;
                    return View("PaymentFailed");
                }

                // Session'dan conversationId kontrol et
                var sessionConversationId = Session["ConversationId"]?.ToString();
                if (string.IsNullOrEmpty(sessionConversationId) || sessionConversationId != conversationId)
                {
                    ViewBag.Error = "Güvenlik hatası: Conversation ID uyuşmazlığı";
                    return View("Error");
                }

                var completeRequest = new Iyzico3DPaymentService.Complete3DPaymentRequest
                {
                    locale = "tr",
                    conversationId = conversationId,
                    paymentId = paymentId,
                    conversationData = conversationData
                };

                var result = await _paymentService.Complete3DPaymentAsync(completeRequest);

                // Sonuç kontrolü
                if (result.status == "success")
                {
                    // Başarılı ödeme
                    ViewBag.PaymentId = result.paymentId;
                    ViewBag.Price = result.paidPrice;
                    ViewBag.AuthCode = result.authCode;
                    ViewBag.BasketId = result.basketId;

                    // Session temizle
                    Session.Remove("ConversationId");

                    return View("PaymentSuccess");
                }
                else
                {
                    // Başarısız ödeme
                    ViewBag.Error = result.errorMessage;
                    ViewBag.ErrorCode = result.errorCode;
                    return View("PaymentFailed");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Error");
            }
        }

        // Client IP adresini almak için helper metod
        private string GetClientIP()
        {
            string ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip))
            {
                ip = Request.ServerVariables["REMOTE_ADDR"];
            }

            // Localhost için varsayılan IP
            if (ip == "::1" || ip == "127.0.0.1")
            {
                ip = "85.34.78.112"; // Test IP
            }

            return ip;
        }

    }
}