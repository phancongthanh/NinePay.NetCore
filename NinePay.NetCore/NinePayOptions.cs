using System;

namespace NinePay.NetCore
{
    /// <summary>
    /// Cấu hình NinePay
    /// </summary>
    public class NinePayOptions
    {
        /// <summary>
        /// Host của NinePay
        /// </summary>
        public string ApiUrl { get; set; } = "https://sand-payment.9pay.vn";
        public string ReturnURL { get; set; } = "/ninepay/return";
        public string IPNURL { get; set; } = "/ninepay/ipn";

        /// <summary>
        /// MerchantKey do NinePay cấp
        /// </summary>
        public string MerchantKey { get; set; } = string.Empty;
        /// <summary>
        /// SecretKey do NinePay cấp
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// ChecksumKey do NinePay cấp
        /// </summary>
        public string ChecksumKey { get; set; } = string.Empty;
    }
}
