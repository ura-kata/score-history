using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;

namespace ScoreHistoryApi.AWSWrapper.Tests
{
    public class TestDynamoDb
    {
        private readonly AmazonDynamoDBClient _client;
        public string TableName { get; }

        public TestDynamoDb(string tableName, string regionSystemName)
        {
            TableName = tableName;

            var region = RegionEndpoint.GetBySystemName(regionSystemName);

            var config = new AmazonDynamoDBConfig()
            {
                RegionEndpoint = region
            };

            _client = new AmazonDynamoDBClient(config);
        }
        public TestDynamoDb(string tableName, Uri endpointUrl)
        {
            TableName = tableName;

            var config = new AmazonDynamoDBConfig
            {
                ServiceURL = endpointUrl.ToString(),
                UseHttp = true
            };


            _client = new AmazonDynamoDBClient(config);
        }

        public async Task Test()
        {
            var table = Table.LoadTable(_client, TableName);
            var document = await table.GetItemAsync(new Primitive("aaaa"));
            var a = document["array"].AsArrayOfString();
            PutItemOperationConfig config = new PutItemOperationConfig()
            {

            };

        }

        public async Task Test2()
        {
            var actions = new List<TransactWriteItem>()
            {
                new TransactWriteItem()
                {
                    Put = new Put()
                    {
                        Item = new Dictionary<string, AttributeValue>()
                        {
                            ["id"] = new AttributeValue("bbbb"),
                            ["str"] = new AttributeValue("data"),
                            ["obj"] = new AttributeValue()
                            {
                                M = new Dictionary<string, AttributeValue>()
                                {
                                    ["a"] = new AttributeValue(){N = "789"},
                                    ["b"] = new AttributeValue(){S = "123"}
                                }
                            },
                        },
                        TableName = TableName
                    }
                }
            };

            var request = new TransactWriteItemsRequest()
            {
                TransactItems = actions,
                ReturnConsumedCapacity = ReturnConsumedCapacity.TOTAL
            };

            try
            {
                var response = await _client.TransactWriteItemsAsync(request);
                Console.WriteLine(response.HttpStatusCode);
            }
            catch (ResourceNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            catch (InternalServerErrorException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            catch (TransactionCanceledException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task UpdateTest()
        {
            var actions = new List<TransactWriteItem>()
            {
                new TransactWriteItem()
                {
                    Put = new Put()
                    {
                        Item = new Dictionary<string, AttributeValue>()
                        {
                            ["id"] = new AttributeValue("cccc"),
                            ["str"] = new AttributeValue("data"),
                            ["obj"] = new AttributeValue()
                            {
                                M = new Dictionary<string, AttributeValue>()
                                {
                                    ["a"] = new AttributeValue(){N = "789"},
                                    ["b"] = new AttributeValue(){S = "123"}
                                }
                            },
                            ["array"] = new AttributeValue()
                            {
                                SS = new List<string>(){"a","b","c"}
                            }
                        },
                        TableName = TableName
                    }
                },
                new TransactWriteItem()
                {
                    Update = new Update()
                    {
                        Key = new Dictionary<string, AttributeValue>()
                        {
                            ["id"] = new AttributeValue("cccc"),
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                        {
                            [":obj"] = new AttributeValue()
                            {
                                M = new Dictionary<string, AttributeValue>()
                                {
                                    ["a"] = new AttributeValue(){N = "78900"},
                                    ["b"] = new AttributeValue(){S = "12300"}
                                }
                            },
                        },
                        UpdateExpression = "SET obj = :obj",
                        TableName = TableName,
                    }
                },
                new TransactWriteItem()
                {
                    Update = new Update()
                    {
                        Key = new Dictionary<string, AttributeValue>()
                        {
                            ["id"] = new AttributeValue("cccc"),
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                        {
                            [":array_item"] = new AttributeValue()
                            {
                                SS = new List<string>(){ "c","d"}
                            },
                        },
                        ExpressionAttributeNames = new Dictionary<string, string>()
                        {
                            ["#array"] = "array",
                        },
                        UpdateExpression = "ADD #array :array_item",
                        TableName = TableName,
                    }
                }
            };

            try
            {
                foreach (var transactWriteItem in actions)
                {
                    // 1つのアイテムに対してトランザクションを発行することはできない
                    var response = await _client.TransactWriteItemsAsync(new TransactWriteItemsRequest()
                    {
                        TransactItems = new List<TransactWriteItem>(){transactWriteItem},
                        ReturnConsumedCapacity = ReturnConsumedCapacity.TOTAL
                    });
                    Console.WriteLine(response.HttpStatusCode);
                }

            }
            catch (ResourceNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (InternalServerErrorException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (TransactionCanceledException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        public async Task GetTest()
        {
            try
            {
                GetItemRequest request = new GetItemRequest()
                {
                    TableName = TableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        ["id"] = new AttributeValue("cccc")
                    },
                    ProjectionExpression = "#bool, #array",
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#bool"] = "bool",
                        ["#array"] = "array"
                    }
                };
                var response  = await _client.GetItemAsync(request);

            }
            catch (ResourceNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (InternalServerErrorException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (TransactionCanceledException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        public async Task InitializeTransactionTest()
        {
            const string id = "aaaa";
            var actions = new List<TransactWriteItem>()
            {
                // new TransactWriteItem()
                // {
                //     ConditionCheck = new ConditionCheck()
                //     {
                //         Key = new Dictionary<string, AttributeValue>()
                //         {
                //             ["id"] = new AttributeValue(id),
                //             ["sort_id"] = new AttributeValue("summary"),
                //         },
                //         ExpressionAttributeNames = new Dictionary<string, string>()
                //         {
                //             ["#id"] = "id"
                //         },
                //         ConditionExpression = "attribute_not_exists(#id)",
                //         TableName = TableName,
                //     }
                // },
                new TransactWriteItem()
                {
                    Put = new Put()
                    {
                        Item = new Dictionary<string, AttributeValue>()
                        {
                            ["id"] = new AttributeValue(id),
                            ["sort_id"] = new AttributeValue("summary"),
                            ["data"] = new AttributeValue()
                            {
                                M = new Dictionary<string, AttributeValue>()
                                {
                                    ["count"] = new AttributeValue(){N = "0"}
                                }
                            },
                        },
                        TableName = TableName,
                        ExpressionAttributeNames = new Dictionary<string, string>()
                        {
                            ["#id"] = "id"
                        },
                        ConditionExpression = "attribute_not_exists(#id)",
                    },
                },
            };

            try
            {
                var response = await _client.TransactWriteItemsAsync(new TransactWriteItemsRequest()
                {
                    TransactItems = actions,
                    ReturnConsumedCapacity = ReturnConsumedCapacity.TOTAL
                });
                Console.WriteLine(response.HttpStatusCode);
            }
            catch (ResourceNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            catch (InternalServerErrorException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            catch (TransactionCanceledException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task AddTransactionTest()
        {
            const string id = "aaaa";
            const int max = 5;
            var actions = new List<TransactWriteItem>()
            {
                new TransactWriteItem()
                {
                    Update = new Update()
                    {
                        Key = new Dictionary<string, AttributeValue>()
                        {
                            ["id"] = new AttributeValue(id),
                            ["sort_id"] = new AttributeValue("summary"),
                        },
                        ExpressionAttributeNames = new Dictionary<string, string>()
                        {
                            ["#data"] = "data",
                            ["#count"] = "count",
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                        {
                            [":increment"] = new AttributeValue(){N = "1"},
                            [":countMax"] = new AttributeValue()
                            {
                                N = max.ToString()
                            }
                        },
                        ConditionExpression = "#data.#count < :countMax",
                        UpdateExpression = "ADD #data.#count :increment",
                        TableName = TableName,
                    },
                },
                new TransactWriteItem()
                {
                    Put = new Put()
                    {
                        Item = new Dictionary<string, AttributeValue>()
                        {
                            ["id"] = new AttributeValue(id),
                            ["sort_id"] = new AttributeValue(Guid.NewGuid().ToString()),
                            ["data"] = new AttributeValue()
                            {
                                M = new Dictionary<string, AttributeValue>()
                                {
                                    ["text"] = new AttributeValue(){S = Guid.NewGuid().ToString()},
                                }
                            },
                        },
                        TableName = TableName
                    }
                },
            };

            try
            {
                var response = await _client.TransactWriteItemsAsync(new TransactWriteItemsRequest()
                {
                    TransactItems = actions,
                    ReturnConsumedCapacity = ReturnConsumedCapacity.TOTAL
                });
                Console.WriteLine(response.HttpStatusCode);
            }
            catch (ResourceNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            catch (InternalServerErrorException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            catch (TransactionCanceledException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
