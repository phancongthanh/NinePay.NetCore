# NinePay.NetCore

NinePay.NetCore là một thư viện được thiết kế để tích hợp cổng thanh toán 9Pay vào các ứng dụng .NET Core. Nó giúp đơn giản hóa quá trình tạo liên kết thanh toán, xử lý callback (ReturnURL, IPN) và xử lý phản hồi thanh toán.

## Tính năng

- **Tích hợp Dependency Injection**: Dễ dàng cấu hình `NinePayOptions` và `NinePayService` với Dependency Injection.
- **Dịch vụ Thanh toán Tùy chỉnh**: Cho phép sử dụng các triển khai tùy chỉnh của interface `INinePayService` để xử lý thanh toán.
- **Xử lý Callback**: Hỗ trợ tự động cấu hình các endpoints để xử lý các callback từ 9Pay (ReturnURL, IPN).
- **Tạo Liên kết Thanh toán**: Cho phép tạo liên kết thanh toán cho các giao dịch 9Pay.
- **Tích hợp Processor**: Hỗ trợ thêm các processor tùy chỉnh để xử lý dữ liệu trả về từ 9Pay.

## Cài đặt

Để cài đặt NinePay.NetCore, sử dụng NuGet Package Manager hoặc .NET CLI:

```bash
dotnet add package NinePay.NetCore
```

## Cấu hình và Thiết lập

### 1. Cấu hình NinePay trong `Startup.cs` hoặc `Program.cs`

Trong `Startup.cs` hoặc `Program.cs` của ứng dụng, đăng ký các dịch vụ của 9Pay và cấu hình các tùy chọn.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddNinePay(options => Configuration.GetSection("NinePay").Bind(options));
    services.AddTransient<INinePayProcessor, TestNinePayProcessor>();
}
```

### 2. Định tuyến các Endpoints của NinePay

Trong phương thức `Configure`, cấu hình các endpoint để xử lý các phản hồi ReturnURL và IPN từ NinePay.

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.MapNinePay();
}
```

### 3. Tạo Liên kết Thanh toán

Sử dụng `NinePayService` để tạo một liên kết thanh toán cho giao dịch.

```csharp
public class SomeClass
{
    private readonly INinePayService _NinePayService;

    public SomeClass(INinePayService NinePayService)
    {
        _NinePayService = NinePayService;
    }

    public async Task CreateLink(NinePayRequest request, string returnUrl)
    {
        var url = await _NinePayService.CreatePaymentLink(TestNinePayProcessor.TYPE, request, returnUrl);
    }
}
```

## Tùy chỉnh Dịch vụ Thanh toán

Bạn có thể ghi đè `NinePayService` hoặc triển khai dịch vụ `INinePayService` của riêng mình và đăng ký nó trong DI container. Dưới đây là ví dụ:

```csharp
public class CustomNinePayService : NinePayService
{
    // Ghi đè các phương thức xử lý yêu cầu ở đây
}
```

Sau đó, đăng ký dịch vụ trong DI container:

```csharp
services.AddNinePay<CustomNinePayService>(options => Configuration.GetSection("NinePay").Bind(options));
```

## Xử lý Callback từ NinePay

Thư viện tự động cấu hình hai endpoint để xử lý callback URLs từ NinePay, bao gồm ReturnURL và IPN:

1. **ReturnURL**: Xử lý qua endpoint `MapGet(options.ReturnURL)`.
2. **IPN**: Xử lý qua endpoint `MapPost(options.IPNURL)`.

Bạn có thể tùy chỉnh cách xử lý phản hồi bằng cách triển khai interface `INinePayProcessor` và đăng ký các processor tùy chỉnh.

Ví dụ về một processor đơn giản:

```csharp
public class TestNinePayProcessor : INinePayProcessor
{
    public const string TYPE = nameof(TestNinePayProcessor);
    private readonly ILogger<TestNinePayProcessor> _logger;

    public string Type => TYPE;

    public TestNinePayProcessor(ILogger<TestNinePayProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessReturnURL(NinePayResponse response)
    {
        _logger.LogInformation("Đang xử lý ReturnURL cho mã yêu cầu: {0}", response.RequestCode);
        return Task.CompletedTask;
    }

    public Task ProcessIPN(NinePayResponse response)
    {
        _logger.LogInformation("Đang xử lý IPN cho mã yêu cầu: {0}", response.RequestCode);
        return Task.CompletedTask;
    }
}
```

### Đăng ký Processor Tùy chỉnh

Đăng ký processor tùy chỉnh như sau:

```csharp
services.AddTransient<INinePayProcessor, TestNinePayProcessor>();
```

## Cấu hình Ví dụ

```json
{
  "NinePay": {
    "ApiUrl": "https://sand-payment.9pay.vn",
    "MerchantKey": "",
    "SecretKey": "",
    "ChecksumKey": ""
  }
}
```

## Giấy phép

NinePay.NetCore là một thư viện mã nguồn mở và được cấp phép dưới [Giấy phép MIT](LICENSE). Bạn có thể tự do sử dụng, sửa đổi và phân phối thư viện trong các dự án của mình.

## Cảnh báo

- Để đảm bảo tính bảo mật, tác giả khuyến khích bạn tham khảo/tải về và tùy chỉnh mã nguồn của thư viện này để phù hợp với yêu cầu bảo mật cao hơn cho hệ thống của bạn trong môi trường sản xuất với các giao dịch thanh toán tiền **THẬT**.
- Mặc dù thư viện này đã được thiết kế để hỗ trợ tích hợp 9Pay, bạn có thể tùy chỉnh và tối ưu mã nguồn để đáp ứng các yêu cầu bảo mật cao hơn, nếu cần thiết.
- Hướng dẫn chi tiết về quy trình tích hợp dịch vụ thanh toán 9Pay có sẵn tại [**đây**](https://developers.9pay.vn/thanh-toan-the-quoc-te/redirect).
