using System;
using System.Net.Http;
using InstantMessagingAPI.Client;
using InstantMessagingWeb.Common;
using InstantMessagingWeb.Common.Caching;
using InstantMessagingWeb.Common.Configuration;
using InstantMessagingWeb.Common.Helpers;
using InstantMessagingWeb.Common.Security;
using InstantMessagingWeb.Common.SignalR;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Polly;
using Polly.Extensions.Http;
using UserApi.Client;
using VideoApi.Client;

namespace InstantMessagingWeb.Extensions
{
    public static class ConfigureServicesExtensions
    {
        public static IServiceCollection AddCustomTypes(this IServiceCollection services)
        {
            services.AddControllers().AddControllersAsServices();

            services.AddMemoryCache();

            services.AddSingleton<ITelemetryInitializer, RequestTelemetry>();
            
            services.AddTransient<UserApiTokenHandler>();
            services.AddTransient<InstantMessagingApiTokenHandler>();
            services.AddTransient<VideoApiTokenHandler>();

            services.AddSingleton<IUserIdProvider, NameUserIdProvider>();
            services.AddScoped<ITokenProvider, TokenProvider>();
            services.AddScoped<AdUserProfileService>();
            services.AddScoped<IUserProfileService, CachedProfileService>();
            services.AddScoped<IConferenceCache, DistributedConferenceCache>();
            services.AddSingleton<IUserCache, DistributedUserCache>();
            services.AddScoped<ILoggingDataExtractor, LoggingDataExtractor>();

            var container = services.BuildServiceProvider();
            var servicesConfiguration = container.GetService<IOptions<HearingServicesConfiguration>>().Value;

            services.AddHttpClient<IInstantMessagingApiClient, InstantMessagingApiClient>()
                .AddHttpMessageHandler<InstantMessagingApiTokenHandler>()
                .AddTypedClient(httpClient => BuildInstantMessagingApiClient(httpClient, servicesConfiguration));

            services.AddHttpClient<IUserApiClient, UserApiClient>()
                .AddHttpMessageHandler<UserApiTokenHandler>()
                .AddTypedClient(httpClient => BuildUserApiClient(httpClient, servicesConfiguration));

            services.AddHttpClient<IVideoApiClient, VideoApiClient>()
                .AddHttpMessageHandler<VideoApiTokenHandler>()
                .AddTypedClient(httpClient => BuildVideoApiClient(httpClient, servicesConfiguration));

            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };

            var connectionStrings = container.GetService<ConnectionStrings>();
            services.AddSignalR()
               .AddAzureSignalR(options =>
               {
                   options.ConnectionString = connectionStrings.SignalR;
                   options.ClaimsProvider = context => context.User.Claims;
               })
               .AddNewtonsoftJsonProtocol(options =>
               {
                   options.PayloadSerializerSettings.Formatting = Formatting.None;
                   options.PayloadSerializerSettings.ContractResolver = contractResolver;
                   options.PayloadSerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                   options.PayloadSerializerSettings.Converters.Add(
                       new StringEnumConverter());
               })
               .AddHubOptions<EventHub.Hub.ImEventHub>(options => { options.EnableDetailedErrors = true; });

            services.AddStackExchangeRedisCache(options => { options.Configuration = connectionStrings.RedisCache; });
            return services;
        }

        public static IServiceCollection AddJsonOptions(this IServiceCollection serviceCollection)
        {
            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };

            serviceCollection.AddMvc()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = contractResolver;
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                });

            return serviceCollection;
        }

        private static IInstantMessagingApiClient BuildInstantMessagingApiClient(HttpClient httpClient,
           HearingServicesConfiguration servicesConfiguration)
        {
            return InstantMessagingApiClient.GetClient(servicesConfiguration.InstantMessagingApiUrl, httpClient);
        }

        private static IUserApiClient BuildUserApiClient(HttpClient httpClient,
           HearingServicesConfiguration serviceSettings)
        {
            return UserApiClient.GetClient(serviceSettings.UserApiUrl, httpClient);
        }

        private static IVideoApiClient BuildVideoApiClient(HttpClient httpClient,
          HearingServicesConfiguration serviceSettings)
        {
            return VideoApiClient.GetClient(serviceSettings.VideoApiUrl, httpClient);
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30));
        }
    }
}
