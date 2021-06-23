using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.Exceptions;
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
        public const string FolderName = "item";
    }

    public static class ScoreItemStorageUtils
    {
        public static ItemTypes CheckItemType(byte[] data)
        {
            if (data == null)
                return ItemTypes.None;

            if (data.Length == 0)
                return ItemTypes.Empty;

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
                if (data.Length < FileSignatures.JpegImage.Length)
                    return false;

                var dataSpan = new Span<byte>(data, 0, 4);

                if (dataSpan.SequenceEqual(FileSignatures.JpegImage))
                    return true;
                if (dataSpan.SequenceEqual(FileSignatures.CannonEosJpegFile))
                    return true;
                if (dataSpan.SequenceEqual(FileSignatures.SamsungD500JpegFile))
                    return true;
                if (dataSpan.SequenceEqual(FileSignatures.StillPictureInterchangeFileFormat))
                    return true;
                if (dataSpan.SequenceEqual(FileSignatures.DigitalCameraJpgUsingExchangeableImageFileFormat))
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

        public static readonly byte[] JpegImage = new byte[] {0xFF, 0xD8, 0xFF, 0xE0,};
        public static readonly byte[] CannonEosJpegFile = new byte[] {0xFF, 0xD8, 0xFF, 0xE2,};
        public static readonly byte[] SamsungD500JpegFile = new byte[] {0xFF, 0xD8, 0xFF, 0xE3,};

        public static readonly byte[] DigitalCameraJpgUsingExchangeableImageFileFormat = new byte[] {0xFF, 0xD8, 0xFF, 0xE1,};
        public static readonly byte[] StillPictureInterchangeFileFormat = new byte[] {0xFF, 0xD8, 0xFF, 0xE8,};
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



        public async Task DeleteObjectAsync(Guid ownerId, Guid scoreId, Guid itemId)
        {
            var prefix = $"{ownerId:D}/{scoreId:D}/{ScoreItemStorageConstant.FolderName}/{itemId:D}";
            await DeleteObjectsAsync(prefix);
        }

        public async Task DeleteAllScoreObjectAsync(Guid ownerId, Guid scoreId)
        {
            var prefix = $"{ownerId:D}/{scoreId:D}/{ScoreItemStorageConstant.FolderName}";
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

    }
}
