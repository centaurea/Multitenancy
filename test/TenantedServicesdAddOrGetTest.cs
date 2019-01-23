﻿using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Centaurea.Multitenancy.Test
{
    public class TenantedServicesdAddOrGetTest : BaseTest
    {
        interface IFake
        {
        };

        class Fake : IFake
        {
        };

        class TenantFake : IFake
        {
        };

        class Dep
        {
            public Dep(IFake fake)
            {
                Faked = fake;
            }

            public IFake Faked { get; set; }
        }

        private IServiceCollection _services;

        public TenantedServicesdAddOrGetTest()
        {
            _services = new ServiceCollection();
        }

        [Fact]
        public void TestMultitenancyDepsAdding()
        {
            _services.ActivateMultitenancy();
            Assert.NotEmpty(_services);
        }

        [Fact]
        public void AddTenantScopedDep()
        {
            _services.AddScoped<Fake>();
            Assert.Equal(1, _services.Count);

            _services.AddScopedForTenant<Fake>(new TenantId("tenant1"));
            Assert.Equal(2, _services.Count);
        }

        [Fact]
        public void AddingDepToDefaultTenantRewriteRegular()
        {
            _services.ActivateMultitenancy();
            _services.AddScoped<IFake, Fake>();
            _services.AddScopedForTenant<IFake, TenantFake>(new TenantId());
            _services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            IServiceProvider provider = _services.BuildMultitenantServiceProvider();
            IFake result = provider.GetRequiredService<IFake>();
            Assert.Equal(typeof(TenantFake), result.GetType());
        }

        [Fact]
        public void AddTenantDependentAndIndependentDepsWorksFine()
        {
            _services.AddScoped<IFake, Fake>();
            _services.AddScopedForTenant<IFake, TenantFake>(new TenantId("test"));

            Assert.Equal(2, _services.Count);
        }

        [Fact]
        public async void ResolveTenantScopedService()
        {
            string ya = "yahoo";
            _services.ActivateMultitenancy();
            _services.AddScoped<IFake, Fake>();
            _services.AddScoped<Fake>();
            _services.AddScoped<Dep>();
            _services.AddScopedForTenant<IFake, TenantFake>(new TenantId(ya));
            Mock<IHttpContextAccessor> accessor = new Mock<IHttpContextAccessor>();
            _services.AddSingleton(accessor.Object);
            IServiceProvider serviceProvider = _services.BuildMultitenantServiceProvider();

            await EmulateRequestExecution(accessor, "google.com", ya, "yahoo");

            IFake service = serviceProvider.GetService<IFake>();
            Assert.Equal(typeof(Fake), service.GetType());

            Dep dep = serviceProvider.GetService<Dep>();
            Assert.Equal(typeof(Fake), dep.Faked.GetType());

            await EmulateRequestExecution(accessor, "yahoo.com", ya, "yahoo");
            service = serviceProvider.GetService<IFake>();
            Fake notOverridenByScopeService = serviceProvider.GetService<Fake>();
            Assert.Equal(typeof(TenantFake), service.GetType());
            Assert.NotNull(notOverridenByScopeService);
            dep = serviceProvider.GetService<Dep>();
            Assert.Equal(typeof(TenantFake), dep.Faked.GetType());
        }
         

        //TODO:Add unit tests for transient and singleton registered services
    }
}