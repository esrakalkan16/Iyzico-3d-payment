using Iyzico3DPayment.Models;
using Iyzico3DPayment.Services;
using System;
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
                var conversationId = Guid.NewGuid().ToString();

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

                model.conversationId = conversationId;
                model.callbackUrl = "https://localhost:44308/Payment/Complete"; // veya Url.Action(...)
                model.currency = "TRY";
                model.basketId = "B123456";
                model.paymentGroup = "PRODUCT";
                model.paymentChannel = "WEB";
                model.buyer = new Iyzico3DPaymentService.Buyer
                {
                    id = "BY123",
                    name = "Yavuz",
                    surname = "Çarpar",
                    email = "yavuz@example.com",
                    identityNumber = "74300864791",
                    registrationAddress = "Yıldız Mah. Test Sok. No:1",
                    ip = Request.UserHostAddress,
                    city = "Elazığ",
                    country = "Turkey"
                };

                model.shippingAddress = new Iyzico3DPaymentService.Address
                {
                    contactName = "Yavuz Ali",
                    city = "Elazığ",
                    country = "Turkey",
                    address = "Yıldız Mah. Test Sok. No:1"
                };

                model.billingAddress = new Iyzico3DPaymentService.Address
                {
                    contactName = "Yavuz Ali",
                    city = "Elazığ",
                    country = "Turkey",
                    address = "Yıldız Mah. Test Sok. No:1"
                };

                model.basketItems = new Iyzico3DPaymentService.BasketItem[]
                {
                   new Iyzico3DPaymentService.BasketItem
               {
                       id = "BI101",
                       name = "Test Ürünü",
                       category1 = "Elektronik",
                       itemType = "PHYSICAL",
                       price = model.paidPrice
                         }
                    };



                // Doğrulama (eksikse ekle)
                _paymentService.ValidateInit3DRequest(model);

                var response = await _paymentService.Init3DPaymentAsync(model);

                if (response.status != "success")
                {
                    ViewBag.Error = response.errorMessage;
                    ViewBag.ErrorCode = response.errorCode;
                    ViewBag.ErrorGroup = response.errorGroup;
                    return View("Error");
                }

                var htmlContent = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(response.threeDSHtmlContent));
                return Content(htmlContent, "text/html");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Error");
            }
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

                if (status != "success" || mdStatus != "1")
                {
                    ViewBag.Error = "3D Secure doğrulama başarısız.";
                    return View("PaymentFailed");
                }

                // DB'den ödeme kaydını al
                var payment = _context.Payments.FirstOrDefault(p => p.ConversationId == conversationId);
                if (payment == null)
                {
                    ViewBag.Error = "Ödeme kaydı bulunamadı.";
                    return View("Error");
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
                    payment.Status = "Completed";
                    _context.SaveChanges();

                    ViewBag.PaymentId = result.paymentId;
                    ViewBag.Price = result.paidPrice;
                    return View("PaymentSuccess");
                }
                else
                {
                    payment.Status = "Failed";
                    _context.SaveChanges();

                    ViewBag.Error = result.errorMessage;
                    return View("PaymentFailed");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Error");
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