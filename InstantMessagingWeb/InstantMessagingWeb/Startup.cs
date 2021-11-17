using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using InstantMessagingWeb.Common.Configuration;
using InstantMessagingWeb.Extensions;
using InstantMessagingWeb.Middleware;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi.Models;

namespace InstantMessagingWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private Settings Settings { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSwaggerGen(c => { //<-- NOTE 'Add' instead of 'Configure'
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Instant Messaging Web App API V 1.0",
                    Version = "v1"
                });
            });

            services.AddJsonOptions();
            RegisterSettings(services);

            services.AddCustomTypes();
            services.RegisterAuthSchemes(Configuration);

            services.AddMvc(opt => opt.Filters.Add(typeof(LoggingMiddleware)));
            // TODO: Configure Fluent Validation
            // services.AddMvc().AddFluentValidation();
            services.AddApplicationInsightsTelemetry(Configuration["ApplicationInsights:InstrumentationKey"]);
            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration => { configuration.RootPath = "ClientApp/dist"; });

            services.AddCors(options => options.AddPolicy("CorsPolicy",
                   builder =>
                   {
                       builder
                           .WithOrigins(Configuration.GetValue<string>("VhServices:VideoWebUrl"))
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials()
                           .SetIsOriginAllowed((host) => true);
                   }));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline. 
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!env.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Instant Messaging Web App API V1"); });
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                IdentityModelEventSource.ShowPII = true;
            }
            else
            {
                app.UseExceptionHandler("/Error");

                if (!Settings.DisableHttpsRedirection)
                {
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts();
                    app.UseHttpsRedirection();
                }
            }

            app.UseCors("CorsPolicy");
            app.UseRouting();
            
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<ExceptionMiddleware>();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();

                var hubPath = Configuration.GetValue<string>("VhServices:ImEventHubPath");
                endpoints.MapHub<EventHub.Hub.ImEventHub>(hubPath, options =>
                {
                    options.Transports = HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling |
                                         HttpTransportType.WebSockets;
                });
            });

        }

        private void RegisterSettings(IServiceCollection services)
        {
            Settings = Configuration.Get<Settings>();
            services.AddSingleton(Settings);    

            services.Configure<AzureAdConfiguration>(options =>
            {
                Configuration.Bind("AzureAd", options);
                options.ApplicationInsights = new ApplicationInsightsConfiguration();
                Configuration.Bind("ApplicationInsights", options.ApplicationInsights);
            });

            services.Configure<HearingServicesConfiguration>(options => Configuration.Bind("VhServices", options));

            var connectionStrings = Configuration.GetSection("ConnectionStrings").Get<ConnectionStrings>();
            services.AddSingleton(connectionStrings);
        }
    }
 }
