using Microsoft.Extensions.Caching.Memory;
using NinePay.NetCore.Web.Controllers;
using System.Text.Json;

namespace NinePay.NetCore.Web.Services
{
    public class TestNinePayProcessor : INinePayProcessor
    {
        public const string TYPE = nameof(TestNinePayProcessor);

        private readonly ILogger<NinePayTestController> _logger;
        private readonly IMemoryCache _cache;

        public string Type => TYPE;

        public TestNinePayProcessor(ILogger<NinePayTestController> logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        public Task ProcessReturnURL(NinePayResponse response)
        {
            _cache.Set($"ReturnURL-{response.RequestCode}", response);

            var data = JsonSerializer.Serialize(response);
            _logger.LogInformation("Process ReturnURL, response: {0}", data);
            return Task.CompletedTask;
        }

        public Task ProcessIPN(NinePayResponse response)
        {
            _cache.Set($"IPN-{response.RequestCode}", response);

            var data = JsonSerializer.Serialize(response);
            _logger.LogInformation("Process IPN, response: {0}", data);
            return Task.CompletedTask;
        }
    }
}
