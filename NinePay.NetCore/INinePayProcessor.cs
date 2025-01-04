using System.Threading.Tasks;

namespace NinePay.NetCore
{
    /// <summary>
    /// Đăng ký 1 processor xử lý phản hồi của NinePay
    /// </summary>
    public interface INinePayProcessor
    {
        /// <summary>
        /// Loại giao dịch đăng ký xử lý, string.Empty nếu muốn xử lý tất cả
        /// </summary>
        string Type => string.Empty;

        /// <summary>
        /// Xử lý trong trường hợp NinePay returnUrl
        /// </summary>
        /// <param name="response">Dữ liệu từ phản hồi của NinePay</param>
        Task ProcessReturnURL(NinePayResponse response) => Task.CompletedTask;

        /// <summary>
        /// Xử lý trong trường hợp NinePay IPN
        /// </summary>
        /// <param name="response">Dữ liệu từ phản hồi của NinePay</param>
        Task ProcessIPN(NinePayResponse response) => Task.CompletedTask;
    }
}
