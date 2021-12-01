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

namespace ScoreHistoryApi.Tests.WithDocker.Logics.Score.ScoreCreatorTests
{
    [Collection(DynamoDbDockerCollection.CollectionName)]
    public class CreateAsyncTests
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

        public CreateAsyncTests(DynamoDbDockerFixture fixture, ITestOutputHelper helper)
        {
            _fixture = fixture;
            _helper = helper;

            _tableName = this.GetType().FullName.Replace(".", "_");

            _configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            _amazonDynamoDb = new DynamoDbClientFactory().SetEndpointUrl(_fixture.Endpoint).Create();
            _commonLogic = new ScoreCommonLogicMock();


            _services = new ServiceCollection();

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

            var logic = provider.GetRequiredService<ScoreCreator>();

            var score = new NewScore()
            {
                Title = "test",
                Description = "テスト"
            };

            // 実行
            await logic.CreateAsync(_ownerId, score);

            // 検証
            var common = provider.GetRequiredService<IScoreCommonLogic>();

            {
                // 楽譜データ
                var o = "sc:" + common.ConvertIdFromGuid(_ownerId);
                var s = common.ConvertIdFromGuid(_commonLogic.DefaultNewGuid);

                var request = new QueryRequest(tableName)
                    .SetNamesAndValue(new { o = o, s = s })
                    .SetKeyConditionExpression("#o = :o and #s = :s");

                var token = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await _amazonDynamoDb.QueryAsync(request, token.Token);

                Assert.Single(response.Items);
                var item = response.Items[0];
                Assert.Equal(o, item["o"].S);
                Assert.Equal(s, item["s"].S);
                Assert.Equal("pr", item["as"].S);

                var data = item["d"].M;
                Assert.True(data["p"].IsLSet);
                Assert.Empty(data["p"].L);
                Assert.True(data["a"].IsLSet);
                Assert.Empty(data["a"].L);
                Assert.Equal("0", data["ac"].N);
                Assert.Equal("0", data["pc"].N);
                Assert.Equal("test", data["t"].S);
                Assert.Equal("テスト", data["d"].S);
            }

            {
                // 楽譜サマリ
                var o = "sc:" + common.ConvertIdFromGuid(_ownerId);
                var s = "summary";

                var request = new QueryRequest(tableName)
                    .SetNamesAndValue(new { o = o, s = s })
                    .SetKeyConditionExpression("#o = :o and #s = :s");

                var token = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await _amazonDynamoDb.QueryAsync(request, token.Token);

                Assert.Single(response.Items);
                var item = response.Items[0];
                Assert.Equal("1", item["sc"].N);
            }

        }
    }
}
