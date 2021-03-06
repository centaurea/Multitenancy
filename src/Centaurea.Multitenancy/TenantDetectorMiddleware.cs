using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Centaurea.Multitenancy
{
    public class TenantDetectorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ITenantConfiguration _cfg;

        public TenantDetectorMiddleware(RequestDelegate next, ITenantConfiguration configuration)
        {
            _next = next;
            _cfg = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Items.Add(Constants.TENANT_CONTEXT_KEY, _cfg.GetMatchingOrDefault(context.Request.Host.Host));

            if (_next != null)
            {
                await _next(context);
            }
        }
    }
}