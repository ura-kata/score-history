using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.Scores;
using ScoreHistoryApi.Tests.WithDocker.Utils;
using ScoreHistoryApi.Tests.WithDocker.Utils.Extensions;
using ScoreHistoryApi.Tests.WithDocker.Utils.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace ScoreHistoryApi.Tests.WithDocker.Logics.Score.InitializerTests
{
    [Collection(DynamoDbDockerCollection.CollectionName)]
    public class InitializeTests
    {
        private readonly DynamoDbDockerFixture _fixture;
        private readonly ITestOutputHelper _helper;
        private readonly string _tableName;
        private readonly ServiceCollection _services;
        private readonly IAmazonDynamoDB _amazonDynamoDb;
        private readonly IConfigurationRoot _configuration;
        private readonly ScoreCommonLogicMock _commonLogic;

        // 定数
        private Guid _ownerId = Guid.Parse("eb184d71-3b6e-4619-a1f6-1ddb41de72f0");
        

        public InitializeTests(DynamoDbDockerFixture fixture, ITestOutputHelper helper)
        {
            _fixture = fixture;
            _helper = helper;

            _tableName = "Initializer";

            _configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            _amazonDynamoDb = new DynamoDbClientFactory().SetEndpointUrl(_fixture.Endpoint).Create();
            _commonLogic = new ScoreCommonLogicMock();


            _services = new ServiceCollection();

            _services.AddScoped<Initializer>();
            _services.AddScoped(_ => _amazonDynamoDb);
            _services.AddScoped<IConfiguration>(_ => _configuration);
            _services.AddScoped<IScoreQuota, ScoreQuota>();
            _services.AddScoped(_=> _commonLogic.Object);

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

            var logic = provider.GetRequiredService<Initializer>();

            // 実行
            await logic.Initialize(_ownerId);

            // 検証
            var common = provider.GetRequiredService<IScoreCommonLogic>();

            {
                // 楽譜データ
                var o = "sc:" + common.ConvertIdFromGuid(_ownerId);

                var request = new QueryRequest(tableName)
                    .SetNamesAndValue(new { o = o })
                    .SetKeyConditionExpression("#o = :o");

                var token = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await _amazonDynamoDb.QueryAsync(request, token.Token);

                Assert.Single(response.Items);
                Assert.Equal(o, response.Items[0]["o"].S);
                Assert.Equal("summary", response.Items[0]["s"].S);
                Assert.Equal("0", response.Items[0]["sc"].N);
                Assert.Equal("AAAAAAAAAAAAAAAAAAAAAA", response.Items[0]["l"].S);
            }

            {
                // 楽譜アイテムデータ
                var o = "si:" + common.ConvertIdFromGuid(_ownerId);

                var request = new QueryRequest(tableName)
                    .SetNamesAndValue(new { o = o })
                    .SetKeyConditionExpression("#o = :o");

                var token = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await _amazonDynamoDb.QueryAsync(request, token.Token);

                Assert.Single(response.Items);
                Assert.Equal(o, response.Items[0]["o"].S);
                Assert.Equal("summary", response.Items[0]["s"].S);
                Assert.Equal("0", response.Items[0]["c"].N);
                Assert.Equal("0", response.Items[0]["t"].N);
                Assert.Equal("AAAAAAAAAAAAAAAAAAAAAA", response.Items[0]["l"].S);
            }
        }
    }
}
