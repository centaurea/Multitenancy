﻿using System;
using System.Linq;
using Centaurea.Multitenancy.Annotation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Centaurea.Multitenancy
{
    public static class MultitenantServicesExtension
    {
        internal static void ActivateMultitenancy(this IServiceCollection services, MultitenancyConfiguration config, 
            Func<IServiceProvider, IMultitenantServiceProvider> rootProviderGetter)
        {
            services.ConfigureTenants(config.Config, config.ConfigKey);
            services.AddSingleton(config.TenantConfiguration);
            services.TryAddSingleton(typeof(ITenantResolver), typeof(DefaultTenantResolver));
            services.AddSingleton<IMultitenantServiceProvider>(rootProviderGetter);
            services.AddSingleton<ITenantAmbientContext, TenantAmbientContextAccessor>();

            services.Replace(ServiceDescriptor.Singleton(typeof(IOptionsMonitorCache<>), typeof(TenantOptionsMonitorCache<>)));
            services.Replace(ServiceDescriptor.Singleton(typeof(IOptions<>), typeof(TenantOptionsManager<>)));
            services.Replace(ServiceDescriptor.Scoped(typeof(IOptionsSnapshot<>), typeof(TenantOptionsManager<>)));

//            foreach (TenantId tenant in config.TenantConfiguration.GetAll().Except(new []{TenantId.DEFAULT_ID}))
//            {
//                services.AddScopedForTenant(typeof(ITenantResolver), typeof(CachedTenantResolver), tenant);
//            }
        }

        public static IServiceProvider BuildMultitenantServiceProvider(this IServiceCollection services, MultitenancyConfiguration config)
        {
            return BuildProvider(getter => new MultitenantServiceProvider(
                MultitenantServiceProvider.InitProviderCollections(services, config, getter)
                    .ToDictionary(kv => kv.Key, kv => (IServiceProvider)kv.Value.BuildServiceProvider())));
        }

        public static IServiceProvider BuildMultitenantServiceProvider(this IServiceCollection services,
            bool validateScopes, MultitenancyConfiguration config)
        {
            return BuildProvider(getter => new MultitenantServiceProvider(MultitenantServiceProvider
                .InitProviderCollections(services, config, getter)
                .ToDictionary(kv => kv.Key, kv => (IServiceProvider) kv.Value.BuildServiceProvider(validateScopes))));
        }

        public static IServiceProvider BuildMultitenantServiceProvider(this IServiceCollection services,
            ServiceProviderOptions opts, MultitenancyConfiguration config)
        {
            return BuildProvider(getter => new MultitenantServiceProvider(MultitenantServiceProvider.InitProviderCollections(services, config, getter)
                .ToDictionary(kv => kv.Key, kv => (IServiceProvider)kv.Value.BuildServiceProvider(opts))));
        }

        private static IMultitenantServiceProvider BuildProvider(Func<Func<IServiceProvider, IMultitenantServiceProvider>, MultitenantServiceProvider> builder)
        {
            IMultitenantServiceProvider rootProvider = null;
            rootProvider = builder(p => rootProvider);
            return rootProvider;
        }

        public static void AddScopedForTenant(this IServiceCollection services, Type serviceType, TenantId tenantId)
        {
            services.Add(new TenantedServiceDescriptor(tenantId, serviceType, serviceType, ServiceLifetime.Scoped));
        }

        public static void AddScopedForTenant(this IServiceCollection services, Type serviceType,
            Type implementationType, TenantId tenantId)
        {
            services.Add(new TenantedServiceDescriptor(tenantId, serviceType, implementationType,
                ServiceLifetime.Scoped));
        }

        public static void AddScopedForTenant<TService>(this IServiceCollection services, TenantId tenantId)
        {
            services.AddScopedForTenant(typeof(TService), tenantId);
        }

        public static void AddScopedForTenant<TService, TImplementation>(this IServiceCollection services,
            TenantId tenantId)
        {
            services.AddScopedForTenant(typeof(TService), typeof(TImplementation), tenantId);
        }

        public static void AddTransientForTenant(this IServiceCollection services, Type serviceType, TenantId tenantId)
        {
            services.Add(new TenantedServiceDescriptor(tenantId, serviceType, serviceType, ServiceLifetime.Transient));
        }

        public static void AddTransientForTenant(this IServiceCollection services, Type serviceType,
            Type implementationType, TenantId tenantId)
        {
            services.Add(new TenantedServiceDescriptor(tenantId, serviceType, implementationType,
                ServiceLifetime.Transient));
        }

        public static void AddTransientForTenant<TService>(this IServiceCollection services, TenantId tenantId)
        {
            services.AddTransientForTenant(typeof(TService), tenantId);
        }

        public static void AddTransientForTenant<TService, TImplementation>(this IServiceCollection services,
            TenantId tenantId)
        {
            services.AddTransientForTenant(typeof(TService), typeof(TImplementation), tenantId);
        }

        public static void AddSingletonForTenant(this IServiceCollection services, Type serviceType, TenantId tenantId)
        {
            services.Add(new TenantedServiceDescriptor(tenantId, serviceType, serviceType, ServiceLifetime.Singleton));
        }

        public static void AddSingletonForTenant(this IServiceCollection services, Type serviceType,
            Type implementationType, TenantId tenantId)
        {
            services.Add(new TenantedServiceDescriptor(tenantId, serviceType, implementationType,
                ServiceLifetime.Singleton));
        }

        public static void AddSingletonForTenant<TService>(this IServiceCollection services, TenantId tenantId)
        {
            services.AddSingletonForTenant(typeof(TService), tenantId);
        }

        public static void AddSingletonForTenant<TService, TImplementation>(this IServiceCollection services,
            TenantId tenantId)
        {
            services.AddSingletonForTenant(typeof(TService), typeof(TImplementation), tenantId);
        }

        internal static void ConfigureTenants(this IServiceCollection services, IConfiguration config, string key = Constants.TENANT_CONF_KEY)
        {
            services.AddSingleton(new TenantConfig { Config = config.GetSection(key) });
        }
    }
}