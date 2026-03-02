using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml.Serialization;
using ecommerce.Virtual.Pos.Abstract;
using ecommerce.Virtual.Pos.Requests;
using ecommerce.Virtual.Pos.Results;
using Microsoft.AspNetCore.Http;
namespace ecommerce.Virtual.Pos.Providers;

    public class KuveytTurkPaymentProvider : IPaymentProvider
    {
        private readonly HttpClient _client;

        public KuveytTurkPaymentProvider(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient();
        }

        public async Task<PaymentGatewayResult> ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            try
            {
                //Total amount (100 = 1TL)
                var amount = Convert.ToInt32(request.TotalAmount * 100m).ToString();

                var merchantOrderId = request.OrderNumber;
                var merchantId = request.BankParameters["merchantId"];
                var customerId = request.BankParameters["customerNumber"];
                var userName = request.BankParameters["userName"];
                var password = request.BankParameters["password"];

                string installment = request.Installment.ToString();
                if (request.Installment < 2)
                    installment = string.Empty;//0 veya 1 olması durumunda taksit bilgisini boş gönderiyoruz

                //merchantId, merchantOrderId, amount, okUrl, failUrl, userName and password
                var hashData = GetSha1(merchantId, merchantOrderId, amount, request.CallbackUrl.ToString(), request.CallbackUrl.ToString(), userName, password);

                var requestXml = $@"<KuveytTurkVPosMessage
                    xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                    xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
                        <APIVersion>1.0.0</APIVersion>
                        <OkUrl>{request.CallbackUrl}</OkUrl>
                        <FailUrl>{request.CallbackUrl}</FailUrl>
                        <HashData>{hashData}</HashData>
                        <MerchantId>{merchantId}</MerchantId>
                        <CustomerId>{customerId}</CustomerId>
                        <UserName>{userName}</UserName>
                        <CardNumber>{request.CardNumber}</CardNumber>
                        <CardExpireDateYear>{string.Format("{0:00}", request.ExpireYear)}</CardExpireDateYear>
                        <CardExpireDateMonth>{string.Format("{0:00}", request.ExpireMonth)}</CardExpireDateMonth>
                        <CardCVV2>{request.CvvCode}</CardCVV2>
                        <CardHolderName>{request.CardHolderName}</CardHolderName>
                        <CardType></CardType>
                        <BatchID>0</BatchID>
                        <TransactionType>Sale</TransactionType>
                        <InstallmentCount>{installment}</InstallmentCount>
                        <Amount>{amount}</Amount>
                        <DisplayAmount>{amount}</DisplayAmount>
                        <CurrencyCode>{string.Format("{0:0000}", int.Parse(request.CurrencyIsoCode))}</CurrencyCode>
                        <MerchantOrderId>{merchantOrderId}</MerchantOrderId>
                        <TransactionSecurity>3</TransactionSecurity>
                        </KuveytTurkVPosMessage>";

                //send request
                var response = await _client.PostAsync(request.BankParameters["gatewayUrl"], new StringContent(requestXml, Encoding.UTF8, "text/xml"));
                string responseContent = await response.Content.ReadAsStringAsync();

                //failed
                if (string.IsNullOrWhiteSpace(responseContent))
                    return PaymentGatewayResult.Failed("Ödeme sırasında bir hata oluştu.");

                //successed
                return PaymentGatewayResult.Successed(responseContent, request.BankParameters["gatewayUrl"]);
            }
            catch (Exception ex)
            {
                return PaymentGatewayResult.Failed(ex.ToString());
            }
        }

        public async Task<VerifyGatewayResult> VerifyGateway(VerifyGatewayRequest request, PaymentGatewayRequest gatewayRequest, IFormCollection form)
        {
            if (form == null)
                return VerifyGatewayResult.Failed("Form verisi alınamadı.");

            var authenticationResponse = form["AuthenticationResponse"].ToString();
            if (string.IsNullOrEmpty(authenticationResponse))
                return VerifyGatewayResult.Failed("Form verisi alınamadı.");

            authenticationResponse = HttpUtility.UrlDecode(authenticationResponse);
            var serializer = new XmlSerializer(typeof(VPosTransactionResponseContract));

            var model = new VPosTransactionResponseContract();
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(authenticationResponse)))
            {
                model = serializer.Deserialize(ms) as VPosTransactionResponseContract;
            }

            if (model.ResponseCode != "00")
            {
                return VerifyGatewayResult.Failed(model.ResponseMessage, model.ResponseCode);
            }

            var merchantOrderId = model.MerchantOrderId;
            var amount = model.VPosMessage.Amount;
            var mD = model.MD;
            var merchantId = request.BankParameters["merchantId"];
            var customerId = request.BankParameters["customerNumber"];
            var userName = request.BankParameters["userName"];
            var password = request.BankParameters["password"];

            //Hash some data in one string result
            using var cryptoServiceProvider = SHA1.Create();
            var hashedPassword = Convert.ToBase64String(cryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(password)));

            //merchantId, merchantOrderId, amount, userName, hashedPassword
            var hashstr = $"{merchantId}{merchantOrderId}{amount}{userName}{hashedPassword}";
            var hashbytes = Encoding.GetEncoding("ISO-8859-9").GetBytes(hashstr);
            var inputbytes = cryptoServiceProvider.ComputeHash(hashbytes);
            var hashData = Convert.ToBase64String(inputbytes);

            var requestXml = $@"<KuveytTurkVPosMessage
                    xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                    xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
                        <APIVersion>1.0.0</APIVersion>
                        <HashData>{hashData}</HashData>
                        <MerchantId>{merchantId}</MerchantId>
                        <CustomerId>{customerId}</CustomerId>
                        <UserName>{userName}</UserName>
                        <CurrencyCode>0949</CurrencyCode>
                        <TransactionType>Sale</TransactionType>
                        <InstallmentCount>0</InstallmentCount>
                        <Amount>{amount}</Amount>
                        <MerchantOrderId>{merchantOrderId}</MerchantOrderId>
                        <TransactionSecurity>3</TransactionSecurity>
                        <KuveytTurkVPosAdditionalData>
                        <AdditionalData>
                        <Key>MD</Key>
                        <Data>{mD}</Data>
                        </AdditionalData>
                        </KuveytTurkVPosAdditionalData>
                        </KuveytTurkVPosMessage>";

            //send request
            var response = await _client.PostAsync(request.BankParameters["verifyUrl"], new StringContent(requestXml, Encoding.UTF8, "text/xml"));
            string responseContent = await response.Content.ReadAsStringAsync();
            responseContent = HttpUtility.UrlDecode(responseContent);

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
            {
                model = serializer.Deserialize(ms) as VPosTransactionResponseContract;
            }

            if (model.ResponseCode == "00")
            {
                return VerifyGatewayResult.Successed(model.OrderId.ToString(), model.OrderId.ToString(),
                    0, 0, model.ResponseMessage,
                    model.ResponseCode);
            }

            return VerifyGatewayResult.Failed(model.ResponseMessage, model.ResponseCode);
        }

        public async Task<CancelPaymentResult> CancelRequest(CancelPaymentRequest request)
        {
            try
            {
                var merchantId = request.BankParameters["merchantId"];
                var customerId = request.BankParameters["customerNumber"];
                var userName = request.BankParameters["userName"];
                var password = request.BankParameters["password"];
                var amount = Convert.ToInt32(request.TotalAmount * 100m).ToString();

                // Hash: merchantId, merchantOrderId, amount, userName, hashedPassword
                // Hash: merchantId, merchantOrderId, amount, userName, hashedPassword
                using var cryptoServiceProvider = SHA1.Create();
                var hashedPassword = Convert.ToBase64String(cryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(password)));
                
                var hashString = $"{merchantId}{request.OrderNumber}{amount}{userName}{hashedPassword}";
                var hashBytes = Encoding.GetEncoding("ISO-8859-9").GetBytes(hashString);
                var hashData = Convert.ToBase64String(cryptoServiceProvider.ComputeHash(hashBytes));

                var requestXml = $@"<KuveytTurkVPosMessage
                    xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                    xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
                        <APIVersion>1.0.0</APIVersion>
                        <HashData>{hashData}</HashData>
                        <MerchantId>{merchantId}</MerchantId>
                        <CustomerId>{customerId}</CustomerId>
                        <UserName>{userName}</UserName>
                        <TransactionType>SaleReversal</TransactionType>
                        <Amount>{amount}</Amount>
                        <MerchantOrderId>{request.OrderNumber}</MerchantOrderId>
                        <CurrencyCode>0949</CurrencyCode>
                    </KuveytTurkVPosMessage>";

                var response = await _client.PostAsync(request.BankParameters["verifyUrl"], new StringContent(requestXml, Encoding.UTF8, "text/xml"));
                string responseContent = await response.Content.ReadAsStringAsync();
                responseContent = HttpUtility.UrlDecode(responseContent);

                var serializer = new XmlSerializer(typeof(VPosTransactionResponseContract));
                VPosTransactionResponseContract model;
                
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
                {
                    model = serializer.Deserialize(ms) as VPosTransactionResponseContract;
                }

                if (model == null || model.ResponseCode != "00")
                {
                    var errorMessage = model?.ResponseMessage ?? "İptal işlemi başarısız.";
                    return CancelPaymentResult.Failed(errorMessage);
                }

                return CancelPaymentResult.Successed(model.OrderId.ToString(), model.RRN);
            }
            catch (Exception ex)
            {
                return CancelPaymentResult.Failed($"İptal işlemi sırasında hata: {ex.Message}");
            }
        }

        public async Task<RefundPaymentResult> RefundRequest(RefundPaymentRequest request)
        {
            try
            {
                var merchantId = request.BankParameters["merchantId"];
                var customerId = request.BankParameters["customerNumber"];
                var userName = request.BankParameters["userName"];
                var password = request.BankParameters["password"];
                var amount = Convert.ToInt32(request.TotalAmount * 100m).ToString();

                // Hash: merchantId, merchantOrderId, amount, userName, hashedPassword
                // Hash: merchantId, merchantOrderId, amount, userName, hashedPassword
                using var cryptoServiceProvider = SHA1.Create();
                var hashedPassword = Convert.ToBase64String(cryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(password)));
                
                var hashString = $"{merchantId}{request.OrderNumber}{amount}{userName}{hashedPassword}";
                var hashBytes = Encoding.GetEncoding("ISO-8859-9").GetBytes(hashString);
                var hashData = Convert.ToBase64String(cryptoServiceProvider.ComputeHash(hashBytes));

                var requestXml = $@"<KuveytTurkVPosMessage
                    xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                    xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
                        <APIVersion>1.0.0</APIVersion>
                        <HashData>{hashData}</HashData>
                        <MerchantId>{merchantId}</MerchantId>
                        <CustomerId>{customerId}</CustomerId>
                        <UserName>{userName}</UserName>
                        <TransactionType>Drawback</TransactionType>
                        <Amount>{amount}</Amount>
                        <MerchantOrderId>{request.OrderNumber}</MerchantOrderId>
                        <CurrencyCode>0949</CurrencyCode>
                    </KuveytTurkVPosMessage>";

                var response = await _client.PostAsync(request.BankParameters["verifyUrl"], new StringContent(requestXml, Encoding.UTF8, "text/xml"));
                string responseContent = await response.Content.ReadAsStringAsync();
                responseContent = HttpUtility.UrlDecode(responseContent);

                var serializer = new XmlSerializer(typeof(VPosTransactionResponseContract));
                VPosTransactionResponseContract model;
                
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
                {
                    model = serializer.Deserialize(ms) as VPosTransactionResponseContract;
                }

                if (model == null || model.ResponseCode != "00")
                {
                    var errorMessage = model?.ResponseMessage ?? "İade işlemi başarısız.";
                    return RefundPaymentResult.Failed(errorMessage);
                }

                return RefundPaymentResult.Successed(model.OrderId.ToString(), model.RRN);
            }
            catch (Exception ex)
            {
                return RefundPaymentResult.Failed($"İade işlemi sırasında hata: {ex.Message}");
            }
        }

        public async Task<PaymentDetailResult> PaymentDetailRequest(PaymentDetailRequest request)
        {
            try
            {
                var merchantId = request.BankParameters["merchantId"];
                var customerId = request.BankParameters["customerNumber"];
                var userName = request.BankParameters["userName"];
                var password = request.BankParameters["password"];
                var amount = "100"; // Sorgu için sabit tutar

                // Hash: merchantId, merchantOrderId, amount, userName, hashedPassword
                // Hash: merchantId, merchantOrderId, amount, userName, hashedPassword
                using var cryptoServiceProvider = SHA1.Create();
                var hashedPassword = Convert.ToBase64String(cryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(password)));
                
                var hashString = $"{merchantId}{request.OrderNumber}{amount}{userName}{hashedPassword}";
                var hashBytes = Encoding.GetEncoding("ISO-8859-9").GetBytes(hashString);
                var hashData = Convert.ToBase64String(cryptoServiceProvider.ComputeHash(hashBytes));

                var requestXml = $@"<KuveytTurkVPosMessage
                    xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                    xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
                        <APIVersion>1.0.0</APIVersion>
                        <HashData>{hashData}</HashData>
                        <MerchantId>{merchantId}</MerchantId>
                        <CustomerId>{customerId}</CustomerId>
                        <UserName>{userName}</UserName>
                        <TransactionType>GetMerchantOrderDetail</TransactionType>
                        <MerchantOrderId>{request.OrderNumber}</MerchantOrderId>
                    </KuveytTurkVPosMessage>";

                var response = await _client.PostAsync(request.BankParameters["verifyUrl"], new StringContent(requestXml, Encoding.UTF8, "text/xml"));
                string responseContent = await response.Content.ReadAsStringAsync();
                responseContent = HttpUtility.UrlDecode(responseContent);

                var serializer = new XmlSerializer(typeof(VPosTransactionResponseContract));
                VPosTransactionResponseContract model;
                
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
                {
                    model = serializer.Deserialize(ms) as VPosTransactionResponseContract;
                }

                if (model == null)
                {
                    return PaymentDetailResult.FailedResult("Sipariş detayı alınamadı.");
                }

                var transactionId = model.OrderId.ToString();
                var referenceNumber = model.RRN;
                var responseCode = model.ResponseCode;
                var bankMessage = model.ResponseMessage;

                // Transaction type'a göre durum belirle
                if (model.TransactionType == "Sale" && model.ResponseCode == "00")
                {
                    return PaymentDetailResult.PaidResult(transactionId, referenceNumber, 
                        bankMessage: bankMessage, responseCode: responseCode);
                }
                else if (model.TransactionType == "SaleReversal")
                {
                    return PaymentDetailResult.CanceledResult(transactionId, referenceNumber, 
                        bankMessage, responseCode);
                }
                else if (model.TransactionType == "Drawback")
                {
                    return PaymentDetailResult.RefundedResult(transactionId, referenceNumber, 
                        bankMessage, responseCode);
                }

                return PaymentDetailResult.FailedResult(errorMessage: bankMessage ?? "İşlem sorgulanamadı.", 
                    errorCode: responseCode);
            }
            catch (Exception ex)
            {
                return PaymentDetailResult.FailedResult($"Sipariş sorgulama hatası: {ex.Message}");
            }
        }

        public Dictionary<string, string> TestParameters => new Dictionary<string, string>
        {
            { "merchantId", "" },
            { "customerNumber", "" },
            { "gatewayUrl", "https://boatest.kuveytturk.com.tr/boa.virtualpos.services/Home/ThreeDModelPayGate" },
            { "userName", "" },
            { "password", "" },
            { "verifyUrl", "https://boatest.kuveytturk.com.tr/boa.virtualpos.services/Home/ThreeDModelProvisionGate" }
        };

        private static string GetSha1(string merchantId, string merchantOrderId, string amount, string okUrl, string failUrl, string userName, string password)
        {
            var provider = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(provider);

            using var cryptoServiceProvider = SHA1.Create();
            var inputBytes = cryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(password));
            var hashedPassword = Convert.ToBase64String(inputBytes);

            var hashString = $"{merchantId}{merchantOrderId}{amount}{okUrl}{failUrl}{userName}{hashedPassword}";
            var hashBytes = Encoding.GetEncoding("ISO-8859-9").GetBytes(hashString);

            return Convert.ToBase64String(cryptoServiceProvider.ComputeHash(hashBytes));
        }

        private class VPosTransactionResponseContract
        {
            public string ACSURL { get; set; }
            public string AuthenticationPacket { get; set; }
            public string HashData { get; set; }
            public bool IsEnrolled { get; set; }
            public bool IsSuccess { get; }
            public bool IsVirtual { get; set; }
            public string MD { get; set; }
            public string MerchantOrderId { get; set; }
            public int OrderId { get; set; }
            public string PareqHtmlFormString { get; set; }
            public string Password { get; set; }
            public string ProvisionNumber { get; set; }
            public string ResponseCode { get; set; }
            public string ResponseMessage { get; set; }
            public string RRN { get; set; }
            public string SafeKey { get; set; }
            public string Stan { get; set; }
            public DateTime TransactionTime { get; set; }
            public string TransactionType { get; set; }
            public KuveytTurkVPosMessage VPosMessage { get; set; }
        }

        public class KuveytTurkVPosMessage
        {
            public decimal Amount { get; set; }
        }
    }

