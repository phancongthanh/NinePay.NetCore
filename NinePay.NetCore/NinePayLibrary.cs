using System.Security.Cryptography;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace NinePay.NetCore
{
    public class NinePayLibrary
    {
        private readonly string _endpoint;

        private readonly SortedList<string, string> _requestData = new SortedList<string, string>();
        private readonly SortedList<string, string> _responseData = new SortedList<string, string>();

        public static string Time => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        public NinePayLibrary(string endpoint)
        {
            _endpoint = endpoint;
        }
        public void AddRequestData(string key, string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            if (_requestData.ContainsKey(key)) _requestData[key] = value;
            else _requestData.Add(key, value);
        }
        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string GetResponseData(string key)
        {
            if (_responseData.TryGetValue(key, out var retValue))
            {
                return retValue;
            }
            else
            {
                return string.Empty;
            }
        }

        public Dictionary<string, string> GetResult()
        {
            return _responseData.ToDictionary(x => x.Key, x => x.Value);
        }
        public string CreateRequestUrl(string merchantSecretKey)
        {
            string queryString = HttpBuildQuery(_requestData);
            string message = BuildMessage("POST", "/payments/create", queryString);
            string signature = Signature(message, merchantSecretKey);

            byte[] encodedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_requestData));
            string baseEncode = Convert.ToBase64String(encodedData);

            queryString = HttpBuildQuery(new SortedList<string, string>()
            {
                { "signature", signature },
                { "baseEncode", baseEncode }
            });

            string paymentUrl = $"{_endpoint}/portal?{queryString}";
            return paymentUrl;
        }

        public static string Signature(string queryHttp, string secretKey)
        {
            using var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            byte[] hashmessage = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(queryHttp));
            return Convert.ToBase64String(hashmessage);
        }
        public static string HttpBuildQuery(SortedList<string, string> parameters)
        {
            StringBuilder queryBuilder = new StringBuilder();

            foreach (var entry in parameters)
            {
                queryBuilder.Append(entry.Key)
                            .Append("=")
                            .Append(entry.Value)
                            .Append("&");
            }

            // Loại bỏ ký tự '&' cuối cùng
            if (queryBuilder.Length > 0)
            {
                queryBuilder.Length -= 1;
            }

            // Mã hóa URL
            var encodedQuery = Uri.EscapeDataString(queryBuilder.ToString());

            // Thay thế các ký tự được mã hóa theo yêu cầu
            encodedQuery = encodedQuery.Replace("%3D", "=").Replace("%26", "&");
            return encodedQuery;
        }
        public string BuildMessage(string method, string path, string? queryHttp = null, string? time = null)
        {
            time ??= Time;
            var sb = new StringBuilder();
            sb.Append(method).Append("\n")
              .Append(_endpoint + path).Append("\n")
              .Append(time);

            if (!string.IsNullOrEmpty(queryHttp))
            {
                sb.Append("\n").Append(queryHttp);
            }

            return sb.ToString();
        }

        public void ValidateAndDecode(string result, string checksum, string checksumKey)
        {
            // Tính toán checksum SHA-256
            using SHA256 sha256 = SHA256.Create();
            string inputToHash = result + checksumKey;
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputToHash));
            StringBuilder generatedChecksum = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                generatedChecksum.Append(b.ToString("x2"));
            }

            string generatedChecksumHex = generatedChecksum.ToString().ToUpper();

            // Kiểm tra checksum
            Console.WriteLine($"Checksum received: {checksum}");
            Console.WriteLine($"Checksum generated: {generatedChecksumHex}");
            if (checksum != generatedChecksumHex)
            {
                Console.WriteLine("Checksum is invalid.");
            }

            // Giải mã chuỗi Base64
            try
            {
                var validBase64String = result.Replace('-', '+').Replace('_', '/').PadRight(4 * ((result.Length + 3) / 4), '=');
                byte[] decodedBytes = Convert.FromBase64String(validBase64String);
                string decodedString = Encoding.UTF8.GetString(decodedBytes);
                var data = JsonSerializer.Deserialize<Dictionary<string, object?>>(decodedString);
                foreach (var item in data)
                {
                    _responseData.Add(item.Key, item.Value?.ToString());
                }
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Error decoding Base64: {ex.Message}");
            }
        }
    }
}
