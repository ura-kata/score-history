using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Logics.Scores;
using ScoreHistoryApi.Tests.WithFake.Utils;
using ScoreHistoryApi.Tests.WithFake.Utils.Extensions;
using ScoreHistoryApi.Tests.WithFake.Utils.Factories;
using Xunit;
using Xunit.Abstractions;

namespace ScoreHistoryApi.Tests.WithFake.Logics.Scores
{
    public class ScoreCreatorTests
    {
        private readonly ITestOutputHelper _helper;
        private readonly ServiceCollection _services;
        private readonly IConfigurationRoot _configuration;

        private Guid _ownerId = Guid.Parse("eb184d71-3b6e-4619-a1f6-1ddb41de72f0");
        private Guid _scoreId = Guid.Parse("0d9fb491-43ae-44a1-9056-55bb25b21187");

        public ScoreCreatorTests(ITestOutputHelper helper)
        {
            _helper = helper;
            _configuration = new TestDefaultConfigurationFactory().Build();

            _services = new ServiceCollection();
            _services.AddScoped<IConfiguration>(_ => _configuration);
            _services.AddScoped(_ => new TestDefaultDynamoDbClientFactory().Build());
            _services.AddScoped(_ => new TestDefaultS3ClientFactory().Build());
            _services.AddScoped<IScoreQuota>(_ => new ScoreQuota());
            _services.AddScoped<IScoreCommonLogic, ScoreCommonLogic>();

            _services.AddScoped<Initializer>();
            _services.AddScoped<ScoreDeleter>();
            _services.AddScoped<ScoreDetailGetter>();
            _services.AddScoped<ScoreCreator>();

            _helper = null;

        }

        [Fact]
        public async Task CreateAsyncTest()
        {
            // 前処理

            await _services.InvokeIgnoreErrorAsync<Initializer>(init => init.InitializeScoreAsync(_ownerId), _helper);

            await _services.InvokeIgnoreErrorAsync<ScoreDeleter>(del => del.DeleteScoreAsync(_ownerId, _scoreId), _helper);
            

            var title = "test score";
            var description = "楽譜の説明";

            await using var provider = _services.BuildServiceProvider();

            var target = provider.GetRequiredService<ScoreCreator>();

            // 実行
            await target.CreateAsync(_ownerId, _scoreId, title, description);


            // 検証

            var getter = provider.GetRequiredService<ScoreDetailGetter>();

            var (scoreData, hashSet) = await getter.GetDynamoDbScoreDataAsync(_ownerId, _scoreId);

            Assert.IsType<DynamoDbScoreDataV1>(scoreData.Data);

            var dataV1 = (DynamoDbScoreDataV1)scoreData.Data;

            Assert.Equal(title, dataV1.Title);
            Assert.Equal(description, hashSet[dataV1.DescriptionHash]);
        }
    }
}
