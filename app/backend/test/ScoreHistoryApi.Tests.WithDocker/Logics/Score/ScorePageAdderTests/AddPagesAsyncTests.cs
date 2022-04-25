using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.Scores;
using ScoreHistoryApi.Models.Scores;
using ScoreHistoryApi.Tests.WithDocker.Utils;
using ScoreHistoryApi.Tests.WithDocker.Utils.Extensions;
using ScoreHistoryApi.Tests.WithDocker.Utils.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace ScoreHistoryApi.Tests.WithDocker.Logics.Score.ScorePageAdderTests
{
    [Collection(DynamoDbDockerCollection.CollectionName)]
    public class AddPagesAsyncTests
    {
        private readonly DynamoDbDockerFixture _fixture;
        private readonly ITestOutputHelper _helper;
        private readonly string _tableName;
        private readonly IConfigurationRoot _configuration;
        private readonly IAmazonDynamoDB _amazonDynamoDb;
        private readonly ScoreCommonLogicMock _commonLogic;
        private readonly ServiceCollection _services;

        // 定数
        private readonly Guid _ownerId = Guid.Parse("eb184d71-3b6e-4619-a1f6-1ddb41de72f0");

        public AddPagesAsyncTests(DynamoDbDockerFixture fixture, ITestOutputHelper helper)
        {
            _fixture = fixture;
            _helper = helper;

            _tableName = this.GetType().FullName.Replace(".", "_");

            _configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            _amazonDynamoDb = new DynamoDbClientFactory().SetEndpointUrl(_fixture.Endpoint).Create();
            _commonLogic = new ScoreCommonLogicMock();


            _services = new ServiceCollection();

            _services.AddScoped<ScorePageAdder>();

            _services.AddScoped<ScoreCreator>();
            _services.AddScoped<Initializer>();
            _services.AddScoped(_ => _amazonDynamoDb);
            _services.AddScoped<IConfiguration>(_ => _configuration);
            _services.AddScoped<IScoreQuota, ScoreQuota>();
            _services.AddScoped(_ => _commonLogic.Object);

            // 共通前処理
            _configuration[EnvironmentNames.ScoreDynamoDbTableName] = _tableName;
        }

        [Fact]
        public async Task Success()
        {
            // 前準備
            var tableName = _tableName;
            await _fixture.CreateTableAsync(tableName);
            await using var provider = _services.BuildServiceProvider();

            var initLogic = provider.GetRequiredService<Initializer>();

            await initLogic.Initialize(_ownerId);

            var creator = provider.GetRequiredService<ScoreCreator>();

            (Guid id, NewScore score) score = (new Guid("d2dca5cb-2640-49e4-87ce-cb47f5ba4072"), new NewScore()
            {
                Title = "test",
                Description = "テスト"
            });

            await creator.CreateAsync(_ownerId, score.id, score.score.Title, score.score.Description);

            var pages = new List<NewScorePage>()
            {
                new()
                {
                    ItemId = new Guid("172e58ec-d002-4fe5-ad2f-89417ecb43a6"),
                    Page = "page01",
                    ObjectName = "object_name01"
                }
            };

            var logic = provider.GetRequiredService<ScorePageAdder>();

            // 実行
            await logic.AddPagesAsync(_ownerId, score.id, pages);

            // 検証
            var common = provider.GetRequiredService<IScoreCommonLogic>();

            {
                var o = "sc:" + common.ConvertIdFromGuid(_ownerId);
                var s = common.ConvertIdFromGuid(score.id);

                var request = new QueryRequest(tableName)
                    .SetNamesAndValue(new { o = o, s = s })
                    .SetKeyConditionExpression("#o = :o and #s = :s");

                var token = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await _amazonDynamoDb.QueryAsync(request, token.Token);

                Assert.Single(response.Items);
                var item = response.Items[0];
                var data = item["d"].M;
                Assert.Equal("1", data["pc"].N);

                var pList = data["p"].L;
                Assert.Single(pList);

                var p = pList[0];
            }

        }
    }
}
