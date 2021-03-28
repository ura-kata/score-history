using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PracticeManagerApi
{
    public class Startup
    {
        public const string AppS3BucketKey = "AppS3Bucket";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static IConfiguration Configuration { get; private set; }
        public const string AppUseMinioKey = "AppUseMinio";

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(option =>
                {
                    option.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
                    option.JsonSerializerOptions.AllowTrailingCommas = true;
                });

            // Add S3 to the ASP.NET Core dependency injection framework.
            services.AddAWSService<Amazon.S3.IAmazonS3>();

            services.AddSwaggerGen();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseCors(config=>
            {
                var corsOrigins = Configuration["CorsOrigins"];
                logger.LogDebug("CORS Origins :{origins}", corsOrigins);
                if (string.IsNullOrWhiteSpace(corsOrigins))
                {
                    config.AllowAnyOrigin();
                }
                else
                {
                    config.WithOrigins(corsOrigins.Split(','));
                }

                var corsHeaders = Configuration["CorsHeaders"];
                logger.LogDebug("CORS Headers :{headers}", corsHeaders);
                if (string.IsNullOrWhiteSpace(corsHeaders))
                {
                    config.AllowAnyHeader();
                }
                else
                {
                    config.WithHeaders(corsHeaders.Split(','));
                }

                var corsMethods = Configuration["CorsMethods"];
                logger.LogDebug("CORS Methods :{methods}", corsMethods);
                if (string.IsNullOrWhiteSpace(corsHeaders))
                {
                    config.AllowAnyMethod();
                }
                else
                {
                    config.WithMethods(corsMethods.Split(','));
                }
            });

            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });
        }
    }
}
