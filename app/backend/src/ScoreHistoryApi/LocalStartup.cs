using System;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Serialization.SystemTextJson.Converters;
using Amazon.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.JsonConverters;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.ScoreItems;
using ScoreHistoryApi.Logics.Scores;

namespace ScoreHistoryApi
{
    public class LocalStartup
    {
        private static readonly string CorsPolicyName = "UraKataCorsPolicy";

        public LocalStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IScoreQuota>(x => new ScoreQuota());
            services.AddSingleton(x =>
                new DynamoDbClientFactory()
                    .SetEndpointUrl(new Uri(Configuration[EnvironmentNames.ScoreDynamoDbEndpointUrl]))
                    .Create());
            services.AddSingleton(x =>
                new S3ClientFactory()
                    .SetEndpointUrl(new Uri(Configuration[EnvironmentNames.ScoreS3EndpointUrl]))
                    .SetCredentials(Configuration[EnvironmentNames.ScoreS3AccessKey],Configuration[EnvironmentNames.ScoreS3SecretKey])
                    .SetUseMinio(true)
                    .Create());
            services.AddScoped<ScoreLogics>();
            services.AddScoped<ScoreItemLogics>();

            services.AddControllers()
                .AddJsonOptions(option =>
                {
                    option.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
                    option.JsonSerializerOptions.DictionaryKeyPolicy = default;
                    option.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    option.JsonSerializerOptions.AllowTrailingCommas = true;
                    option.JsonSerializerOptions.Converters.Add(new ScoreAccessesJsonConverter());
                    option.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                });

            services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("dev",new OpenApiInfo()
                {
                    Version = "dev"
                });
            });
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

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(CorsPolicyName);

            app.Use(async (context, next) =>
            {
                context.User.AddIdentity(new ClaimsIdentity(new[]
                {
                    new Claim("sub", Configuration[EnvironmentNames.DevelopmentSub]),
                    new Claim("principalId", Configuration[EnvironmentNames.DevelopmentPrincipalId]),
                    new Claim("cognito:username", Configuration[EnvironmentNames.DevelopmentCognitoUsername]),
                    new Claim("email", Configuration[EnvironmentNames.DevelopmentEmail]),
                }));
                await next.Invoke();
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/dev/swagger.json", "Ura-Kata Score History API");
            });
        }
    }
}
