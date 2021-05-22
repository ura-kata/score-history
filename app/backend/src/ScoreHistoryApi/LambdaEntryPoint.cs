using System;
using Amazon.Lambda.AspNetCoreServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ScoreHistoryApi
{
    public class LambdaEntryPoint : APIGatewayProxyFunction, IDisposable
    {
        private IHost _webHost = null;

        protected override void Init(IWebHostBuilder builder)
        {
            builder.UseStartup<Startup>();
        }

        protected override void Init(IHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddEnvironmentVariables("URA_KATA_");
            });
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
