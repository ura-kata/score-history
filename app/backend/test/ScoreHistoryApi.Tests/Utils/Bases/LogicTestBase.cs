using System;
using System.Collections.Generic;
using System.Text;
using Amazon.DynamoDBv2;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.Scores;

namespace ScoreHistoryApi.Tests.Utils.Bases
{
    public abstract class LogicTestBase<TLogic> where TLogic : class
    {
        protected LogicTestBase()
        {
            Services = new ServiceCollection();

            AmazonDynamoDb = new Mock<IAmazonDynamoDB>();
            AmazonS3 = new Mock<IAmazonS3>();
            ScoreQuota = new ScoreQuota();
            Configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            ScoreCommonLogic = new Mock<IScoreCommonLogic>();


            Services.AddScoped<TLogic>();
            Services.AddScoped(_ => AmazonDynamoDb.Object);
            Services.AddScoped(_ => AmazonS3.Object);
            Services.AddScoped<IScoreQuota>(_ => ScoreQuota);
            Services.AddScoped<IConfiguration>(_ => Configuration);
            Services.AddScoped(_ => ScoreCommonLogic.Object);
        }

        public IConfigurationRoot Configuration { get; set; }

        public Mock<IAmazonS3> AmazonS3 { get; set; }

        public Mock<IAmazonDynamoDB> AmazonDynamoDb { get; set; }
        public IScoreQuota ScoreQuota { get; set; }

        public ServiceCollection Services { get; set; }
        public Mock<IScoreCommonLogic> ScoreCommonLogic { get; set; }

        public TLogic GetLogic()
        {
            using var provider = Services.BuildServiceProvider();
            return provider.GetRequiredService<TLogic>();
        }
    }
}
