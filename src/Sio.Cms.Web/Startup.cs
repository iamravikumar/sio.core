using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WebEncoders;
using Sio.Cms.Lib.Models.Cms;
using Sio.Cms.Lib.Services;
using Sio.Identity.Services;
using Sio.Cms.Hub;
using Sio.Cms.Messenger.Hubs;
using Sio.Cms.Lib.Models.Account;

namespace Sio.Cms.Web
{
    public partial class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            _env = env;

        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment _env { get; }
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<SioCmsContext>();
            services.AddDbContext<SioDbContext>();

            // Enforce Request using https schema
            if (_env.IsDevelopment())
            {
                if (SioService.GetConfig<bool>("IsHttps"))
                {
                    services.AddHttpsRedirection(options =>
                    {
                        options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
                        options.HttpsPort = 5001;
                    });
                }
            }
            else
            {
                if (SioService.GetConfig<bool>("IsHttps"))
                {
                    services.AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
                    options.HttpsPort = 443;
                });
                }
            }

            // Config cookie options
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });


            //services.TryAddSingleton<IApiDescriptionGroupCollectionProvider, ApiDescriptionGroupCollectionProvider>();
            //services.TryAddEnumerable(
            //    ServiceDescriptor.Transient<IApiDescriptionProvider, DefaultApiDescriptionProvider>());

            // Config Authenticate 
            // App_Start/Startup.Auth.cs
            ConfigAuthorization(services, Configuration);

            
            //When View Page Source That changes only the HTML encoder, leaving the JavaScript and URL encoders with their (ASCII) defaults.
            services.Configure<WebEncoderOptions>(options => options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));            

            // add application services.
            services.AddTransient<IEmailSender, AuthEmailMessageSender>();
            services.AddTransient<ISmsSender, AuthSmsMessageSender>();
            services.AddSingleton<SioService>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            // add signalr
            services.AddSignalR();

            // Config server caching
            services.AddMvc(options =>
            {
                options.CacheProfiles.Add("Default",
                    new CacheProfile()
                    {
                        Duration = 60
                    });
                options.CacheProfiles.Add("Never",
                    new CacheProfile()
                    {
                        Location = ResponseCacheLocation.None,
                        NoStore = true
                    });
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddMemoryCache();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (_env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error/404");
                app.UseHsts();
            }
            // add Ip Filter            

            if (SioService.GetConfig<bool>("IsHttps"))
            {
                app.UseHttpsRedirection();
            }
            app.UseCors(opt =>
            {
                opt.AllowAnyOrigin();
                opt.AllowAnyHeader();
                opt.AllowAnyMethod();
            });

            var cachePeriod = _env.IsDevelopment() ? "600" : "604800";
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    // Requires the following import:
                    // using Microsoft.AspNetCore.Http;
                    ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age={cachePeriod}");
                }
            });
            app.UseCookiePolicy();
            app.UseSignalR(route =>
            {
                route.MapHub<PortalHub>("/portalhub");
                route.MapHub<SioChatHub>("/SioChatHub");
            });

            app.UseAuthentication();

            ConfigRoutes(app);
        }
    }
}
