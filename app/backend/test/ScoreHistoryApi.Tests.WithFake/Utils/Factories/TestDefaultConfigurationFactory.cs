using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace ScoreHistoryApi.Tests.WithFake.Utils.Factories
{
    public class TestDefaultConfigurationFactory
    {
        public const string ScoreDynamoDbTableName = "ura-kata-score-history";
        public const string ScoreBucketName = "ura-kata-score-history-bucket";

        public IConfigurationRoot Build() => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>()
            {
                [EnvironmentNames.ScoreDynamoDbTableName] = ScoreDynamoDbTableName,
                [EnvironmentNames.ScoreLargeDataDynamoDbTableName] = ScoreDynamoDbTableName,
                [EnvironmentNames.ScoreItemDynamoDbTableName] = ScoreDynamoDbTableName,
                [EnvironmentNames.ScoreItemRelationDynamoDbTableName] = ScoreDynamoDbTableName,
                [EnvironmentNames.ScoreItemS3Bucket] = ScoreBucketName,
                [EnvironmentNames.ScoreDataSnapshotS3Bucket] = ScoreBucketName,
            })
            .Build();
    }
}
