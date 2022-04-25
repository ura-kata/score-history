using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Logics.Scores;
using Xunit;

namespace ScoreHistoryApi.Tests.WithFake.Logics.Scores
{
    public class ScoreDescriptionSetterTests
    {

        public const string ScoreTableName = "ura-kata-score-history";
        public const string ScoreBucket = "ura-kata-score-history-bucket";

        [Fact]
        public async Task UpdateDescriptionAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    [EnvironmentNames.ScoreDynamoDbTableName] = ScoreTableName,
                    [EnvironmentNames.ScoreLargeDataDynamoDbTableName] = ScoreTableName,
                    [EnvironmentNames.ScoreItemDynamoDbTableName] = ScoreTableName,
                    [EnvironmentNames.ScoreItemRelationDynamoDbTableName] = ScoreTableName,
                    [EnvironmentNames.ScoreItemS3Bucket] = ScoreBucket,
                    [EnvironmentNames.ScoreDataSnapshotS3Bucket] = ScoreBucket,
                })
                .Build();
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(factory.Create());
            serviceCollection.AddSingleton<ScoreQuota>();
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddScoped<Initializer>();
            serviceCollection.AddScoped<ScoreDeleter>();
            serviceCollection.AddScoped<ScoreDetailGetter>();
            serviceCollection.AddScoped<ScoreCreator>();
            serviceCollection.AddScoped<ScoreDescriptionSetter>();


            await using var provider = serviceCollection.BuildServiceProvider();


            var initializer = provider.GetRequiredService<Initializer>();
            var deleter = provider.GetRequiredService<ScoreDeleter>();
            var getter = provider.GetRequiredService<ScoreDetailGetter>();
            var creator = provider.GetRequiredService<ScoreCreator>();

            var target = provider.GetRequiredService<ScoreDescriptionSetter>();

            var ownerId = Guid.Parse("4984ad41-4b7c-474d-953a-ac7c11081fbd");
            var scoreId = Guid.Parse("0d9fb491-43ae-44a1-9056-55bb25b21187");

            var title = "test score";
            var description = "楽譜の説明";

            try
            {
                await initializer.InitializeScoreAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await deleter.DeleteAsync(ownerId, scoreId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await creator.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newDescription = "新しい楽譜の説明";
            await target.UpdateDescriptionAsync(ownerId, scoreId, newDescription);

            var (scoreData, hashSet) = await getter.GetDynamoDbScoreDataAsync(ownerId, scoreId);

            Assert.IsType<DynamoDbScoreDataV1>(scoreData.Data);

            var dataV1 = (DynamoDbScoreDataV1)scoreData.Data;

            Assert.Equal(newDescription, hashSet[dataV1.DescriptionHash]);

        }
    }
}
