using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InstantMessagingWeb.AuthenticationSchemes;
using InstantMessagingWeb.Common.Configuration;
using InstantMessagingWeb.Common.Model;

namespace InstantMessagingWeb.Extensions
{
    public static class ConfigureAuthSchemeExtensions
    {
        public static void RegisterAuthSchemes(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var azureAdConfiguration = configuration.GetSection("AzureAd").Get<AzureAdConfiguration>();

            var videoHearingServicesConfiguration = configuration.GetSection("VhServices").Get<HearingServicesConfiguration>();
            var eventhubPath = videoHearingServicesConfiguration.EventHubPath;

            var providerSchemes = new List<IProviderSchemes>
            {
                new VhAadScheme(azureAdConfiguration, eventhubPath),
            };

            var authenticationBuilder = serviceCollection.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddPolicyScheme(JwtBearerDefaults.AuthenticationScheme, "Handler", options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        Console.WriteLine($"context Request Path: {0}", context.Request.Path);
                        var isEventHubRequest = context.Request.Path.StartsWithSegments("/imeventhub");
                        var provider = GetProviderFromRequest(context.Request, providerSchemes);
                        return providerSchemes.Single(s => s.Provider == provider).GetScheme(isEventHubRequest);
                    };
                });

            foreach (var scheme in providerSchemes)
            {
                authenticationBuilder = scheme.AddSchemes(authenticationBuilder);
            }

            serviceCollection.AddMemoryCache();
            serviceCollection.AddAuthPolicies(providerSchemes);
        }

        public static AuthProvider GetProviderFromRequest(HttpRequest httpRequest, IList<IProviderSchemes> providerSchemes)
        {
            var defaultScheme = AuthProvider.VHAAD;
            if (httpRequest.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var jwtToken = new JwtSecurityToken(authHeader.ToString().Replace("Bearer ", string.Empty));
                return providerSchemes.SingleOrDefault(s => s.BelongsToScheme(jwtToken))?.Provider ?? defaultScheme;
            }

            return defaultScheme;
        }

        private static void AddAuthPolicies(this IServiceCollection serviceCollection, IList<IProviderSchemes> providerSchemes)
        {
            serviceCollection.AddAuthorization(options => AddPolicies(options, providerSchemes));
            serviceCollection.AddMvc(options => options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())));
        }

        private static void AddPolicies(AuthorizationOptions options, IList<IProviderSchemes> schemes)
        {
            var allRoles = new[]
            {
                AppRoles.CitizenRole, AppRoles.JudgeRole, AppRoles.RepresentativeRole, AppRoles.CaseAdminRole,
                AppRoles.VhOfficerRole, AppRoles.JudicialOfficeHolderRole, AppRoles.StaffMember
            };

            var rolePolicies = new Dictionary<string, string[]>
            {
                [AppRoles.JudgeRole] = new[] { AppRoles.JudgeRole },
                [AppRoles.VhOfficerRole] = new[] { AppRoles.VhOfficerRole },
                ["Host"] = new[] {AppRoles.JudgeRole, AppRoles.StaffMember },
                ["Judicial"] = new[] { AppRoles.JudgeRole, AppRoles.JudicialOfficeHolderRole, AppRoles.StaffMember },
                ["Individual"] = new[] { AppRoles.CitizenRole, AppRoles.RepresentativeRole, AppRoles.QuickLinkParticipant, AppRoles.QuickLinkObserver },
                [AppRoles.StaffMember] = new[] { AppRoles.StaffMember },
                [AppRoles.RepresentativeRole] = new[] { AppRoles.RepresentativeRole },
                [AppRoles.CitizenRole] = new[] { AppRoles.CitizenRole }
            };

            foreach (var scheme in schemes.SelectMany(s => s.GetProviderSchemes()))
            {
                options.AddPolicy(scheme, new AuthorizationPolicyBuilder()
               .RequireAuthenticatedUser()
               .RequireRole(allRoles)
               .AddAuthenticationSchemes(scheme)
               .Build());
            }

            foreach (var policy in rolePolicies)
            {
                var policyBuilder = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireRole(policy.Value);

                // TODO: These didnt use to include the EventHubSchemes but should they have?
                foreach (var schemeName in schemes.Select(s => s.SchemeName))
                {
                    policyBuilder = policyBuilder.AddAuthenticationSchemes(schemeName);
                }

                options.AddPolicy(policy.Key, policyBuilder.Build());
            }
        }
    }
}
