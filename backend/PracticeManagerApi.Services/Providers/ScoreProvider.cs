using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using PracticeManagerApi.Services.Objects;
using PracticeManagerApi.Services.Storage;

namespace PracticeManagerApi.Services.Providers
{
    public class ScoreProvider: IScoreProvider
    {
        public string UserName { get; }
        private readonly DateTimeOffset _now;
        private readonly IScoreStorage _storage;

        public const string IdsRoot = "ids";
        public const string RepositoriesRoot = "repositories";
        public const string ObjectDirectoryName = "objects";

        public const string IdObjectName = "ID";
        public const string HeadObjectName = "HEAD";
        public const string PropertyObjectName = "PROPERTY";

        public const string VersionHashType = "version";
        public const string PageHashType = "page";
        public const string CommentHashType = "comment";
        public const string PropertyHashType = "property";

        #region Utility --------------------------------------------------------------------------------------------------------------------


        public static string JoinKeys(ReadOnlySpan<char> key1, ReadOnlySpan<char> key2) =>
            string.Concat(key1, "/", key2);

        public static string JoinKeys(ReadOnlySpan<char> key1, ReadOnlySpan<char> key2, ReadOnlySpan<char> key3) =>
            string.Concat(string.Concat(key1, "/", key2), "/", key3);

        public static string JoinKeys(
            ReadOnlySpan<char> key1, ReadOnlySpan<char> key2,
            ReadOnlySpan<char> key3, ReadOnlySpan<char> key4) =>
            string.Concat(string.Concat(key1, "/", key2, "/"), key3, "/", key4);

        public static string JoinKeys(
            ReadOnlySpan<char> key1, ReadOnlySpan<char> key2, ReadOnlySpan<char> key3,
            ReadOnlySpan<char> key4, ReadOnlySpan<char> key5) =>
            string.Concat(string.Concat(string.Concat(key1, "/", key2, "/"), key3, "/", key4), "/", key5);

        public static string JoinKeys(
            ReadOnlySpan<char> key1, ReadOnlySpan<char> key2, ReadOnlySpan<char> key3,
            ReadOnlySpan<char> key4, ReadOnlySpan<char> key5, ReadOnlySpan<char> key6) =>
            new StringBuilder().Append(key1)
                .Append("/").Append(key2)
                .Append("/").Append(key3)
                .Append("/").Append(key4)
                .Append("/").Append(key5)
                .Append("/").Append(key6).ToString();

        public static string ComputeHash(string type, byte[] contents)
        {
            var contentsBytes = contents;
            var headerBytes = System.Text.Encoding.UTF8.GetBytes(type + " " + contentsBytes.Length + "\0");

            var buffer = new byte[headerBytes.Length + contentsBytes.Length];

            Array.Copy(headerBytes, 0, buffer, 0, headerBytes.Length);
            Array.Copy(contentsBytes, 0, buffer, headerBytes.Length, contentsBytes.Length);

            using var sh1 = System.Security.Cryptography.SHA1.Create();

            var hash = sh1.ComputeHash(buffer);

            var sb = new System.Text.StringBuilder(40);
            for (int i = 0; i < hash.Length; ++i)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }
        public static string ComputeHash(string type, string contents)
        {
            var contentsBytes = System.Text.Encoding.UTF8.GetBytes(contents);
            return ComputeHash(type, contentsBytes);
        }

        public static string CreateUuid() => Guid.NewGuid().ToString("D");

        public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            AllowTrailingCommas = true,
            IgnoreReadOnlyProperties = true,
            IgnoreNullValues = true,
            WriteIndented = false,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Disallow,
        };

