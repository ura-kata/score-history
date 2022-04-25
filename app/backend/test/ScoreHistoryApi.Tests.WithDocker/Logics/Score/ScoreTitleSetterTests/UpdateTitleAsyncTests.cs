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
using ScoreHistoryApi.Models.Scores;
using ScoreHistoryApi.Tests.WithDocker.Utils;
using ScoreHistoryApi.Tests.WithDocker.Utils.Extensions;
using ScoreHistoryApi.Tests.WithDocker.Utils.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace ScoreHistoryApi.Tests.WithDocker.Logics.Score.ScoreTitleSetterTests
{
    [Collection(DynamoDbDockerCollection.CollectionName)]
    public class UpdateTitleAsyncTests
    {
        private readonly DynamoDbDockerFixture _fixture;
        private readonly ITestOutputHelper _helper;
        private readonly string _tableName;
        private readonly IConfigurationRoot _configuration;
        private readonly IAmazonDynamoDB _amazonDynamoDb;
        private readonly ScoreCommonLogicMock _commonLogic;
        private readonly ServiceCollection _services;

        // 定数
        private Guid _ownerId = Guid.Parse("eb184d71-3b6e-4619-a1f6-1ddb41de72f0");

        public UpdateTitleAsyncTests(DynamoDbDockerFixture fixture, ITestOutputHelper helper)
        {
            _fixture = fixture;
            _helper = helper;


            _tableName = this.GetType().FullName.Replace(".", "_");

            _configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            _amazonDynamoDb = new DynamoDbClientFactory().SetEndpointUrl(_fixture.Endpoint).Create();
            _commonLogic = new ScoreCommonLogicMock();


            _services = new ServiceCollection();

            _services.AddScoped<ScoreTitleSetter>();

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

            var scores = new (Guid id, NewScore score)[]
            {
                (ScoreCommonLogicMock.ConvertTo(1), new(){Title = "test1", Description = "テスト1"}),
            };
            var scoreId = scores[0].id;
            var newTitle = "update_title";


            {
                var initializer = provider.GetRequiredService<Initializer>();
                var creator = provider.GetRequiredService<ScoreCreator>();

                await initializer.Initialize(_ownerId);

                foreach (var score in scores)
                {
                    await creator.CreateAsync(_ownerId, score.id, score.score.Title, score.score.Description);
                }
            }
            var logic = provider.GetRequiredService<ScoreTitleSetter>();

            // 実行
            await logic.UpdateTitleAsync(_ownerId, scoreId, newTitle);

            // 検証
            var common = provider.GetRequiredService<IScoreCommonLogic>();

            {
                // 楽譜データ
                var o = "sc:" + common.ConvertIdFromGuid(_ownerId);
                var s = common.ConvertIdFromGuid(scoreId);

                var request = new QueryRequest(tableName)
                    .SetNamesAndValue(new { o = o, s= s })
                    .SetKeyConditionExpression("#o = :o AND #s = :s");

                var token = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await _amazonDynamoDb.QueryAsync(request, token.Token);

                Assert.Single(response.Items);
                Assert.Equal(newTitle, response.Items[0]["d"].M["t"].S);
            }
        }

    }
}
