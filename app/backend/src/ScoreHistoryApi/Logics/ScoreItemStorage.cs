using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.ScoreObjectStorages;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ScoreHistoryApi.Logics
{
    public static class ScoreItemStorageConstant
    {
        public const int ThumbnailMaxWidthPixel = 200;
        public const string ThumbnailFileName = "thumbnail.jpg";
        public const string PngFileName = "image.png";
        public const string JpegFileName = "image.jpg";
    }

    public static class ScoreItemStorageUtils
    {
        public static ItemTypes CheckItemType(byte[] data)
        {
            if (data == null)
                return ItemTypes.None;

            if (CheckPng(data))
                return ItemTypes.ImagePng;

            if (CheckJpeg(data))
                return ItemTypes.ImageJpeg;

            return ItemTypes.None;

            static bool CheckPng(byte[] data)
            {
                if (data.Length < FileSignatures.Png.Length)
                    return false;

                var dataSpan = new Span<byte>(data, 0, FileSignatures.Png.Length);

                return dataSpan.SequenceEqual(FileSignatures.Png);
            }

            static bool CheckJpeg(byte[] data)
            {
                if (data.Length < FileSignatures.Jpeg1.Length)
                    return false;

                var dataSpan = new Span<byte>(data, 0, 4);

                if (dataSpan.SequenceEqual(FileSignatures.Jpeg1))
                    return true;
                if (dataSpan.SequenceEqual(FileSignatures.Jpeg2))
                    return true;
                if (dataSpan.SequenceEqual(FileSignatures.Jpeg3))
                    return true;
                return false;
            }
        }

        public static string GetFileName(ItemTypes itemType)
        {
            return itemType switch
            {
                ItemTypes.ImageJpeg => ScoreItemStorageConstant.JpegFileName,
                ItemTypes.ImagePng => ScoreItemStorageConstant.PngFileName,
                ItemTypes.None => throw new NotSupportedException(),
                _ => throw new NotSupportedException()
            };
        }
    }

    public static class FileSignatures
    {
        public static readonly byte[] Png = {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        };

        public static readonly byte[] Jpeg1 = new byte[] {0xFF, 0xD8, 0xFF, 0xE0,};
        public static readonly byte[] Jpeg2 = new byte[] {0xFF, 0xD8, 0xFF, 0xE2,};
        public static readonly byte[] Jpeg3 = new byte[] {0xFF, 0xD8, 0xFF, 0xE3,};
    }

    public class ScoreItemStorage : IScoreItemStorage
    {
        public string BucketName { get; } = "ura-kata-score-history-bucket";
        private readonly IScoreQuota _quota;
        private readonly IAmazonS3 _s3Client;

        public ScoreItemStorage(IScoreQuota quota, IAmazonS3 s3Client, IConfiguration configuration)
        {
            var bucketName = configuration[EnvironmentNames.ScoreItemS3Bucket];
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreItemS3Bucket}' is not found.");
            }

            BucketName = bucketName;
            _quota = quota;
            _s3Client = s3Client;
        }
        public ScoreItemStorage(IScoreQuota quota, IAmazonS3 s3Client, string bucketName)
        {
            BucketName = bucketName;
            _quota = quota;
            _s3Client = s3Client;
        }

        public async Task<SavedItemData> SaveObjectAsync(
            Guid ownerId, Guid scoreId, byte[] data,
            ScoreObjectAccessControls accessControl)
        {
            return await SaveObjectAsync(ownerId, scoreId, Guid.NewGuid(), data, accessControl);
        }
        public async Task<SavedItemData> SaveObjectAsync(Guid ownerId, Guid scoreId, Guid itemId, byte[] data, ScoreObjectAccessControls accessControl)
        {
            var itemType = ScoreItemStorageUtils.CheckItemType(data);

            if (itemType == ItemTypes.None)
                throw new ArgumentException(nameof(data));

            var keyDir = $"{ownerId:D}/{scoreId:D}/{itemId:D}/";


            await using var srcStream = new MemoryStream(data);

            using var thumbnailImage = await Image.LoadAsync(srcStream);
            srcStream.Seek(0, SeekOrigin.Begin);


            var height = (int) (thumbnailImage.Height * (double) ScoreItemStorageConstant.ThumbnailMaxWidthPixel / thumbnailImage.Width);
            thumbnailImage.Mutate(x=>x.Resize(ScoreItemStorageConstant.ThumbnailMaxWidthPixel,height));

            await using var thumbnailStream = new MemoryStream();

            await thumbnailImage.SaveAsJpegAsync(thumbnailStream);
            thumbnailStream.Seek(0, SeekOrigin.Begin);

            var thumbnailSize = thumbnailStream.Length;

            var objectFileName = ScoreItemStorageUtils.GetFileName(itemType);
            var thumbnailFileName = ScoreItemStorageConstant.ThumbnailFileName;

            var acl = accessControl switch
            {
                ScoreObjectAccessControls.Private => S3CannedACL.Private,
                ScoreObjectAccessControls.Public => S3CannedACL.PublicRead,
                _ => throw new NotSupportedException(),
            };

            var objectSaveRequest = new PutObjectRequest()
            {
                BucketName = BucketName,
                Key = keyDir + objectFileName,
                CannedACL = acl,
                InputStream = srcStream,
            };

            var thumbnailSaveRequest = new PutObjectRequest()
            {
                BucketName = BucketName,
                Key = keyDir + thumbnailFileName,
                CannedACL = acl,
                InputStream = thumbnailStream,
            };

            await _s3Client.PutObjectAsync(thumbnailSaveRequest);
            await _s3Client.PutObjectAsync(objectSaveRequest);

            return new SavedItemData()
            {
                Data = data,
                OwnerId = ownerId,
                ScoreId = scoreId,
                Type = itemType,
                ItemId = itemId,
                ObjectName = objectFileName,
                Size = data.Length,
                Extra = new Thumbnail()
                {
                    ObjectName = thumbnailFileName,
                    Size = thumbnailSize,
                },
                AccessControl = ScoreObjectAccessControls.Private
            };
        }


        public async Task DeleteObjectAsync(Guid ownerId, Guid scoreId, Guid itemId)
        {
            var prefix = $"{ownerId:D}/{scoreId:D}/{itemId:D}";
            await DeleteObjectsAsync(prefix);
        }

        public async Task DeleteAllScoreObjectAsync(Guid ownerId, Guid scoreId)
        {
            var prefix = $"{ownerId:D}/{scoreId:D}";
            await DeleteObjectsAsync(prefix);
        }

        public async Task DeleteAllOwnerObjectAsync(Guid ownerId)
        {
            var prefix = $"{ownerId:D}";
            await DeleteObjectsAsync(prefix);
        }

        public async Task DeleteObjectsAsync(string prefix)
        {
            var objectKeyList = new List<string>();
            string continuationToken = default;

            do
            {
                var listRequest = new ListObjectsV2Request()
                {
                    BucketName = BucketName,
                    Prefix = prefix,
                    ContinuationToken = string.IsNullOrWhiteSpace(continuationToken) ? null : continuationToken,
                };
                var listResponse = await _s3Client.ListObjectsV2Async(listRequest);

                objectKeyList.AddRange(listResponse.S3Objects.Select(x => x.Key));

                continuationToken = listResponse.NextContinuationToken;

            } while (!string.IsNullOrEmpty(continuationToken));

            var request = new DeleteObjectsRequest()
            {
                BucketName = BucketName,
                Objects = objectKeyList.Select(x=>new KeyVersion()
                {
                    Key = x
                }).ToList(),
            };
            await _s3Client.DeleteObjectsAsync(request);
        }

        public async Task SetAccessControlPolicyAsync(Guid ownerId, Guid scoreId,
            ScoreObjectAccessControls accessControl)
        {
            var prefix = $"{ownerId:D}/{scoreId:D}";

            var objectKeyList = new List<string>();
            string continuationToken = default;

            do
            {
                var listRequest = new ListObjectsV2Request()
                {
                    BucketName = BucketName,
                    Prefix = prefix,
                    ContinuationToken = string.IsNullOrWhiteSpace(continuationToken) ? null : continuationToken,
                };
                var listResponse = await _s3Client.ListObjectsV2Async(listRequest);

                objectKeyList.AddRange(listResponse.S3Objects.Select(x => x.Key));

                continuationToken = listResponse.NextContinuationToken;

            } while (!string.IsNullOrEmpty(continuationToken));


            var acl = accessControl switch
            {
                ScoreObjectAccessControls.Private => S3CannedACL.Private,
                ScoreObjectAccessControls.Public => S3CannedACL.PublicRead,
                _ => throw new NotSupportedException(),
            };

            foreach (var key in objectKeyList)
            {
                var request = new PutACLRequest()
                {
                    BucketName = BucketName,
                    CannedACL = acl,
                    Key = key,
                };
                await _s3Client.PutACLAsync(request);
            }

        }
    }
}
