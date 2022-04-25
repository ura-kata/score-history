using System;
using Amazon.S3;
using ScoreHistoryApi.Factories;

namespace ScoreHistoryApi.Tests.WithFake.Utils.Factories
{
    public class TestDefaultS3ClientFactory
    {
        public const string Endpoint = "http://localhost:19000";
        public IAmazonS3 Build() =>
            new S3ClientFactory().SetEndpointUrl(new Uri(Endpoint)).Create();
    }
}
