﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Centaurea.Multitenancy.Annotation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Centaurea.Multitenancy.Test
{
    public class BaseTest
    {
        protected Mock<HttpContext> GetHttpContextMock(string host, Dictionary<object, object> requestData)
        {
            Mock<HttpContext> ctxMock = new Mock<HttpContext>();
            ctxMock.Setup(httpContext => httpContext.Request).Returns(GetMockedRequest(host).Object);
            ctxMock.Setup(ctx => ctx.Items).Returns(requestData);
            return ctxMock;
        }

        private Mock<HttpRequest> GetMockedRequest(string host = "www.site.com")
        {
            Mock<HttpRequest> requestMock = new Mock<HttpRequest>();
            requestMock.Setup(r => r.Host).Returns(new HostString(host));
            return requestMock;
        }

        protected Mock<IApplicationBuilder> InitAppMockWithMiddleware(Dictionary<string, string> middlewareConfig)
        {
            Mock<IApplicationBuilder> appMock = new Mock<IApplicationBuilder>();
            ServiceCollection services = new ServiceCollection();
            var tenantConf = GetTenantConfiguration(mapps: middlewareConfig.Select(p => (p.Key, p.Value)).ToArray());
            services.AddSingleton<IHttpContextAccessor>(new Mock<IHttpContextAccessor>().Object);
            appMock.Setup(app => app.ApplicationServices)
                .Returns(services.BuildMultitenantServiceProvider(tenantConf));
            appMock.Object.UseMultitenancy();
            appMock.Setup(app => app.Build())
                .Returns(ctx => new TenantDetectorMiddleware(null, tenantConf.TenantConfiguration).InvokeAsync(ctx));
            return appMock;
        }

        protected async Task<TenantDetectorMiddleware> EmulateRequestExecution(Mock<IHttpContextAccessor> accessor,
            string host, string tenantId, string tenantRegexp)
        {
            Mock<HttpContext> ctx = GetHttpContextMock(host, new Dictionary<object, object>());
            accessor.Setup(acc => acc.HttpContext).Returns(ctx.Object);

            TenantDetectorMiddleware detector = new TenantDetectorMiddleware(null,
                MultitenantMappingConfiguration.FromDictionary(
                    new Dictionary<string, string> { { tenantId, tenantRegexp } }));
            await detector.InvokeAsync(ctx.Object);
            return detector;
        }

        protected MultitenancyConfiguration DefaultConfig { get => GetTenantConfiguration(); }

        protected MultitenancyConfiguration GetTenantConfiguration(IConfiguration config = null, params (string, string)[] mapps)
        {
            return new MultitenancyConfiguration
            {
                TenantConfiguration = MultitenantMappingConfiguration.FromDictionary(mapps.ToDictionary(t => t.Item1, t => t.Item2)),
                Config = config ?? new Mock<IConfiguration>().Object
            };
        }
    }
}