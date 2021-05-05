using System;
using System.Threading.Tasks;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using Xunit;

namespace ScoreHistoryApi.Tests.WithFake.Logics
{
    public class ScoreDatabaseTests
    {
        [Fact]
        public async Task CreateAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var tableName = "ura-kata-score-history";
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), tableName);

            var ownerId = Guid.Parse("f2240c15-0f2d-41ce-941d-6b173bae94c0");
            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var title = "test score";
            var description = "楽譜の説明";

            await target.CreateAsync(ownerId, title, description);

        }
    }
}
