﻿using Castle.DynamicProxy;
using Dapr.Client;
using JetBrains.Annotations;
using LINGYUN.Abp.Dapr.Client;
using LINGYUN.Abp.Dapr.Client.DynamicProxying;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Reflection;
using Volo.Abp;
using Volo.Abp.Castle.DynamicProxy;
using Volo.Abp.Json.SystemTextJson;
using Volo.Abp.Validation;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionDynamicDaprClientProxyExtensions
    {
        private static readonly ProxyGenerator ProxyGeneratorInstance = new ProxyGenerator();

        public static IServiceCollection AddDaprClient(
            [NotNull] this IServiceCollection services,
            Action<DaprClientBuilder> setup = null)
        {
            Check.NotNull(services, nameof(services));

            services.TryAddSingleton(provider =>
            {
                var abpSystemTextJsonSerializerOptions = provider.GetRequiredService<IOptions<AbpSystemTextJsonSerializerOptions>>().Value;
                var abpDaprClientOptions = provider.GetRequiredService<IOptions<AbpDaprClientOptions>>().Value;

                var builder = new DaprClientBuilder()
                .UseHttpEndpoint(abpDaprClientOptions.HttpEndpoint)
                .UseJsonSerializationOptions(abpSystemTextJsonSerializerOptions.JsonSerializerOptions);

                if (!abpDaprClientOptions.GrpcEndpoint.IsNullOrWhiteSpace() &&
                    abpDaprClientOptions.GrpcChannelOptions != null)
                {
                    builder
                        .UseGrpcEndpoint(abpDaprClientOptions.GrpcEndpoint)
                        .UseGrpcChannelOptions(abpDaprClientOptions.GrpcChannelOptions);
                }

                setup?.Invoke(builder);

                return builder.Build();
            });

            services.AddHttpClient(AbpDaprClientModule.DaprHttpClient);

            return services;
        }

        public static IServiceCollection AddDaprClientProxies(
            [NotNull] this IServiceCollection services,
            [NotNull] Assembly assembly,
            [NotNull] string remoteServiceConfigurationName = DaprRemoteServiceConfigurationDictionary.DefaultName,
            bool asDefaultServices = true)
        {
            Check.NotNull(services, nameof(assembly));

            var serviceTypes = assembly.GetTypes().Where(IsSuitableForDynamicActorProxying).ToArray();

            foreach (var serviceType in serviceTypes)
            {
                services.AddDaprClientProxy(
                    serviceType,
                    remoteServiceConfigurationName,
                    asDefaultServices
                );
            }

            return services;
        }

        public static IServiceCollection AddDaprClientProxy<T>(
            [NotNull] this IServiceCollection services,
            [NotNull] string remoteServiceConfigurationName = DaprRemoteServiceConfigurationDictionary.DefaultName,
            bool asDefaultService = true)
        {
            return services.AddDaprClientProxy(
                typeof(T),
                remoteServiceConfigurationName,
                asDefaultService
            );
        }

        public static IServiceCollection AddDaprClientProxy(
            [NotNull] this IServiceCollection services,
            [NotNull] Type type,
            [NotNull] string remoteServiceConfigurationName = DaprRemoteServiceConfigurationDictionary.DefaultName,
            bool asDefaultService = true)
        {
            Check.NotNull(services, nameof(services));
            Check.NotNull(type, nameof(type));
            Check.NotNullOrWhiteSpace(remoteServiceConfigurationName, nameof(remoteServiceConfigurationName));

            // AddHttpClientFactory(services, remoteServiceConfigurationName);

            services.Configure<AbpDaprClientProxyOptions>(options =>
            {
                options.DaprClientProxies[type] = new DynamicDaprClientProxyConfig(type, remoteServiceConfigurationName);
            });

            var interceptorType = typeof(DynamicDaprClientProxyInterceptor<>).MakeGenericType(type);
            services.AddTransient(interceptorType);

            var interceptorAdapterType = typeof(AbpAsyncDeterminationInterceptor<>).MakeGenericType(interceptorType);

            var validationInterceptorAdapterType =
                typeof(AbpAsyncDeterminationInterceptor<>).MakeGenericType(typeof(ValidationInterceptor));

            if (asDefaultService)
            {
                services.AddTransient(
                    type,
                    serviceProvider => ProxyGeneratorInstance
                        .CreateInterfaceProxyWithoutTarget(
                            type,
                            (IInterceptor)serviceProvider.GetRequiredService(validationInterceptorAdapterType),
                            (IInterceptor)serviceProvider.GetRequiredService(interceptorAdapterType)
                        )
                );
            }

            return services;
        }

        private static bool IsSuitableForDynamicActorProxying(Type type)
        {
            //TODO: Add option to change type filter

            return type.IsInterface
                && type.IsPublic
                && !type.IsGenericType
                && typeof(IRemoteService).IsAssignableFrom(type);
        }
    }
}
