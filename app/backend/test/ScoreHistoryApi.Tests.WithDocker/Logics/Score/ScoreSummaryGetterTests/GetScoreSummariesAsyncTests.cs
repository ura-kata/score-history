using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.Scores;
using ScoreHistoryApi.Models.Scores;
using ScoreHistoryApi.Tests.WithDocker.Utils;
using ScoreHistoryApi.Tests.WithDocker.Utils.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace ScoreHistoryApi.Tests.WithDocker.Logics.Score.ScoreSummaryGetterTests
{

    [Collection(DynamoDbDockerCollection.CollectionName)]
    public class GetScoreSummariesAsyncTests
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

        public GetScoreSummariesAsyncTests(DynamoDbDockerFixture fixture, ITestOutputHelper helper)
        {
            _fixture = fixture;
            _helper = helper;

            _tableName = this.GetType().FullName.Replace(".", "_");

            _configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            _amazonDynamoDb = new DynamoDbClientFactory().SetEndpointUrl(_fixture.Endpoint).Create();
            _commonLogic = new ScoreCommonLogicMock();


            _services = new ServiceCollection();

            _services.AddScoped<ScoreSummaryGetter>();
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
                (ScoreCommonLogicMock.ConvertTo(2), new(){Title = "test2", Description = "テスト2"}),
            };
            

            {
                var initializer = provider.GetRequiredService<Initializer>();
                var creator = provider.GetRequiredService<ScoreCreator>();

                await initializer.Initialize(_ownerId);

                foreach (var score in scores)
                {
                    await creator.CreateAsync(_ownerId, score.id, score.score.Title, score.score.Description);
                }
            }
            var logic = provider.GetRequiredService<ScoreSummaryGetter>();

            // 実行
            var actual = await logic.GetScoreSummariesAsync(_ownerId);

            // 検証
            var common = provider.GetRequiredService<IScoreCommonLogic>();

            Assert.NotEmpty(actual);
            
            foreach (var (exp, act) in scores
                .OrderBy(x=>x.score.Title)
                .Zip(actual.OrderBy(y=>y.Title)))
            {
                Assert.Equal(exp.score.Title, act.Title);
                Assert.Equal(exp.score.Description, act.Description);
                Assert.Equal(exp.id, act.Id);
                Assert.Equal(_ownerId, act.OwnerId);
            }

        }
    }
}
