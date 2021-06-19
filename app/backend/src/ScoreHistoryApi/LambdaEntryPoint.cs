using System;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ScoreHistoryApi
{
    public class LambdaEntryPoint : APIGatewayProxyFunction, IDisposable
    {
        private IHost _webHost = null;

        protected override void Init(IWebHostBuilder builder)
        {
            builder
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddEnvironmentVariables("URA_KATA_");
                })
                .ConfigureLogging((context, loggingBuilder) =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddLambdaLogger(context.Configuration, "Logging");
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                })
                .UseStartup<Startup>();
        }

        protected override void PostCreateHost(IHost webHost)
        {
            _webHost = webHost;
            base.PostCreateHost(webHost);
        }

        public void Dispose()
        {
            _webHost?.Dispose();
            _webHost = null;
        }
    }
}
