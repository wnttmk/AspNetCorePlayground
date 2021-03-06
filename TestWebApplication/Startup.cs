﻿using System.Data.SqlClient;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TestWebApplication.Conventions;
using TestWebApplication.Event;
using WeihanLi.AspNetCore.Authentication;
using WeihanLi.AspNetCore.Authentication.HeaderAuthentication;
using WeihanLi.AspNetCore.Authentication.QueryAuthentication;
using WeihanLi.Extensions;
using WeihanLi.Web.Extensions;

namespace TestWebApplication
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(HeaderAuthenticationDefaults.AuthenticationSchema)
                .AddHeader(HeaderAuthenticationDefaults.AuthenticationSchema, options => { options.AdditionalHeaderToClaims.Add("UserEmail", ClaimTypes.Email); })
                .AddQuery(QueryAuthenticationDefaults.AuthenticationSchema, options => { options.AdditionalQueryToClaims.Add("UserEmail", ClaimTypes.Email); })
                ;

            var anonymousPolicyName = "anonymous";

            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy(anonymousPolicyName, builder => builder.RequireAssertion(context => context.User.Identity.IsAuthenticated));

            //    options.DefaultPolicy = new AuthorizationPolicyBuilder(HeaderAuthenticationDefaults.AuthenticationSchema)
            //        .RequireAuthenticatedUser()
            //        .RequireAssertion(context => context.User.GetUserId<int>() > 0)
            //        .Build();
            //});

            services.AddMvc(options =>
                {
                    options.Conventions.Add(new ApiControllerVersionConvention());
                })
                // .AddAnonymousPolicyTransformer(anonymousPolicyName)
                .AddViewLocalization(options => options.ResourcesPath = "Resources")
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = ApiVersion.Default;
            });

            services.AddEvent();
            services.AddSingleton<PageViewEventHandler>();

            services.AddLocalization(options => { options.ResourcesPath = "Resources"; });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IEventSubscriptionManager eventSubscriptionManager)
        {
            eventSubscriptionManager.Subscribe<PageViewEvent, PageViewEventHandler>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHealthCheck("/health", serviceProvider =>
                {
                    // 检查数据库访问是否正常
                    var configuration = serviceProvider.GetService<IConfiguration>();
                    var connString = configuration.GetConnectionString("Configurations");
                    using (var conn = new SqlConnection(connString))
                    {
                        conn.EnsureOpen();
                    }
                    return true;
                });

            //app.Use(async (context, next) =>
            //{
            //    var eventPublisher = context.RequestServices.GetRequiredService<IEventPublisher>();
            //    await eventPublisher.Publish("pageView", new PageViewEvent() { Path = context.Request.Path.Value });
            //    await next();
            //});

            var supportedCultures = new[]
            {
                new CultureInfo("zh"),
                new CultureInfo("en"),
            };
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("zh"),
                // Formatting numbers, dates, etc.
                SupportedCultures = supportedCultures,
                // UI strings that we have localized.
                SupportedUICultures = supportedCultures
            });

            app.UseAuthentication();
            app.UseMvcWithDefaultRoute();
        }
    }
}
