using Iyzico3DPayment.Models;
using Iyzico3DPayment.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Iyzico3DPayment.Controllers
{
    public class PaymentController : Controller



    {
        private readonly ApplicationDbContext _context;
        private readonly Iyzico3DPaymentService _paymentService;

        public PaymentController()
        {


            _context = new ApplicationDbContext();
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


        [HttpPost]
        public async Task<ActionResult> Init(Iyzico3DPaymentService.Init3DPaymentRequest model)
        {
            try
            {
                // DEBUG: Form'dan gelen verileri kontrol et
                System.Diagnostics.Debug.WriteLine("=== DEBUG: Form Verileri ===");
                System.Diagnostics.Debug.WriteLine($"model.paymentCard null mu? {model.paymentCard == null}");

                if (model.paymentCard != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Raw Card Number: '{model.paymentCard.cardNumber}'");
                    System.Diagnostics.Debug.WriteLine($"Raw Expire Month: '{model.paymentCard.expireMonth}'");
                    System.Diagnostics.Debug.WriteLine($"Raw Expire Year: '{model.paymentCard.expireYear}'");
                    System.Diagnostics.Debug.WriteLine($"Raw CVC: '{model.paymentCard.cvc}'");
                    System.Diagnostics.Debug.WriteLine($"Card Holder Name: '{model.paymentCard.cardHolderName}'");
                }
                System.Diagnostics.Debug.WriteLine($"Paid Price: {model.paidPrice}");
                System.Diagnostics.Debug.WriteLine($"Installment: {model.installment}");
                System.Diagnostics.Debug.WriteLine("=== DEBUG END ===");

                // 1. Kart bilgilerini temizle ve formatla
                if (model.paymentCard == null)
                {
                    // Manuel olarak form'dan al
                    model.paymentCard = new Iyzico3DPaymentService.PaymentCard
                    {
                        cardNumber = Request.Form["paymentCard.cardNumber"] ?? Request.Form["cardNumber"],
                        expireMonth = Request.Form["paymentCard.expireMonth"] ?? Request.Form["expireMonth"],
                        expireYear = Request.Form["paymentCard.expireYear"] ?? Request.Form["expireYear"],
                        cvc = Request.Form["paymentCard.cvc"] ?? Request.Form["cvc"],
                        cardHolderName = Request.Form["paymentCard.cardHolderName"] ?? Request.Form["cardHolderName"]
                    };

                    System.Diagnostics.Debug.WriteLine("Manuel form verisi alındı!");
                }

                if (model.paymentCard != null)
                {
                    model.paymentCard.cardNumber = model.paymentCard.cardNumber?.Replace(" ", "").Replace("-", "");
                    model.paymentCard.expireMonth = model.paymentCard.expireMonth?.PadLeft(2, '0');
                    model.paymentCard.expireYear = model.paymentCard.expireYear?.Length == 2 ?
                        "20" + model.paymentCard.expireYear : model.paymentCard.expireYear;
                }

                // Debug için logla
                System.Diagnostics.Debug.WriteLine($"Card Number: {model.paymentCard.cardNumber}");
                System.Diagnostics.Debug.WriteLine($"Expire Month: {model.paymentCard.expireMonth}");
                System.Diagnostics.Debug.WriteLine($"Expire Year: {model.paymentCard.expireYear}");
                System.Diagnostics.Debug.WriteLine($"CVC: {model.paymentCard.cvc}");

                var conversationId = Guid.NewGuid().ToString();

                // 2. Ödeme kaydını DB'ye ekle
                var prePayment = new Payment
                {
                    ConversationId = conversationId,
                    PaidAmount = model.paidPrice,
                    InstallmentCount = model.installment,
                    CardHolderName = model.paymentCard.cardHolderName,
                    CardNumberMasked = MaskCardNumber(model.paymentCard.cardNumber),
                    ExpireMonth = model.paymentCard.expireMonth,
                    ExpireYear = model.paymentCard.expireYear,
                    CvvHash = "***",
                    CreatedAt = DateTime.Now,
                    Status = "Pending"
                };
                _context.Payments.Add(prePayment);
                _context.SaveChanges();

                // 3. Zorunlu alanları doldur
                model.conversationId = conversationId;
                model.callbackUrl = Url.Action("Complete", "Payment", null, Request.Url.Scheme);
                model.currency = "TRY";
                model.paymentGroup = "PRODUCT";
                model.paymentChannel = "WEB";
                model.basketId = "B" + DateTime.Now.Ticks.ToString().Substring(0, 10);
                model.locale = "tr";

                // 4. Buyer bilgileri (zorunlu alanlarla birlikte)
                model.buyer = new Iyzico3DPaymentService.Buyer
                {
                    id = "BY" + DateTime.Now.Ticks.ToString().Substring(0, 8),
                    name = "Test",
                    surname = "User",
                    gsmNumber = "+905555555555",
                    email = "test@example.com",
                    identityNumber = "12345678901",
                    registrationAddress = "Test Mahallesi Test Sokak No:1",
                    ip = GetClientIP(),
                    city = "Istanbul",
                    country = "Turkey",
                    zipCode = "34000",
                    registrationDate = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd HH:mm:ss"),
                    lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                // 5. Shipping Address
                model.shippingAddress = new Iyzico3DPaymentService.Address
                {
                    contactName = "Test User",
                    city = "Istanbul",
                    country = "Turkey",
                    address = "Test Mahallesi Test Sokak No:1",
                    zipCode = "34000"
                };

                // 6. Billing Address
                model.billingAddress = new Iyzico3DPaymentService.Address
                {
                    contactName = "Test User",
                    city = "Istanbul",
                    country = "Turkey",
                    address = "Test Mahallesi Test Sokak No:1",
                    zipCode = "34000"
                };

                // 7. Basket Items
                model.basketItems = new[]
                {
            new Iyzico3DPaymentService.BasketItem
            {
                id = "BI" + DateTime.Now.Ticks.ToString().Substring(0, 8),
                name = "Test Ürünü",
                category1 = "Elektronik",
                category2 = "Telefon",
                itemType = "PHYSICAL",
                price = model.paidPrice
            }
        };

                // 8. Ödeme kartı bilgilerini kontrol et
                if (string.IsNullOrEmpty(model.paymentCard.cardNumber) ||
                    string.IsNullOrEmpty(model.paymentCard.expireMonth) ||
                    string.IsNullOrEmpty(model.paymentCard.expireYear) ||
                    string.IsNullOrEmpty(model.paymentCard.cvc))
                {
                    ViewBag.Error = "Kart bilgileri eksik veya hatalı.";
                    return View("Error");
                }

                // 9. Request'i doğrula
                _paymentService.ValidateInit3DRequest(model);

                // 10. Ödeme başlat
                var response = await _paymentService.Init3DPaymentAsync(model);

                // 11. Yanıtı kontrol et
                if (response.status != "success")
                {
                    // DB'deki kaydı güncelle
                    prePayment.Status = "Failed";
                    _context.SaveChanges();

                    // Detaylı hata logla
                    System.Diagnostics.Debug.WriteLine($"İyzico Error Status: {response.status}");
                    System.Diagnostics.Debug.WriteLine($"İyzico Error Message: {response.errorMessage}");
                    System.Diagnostics.Debug.WriteLine($"İyzico Error Code: {response.errorCode}");
                    System.Diagnostics.Debug.WriteLine($"İyzico Error Group: {response.errorGroup}");

                    ViewBag.Error = $"Ödeme başlatılamadı: {response.errorMessage} (Kod: {response.errorCode})";
                    ViewBag.ErrorCode = response.errorCode;
                    ViewBag.ErrorGroup = response.errorGroup;
                    return View("Error");
                }

                // 12. Başarılıysa 3D Secure sayfasını göster
                var htmlContent = System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(response.threeDSHtmlContent));

                return Content(htmlContent, "text/html");
            }
            catch (Exception ex)
            {
                // Hata durumunda DB kaydını güncelle
                try
                {
                    var failedPayment = _context.Payments
                        .FirstOrDefault(p => p.ConversationId == model.conversationId);
                    if (failedPayment != null)
                    {
                        failedPayment.Status = "Error";
                        _context.SaveChanges();
                    }
                }
                catch { }

                System.Diagnostics.Debug.WriteLine($"Init Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                ViewBag.Error = $"Bir hata oluştu: {ex.Message}";
                return View("Error");
            }
        }

        // IP adresini alma yardımcı metodu
        private string GetClientIP()
        {
            string ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip))
            {
                ip = Request.ServerVariables["REMOTE_ADDR"];
            }
            if (string.IsNullOrEmpty(ip))
            {
                ip = Request.UserHostAddress;
            }
            if (string.IsNullOrEmpty(ip))
            {
                ip = "85.34.78.112"; // Fallback IP
            }
            return ip;
        }
     


        [HttpPost]
        public async Task<ActionResult> Complete()
        {
            try
            {
                var status = Request.Form["status"];
                var paymentId = Request.Form["paymentId"];
                var conversationData = Request.Form["conversationData"];
                var conversationId = Request.Form["conversationId"];
                var mdStatus = Request.Form["mdStatus"];

                // Debug logları
                System.Diagnostics.Debug.WriteLine($"=== COMPLETE DEBUG ===");
                System.Diagnostics.Debug.WriteLine($"Status: {status}");
                System.Diagnostics.Debug.WriteLine($"PaymentId: {paymentId}");
                System.Diagnostics.Debug.WriteLine($"ConversationId: {conversationId}");
                System.Diagnostics.Debug.WriteLine($"MdStatus: {mdStatus}");
                System.Diagnostics.Debug.WriteLine($"=== DEBUG END ===");

                // 3D Secure kontrolü
                if (status != "success" || mdStatus != "1")
                {
                    ViewBag.Error = "3D Secure doğrulama başarısız. Lütfen kartınızın 3D Secure özelliğinin aktif olduğundan ve doğru şifre girdiğinizden emin olun.";
                    ViewBag.ErrorCode = "3DS_FAILED";
                    ViewBag.MdStatus = mdStatus;

                    // DB'den ödeme kaydını bul ve güncelle
                    var failedPayment = _context.Payments.FirstOrDefault(p => p.ConversationId == conversationId);
                    if (failedPayment != null)
                    {
                        failedPayment.Status = "3DS_Failed";
                        failedPayment.IsSuccess = false;
                        failedPayment.ErrorMessage = "3D Secure doğrulama başarısız";
                        failedPayment.ErrorCode = "3DS_FAILED";
                        _context.SaveChanges();
                    }

                    return View("PaymentFailed");
                }

                // DB'den ödeme kaydını al
                var payment = _context.Payments.FirstOrDefault(p => p.ConversationId == conversationId);
                if (payment == null)
                {
                    ViewBag.Error = "Ödeme kaydı bulunamadı. İşlem referans numarası sistemde kayıtlı değil.";
                    ViewBag.ErrorCode = "PAYMENT_NOT_FOUND";
                    return View("PaymentFailed");
                }

                // İyzico'dan tamamla
                var completeRequest = new Iyzico3DPaymentService.Complete3DPaymentRequest
                {
                    locale = "tr",
                    conversationId = conversationId,
                    paymentId = paymentId,
                    conversationData = conversationData
                };

                var result = await _paymentService.Complete3DPaymentAsync(completeRequest);

                if (result.status == "success")
                {
                    // Başarılı ödeme
                    payment.Status = "Completed";
                    payment.IsSuccess = true;
                    payment.PaymentId = result.paymentId;
                    payment.CompletedAt = DateTime.Now;
                    payment.ErrorMessage = null; // Başarılıysa hata mesajını temizle
                    payment.ErrorCode = null;
                    _context.SaveChanges();

                    // Başarı sayfası için veriler
                    ViewBag.PaymentId = result.paymentId;
                    ViewBag.Price = result.paidPrice;
                    ViewBag.ConversationId = conversationId;
                    ViewBag.CardMask = payment.CardNumberMasked;
                    ViewBag.CardHolder = payment.CardHolderName;
                    ViewBag.InstallmentCount = payment.InstallmentCount;
                    ViewBag.CompletedAt = payment.CompletedAt;

                    return View("PaymentSuccess");
                }
                else
                {
                    // İyzico'dan hata
                    payment.Status = "Failed";
                    payment.IsSuccess = false;
                    payment.ErrorMessage = result.errorMessage;
                    payment.ErrorCode = result.errorCode;
                    _context.SaveChanges();

                    ViewBag.Error = result.errorMessage ?? "Ödeme işlemi tamamlanamadı.";
                    ViewBag.ErrorCode = result.errorCode;
                    ViewBag.ErrorGroup = result.errorGroup;

                    return View("PaymentFailed");
                }
            }
            catch (Exception ex)
            {
                // Genel hata
                System.Diagnostics.Debug.WriteLine($"Complete Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                // DB'de hata kaydını güncelle
                try
                {
                    var conversationId = Request.Form["conversationId"];
                    var errorPayment = _context.Payments.FirstOrDefault(p => p.ConversationId == conversationId);
                    if (errorPayment != null)
                    {
                        errorPayment.Status = "Error";
                        errorPayment.IsSuccess = false;
                        errorPayment.ErrorMessage = ex.Message;
                        errorPayment.ErrorCode = "SYSTEM_ERROR";
                        _context.SaveChanges();
                    }
                }
                catch { }

                ViewBag.Error = $"Sistem hatası oluştu: {ex.Message}";
                ViewBag.ErrorCode = "SYSTEM_ERROR";

                return View("PaymentFailed");
            }
        }

        private string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
                return "**** **** **** ****";
            return "**** **** **** " + cardNumber.Substring(cardNumber.Length - 4);
        }
    }
}