        public static byte[] Serialize<TValue>(TValue value) =>
            JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions);

        public static TValue Deserialize<TValue>(ReadOnlySpan<byte> utf8Json) =>
            JsonSerializer.Deserialize<TValue>(utf8Json, JsonSerializerOptions);

        public static string CreateObjectKey(string owner, string scoreName, string hash) =>
            JoinKeys(RepositoriesRoot, owner, scoreName, ObjectDirectoryName, hash.AsSpan(0, 2), hash.AsSpan(2));


        #endregion --------------------------------------------------------------------------------------------------------------------

        protected ScoreProvider(DateTimeOffset now, IScoreStorage storage, string userName)
        {
            UserName = userName;
            _now = now;
            _storage = storage;
        }

        private void CreateProperty(string owner, string scoreName, NewScoreV2Property property)
        {
            var propertyObject = new ScoreV2PropertyObject()
            {
                CreateAt = _now,
                Author = UserName,
                Title = property.Title,
                Description = property.Description
            };

            
            var propertyData = Serialize(propertyObject);

            var propertyHash = ComputeHash(PropertyHashType, propertyData);
            var objectKey = CreateObjectKey(owner, scoreName, propertyHash);
            _storage.SetObjectBytes(objectKey, propertyData);


            var propertyKey = JoinKeys(RepositoriesRoot, owner, scoreName, PropertyObjectName);
            _storage.SetObjectString(propertyKey, propertyHash);
        }

        private void CreateInitialVersion(string owner, string scoreName)
        {
            var versionObject = new ScoreV2VersionObject()
            {
                CreateAt = _now,
                Author = UserName,
                Message = "create score",
                Comments = new Dictionary<string, string[]>(),
                Pages = new string[0],
                Parent = "",
            };


            var versionData = Serialize(versionObject);

            var versionHash = ComputeHash(PropertyHashType, versionData);
            var objectKey = CreateObjectKey(owner, scoreName, versionHash);
            _storage.SetObjectBytes(objectKey, versionData);


            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);
            _storage.SetObjectString(headKey, versionHash);
        }

        public void CreateScore(string owner, string scoreName, NewScoreV2Property property)
        {

            // Todo property の検証を行う

            var scoreKey = JoinKeys(owner, scoreName);
            var scoreRootKey = JoinKeys(RepositoriesRoot, scoreKey);

            if (_storage.ExistPath(scoreRootKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is existed.");
            }

            var scoreId = CreateUuid();

            var scoreRefObject = scoreKey;
            var scoreRefObjectKey = JoinKeys(IdsRoot, scoreId);
            _storage.SetObjectString(scoreRefObjectKey, scoreRefObject);


            CreateProperty(owner, scoreName, property);

            CreateInitialVersion(owner, scoreName);


            var idKey = JoinKeys(scoreRootKey, IdObjectName);
            var idObject = scoreId;
            _storage.SetObjectString(idKey, idObject);
        }

        public void UpdateProperty(string owner, string scoreName, string parentPropertyHash, PatchScoreV2Property property)
        {

            // Todo property の検証を行う

            var propertyKey = JoinKeys(RepositoriesRoot, owner, scoreName, PropertyObjectName);

            var propertyHashFromStorage = _storage.GetObjectString(propertyKey).TrimEnd('\n', '\r');

            if (propertyHashFromStorage.Equals(parentPropertyHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"'{parentPropertyHash}' is old.");
            }


            var propertyObject = new ScoreV2PropertyObject()
            {
                CreateAt = _now,
                Author = UserName,
                Title = property.Title,
                Description = property.Description,
                Parent = parentPropertyHash
            };

            var propertyData = Serialize(propertyObject);
            var propertyHash = ComputeHash(PropertyHashType, propertyData);
            
            _storage.SetObjectString(propertyKey, propertyHash);


            var checkedPropertyHashFromStorage = _storage.GetObjectString(propertyKey).TrimEnd('\n', '\r');

            if (checkedPropertyHashFromStorage.Equals(propertyHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"There was an interrupt in the update.");
            }

            var objectKey = CreateObjectKey(owner, scoreName, propertyHash);
            _storage.SetObjectBytes(objectKey, propertyData);
        }

        public void DeleteScore(string owner, string scoreName)
        {
            var scoreRootKey = JoinKeys(RepositoriesRoot, owner, scoreName);

            if (false == _storage.DeletePath(scoreRootKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }
        }

        private void CheckHead(string headKey, string parentVersionHash)
        {
            var versionHashFromHead = _storage.GetObjectString(headKey).TrimEnd('\n', '\r');

            if (false == versionHashFromHead.Equals(parentVersionHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"'{parentVersionHash}' is old.");
            }
        }

        private void UpdateHead(string headKey, string newVersionHash)
        {
            _storage.SetObjectString(headKey, newVersionHash);

            var checkedVersionHashFromHead = _storage.GetObjectString(headKey).TrimEnd('\n', '\r');

            if (checkedVersionHashFromHead.Equals(newVersionHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"There was an interrupt in the update.");
            }
        }

        public void InsertPages(string owner, string scoreName, string parentVersionHash, int index, NewScoreV2Page[] pages)
        {
            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);

            if (false == _storage.ExistObject(headKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }

            CheckHead(headKey, parentVersionHash);

            // update object

            var parentVersionObjectKey = CreateObjectKey(owner, scoreName, parentVersionHash);

            var parentData = _storage.GetObjectBytes(parentVersionObjectKey);

            var versionObject = Deserialize<ScoreV2VersionObject>(parentData);

            var pageList = versionObject.Pages?.ToList() ?? new List<string>();

            // Todo Convert local function を作成する。その時検証をする。

            var newPages = pages.Select(page => new ScoreV2PageObject()
                {
                    Author = UserName,
                    CreateAt = _now,
                    Number = page.Number,
                    Image = page.Image,
                    Thumbnail = page.Thumbnail,
                })
                .Select(Serialize)
                .Select(pageData => (data: pageData, hash: ComputeHash(PageHashType, pageData)))
                .ToArray();

            pageList.InsertRange(index, newPages.Select(x => x.hash));

            versionObject.Pages = pageList.ToArray();


            // update hash
            var newVersionData = Serialize(versionObject);

            var newVersionHash = ComputeHash(VersionHashType, newVersionData);

            UpdateHead(headKey, newVersionHash);

            // register page objects

            foreach (var (data,hash) in newPages)
            {
                var objectKey = CreateObjectKey(owner, scoreName, hash);
                _storage.SetObjectBytes(objectKey, data);
            }
        }

        public void DeletePages(string owner, string scoreName, string parentVersionHash, string[] hashList)
        {
            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);

            if (false == _storage.ExistObject(headKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }

            CheckHead(headKey, parentVersionHash);

            // update object

            var parentVersionObjectKey = CreateObjectKey(owner, scoreName, parentVersionHash);

            var parentData = _storage.GetObjectBytes(parentVersionObjectKey);

            var versionObject = Deserialize<ScoreV2VersionObject>(parentData);

            var pageList = versionObject.Pages?.ToList() ?? new List<string>();

            void Remove(string hash)
            {
                for (var i = 0; i < pageList.Count; i++)
                {
                    if (pageList[i].Equals(hash, StringComparison.Ordinal))
                    {
                        pageList.RemoveAt(i);
                        return;
                    }
                }
                throw new InvalidOperationException($"'{hash}' is not found in pages.");
            }

            foreach (var hash in hashList)
            {
                Remove(hash);
            }
            
            versionObject.Pages = pageList.ToArray();


            // update hash
            var newVersionData = Serialize(versionObject);

            var newVersionHash = ComputeHash(VersionHashType, newVersionData);

            UpdateHead(headKey, newVersionHash);

        }

        public void UpdatePages(string owner, string scoreName, string parentVersionHash, PatchScoreV2Page[] pages)
        {
            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);

            if (false == _storage.ExistObject(headKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }

            CheckHead(headKey, parentVersionHash);

            // update object

            var parentVersionObjectKey = CreateObjectKey(owner, scoreName, parentVersionHash);

            var parentData = _storage.GetObjectBytes(parentVersionObjectKey);

            var versionObject = Deserialize<ScoreV2VersionObject>(parentData);

            var pageList = versionObject.Pages?.ToArray() ?? new string[0];

            int IndexOf(string hash)
            {
                for (var i = 0; i < pageList.Length; i++)
                {
                    if (pageList[i].Equals(hash, StringComparison.Ordinal))
                    {
                        return i;
                    }
                }
                throw new InvalidOperationException($"'{hash}' is not found in pages.");
            }

            // Todo Convert local function を作成する。その時検証をする。

            var newPageList = pages.Select(page => (
                    targetIndex: IndexOf(page.Target),
                    pageObject: new ScoreV2PageObject()
                    {
                        Author = UserName,
                        CreateAt = _now,
                        Number = page.Number,
                        Image = page.Image,
                        Thumbnail = page.Thumbnail,
                    }))
                .Select(x => (x.targetIndex, pageData: Serialize(x.pageObject)))
                .Select(x => (x.targetIndex, x.pageData, pageHash: ComputeHash(PageHashType, x.pageData)))
                .ToArray();

            foreach (var (targetPageIndex, _, newPageHash) in newPageList)
            {
                pageList[targetPageIndex] = newPageHash;
            }

            versionObject.Pages = pageList;


            // update hash
            var newVersionData = Serialize(versionObject);

            var newVersionHash = ComputeHash(VersionHashType, newVersionData);

            UpdateHead(headKey, newVersionHash);

            // register page objects

            foreach (var (targetPageIndex, newPageData, newPageHash) in newPageList)
            {
                var objectKey = CreateObjectKey(owner, scoreName, newPageHash);
                _storage.SetObjectBytes(objectKey, newPageData);
            }
        }

        public void AddComments(string owner, string scoreName, string parentVersionHash, NewScoreV2Comment[] comments)
        {
            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);

            if (false == _storage.ExistObject(headKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }

            CheckHead(headKey, parentVersionHash);

            // update object

            var parentVersionObjectKey = CreateObjectKey(owner, scoreName, parentVersionHash);

            var parentData = _storage.GetObjectBytes(parentVersionObjectKey);

            var versionObject = Deserialize<ScoreV2VersionObject>(parentData);

            var commentSet= versionObject.Comments?.ToDictionary(
                x => x.Key,
                x => x.Value?.ToArray() ?? new string[0]) ?? new Dictionary<string, string[]>();

            // Todo Convert local function を作成する。その時検証をする。

            var newComments = comments.Select(comment => (
                    targetPageHash: comment.TargetPage,
                    newCommentObject: new ScoreV2CommentObject()
                    {
                        Author = UserName,
                        CreateAt = _now,
                        Comment = comment.Comment,
                    }))
                .Select(x => (x.targetPageHash, newCommentData: Serialize(x.newCommentObject)))
                .Select(x => (
                    x.targetPageHash,
                    x.newCommentData,
                    newCommentHash: ComputeHash(CommentHashType, x.newCommentData)))
                .ToArray();

            // Todo newComment ごとに ToList をしてしまうので非効率
            foreach (var newComment in newComments)
            {
                if (commentSet.TryGetValue(newComment.targetPageHash, out var cmt))
                {
                    var commentList = cmt?.ToList() ?? new List<string>();

                    commentList.Add(newComment.newCommentHash);

                    commentSet[newComment.targetPageHash] = commentList.ToArray();
                }
                else
                {
                    throw new InvalidOperationException($"'{newComment.targetPageHash}' is not found in comment keys.");
                }
            }
            
            versionObject.Comments = commentSet;


            // update hash
            var newVersionData = Serialize(versionObject);

            var newVersionHash = ComputeHash(VersionHashType, newVersionData);

            UpdateHead(headKey, newVersionHash);

            // register page objects

            foreach (var (_, newCommentData, newCommentHash) in newComments)
            {
                var objectKey = CreateObjectKey(owner, scoreName, newCommentHash);
                _storage.SetObjectBytes(objectKey, newCommentData);
            }
        }

        public void DeleteComments(string owner, string scoreName, string parentVersionHash,
            string targetPage,
            string[] hashList)
        {
            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);

            if (false == _storage.ExistObject(headKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }

            CheckHead(headKey, parentVersionHash);

            // update object

            var parentVersionObjectKey = CreateObjectKey(owner, scoreName, parentVersionHash);

            var parentData = _storage.GetObjectBytes(parentVersionObjectKey);

            var versionObject = Deserialize<ScoreV2VersionObject>(parentData);

            var commentSet = versionObject.Comments?.ToDictionary(
                x => x.Key,
                x => x.Value?.ToArray() ?? new string[0]) ?? new Dictionary<string, string[]>();

            if (false == commentSet.TryGetValue(targetPage, out var cmt))
            {
                throw new InvalidOperationException($"'{targetPage}' is not found in comment keys.");
            }

            var commentList = cmt?.ToList() ?? new List<string>();

            int FindIndex(string hash)
            {
                for (var i = 0; i < commentList.Count; i++)
                {
                    if (commentList[i].Equals(hash, StringComparison.Ordinal))
                    {
                        return i;
                    }
                }
                throw new InvalidOperationException($"'{hash}' is not found in comments.");
            }

            foreach (var commentHash in hashList)
            {
                var targetCommentIndex = FindIndex(commentHash);
                commentList.RemoveAt(targetCommentIndex);
            }

            commentSet[targetPage] = commentList.ToArray();


            versionObject.Comments = commentSet;


            // update hash
            var newVersionData = Serialize(versionObject);

            var newVersionHash = ComputeHash(VersionHashType, newVersionData);

            UpdateHead(headKey, newVersionHash);

        }

        public void UpdateComments(string owner, string scoreName, string parentVersionHash, PatchScoreV2Comment[] comments)
        {
            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);

            if (false == _storage.ExistObject(headKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }

            CheckHead(headKey, parentVersionHash);

            // update object

            var parentVersionObjectKey = CreateObjectKey(owner, scoreName, parentVersionHash);

            var parentData = _storage.GetObjectBytes(parentVersionObjectKey);

            var versionObject = Deserialize<ScoreV2VersionObject>(parentData);

            var commentSet = versionObject.Comments?.ToDictionary(
                x => x.Key,
                x => x.Value?.ToArray() ?? new string[0]) ?? new Dictionary<string, string[]>();

            // Todo Convert local function を作成する。その時検証をする。

            var newComments = comments.Select(comment => (
                    targetPageHash: comment.TargetPage,
                    targetComment: comment.TargetComment,
                    newCommentObject: new ScoreV2CommentObject()
                    {
                        Author = UserName,
                        CreateAt = _now,
                        Comment = comment.Comment,
                    }))
                .Select(x => (x.targetPageHash, x.targetComment, newCommentData: Serialize(x.newCommentObject)))
                .Select(x => (
                    x.targetPageHash,
                    x.targetComment,
                    x.newCommentData,
                    newCommentHash: ComputeHash(CommentHashType, x.newCommentData)))
                .ToArray();

            // Todo newComment ごとに ToList をしてしまうので非効率
            foreach (var newComment in newComments)
            {
                if (commentSet.TryGetValue(newComment.targetPageHash, out var cmt))
                {
                    var commentList = cmt?.ToArray() ?? new string[0];

                    int FindIndex()
                    {
                        for (var i = 0; i < commentList.Length; i++)
                        {
                            if (commentList[i].Equals(newComment.targetComment, StringComparison.Ordinal))
                            {
                                return i;
                            }
                        }
                        throw new InvalidOperationException($"'{newComment.targetComment}' is not found in comments.");
                    }

                    var index = FindIndex();

                    commentList[index] = newComment.newCommentHash;

                    commentSet[newComment.targetPageHash] = commentList;
                }
                else
                {
                    throw new InvalidOperationException($"'{newComment.targetPageHash}' is not found in comment keys.");
                }
            }

            versionObject.Comments = commentSet;


            // update hash
            var newVersionData = Serialize(versionObject);

            var newVersionHash = ComputeHash(VersionHashType, newVersionData);

            UpdateHead(headKey, newVersionHash);

            // register page objects

            foreach (var (_, _, newCommentData, newCommentHash) in newComments)
            {
                var objectKey = CreateObjectKey(owner, scoreName, newCommentHash);
                _storage.SetObjectBytes(objectKey, newCommentData);
            }
        }

        public ScoreV2ObjectSet GetObjects(string owner, string scoreName, string[] hashList)
        {
            var result = new ScoreV2ObjectSet();

            foreach (var hash in hashList)
            {
                var objectKey = CreateObjectKey(owner, scoreName, hash);

                // Object がなければここで例外が発生する
                try
                {
                    var json = _storage.GetObjectString(objectKey);

                    result[hash] = json;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"'{hash}' is not found in objects.", ex);
                }
            }

            return result;
        }
    }
}
