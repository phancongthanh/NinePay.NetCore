using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace NinePay.NetCore
{
    public class NinePayService : INinePayService
    {
        protected readonly NinePayOptions _options;
        protected readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly HttpClient _httpClient;

        protected string CacheKey => nameof(NinePayService);

        public NinePayService(IOptions<NinePayOptions> options, IHttpContextAccessor httpContextAccessor, HttpClient httpClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _options = options.Value;
            _httpClient = httpClient;
        }

        protected Task SetCustomData(string requestCode, IDictionary<string, string> customData)
        {
            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new NullReferenceException("httpContext is null!");

            var memoryCache = httpContext.RequestServices.GetRequiredService<IMemoryCache>();
            // Lưu trữ custome data trong memory cache
            memoryCache.Set($"{CacheKey}-{requestCode}", customData, TimeSpan.FromDays(1));
            return Task.CompletedTask;
        }

        protected Task<IDictionary<string, string>?> GetCustomData(string requestCode)
        {
            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new NullReferenceException("httpContext is null!");

            var memoryCache = httpContext.RequestServices.GetRequiredService<IMemoryCache>();
            // Lưu trữ custome data trong memory cache
            var customData = memoryCache.Get<IDictionary<string, string>>($"{CacheKey}-{requestCode}");
            return Task.FromResult<IDictionary<string, string>?>(customData);
        }

        public async Task<string> CreatePaymentLink(string type, NinePayRequest request, string returnUrl)
        {
            // Tạo đối tượng NinePayLibrary để xây dựng URL
            var ninePay = new NinePayLibrary(_options.ApiUrl);

            // Thêm các trường bắt buộc vào yêu cầu thanh toán
            ninePay.AddRequestData("merchantKey", _options.MerchantKey);
            ninePay.AddRequestData("time", NinePayLibrary.Time);

            // Thêm URL callback để nhận kết quả trả về từ NinePay sau khi thanh toán
            ninePay.AddRequestData("return_url", await GetAbsoluteUrl(_options.ReturnURL));

            // Thêm các thông tin giao dịch
            ninePay.AddRequestData("invoice_no", request.RequestCode); // Mã giao dịch
            ninePay.AddRequestData("description", request.OrderCode); // Mã đơn hàng
            ninePay.AddRequestData("amount", request.Amount.ToString("0", CultureInfo.InvariantCulture)); // Số tiền

            // Thêm thông tin xử lý
            var customData = new Dictionary<string, string>();
            customData.Add("user_Type", type); // Loại request
            customData.Add("user_returnUrl", returnUrl); // URL tùy chỉnh để quay lại sau khi xử lý

            // Thêm các dữ liệu 9Pay tuỳ chỉnh, nếu có
            if (request.NinePayData != null)
                foreach (var item in request.NinePayData)
                    ninePay.AddRequestData(item.Key, item.Value);

            // Thêm các dữ liệu tuỳ chỉnh của người dùng, nếu có
            if (request.Data != null)
                foreach (var item in request.Data)
                    customData.Add($"user_{item.Key}", item.Value);

            // Tạo chuỗi truy vấn URL chứa toàn bộ thông tin giao dịch và chữ ký bảo mật
            var url = ninePay.CreateRequestUrl(_options.SecretKey);

            // Lưu trữ custome data
            await SetCustomData(request.RequestCode, customData);

            // Trả về đường dẫn thanh toán
            return url;
        }

        /// <summary>
        /// Lấy đường dẫn tuyệt đối cho returnUrl
        /// </summary>
        protected virtual Task<string> GetAbsoluteUrl(string relativePath)
        {
            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new NullReferenceException("httpContext is null!");

            var request = httpContext.Request;
            var host = request.Host.HasValue ? request.Host.Value : throw new InvalidOperationException("Request host is missing.");
            var scheme = request.Scheme;

            return Task.FromResult($"{scheme}://{host}{relativePath}");
        }

        public async Task<(string type, NinePayResponse response, string returnUrl)> ProcessCallBack()
        {
            // Lấy thông tin HTTP context từ accessor
            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new NullReferenceException("httpContext is null!"); // Kiểm tra null để đảm bảo context tồn tại

            var result = httpContext.Request.HasFormContentType
                ? httpContext.Request.Form["result"].ToString()
                : httpContext.Request.Query["result"].ToString();
            var checksum = httpContext.Request.HasFormContentType
                ? httpContext.Request.Form["checksum"].ToString()
                : httpContext.Request.Query["checksum"].ToString();

            // Tạo NinePayLibrary để xử lý các tham số từ query string
            var ninePay = new NinePayLibrary(_options.ApiUrl);
            // Xử lý phản hồi từ NinePay
            ninePay.ValidateAndDecode(result, checksum, _options.ChecksumKey);

            var status = ninePay.GetResponseData("status");
            var error_code = ninePay.GetResponseData("error_code");

            // Xác định trạng thái giao dịch dựa vào kết quả kiểm tra hash và mã phản hồi
            bool? check = status == "5" && error_code == "000" ? (bool?)true : // Giao dịch thành công
                           status == "5" && error_code == "000" ? (bool?)null : // Giao dịch đang xử lý
                           false; // Giao dịch thất bại

            // Lấy các thông tin quan trọng từ phản hồi
            var requestCode = ninePay.GetResponseData("invoice_no");
            var code = ninePay.GetResponseData("description"); // Mã đơn hàng
            // Chuyển đổi số tiền từ chuỗi sang kiểu decimal, nếu không chuyển được thì gán giá trị mặc định
            if (!decimal.TryParse(ninePay.GetResponseData("amount"), out var amount))
                amount = 0;

            var customData = await GetCustomData(requestCode) ?? new Dictionary<string,string>();

            var type = customData["user_Type"]; // Loại giao dịch (do user cung cấp)
            var returnUrl = customData["user_returnUrl"]; // URL để người dùng quay lại

            // Lấy các trường dữ liệu nhận được từ 9Pay và lưu vào dictionary
            var fields = ninePay.GetResult();

            // Lấy các trường dữ liệu bắt đầu bằng "user_" (trừ một số trường cố định) và lưu vào dictionary
            var data = customData
                .Where(x => x.Key.StartsWith("user_")) // Chỉ lấy các trường bắt đầu bằng "user_"
                .Where(x => x.Key != "user_Type" && x.Key != "user_returnUrl") // Loại bỏ các trường "user_Type" và "user_returnUrl"
                .ToDictionary(x => x.Key.Substring(5), x => x.Value.ToString()); // Bỏ tiền tố "user_" trong key

            // Tạo đối tượng NinePayResponse chứa thông tin phản hồi
            var response = new NinePayResponse
            {
                Result = check, // Kết quả giao dịch
                RequestCode = requestCode, // Mã giao dịch
                OrderCode = code, // Mã đơn hàng
                Amount = amount, // Số tiền giao dịch
                NinePayData = fields, // Các dữ liệu từ 9Pay
                Data = data // Các dữ liệu custom từ người dùng
            };

            // Trả về tuple gồm loại giao dịch (type), phản hồi (response) và URL quay lại (returnUrl)
            return (type, response, returnUrl);
        }

        public async Task<(bool? result, IDictionary<string, string?> data)> QueryDR(string requestCode)
        {
            // URL endpoint của API NinePay
            var url = $"{_options.ApiUrl}/v2/payments/{requestCode}/inquire";

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            var time = NinePayLibrary.Time;
            var message = new NinePayLibrary(_options.ApiUrl).BuildMessage("GET", $"/v2/payments/{requestCode}/inquire", time: time);
            var signature = NinePayLibrary.Signature(message, _options.SecretKey);
            requestMessage.Headers.TryAddWithoutValidation("Date", time);
            requestMessage.Headers.TryAddWithoutValidation("Authorization", $"Signature Algorithm=HS256,Credential={_options.MerchantKey},SignedHeaders=,Signature={signature}");

            // Gửi yêu cầu GET đến API
            var response = await _httpClient.SendAsync(requestMessage);

            // Kiểm tra nếu phản hồi thành công (HTTP 200)
            if (!response.IsSuccessStatusCode) Console.WriteLine(await response.Content.ReadAsStringAsync());
            response.EnsureSuccessStatusCode();

            // Đọc phản hồi từ API dưới dạng Dictionary<string, string>
            var data = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>() ?? new Dictionary<string, object>();
            
            var status = data["status"].ToString();
            var error_code = data["error_code"].ToString();

            // Xác định trạng thái giao dịch dựa mã phản hồi
            bool? result = status == "5" && error_code == "000" ? (bool?)true : // Giao dịch thành công
                           status == "5" && error_code == "000" ? (bool?)null : // Giao dịch đang xử lý
                           false; // Giao dịch thất bại

            // Lấy danh sách các trường từ kết quả trả về
            var ninePayData = data
                .ToDictionary(x => x.Key, x => x.Value?.ToString());

            // Trả về kết quả giao dịch và dữ liệu
            return (result, ninePayData);
        }
    }
}
