using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.Json;

namespace NinePay.NetCore
{
    public static class NinePayExtensions
    {
        /// <summary>
        /// Cấu hình NinePayOptions và NinePayService vào Dependency Injection container.
        /// </summary>
        /// <param name="services">IServiceCollection của ứng dụng.</param>
        /// <param name="configureOptions">Hàm cấu hình NinePayOptions.</param>
        /// <returns>IServiceCollection sau khi thêm cấu hình.</returns>
        public static IServiceCollection AddNinePay(this IServiceCollection services, Action<NinePayOptions> configureOptions)
        {
            // Đăng ký memcache để lưu custom data
            services.AddMemoryCache();

            services.AddNinePay<NinePayService>(configureOptions);

            return services;
        }

        /// <summary>
        /// Cấu hình NinePayOptions và NinePayService vào Dependency Injection container.
        /// </summary>
        /// <typeparam name="T">Dịch vụ xử lý tùy chỉnh triển khai INinePayService</typeparam>
        /// <param name="services">IServiceCollection của ứng dụng.</param>
        /// <param name="configureOptions">Hàm cấu hình NinePayOptions.</param>
        /// <returns>IServiceCollection sau khi thêm cấu hình.</returns>
        public static IServiceCollection AddNinePay<T>(this IServiceCollection services, Action<NinePayOptions> configureOptions)
            where T : class, INinePayService
        {
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions), "ConfigureOptions cannot be null.");
            }

            services.AddHttpContextAccessor();
            services.AddHttpClient<INinePayService, T>();

            // Đăng ký NinePayOptions từ hàm cấu hình
            services.Configure(configureOptions);

            // Đăng ký INinePayService với NinePayService
            services.AddScoped<INinePayService, T>();

            return services;
        }

        /// <summary>
        /// Cấu hình 2 endpoints để xử lý phản hồi của NinePay
        /// </summary>
        public static IEndpointRouteBuilder MapNinePay(this IEndpointRouteBuilder endpoints)
        {
            // Lấy thông tin cấu hình từ NinePayOptions
            var options = endpoints.ServiceProvider.GetRequiredService<IOptions<NinePayOptions>>().Value;

            // Endpoint đầu tiên để xử lý callback (ReturnURL) từ NinePay
            endpoints.MapGet(options.ReturnURL, async context =>
            {
                // Lấy dịch vụ NinePayService từ container DI
                var onePayService = context.RequestServices.GetRequiredService<INinePayService>();

                // Lấy danh sách các processor đã được đăng ký
                var onePayProcessors = context.RequestServices.GetServices<INinePayProcessor>().ToArray();

                // Xử lý callback và lấy thông tin trả về
                var (type, response, returnUrl) = await onePayService.ProcessCallBack();

                // Lọc các processor theo loại (nếu có loại phù hợp)
                var processors = onePayProcessors.Where(x => string.IsNullOrEmpty(x.Type) || x.Type == type).ToArray();

                // Gọi phương thức ProcessURL của các processor phù hợp
                foreach (var processor in processors) await processor.ProcessReturnURL(response);

                // Chuyển hướng người dùng đến URL đã đăng ký trong CreatePaymentLink
                context.Response.Redirect(returnUrl);
            });

            // Endpoint thứ hai để xử lý IPN từ NinePay
            endpoints.MapPost(options.IPNURL, async context =>
            {
                // Lấy dịch vụ NinePayService từ container DI
                var onePayService = context.RequestServices.GetRequiredService<INinePayService>();

                // Lấy danh sách các processor đã được đăng ký
                var onePayProcessors = context.RequestServices.GetServices<INinePayProcessor>().ToArray();

                try
                {
                    // Xử lý callback và lấy thông tin trả về
                    var (type, response, returnUrl) = await onePayService.ProcessCallBack();

                    // Lọc các processor theo loại (nếu có loại phù hợp)
                    var processors = onePayProcessors.Where(x => string.IsNullOrEmpty(x.Type) || x.Type == type).ToArray();

                    // Gọi phương thức ProcessIPN của các processor phù hợp
                    foreach (var processor in processors) await processor.ProcessIPN(response);

                    // Trả về phản hồi cho NinePay xác nhận đã xử lý
                    await context.Response.WriteAsync(string.Empty);
                }
                catch (Exception ex)
                {
                    // Trả về phản hồi cho NinePay xác nhận đã xử lý
                    await context.Response.WriteAsync(string.Empty);
                }
            });

            // Trả về danh sách endpoints đã cấu hình
            return endpoints;
        }

        /// <summary>
        /// Sinh mã ngẫu nhiên cho giao dịch
        /// </summary>
        /// <param name="length">Độ dài chuỗi</param>
        /// <param name="chars">Các ký tự trong mã</param>
        /// <returns>Mã</returns>
        public static string GenerateRandomString(int length = 12, string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
        {
            var random = new Random(Guid.NewGuid().GetHashCode());
            char[] stringChars = new char[length];

            for (int i = 0; i < length; i++)
                stringChars[i] = chars[random.Next(chars.Length)];

            return new string(stringChars);
        }
    }
}
