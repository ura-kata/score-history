using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.JsonConverters;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.Scores;

namespace ScoreHistoryApi
{
    public class Startup
    {
        private static readonly string CorsPolicyName = "UraKataCorsPolicy";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static IConfiguration Configuration { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IScoreQuota>(x => new ScoreQuota());
            services.AddSingleton(x =>
                new DynamoDbClientFactory()
                    .SetRegionSystemName(Configuration[EnvironmentNames.ScoreDynamoDbRegionSystemName]).Create());
            services.AddSingleton(x =>
                new S3ClientFactory().SetRegionSystemName(Configuration[EnvironmentNames.ScoreS3RegionSystemName])
                    .Create());
            services.AddScoped<ScoreLogics>();

            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyName, builder =>
                {
                    var corsOrigins = Configuration[EnvironmentNames.CorsOrigins];
                    if (string.IsNullOrWhiteSpace(corsOrigins))
                    {
                        builder.AllowAnyOrigin();
                    }
                    else
                    {
                        builder.WithOrigins(corsOrigins.Split(','));
                    }

                    var corsHeaders = Configuration[EnvironmentNames.CorsHeaders];
                    if (string.IsNullOrWhiteSpace(corsHeaders))
                    {
                        builder.AllowAnyHeader();
                    }
                    else
                    {
                        builder.WithHeaders(corsHeaders.Split(','));
                    }

                    var corsMethods = Configuration[EnvironmentNames.CorsMethods];
                    if (string.IsNullOrWhiteSpace(corsHeaders))
                    {
                        builder.AllowAnyMethod();
                    }
                    else
                    {
                        builder.WithMethods(corsMethods.Split(','));
                    }

                    var corsCredentials = Configuration[EnvironmentNames.CorsCredentials];
                    if (bool.TryParse(corsCredentials, out var allowCredentials) && allowCredentials)
                    {
                        builder.AllowCredentials();
                    }
                });
            });
            services.AddControllers().AddJsonOptions(option =>
            {
                option.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
                option.JsonSerializerOptions.DictionaryKeyPolicy = default;
                option.JsonSerializerOptions.PropertyNamingPolicy = default;
                option.JsonSerializerOptions.AllowTrailingCommas = true;
                option.JsonSerializerOptions.Converters.Add(new ScoreAccessesJsonConverter());
                option.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseCors(CorsPolicyName);

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
