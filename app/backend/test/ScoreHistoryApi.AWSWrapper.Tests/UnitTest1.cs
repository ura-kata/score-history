using System;
using System.Threading.Tasks;
using Xunit;

namespace ScoreHistoryApi.AWSWrapper.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            var target = new TestDynamoDb("test", new Uri("http://localhost:18000"));

            await target.Test();

        }
        [Fact]
        public async Task Test2()
        {
            var target = new TestDynamoDb("test", new Uri("http://localhost:18000"));

            await target.Test2();

        }
        [Fact]
        public async Task UpdateTest()
        {
            var target = new TestDynamoDb("test", new Uri("http://localhost:18000"));

            await target.UpdateTest();

        }


        [Fact]
        public async Task GetTest()
        {
            var target = new TestDynamoDb("test", new Uri("http://localhost:18000"));

            await target.GetTest();

        }

        [Fact]
        public async Task InitializeTransactionTest()
        {
            var target = new TestDynamoDb("add-test", new Uri("http://localhost:18000"));

            await target.InitializeTransactionTest();

        }

        [Fact]
        public async Task AddTransactionTest()
        {
            var target = new TestDynamoDb("add-test", new Uri("http://localhost:18000"));

            await target.AddTransactionTest();

        }
    }
}
