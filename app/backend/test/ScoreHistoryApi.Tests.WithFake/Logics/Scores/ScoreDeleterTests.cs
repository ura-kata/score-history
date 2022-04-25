using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.Scores;
using ScoreHistoryApi.Models.Scores;
using ScoreHistoryApi.Tests.WithFake.Utils.Extensions;
using ScoreHistoryApi.Tests.WithFake.Utils.Factories;
using Xunit;
using Xunit.Abstractions;

namespace ScoreHistoryApi.Tests.WithFake.Logics.Scores
{
    public class ScoreDeleterTests
    {
        private readonly ITestOutputHelper _helper;
        private readonly ServiceCollection _services;
        private readonly IConfigurationRoot _configuration;

        private Guid _ownerId = Guid.Parse("36a2264f-9843-4c51-96d4-92c4626571ef");
        private Guid _scoreId = Guid.Parse("ce815421-4538-4b2e-bcb5-4a43f8c01320");

        public ScoreDeleterTests(ITestOutputHelper helper)
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
            _services.AddScoped<ScoreCreator>();
            _services.AddScoped<ScorePageAdder>();
            _services.AddScoped<ScoreAnnotationAdder>();
            _services.AddScoped<ScoreSnapshotCreator>();

            _services.AddScoped<ScoreDeleter>();

            _helper = null;

        }

        [Fact]
        public async Task DeleteAsyncTest()
        {
            // 前処理

            var title = "test score";
            var description = "楽譜の説明(楽譜削除)";

            await _services.InvokeIgnoreErrorAsync<Initializer>(init => init.InitializeScoreAsync(_ownerId));
            await _services.InvokeIgnoreErrorAsync<ScoreCreator>(creator =>
                creator.CreateAsync(_ownerId, _scoreId, title, description));

            var newAnnotations = new List<NewScoreAnnotation>()
            {
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
            };

            await _services.InvokeIgnoreErrorAsync<ScoreAnnotationAdder>(add =>
                add.AddAnnotationsInnerAsync(_ownerId, _scoreId, newAnnotations));


            var newPages = new List<NewScorePage>()
            {
                new NewScorePage()
                {
                    Page = "1",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "2",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "3",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "4",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "5",
                    ItemId = Guid.NewGuid(),
                }
            };


            await _services.InvokeIgnoreErrorAsync<ScorePageAdder>(add =>
                add.AddPagesAsync(_ownerId, _scoreId, newPages));
            

            var snapshotNames = new string[]
            {
                "スナップショット名1",
                "スナップショット名2",
                "スナップショット名3",
                "スナップショット名4",
            }.OrderBy(x => x).ToArray();

            await _services.InvokeIgnoreErrorAsync<ScoreSnapshotCreator>(async creator =>
            {
                foreach (var snapshotName in snapshotNames)
                {
                    await creator.CreateSnapshotAsync(_ownerId, _scoreId, snapshotName);
                }
            });

            await using var provider = _services.BuildServiceProvider();

            var deleter = provider.GetRequiredService<ScoreDeleter>();


            // 実行

            await deleter.DeleteAsync(_ownerId, _scoreId);
        }
    }
}
