using System;
using Amazon.DynamoDBv2;
using ScoreHistoryApi.Factories;

namespace ScoreHistoryApi.Tests.WithFake.Utils.Factories
{
    public class TestDefaultDynamoDbClientFactory
    {
        public const string Endpoint = "http://localhost:18000";
        public IAmazonDynamoDB Build() =>
            new DynamoDbClientFactory().SetEndpointUrl(new Uri(Endpoint)).Create();
    }
}
