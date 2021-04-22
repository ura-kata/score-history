using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Newtonsoft.Json;
using ScoreHistoryApi.Models.Users;
using ScoreHistoryApi.Models.Versions;
using Xunit;

namespace ScoreHistoryApi.Tests
{
    public class UserControllerTests
    {
        [Fact]
        public async Task TestSuccessGetUser()
        {
            var lambdaFunction = new LambdaEntryPoint();
            var requestText = await File.ReadAllTextAsync("./Requests/UserRequests/UserController-Get.json");
            var request = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(requestText);
            var context = new TestLambdaContext();
            
            
            var response = await lambdaFunction.FunctionHandlerAsync(request, context);

            Assert.Equal(200, response.StatusCode);

            var user = JsonConvert.DeserializeObject<User>(response.Body);


            Assert.Equal(new Guid("00000000-0000-0000-0000-000000000000"), user.Id);
            Assert.Equal("test-user@example.com", user.Email);
            Assert.Equal("test-user", user.Username);
        }
    }
}
