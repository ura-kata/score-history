using Xunit;

namespace ScoreHistoryApi.Tests.WithDocker.Utils
{
    [CollectionDefinition(DynamoDbDockerCollection.CollectionName)]
    public class DynamoDbDockerCollection : ICollectionFixture<DynamoDbDockerFixture>
    {
        public const string CollectionName = "DynamoDB docker collection";
        // no code
        // collection definition
    }
}
