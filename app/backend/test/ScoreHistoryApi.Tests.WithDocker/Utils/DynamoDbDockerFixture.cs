using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Docker.DotNet;
using Docker.DotNet.Models;
using ScoreHistoryApi.Factories;

namespace ScoreHistoryApi.Tests.WithDocker.Utils
{
    public class DynamoDbDockerFixture : IDisposable
    {
        public string ContainerName { get; } = "test_dynamo_db_ce456907";
        public string Port { get; } = "18123";
        public Uri Endpoint => new Uri($"http://localhost:{Port}");

        public DynamoDbDockerFixture()
        {
            Client = new DockerClientConfiguration().CreateClient();

            // docker run -p 18123:8000 -d amazon/dynamodb-local -jar DynamoDBLocal.jar -sharedDb -inMemory

            try
            {

                var task = Client.Containers.CreateContainerAsync(new CreateContainerParameters()
                {
                    Name = ContainerName,
                    Image = "amazon/dynamodb-local",
                    Cmd = new[] { "-jar", "DynamoDBLocal.jar", "-sharedDb", "-inMemory" },
                    HostConfig = new HostConfig()
                    {
                        DNS = new[] { "8.8.8.8", "8.8.4.4" },
                        PortBindings = new Dictionary<string, IList<PortBinding>>()
                        {
                            ["8000/tcp"] = new[] { new PortBinding() { HostPort = Port } }
                        },
                        AutoRemove = true,
                    },

                });

                task.Wait();

            }
            catch (AggregateException ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.InnerException is DockerApiException dex)
                {
                    if (dex.StatusCode != HttpStatusCode.Conflict)
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            var startTask = Client.Containers.StartContainerAsync(ContainerName, new ContainerStartParameters());

            startTask.Wait();

            var startResult = startTask.Result;
            if (startResult)
            {
                Console.WriteLine($"start '{ContainerName}'");
            }
        }

        public DockerClient Client { get; }
        private bool _disposed = false;

        public void Dispose()
        {
            if(_disposed)return;
            _disposed = true;


            try
            {
                var stopTask = Client.Containers.StopContainerAsync(ContainerName, new ContainerStopParameters() { });
                stopTask.Wait();
                var stopResult = stopTask.Result;
                if (stopResult)
                {
                    Console.WriteLine($"stopped '{ContainerName}'");
                }
            }
            catch (AggregateException ex)
            {
                Console.WriteLine(ex.ToString());
                if (ex.InnerException is DockerContainerNotFoundException)
                {
                    
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            Client.Dispose();
        }

        public async Task CreateTableAsync(string tableName)
        {
            var endpoint = Endpoint;

            using var client = new DynamoDbClientFactory().SetEndpointUrl(endpoint).Create();

            await client.CreateTableAsync(new CreateTableRequest()
            {
                TableName = tableName,
                AttributeDefinitions = new List<AttributeDefinition>()
                {
                    new()
                    {
                        AttributeName = "o",
                        AttributeType = ScalarAttributeType.S
                    },
                    new()
                    {
                        AttributeName = "s",
                        AttributeType = ScalarAttributeType.S
                    }
                },
                KeySchema = new List<KeySchemaElement>()
                {
                    new()
                    {
                        AttributeName = "o",
                        KeyType = KeyType.HASH
                    },
                    new()
                    {
                        AttributeName = "s",
                        KeyType = KeyType.RANGE
                    }
                },
                ProvisionedThroughput = new ProvisionedThroughput()
                {
                    ReadCapacityUnits = 1,
                    WriteCapacityUnits = 1
                }
            });
        }
    }
}
