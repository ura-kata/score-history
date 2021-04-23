using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using ScoreHistoryApi.Models.Versions;
using Xunit;

namespace ScoreHistoryApi.Tests
{
    public class VersionControllerTests
    {
        [Fact]
        public async Task TestSuccessGetVersion()
        {

            using var lambdaFunction = new LambdaEntryPoint();
            var requestText = await File.ReadAllTextAsync("./Requests/VersionRequests/VersionController-Get.json");
            var request = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(requestText);
            var context = new TestLambdaContext();
            var version = "dev";

            Startup.Configuration[EnvironmentNames.ApiVersion] = version;

            var response = await lambdaFunction.FunctionHandlerAsync(request, context);

            Assert.Equal(200, response.StatusCode);

            var json = JsonConvert.DeserializeObject<ApiVersion>(response.Body);


            Assert.Equal(version, json.Version);
        }
    }
}
