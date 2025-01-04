using System.Collections.Generic;

namespace NinePay.NetCore
{
    public class NinePayRequest
    {
        /// <summary>
        /// Mã giao dịch là duy nhất
        /// </summary>
        public string RequestCode { get; set; } = NinePayExtensions.GenerateRandomString();

        /// <summary>
        /// Mã sản phẩm
        /// </summary>
        public string OrderCode { get; set; } = string.Empty;

        /// <summary>
        /// Số tiền
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Các thông tin của 9pay
        /// </summary>
        public IDictionary<string, string> NinePayData { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Các thông tin tuy chỉnh
        /// </summary>
        public IDictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }
}
