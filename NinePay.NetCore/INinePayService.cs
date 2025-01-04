using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NinePay.NetCore
{
    public interface INinePayService
    {
        /// <summary>
        /// Tạo link thanh toán NinePay cho giao dịch
        /// </summary>
        /// <param name="type">Kiểu giao dịch</param>
        /// <param name="request">Thông tin giao dịch</param>
        /// <param name="returnUrl">Đường dẫn chuyển hướng về sau thanh toán</param>
        /// <returns>Link thanh toán</returns>
        Task<string> CreatePaymentLink(string type, NinePayRequest request, string returnUrl);

        /// <summary>
        /// Truy vấn trạng thái giao dịch
        /// </summary>
        /// <param name="requestCode">Mã giao dịch đã gửi đi</param>
        /// <returns>
        /// <para>result: true - Giao dịch đã thành công, false - Giao dịch thất bại, null - Giao dịch đang xử lý</para>
        /// <para>data: Dữ liệu trả về từ NinePay</para>
        /// </returns>
        Task<(bool? result, IDictionary<string, string?> data)> QueryDR(string requestCode);

        /// <summary>
        /// Xử lý phản hồi của NinePay khi returnUrl và IPN
        /// </summary>
        /// <returns>
        /// <para>type: Kiểu giao dịch</para>
        /// <para>response: Dữ liệu</para>
        /// <para>returnUrl: Đường dẫn chuyển hướng</para>
        /// </returns>
        Task<(string type, NinePayResponse response, string returnUrl)> ProcessCallBack();
    }
}
