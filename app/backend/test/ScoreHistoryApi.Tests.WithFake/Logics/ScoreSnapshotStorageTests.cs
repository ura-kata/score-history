using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Runtime;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.ScoreObjectStorages;
using ScoreHistoryApi.Models.Scores;
using Xunit;

namespace ScoreHistoryApi.Tests.WithFake.Logics
{
    public class ScoreSnapshotStorageTests
    {
        [Fact]
        public async Task CreateAsyncTest()
        {
            var accessKey = "minio_test";
            var secretKey = "minio_test_pass";
            var factory = new S3ClientFactory()
                .SetEndpointUrl(new Uri("http://localhost:19000"))
                .SetCredentials(new BasicAWSCredentials(accessKey, secretKey));
            var bucketName = "ura-kata-score-snapshot-bucket";
            var target = new ScoreSnapshotStorage(factory.Create(), bucketName);

            var ownerId = Guid.Parse("80b16bc7-5258-441f-9a2a-d6d95fc16c4a");
            var scoreId = Guid.Parse("fc4ac609-0914-4cd6-9caa-cada12c7b03d");
            var snapshotId = Guid.Parse("3dd43d78-ff83-46f5-8ed3-fddbc06ec943");

            var data = new ScoreSnapshot()
            {
                id = snapshotId,
                Name = "スナップショット名",
                Data = new ScoreData()
                {
                    Title = "楽譜",
                    Description = "楽譜の説明",
                    Annotations = new []{
                        new ScoreAnnotation()
                        {
                            Id = 0,
                            ContentHash = "hash00",
                        },
                        new ScoreAnnotation()
                        {
                            Id = 1,
                            ContentHash = "hash01",
                        },
                    },
                    Pages = new []
                    {
                        new ScorePage()
                        {
                            Id = 0,
                            Page = "page1",
                            ItemId = new Guid("3b74ca20-0e47-49b4-941f-45176766ae7d"),
                        },
                        new ScorePage()
                        {
                            Id = 1,
                            Page = "page2",
                            ItemId = new Guid("e3c0a4a6-344d-4247-9932-070ae822186b"),
                        },
                    },
                    AnnotationDataSet = new Dictionary<string, string>()
                    {
                        ["hash00"] = "アノテーション1",
                        ["hash01"] = "アノテーション2",
                    },
                }
            };
            await target.CreateAsync(ownerId, scoreId, data, ScoreObjectAccessControls.Public);
        }

        [Fact]
        public async Task DeleteAsyncTest()
        {
            var accessKey = "minio_test";
            var secretKey = "minio_test_pass";
            var factory = new S3ClientFactory()
                .SetEndpointUrl(new Uri("http://localhost:19000"))
                .SetCredentials(new BasicAWSCredentials(accessKey, secretKey));
            var bucketName = "ura-kata-score-snapshot-bucket";
            var target = new ScoreSnapshotStorage(factory.Create(), bucketName);

            var ownerId = Guid.Parse("31937be9-d1df-4076-b2b6-9cb5e2d94a17");
            var scoreId = Guid.Parse("8a9aaa16-2ca2-4b22-9cf5-21e7b681dbc9");
            var snapshotId = Guid.Parse("f30cd5f5-b807-4273-9299-f95d0baf85b9");

            var data = new ScoreSnapshot()
            {
                id = snapshotId,
                Name = "スナップショット名(delete)",
                Data = new ScoreData()
                {
                    Title = "楽譜",
                    Description = "楽譜の説明",
                    Annotations = new []{
                        new ScoreAnnotation()
                        {
                            Id = 0,
                            ContentHash = "hash00",
                        },
                        new ScoreAnnotation()
                        {
                            Id = 1,
                            ContentHash = "hash01",
                        },
                    },
                    Pages = new []
                    {
                        new ScorePage()
                        {
                            Id = 0,
                            Page = "page1",
                            ItemId = new Guid("3b74ca20-0e47-49b4-941f-45176766ae7d"),
                        },
                        new ScorePage()
                        {
                            Id = 1,
                            Page = "page2",
                            ItemId = new Guid("e3c0a4a6-344d-4247-9932-070ae822186b"),
                        },
                    },
                    AnnotationDataSet = new Dictionary<string, string>()
                    {
                        ["hash00"] = "アノテーション1",
                        ["hash01"] = "アノテーション2",
                    },
                }
            };

            try
            {
                await target.CreateAsync(ownerId, scoreId, data, ScoreObjectAccessControls.Public);
            }
            catch
            {
                // 握りつぶす
            }

            await target.DeleteAsync(ownerId, scoreId, snapshotId);
        }

        [Fact]
        public async Task DeleteAllAsyncTest()
        {
            var accessKey = "minio_test";
            var secretKey = "minio_test_pass";
            var factory = new S3ClientFactory()
                .SetEndpointUrl(new Uri("http://localhost:19000"))
                .SetCredentials(new BasicAWSCredentials(accessKey, secretKey));
            var bucketName = "ura-kata-score-snapshot-bucket";
            var target = new ScoreSnapshotStorage(factory.Create(), bucketName);

            var ownerId = Guid.Parse("e9ca7322-9dd6-4429-b1d8-d3c9244a68ed");
            var scoreId = Guid.Parse("eea7e8c1-15ec-4f69-a675-364f24099267");

            var snapshotIds = new[]
            {
                Guid.Parse("0ea2f185-8355-439c-a427-d5734c17f886"),
                Guid.Parse("18be42eb-de3c-4899-bdc3-7ed512ce07ae"),
                Guid.Parse("7569ad9a-1bb4-4c33-9cb8-11e5c680a404"),
            };

            var data = new ScoreSnapshot()
            {
                Name = "スナップショット名(delete all)",
                Data = new ScoreData()
                {
                    Title = "楽譜",
                    Description = "楽譜の説明",
                    Annotations = new []{
                        new ScoreAnnotation()
                        {
                            Id = 0,
                            ContentHash = "hash00",
                        },
                        new ScoreAnnotation()
                        {
                            Id = 1,
                            ContentHash = "hash01",
                        },
                    },
                    Pages = new []
                    {
                        new ScorePage()
                        {
                            Id = 0,
                            Page = "page1",
                            ItemId = new Guid("3b74ca20-0e47-49b4-941f-45176766ae7d"),
                        },
                        new ScorePage()
                        {
                            Id = 1,
                            Page = "page2",
                            ItemId = new Guid("e3c0a4a6-344d-4247-9932-070ae822186b"),
                        },
                    },
                    AnnotationDataSet = new Dictionary<string, string>()
                    {
                        ["hash00"] = "アノテーション1",
                        ["hash01"] = "アノテーション2",
                    },
                }
            };

            try
            {
                foreach (var snapshotId in snapshotIds)
                {
                    data.id = snapshotId;
                    await target.CreateAsync(ownerId, scoreId, data, ScoreObjectAccessControls.Public);
                }
            }
            catch
            {
                // 握りつぶす
            }

            await target.DeleteAllAsync(ownerId, scoreId);
        }
    }
}
