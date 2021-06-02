using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Models.Scores;
using Xunit;

// TODO 冪等性が担保されるようにテストを修正する
namespace ScoreHistoryApi.Tests.WithFake.Logics
{
    public class ScoreDatabaseTests
    {
        public const string ScoreTableName = "ura-kata-score-history";
        public const string ScoreDataTableName = "ura-kata-score-history";
        public const string ScoreItemRelationDynamoDbTableName = "ura-kata-score-history";

        [Fact]
        public async Task CreateAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), ScoreTableName, ScoreDataTableName,
                ScoreItemRelationDynamoDbTableName);

            var ownerId = Guid.Parse("eb184d71-3b6e-4619-a1f6-1ddb41de72f0");
            var scoreId = Guid.Parse("0d9fb491-43ae-44a1-9056-55bb25b21187");
            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            try
            {
                await target.DeleteAsync(ownerId, scoreId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var title = "test score";
            var description = "楽譜の説明";

            await target.CreateAsync(ownerId, scoreId, title, description);

            var (scoreData, hashSet) = await target.GetDynamoDbScoreDataAsync(ownerId, scoreId);

            Assert.IsType<DynamoDbScoreDataV1>(scoreData.Data);

            var dataV1 = (DynamoDbScoreDataV1) scoreData.Data;

            Assert.Equal(title, dataV1.Title);
            Assert.Equal(description, hashSet[dataV1.DescriptionHash]);
        }

        [Fact]
        public async Task UpdateTitleAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), ScoreTableName, ScoreDataTableName,
                ScoreItemRelationDynamoDbTableName);

            var ownerId = Guid.Parse("27f30e40-bf83-427f-b53d-39b5d0f01ac7");
            var scoreId = Guid.Parse("4afded99-4070-4ba7-85ed-6c3776602895");

            var title = "test score";
            var description = "楽譜の説明";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newTitle = "new test score";
            await target.UpdateTitleAsync(ownerId, scoreId, newTitle);
        }

        [Fact]
        public async Task UpdateDescriptionAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), ScoreTableName, ScoreDataTableName,
                ScoreItemRelationDynamoDbTableName);

            var ownerId = Guid.Parse("4984ad41-4b7c-474d-953a-ac7c11081fbd");
            var scoreId = Guid.Parse("0d9fb491-43ae-44a1-9056-55bb25b21187");

            var title = "test score";
            var description = "楽譜の説明";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.DeleteAsync(ownerId, scoreId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newDescription = "新しい楽譜の説明";
            await target.UpdateDescriptionAsync(ownerId, scoreId, newDescription);

            var (scoreData, hashSet) = await target.GetDynamoDbScoreDataAsync(ownerId, scoreId);

            Assert.IsType<DynamoDbScoreDataV1>(scoreData.Data);

            var dataV1 = (DynamoDbScoreDataV1)scoreData.Data;

            Assert.Equal(newDescription, hashSet[dataV1.DescriptionHash]);

        }

        [Fact]
        public async Task AddPagesAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), ScoreTableName, ScoreDataTableName,
                ScoreItemRelationDynamoDbTableName);

            var ownerId = Guid.Parse("13eff3dd-beb9-4471-adc4-dc4540ee3445");
            var scoreId = Guid.Parse("0d9fb491-43ae-44a1-9056-55bb25b21187");

            var title = "test score";
            var description = "楽譜の説明";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newPages = new List<NewScorePage>()
            {
                new NewScorePage()
                {
                    Page = "1",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "2",
                    ItemId = Guid.NewGuid(),
                }
            };
            await target.AddPagesAsync(ownerId, scoreId, newPages);
        }



        [Fact]
        public async Task RemovePagesAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), ScoreTableName, ScoreDataTableName,
                ScoreItemRelationDynamoDbTableName);

            var ownerId = Guid.Parse("806dcb04-eaba-4233-9069-0beead6cb075");
            var scoreId = Guid.Parse("727679a2-c1eb-4089-9817-9a9bfb7a23b1");

            var title = "test score";
            var description = "楽譜の説明";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newPages = new List<NewScorePage>()
            {
                new NewScorePage()
                {
                    Page = "1",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "2",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "3",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "4",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "5",
                    ItemId = Guid.NewGuid(),
                }
            };

            try
            {
                await target.AddPagesAsync(ownerId, scoreId, newPages);
            }
            catch (Exception)
            {
                // 握りつぶす
            }

            var pageIds = new List<long>()
            {
                1,3
            };
            await target.RemovePagesAsync(ownerId, scoreId, pageIds);

        }


        [Fact]
        public async Task ReplacePagesAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), ScoreTableName, ScoreDataTableName,
                ScoreItemRelationDynamoDbTableName);

            var ownerId = Guid.Parse("f9aa6c43-aab0-485e-a5a7-f6dbf592b0a4");
            var scoreId = Guid.Parse("9db807b1-42fe-4e1a-b85c-61905b176616");

            var title = "test score";
            var description = "楽譜の説明";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newPages = new List<NewScorePage>()
            {
                new NewScorePage()
                {
                    Page = "1",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "2",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "3",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "4",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "5",
                    ItemId = Guid.NewGuid(),
                }
            };

            try
            {
                await target.AddPagesAsync(ownerId, scoreId, newPages);
            }
            catch (Exception)
            {
                // 握りつぶす
            }

            var pages = new List<PatchScorePage>()
            {
                new PatchScorePage()
                {
                    TargetPageId = 1,
                    Page = "replaced " + Guid.NewGuid(),
                    ItemId = Guid.NewGuid()
                },
                new PatchScorePage()
                {
                    TargetPageId = 3,
                    Page = "replaced " + Guid.NewGuid(),
                    ItemId = Guid.NewGuid()
                },
            };
            await target.ReplacePagesAsync(ownerId, scoreId, pages);

        }

        [Fact]
        public async Task AddAnnotationsAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), ScoreTableName, ScoreDataTableName,
                ScoreItemRelationDynamoDbTableName);

            var ownerId = Guid.Parse("3c9e2b24-2ba0-4b22-8f1d-60dc5c60b555");
            var scoreId = Guid.Parse("90fcc364-2a67-42b8-8b93-15a84370b1e4");

            var title = "test score";
            var description = "楽譜の説明";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newAnnotations = new List<NewScoreAnnotation>()
            {
                new NewScoreAnnotation(){Content = "アノテーション1"},
                new NewScoreAnnotation(){Content = "アノテーション2"},
                new NewScoreAnnotation(){Content = "アノテーション3"},
            };
            await target.AddAnnotationsAsync(ownerId, scoreId, newAnnotations);
        }


        [Fact]
        public async Task RemoveAnnotationsAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), ScoreTableName, ScoreDataTableName,
                ScoreItemRelationDynamoDbTableName);

            var ownerId = Guid.Parse("9e88f09f-eed7-441e-a0e2-224aea4a3fc0");
            var scoreId = Guid.Parse("27badfc9-372f-4423-aa41-cfa397c9b01d");

            var title = "test score";
            var description = "楽譜の説明";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newAnnotations = new List<NewScoreAnnotation>()
            {
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
            };

            try
            {
                await target.AddAnnotationsAsync(ownerId, scoreId, newAnnotations);
            }
            catch (Exception)
            {
                // 握りつぶす
            }

            var annotationIds = new List<long>()
            {
                1,3
            };
            await target.RemoveAnnotationsAsync(ownerId, scoreId, annotationIds);

        }


        [Fact]
        public async Task ReplaceAnnotationsAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), ScoreTableName, ScoreDataTableName,
                ScoreItemRelationDynamoDbTableName);

            var ownerId = Guid.Parse("5f2dc1b1-bc2c-4ba5-a188-05e1c37307ad");
            var scoreId = Guid.Parse("9fc3f5e5-66b6-4443-be68-1cc96155550f");

            var title = "test score";
            var description = "楽譜の説明";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newAnnotations = new List<NewScoreAnnotation>()
            {
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
            };

            try
            {
                await target.AddAnnotationsAsync(ownerId, scoreId, newAnnotations);
            }
            catch (Exception)
            {
                // 握りつぶす
            }

            var anns = new List<PatchScoreAnnotation>()
            {
                new PatchScoreAnnotation(){TargetAnnotationId = 1, Content = "replaced " + Guid.NewGuid()},
                new PatchScoreAnnotation(){TargetAnnotationId = 3, Content = "replaced " + Guid.NewGuid()},
            };
            await target.ReplaceAnnotationsAsync(ownerId, scoreId, anns);

        }

        [Fact]
        public async Task GetScoreDetailAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), ScoreTableName, ScoreDataTableName,
                ScoreItemRelationDynamoDbTableName);

            var ownerId = Guid.Parse("a5373d09-08f4-4dbd-be92-03bd4732b124");
            var scoreId = Guid.Parse("8127bf3d-0d80-4635-bdb7-9c544ccea46f");

            var title = "test score";
            var description = "楽譜の説明";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newAnnotations = new List<NewScoreAnnotation>()
            {
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
            };

            try
            {
                await target.AddAnnotationsAsync(ownerId, scoreId, newAnnotations);
            }
            catch (Exception)
            {
                // 握りつぶす
            }


            var newPages = new List<NewScorePage>()
            {
                new NewScorePage()
                {
                    Page = "1",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "2",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "3",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "4",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "5",
                    ItemId = Guid.NewGuid(),
                }
            };

            try
            {
                await target.AddPagesAsync(ownerId, scoreId, newPages);
            }
            catch (Exception)
            {
                // 握りつぶす
            }

            var record = await target.GetDynamoDbScoreDataAsync(ownerId, scoreId);

        }

        [Fact]
        public async Task CreateSnapshotAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), ScoreTableName, ScoreDataTableName,
                ScoreItemRelationDynamoDbTableName);

            var ownerId = Guid.Parse("4f0d25c8-0b33-4c00-92dc-2e85c3ac58a5");
            var scoreId = Guid.Parse("fd32d482-477d-4cb4-ab78-88e86a073a31");
            var snapshotId = Guid.Parse("6d1d0a52-8371-4f78-b61b-785522d2577d");

            var title = "test score";
            var description = "楽譜の説明";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newAnnotations = new List<NewScoreAnnotation>()
            {
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
            };

            try
            {
                await target.AddAnnotationsAsync(ownerId, scoreId, newAnnotations);
            }
            catch (Exception)
            {
                // 握りつぶす
            }


            var newPages = new List<NewScorePage>()
            {
                new NewScorePage()
                {
                    Page = "1",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "2",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "3",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "4",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "5",
                    ItemId = Guid.NewGuid(),
                }
            };

            try
            {
                await target.AddPagesAsync(ownerId, scoreId, newPages);
            }
            catch (Exception)
            {
                // 握りつぶす
            }

            string snapshotName = "snapshot name";

            await target.CreateSnapshotAsync(ownerId, scoreId, snapshotId, snapshotName);

        }

        [Fact]
        public async Task DeleteSnapshotAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), ScoreTableName, ScoreDataTableName,
                ScoreItemRelationDynamoDbTableName);

            var ownerId = Guid.Parse("3656f0fe-0068-4019-acc3-db042f6684b3");
            var scoreId = Guid.Parse("aa917a9b-453e-4bc2-8381-b61404725d6a");
            var snapshotId = Guid.Parse("7a82b4e0-02aa-4b5e-b323-7f13f61302c7");

            var title = "test score";
            var description = "楽譜の説明(スナップショット削除)";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newAnnotations = new List<NewScoreAnnotation>()
            {
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
            };

            try
            {
                await target.AddAnnotationsAsync(ownerId, scoreId, newAnnotations);
            }
            catch (Exception)
            {
                // 握りつぶす
            }


            var newPages = new List<NewScorePage>()
            {
                new NewScorePage()
                {
                    Page = "1",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "2",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "3",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "4",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "5",
                    ItemId = Guid.NewGuid(),
                }
            };

            try
            {
                await target.AddPagesAsync(ownerId, scoreId, newPages);
            }
            catch (Exception)
            {
                // 握りつぶす
            }

            string snapshotName = "snapshot name(delete)";

            try
            {
                await target.CreateSnapshotAsync(ownerId, scoreId, snapshotId, snapshotName);
            }
            catch (Exception)
            {
                // 握りつぶす
            }

            await target.DeleteSnapshotAsync(ownerId, scoreId, snapshotId);

        }


        [Fact]
        public async Task GetSnapshotNamesAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), ScoreTableName, ScoreDataTableName,
                ScoreItemRelationDynamoDbTableName);

            var ownerId = Guid.Parse("afd15615-99b6-46c1-92fe-3da242d57e9d");
            var scoreId = Guid.Parse("89405e01-67f1-42e6-8673-e932a4b20d26");

            var title = "test score";
            var description = "楽譜の説明(スナップショット削除)";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newAnnotations = new List<NewScoreAnnotation>()
            {
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
            };

            try
            {
                await target.AddAnnotationsAsync(ownerId, scoreId, newAnnotations);
            }
            catch (Exception)
            {
                // 握りつぶす
            }


            var newPages = new List<NewScorePage>()
            {
                new NewScorePage()
                {
                    Page = "1",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "2",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "3",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "4",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "5",
                    ItemId = Guid.NewGuid(),
                }
            };

            try
            {
                await target.AddPagesAsync(ownerId, scoreId, newPages);
            }
            catch (Exception)
            {
                // 握りつぶす
            }

            var snapshotNames = new[]
            {
                (id: new Guid("5542709f-2810-40c1-a3c4-fbcd2217fb65"), name: "スナップショット名1(Get)"),
                (id: new Guid("a2821d08-3e25-405f-8b7e-1b6543b86e02"), name: "スナップショット名2(Get)"),
                (id: new Guid("d191a5bc-ffca-4ab7-978e-4bf8236b4bdc"), name: "スナップショット名3(Get)"),
                (id: new Guid("e5a56aff-1449-48e0-acd1-89e8506bbb6b"), name: "スナップショット名4(Get)"),
            }.OrderBy(x => x).ToArray();

            try
            {
                foreach (var snapshotName in snapshotNames)
                {
                    await target.CreateSnapshotAsync(ownerId, scoreId, snapshotName.id, snapshotName.name);
                }
            }
            catch (Exception)
            {
                // 握りつぶす
            }

            var actual = await target.GetSnapshotSummariesAsync(ownerId, scoreId);

            Assert.Equal(
                snapshotNames.OrderBy(x => x.id).Select(x=>(x.id,x.name)).ToArray(),
                actual.OrderBy(x => x.Id).Select(x=>(x.Id,x.Name)).ToArray()
                );
        }


        [Fact]
        public async Task DeleteAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), ScoreTableName, ScoreDataTableName,
                ScoreItemRelationDynamoDbTableName);

            var ownerId = Guid.Parse("36a2264f-9843-4c51-96d4-92c4626571ef");
            var scoreId = Guid.Parse("ce815421-4538-4b2e-bcb5-4a43f8c01320");

            var title = "test score";
            var description = "楽譜の説明(楽譜削除)";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newAnnotations = new List<NewScoreAnnotation>()
            {
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
            };

            try
            {
                await target.AddAnnotationsAsync(ownerId, scoreId, newAnnotations);
            }
            catch (Exception)
            {
                // 握りつぶす
            }


            var newPages = new List<NewScorePage>()
            {
                new NewScorePage()
                {
                    Page = "1",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "2",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "3",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "4",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "5",
                    ItemId = Guid.NewGuid(),
                }
            };

            try
            {
                await target.AddPagesAsync(ownerId, scoreId, newPages);
            }
            catch (Exception)
            {
                // 握りつぶす
            }

            var snapshotNames = new string[]
            {
                "スナップショット名1",
                "スナップショット名2",
                "スナップショット名3",
                "スナップショット名4",
            }.OrderBy(x => x).ToArray();

            try
            {
                foreach (var snapshotName in snapshotNames)
                {
                    await target.CreateSnapshotAsync(ownerId, scoreId, snapshotName);
                }
            }
            catch (Exception)
            {
                // 握りつぶす
            }

            await target.DeleteAsync(ownerId, scoreId);
        }

        [Fact]
        public async Task SetAccessAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), ScoreTableName, ScoreDataTableName,
                ScoreItemRelationDynamoDbTableName);

            var ownerId = Guid.Parse("721be298-bf3f-40f1-9f75-0679c4d06147");
            var scoreId = Guid.Parse("6884c1c3-55ab-4c62-b515-a039d18b14e9");

            var title = "test score";
            var description = "楽譜の説明(set access)";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            await target.SetAccessAsync(ownerId, scoreId, ScoreAccesses.Public);
        }
    }


}
