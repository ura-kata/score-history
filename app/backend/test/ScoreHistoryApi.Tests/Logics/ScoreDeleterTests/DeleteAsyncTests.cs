using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.Internal;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.Scores;
using ScoreHistoryApi.Tests.Utils.Bases;
using Xunit;

namespace ScoreHistoryApi.Tests.Logics.ScoreDeleterTests
{
    public class DeleteAsyncTests: LogicTestBase<ScoreDeleter>
    {
        private Guid _ownerId = Guid.Parse("36a2264f-9843-4c51-96d4-92c4626571ef");
        private Guid _scoreId = Guid.Parse("ce815421-4538-4b2e-bcb5-4a43f8c01320");

        public DeleteAsyncTests()
        {

            // 共通前処理

        }
        
        [Fact()]
        public async Task Success()
        {
            // 前準備

            //AmazonDynamoDb.a

            // 実行
            var logic = GetLogic();
            await logic.DeleteAsync(_ownerId, _scoreId);

            // 検証
        }
    }

    public class DeleteMainAsyncTests : LogicTestBase<ScoreDeleter>
    {
        private Guid _ownerId = Guid.Parse("36a2264f-9843-4c51-96d4-92c4626571ef");
        private Guid _scoreId = Guid.Parse("ce815421-4538-4b2e-bcb5-4a43f8c01320");
        private string _tableName = "testTable";
        private string _bucketName = "testBucket";

        public DeleteMainAsyncTests()
        {

            // 共通前処理

            Configuration[EnvironmentNames.ScoreDynamoDbTableName] = _tableName;
            Configuration[EnvironmentNames.ScoreItemS3Bucket] = _bucketName;

            ScoreCommonLogic.Setup(x => x.ConvertIdFromGuid(_ownerId)).Returns("01234567890123456789aa");
            ScoreCommonLogic.Setup(x => x.ConvertIdFromGuid(_scoreId)).Returns("01234567890123456789bb");
            var lockId = new Guid("ce456907-badb-106d-1723-ddcfba5902ad");
            ScoreCommonLogic.Setup(x => x.NewGuid()).Returns(lockId);
            ScoreCommonLogic.Setup(x => x.ConvertIdFromGuid(lockId)).Returns("01234567890123456789cc");
        }

        [Fact()]
        public async Task Success()
        {
            // 前準備
            
            // 実行
            var logic = GetLogic();
            await logic.DeleteMainAsync(_ownerId, _scoreId);

            // 検証

            var req = new TransactWriteItemsRequest()
            {
                ReturnConsumedCapacity = ReturnConsumedCapacity.TOTAL,
                TransactItems = new List<TransactWriteItem>()
                {
                    new TransactWriteItem()
                    {
                        Delete = new Delete()
                        {
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                ["o"] = new AttributeValue("sc:01234567890123456789aa"),
                                ["s"] = new AttributeValue("01234567890123456789bb")
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#score"] = "s"
                            },
                            ConditionExpression = "attribute_exists(#score)",
                            TableName = _tableName
                        }
                    },
                    new TransactWriteItem()
                    {
                        Update = new Update()
                        {
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                ["o"] = new AttributeValue("sc:01234567890123456789aa"),
                                ["s"] = new AttributeValue("summary")
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#sc"] = "sc",
                                ["#lock"] = "l"
                            },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                            {
                                [":increment"] = new AttributeValue(){N = "-1"},
                                [":newL"] = new AttributeValue("01234567890123456789cc"),
                            },
                            TableName = _tableName
                        }
                    }
                }
            };
            var compare = new LambdaEqualityCompare<TransactWriteItemsRequest>((x, y) =>
            {
                if (x.ReturnConsumedCapacity != y.ReturnConsumedCapacity) return false;
                throw new NotImplementedException();
                return true;
            });
            AmazonDynamoDb.Verify(x => x.TransactWriteItemsAsync(It.Is(req, compare), default), Times.Once);
        }
    }

    public static class AmazonDynamoDbMock
    {
        public static void A(this Mock<IAmazonDynamoDB> mock)
        {

        }
    }

    public static class ItEx
    {
        public static T Is<T>(T value, Func<T, T, bool> lambda)
        {
            var compare = new LambdaEqualityCompare<T>(lambda);
            return It.Is(value, compare);
        }
    }

    public class LambdaEqualityCompare<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _lambda;

        public LambdaEqualityCompare(Func<T, T, bool> lambda)
        {
            _lambda = lambda ?? throw new ArgumentNullException(nameof(lambda));
        }
        public bool Equals(T x, T y)
        {
            return _lambda(x, y);
        }

        public int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }
    }
}
