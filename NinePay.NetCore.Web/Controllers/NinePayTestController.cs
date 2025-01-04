using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using NinePay.NetCore.Web.Services;

namespace NinePay.NetCore.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NinePayTestController : ControllerBase
    {
        private readonly ILogger<NinePayTestController> _logger;
        private readonly INinePayService _onePayService;
        private readonly IMemoryCache _cache;

        public NinePayTestController(ILogger<NinePayTestController> logger, INinePayService onePayService, IMemoryCache cache)
        {
            _logger = logger;
            _onePayService = onePayService;
            _cache = cache;
        }

        [HttpPost]
        public async Task<string> CreatePaymentUrl([FromBody] NinePayRequest request, [FromQuery] string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(request.RequestCode)) return string.Empty;
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Url.Action(nameof(GetResponse), new { code = request.RequestCode });
            }
            var url = await _onePayService.CreatePaymentLink(TestNinePayProcessor.TYPE, request, returnUrl);
            return url;
        }

        [HttpGet]
        public async Task<object> GetResponse(string code)
        {
            var returnurlResponse = _cache.Get<NinePayResponse>($"ReturnURL-{code}");
            var ipnResponse = _cache.Get<NinePayResponse>($"IPN-{code}");
            var (result, ninePayData) = await _onePayService.QueryDR(returnurlResponse.RequestCode);
            return new { returnurlResponse, ipnResponse, querydr = new { result, ninePayData } };
        }
    }
}
