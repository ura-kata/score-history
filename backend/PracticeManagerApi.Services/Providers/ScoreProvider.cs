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
    /// <summary>
    /// 楽譜のデータのプロバイダー
    /// </summary>
    public class ScoreProvider: IScoreProvider
    {
        /// <summary>
        /// ユーザー名
        /// </summary>
        public string UserName { get; }
        private readonly DateTimeOffset _now;
        private readonly IScoreStorage _storage;

        /// <summary>Ids の root key</summary>
        public const string IdsRoot = "ids";
        /// <summary>Repositories の root key</summary>
        public const string RepositoriesRoot = "repositories";
        /// <summary>Object の directory name</summary>
        public const string ObjectDirectoryName = "objects";
        /// <summary>Ref Object の directory name</summary>
        public const string RefsDirectoryName = "refs";
        /// <summary>Version Ref Object の directory name</summary>
        public const string VersionRefsDirectoryName = "versions";

        /// <summary>ID Object の名前</summary>
        public const string IdObjectName = "ID";
        /// <summary>HEAD Object の名前</summary>
        public const string HeadObjectName = "HEAD";

        /// <summary>Version Object Hash Type</summary>
        public const string VersionHashType = "version";
        /// <summary>Page Object Hash Type</summary>
        public const string PageHashType = "page";
        /// <summary>Comment Object Hash Type</summary>
        public const string CommentHashType = "comment";

        #region Utility --------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Key を接続する
        /// </summary>
        /// <param name="key1">Key1</param>
        /// <param name="key2">Key2</param>
        /// <returns>接続された Key</returns>
        public static string JoinKeys(ReadOnlySpan<char> key1, ReadOnlySpan<char> key2) =>
            string.Concat(key1, "/", key2);

        /// <summary>
        /// Key を接続する
        /// </summary>
        /// <param name="key1">Key1</param>
        /// <param name="key2">Key2</param>
        /// <param name="key3">Key3</param>
        /// <returns>接続された Key</returns>
        public static string JoinKeys(ReadOnlySpan<char> key1, ReadOnlySpan<char> key2, ReadOnlySpan<char> key3) =>
            string.Concat(string.Concat(key1, "/", key2), "/", key3);
        
        /// <summary>
        /// Key を接続する
        /// </summary>
        /// <param name="key1">Key1</param>
        /// <param name="key2">Key2</param>
        /// <param name="key3">Key3</param>
        /// <param name="key4">Key4</param>
        /// <returns>接続された Key</returns>
        public static string JoinKeys(
            ReadOnlySpan<char> key1, ReadOnlySpan<char> key2,
            ReadOnlySpan<char> key3, ReadOnlySpan<char> key4) =>
            string.Concat(string.Concat(key1, "/", key2, "/"), key3, "/", key4);
        
        /// <summary>
        /// Key を接続する
        /// </summary>
        /// <param name="key1">Key1</param>
        /// <param name="key2">Key2</param>
        /// <param name="key3">Key3</param>
        /// <param name="key4">Key4</param>
        /// <param name="key5">Key5</param>
        /// <returns>接続された Key</returns>
        public static string JoinKeys(
            ReadOnlySpan<char> key1, ReadOnlySpan<char> key2, ReadOnlySpan<char> key3,
            ReadOnlySpan<char> key4, ReadOnlySpan<char> key5) =>
            string.Concat(string.Concat(string.Concat(key1, "/", key2, "/"), key3, "/", key4), "/", key5);

        /// <summary>
        /// Key を接続する
        /// </summary>
        /// <param name="key1">Key1</param>
        /// <param name="key2">Key2</param>
        /// <param name="key3">Key3</param>
        /// <param name="key4">Key4</param>
        /// <param name="key5">Key5</param>
        /// <param name="key6">Key6</param>
        /// <returns>接続された Key</returns>
        public static string JoinKeys(
            ReadOnlySpan<char> key1, ReadOnlySpan<char> key2, ReadOnlySpan<char> key3,
            ReadOnlySpan<char> key4, ReadOnlySpan<char> key5, ReadOnlySpan<char> key6) =>
            new StringBuilder().Append(key1)
                .Append("/").Append(key2)
                .Append("/").Append(key3)
                .Append("/").Append(key4)
                .Append("/").Append(key5)
                .Append("/").Append(key6).ToString();

        /// <summary>
        /// Hash を計算する
        /// </summary>
        /// <param name="type">Hash Type</param>
        /// <param name="contents">コンテンツ</param>
        /// <returns>Hash</returns>
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

        /// <summary>
        /// Hash を計算する
        /// </summary>
        /// <param name="type">Hash Type</param>
        /// <param name="contents">コンテンツ</param>
        /// <returns>Hash</returns>
        public static string ComputeHash(string type, string contents)
        {
            var contentsBytes = System.Text.Encoding.UTF8.GetBytes(contents);
            return ComputeHash(type, contentsBytes);
        }

        /// <summary>
        /// UUID を作成する
        /// </summary>
        /// <returns>UUID</returns>
        public static string CreateUuid() => Guid.NewGuid().ToString("D");

        /// <summary>
        /// JSON Serializer のオプション
        /// </summary>
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

        /// <summary>
        /// JSON に Serialize する
        /// </summary>
        /// <typeparam name="TValue">value の型</typeparam>
        /// <param name="value">値</param>
        /// <returns>JSON</returns>
        public static byte[] Serialize<TValue>(TValue value) =>
            JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions);

        /// <summary>
        /// JSON を Deserialize する
        /// </summary>
        /// <typeparam name="TValue">value の型</typeparam>
        /// <param name="utf8Json">JSON</param>
        /// <returns>value</returns>
        public static TValue Deserialize<TValue>(ReadOnlySpan<byte> utf8Json) =>
            JsonSerializer.Deserialize<TValue>(utf8Json, JsonSerializerOptions);

        /// <summary>
        /// Object の Key を生成する
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <param name="scoreName">楽譜の名前</param>
        /// <param name="hash">Hash</param>
        /// <returns>Key</returns>
        public static string CreateObjectKey(string owner, string scoreName, string hash) =>
            JoinKeys(RepositoriesRoot, owner, scoreName, ObjectDirectoryName, hash.AsSpan(0, 2), hash.AsSpan(2));

        public static string CreateVersionRefObjectKey(string owner, string scoreName, int version) =>
            JoinKeys(RepositoriesRoot, owner, scoreName,
                RefsDirectoryName, VersionRefsDirectoryName, version.ToString("0000000000"));

        #endregion --------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="now"></param>
        /// <param name="storage"></param>
        /// <param name="userName"></param>
        public ScoreProvider(DateTimeOffset now, IScoreStorage storage, string userName)
        {
            UserName = userName;
            _now = now;
            _storage = storage;
        }

        
        /// <summary>
        /// 楽譜を作成する
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <param name="scoreName">楽譜の名前</param>
        /// <param name="property">プロパティ</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void CreateScore(string owner, string scoreName, InitialScoreV2Property property)
        {
            if (string.IsNullOrWhiteSpace(owner))
                throw new ArgumentException(nameof(owner));

            if (string.IsNullOrWhiteSpace(scoreName))
                throw new ArgumentException(nameof(scoreName));

            if (null == property.Title)
                throw new ArgumentException($"{nameof(property)}.{nameof(property.Title)}");

            if (null == property.Description)
                throw new ArgumentException($"{nameof(property)}.{nameof(property.Description)}");

            var scoreKey = JoinKeys(owner, scoreName);
            var scoreRootKey = JoinKeys(RepositoriesRoot, scoreKey);

            if (_storage.ExistDirectory(scoreRootKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is existed.");
            }

            // save Score Ref Object
            var scoreId = CreateUuid();
            var scoreRefObject = scoreKey;
            var scoreRefObjectKey = JoinKeys(IdsRoot, scoreId);
            _storage.SetObjectString(scoreRefObjectKey, scoreRefObject);


            // create Version Object
            var propertyItem = new ScoreV2PropertyItem()
            {
                Title = property.Title,
                Description = property.Description,
            };

            var versionObject = new ScoreV2VersionObject()
            {
                Author = UserName,
                CreateAt = _now,
                Parent = "",
                Comments = new Dictionary<string, string[]>(),
                Pages = new string[0],
                Property = propertyItem,
                Message = "create score"
            };


            // update version
            UpdateVersionObject(owner, scoreName, "", versionObject);


            // save ID
            var idKey = JoinKeys(scoreRootKey, IdObjectName);
            var idObject = scoreId;
            _storage.SetObjectString(idKey, idObject);
        }

        /// <summary>
        /// 楽譜の Property と Head を取得する
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <param name="scoreName">楽譜の名前</param>
        /// <returns>Property と Head</returns>
        public ScoreV2Latest GetScore(string owner, string scoreName)
        {
            if (string.IsNullOrWhiteSpace(owner))
                throw new ArgumentException(nameof(owner));

            if (string.IsNullOrWhiteSpace(scoreName))
                throw new ArgumentException(nameof(scoreName));

            var scoreKey = JoinKeys(owner, scoreName);
            var scoreRootKey = JoinKeys(RepositoriesRoot, scoreKey);

            if (false == _storage.ExistDirectory(scoreRootKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }


            // load HEAD
            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);
            var headHash = _storage.GetObjectString(headKey).TrimEnd('\n', '\r');


            // deserialize Version Object
            var headObjectKey = CreateObjectKey(owner, scoreName, headHash);
            var headData = _storage.GetObjectBytes(headObjectKey);
            var headObject = Deserialize<ScoreV2VersionObject>(headData);


            return new ScoreV2Latest()
            {
                HeadHash = headHash,
                Head = headObject,
            };
        }

        /// <summary>
        /// 楽譜の Property と Head を一覧で取得する
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <returns>Property と Head</returns>
        public ScoreV2LatestSet GetScores(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner))
                throw new ArgumentException(nameof(owner));


            var ownerKey = JoinKeys(RepositoriesRoot, owner);

            if (false == _storage.ExistDirectory(ownerKey))
            {
                throw new InvalidOperationException($"'{owner}' is not found.");
            }

            var scoreNames = _storage.GetChildrenDirectoryNames(ownerKey);

            var latestList = scoreNames.Select(scoreName => (key: JoinKeys(owner, scoreName), value: GetScore(owner, scoreName)))
                .ToArray();

            var result =new ScoreV2LatestSet();
            foreach (var latest in latestList)
            {
                result[latest.key] = latest.value;
            }

            return result;
        }

        /// <summary>
        /// 楽譜の Property と Head を一覧で取得する
        /// </summary>
        /// <returns>Property と Head</returns>
        public ScoreV2LatestSet GetScores()
        {
            if (string.IsNullOrWhiteSpace(UserName))
                throw new InvalidOperationException(nameof(UserName));


            var owner = UserName;
            var ownerKey = JoinKeys(RepositoriesRoot, owner);

            if (false == _storage.ExistDirectory(ownerKey))
            {
                return new ScoreV2LatestSet();
            }

            var scoreNames = _storage.GetChildrenDirectoryNames(ownerKey);

            var latestList = scoreNames.Select(scoreName => (key: JoinKeys(owner, scoreName), value: GetScore(owner, scoreName)))
                .ToArray();

            var result = new ScoreV2LatestSet();
            foreach (var latest in latestList)
            {
                result[latest.key] = latest.value;
            }

            // Todo Shared な楽譜を取得する

            return result;
        }

        /// <summary>
        /// 楽譜のプロパティを更新する
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <param name="scoreName">楽譜の名前</param>
        /// <param name="parentVersionHash">親となる Property の Hash</param>
        /// <param name="property">更新するプロパティ</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void UpdateProperty(string owner, string scoreName, string parentVersionHash, PatchScoreV2Property property)
        {
            if (string.IsNullOrWhiteSpace(owner))
                throw new ArgumentException(nameof(owner));
            if (string.IsNullOrWhiteSpace(scoreName))
                throw new ArgumentException(nameof(scoreName));
            if (string.IsNullOrWhiteSpace(parentVersionHash))
                throw new ArgumentException(nameof(parentVersionHash));
            if (property == null)
                throw new ArgumentNullException(nameof(property));


            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);

            var headHashFromStorage = _storage.GetObjectString(headKey).TrimEnd('\n', '\r');

            if (false == headHashFromStorage.Equals(parentVersionHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"'{parentVersionHash}' is old.");
            }

            var versionObjectData = _storage.GetObjectBytes(parentVersionHash);
            var versionObjectObject = Deserialize<ScoreV2VersionObject>(versionObjectData);

            // update property object
            var propertyItem = versionObjectObject.Property ?? new ScoreV2PropertyItem();

            propertyItem.Title = property.Title ?? propertyItem.Title;
            propertyItem.Description = property.Description ?? propertyItem.Description;

            versionObjectObject.Property = propertyItem;


            // update version
            UpdateVersionObject(owner, scoreName, parentVersionHash, versionObjectObject);
        }

        /// <summary>
        /// Version Object の登録と HEAD の更新を行う
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="scoreName"></param>
        /// <param name="parentVersionHash"></param>
        /// <param name="versionObject"></param>
        /// <returns>HEAD hash, serialize object</returns>
        private (string hash, byte[] data) UpdateVersionObject(string owner, string scoreName, string parentVersionHash, ScoreV2VersionObject versionObject)
        {
            // update parent
            versionObject.Parent = parentVersionHash;


            // save property objects
            var updatedVersionData = Serialize(versionObject);
            var updatedVersionObjectHash = ComputeHash(VersionHashType, updatedVersionData);
            var updatedVersionObjectKey = CreateObjectKey(owner, scoreName, updatedVersionObjectHash);
            _storage.SetObjectBytes(updatedVersionObjectKey, updatedVersionData);


            // update HEAD
            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);
            _storage.SetObjectString(headKey, updatedVersionObjectHash);

            return (updatedVersionObjectHash, updatedVersionData);
        }

        /// <summary>
        /// 楽譜を削除する
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <param name="scoreName">楽譜の名前</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void DeleteScore(string owner, string scoreName)
        {
            var scoreRootKey = JoinKeys(RepositoriesRoot, owner, scoreName);

            if (false == _storage.DeleteDirectory(scoreRootKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }
        }

        /// <summary>
        /// 現在の HEAD が指定した HASH であるか確認する
        /// </summary>
        /// <param name="headKey">HEAD Object の Key</param>
        /// <param name="hash">確認する Hash</param>
        /// <exception cref="InvalidOperationException"></exception>
        private void CheckHeadBeforeUpdating(string headKey, string hash)
        {
            var versionHashFromHead = _storage.GetObjectString(headKey).TrimEnd('\n', '\r');

            if (false == versionHashFromHead.Equals(hash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"'{hash}' is old.");
            }
        }


        /// <summary>
        /// ページを指定した位置に挿入する
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <param name="scoreName">楽譜名</param>
        /// <param name="parentVersionHash">親となる Version の Hash</param>
        /// <param name="index">挿入する位置</param>
        /// <param name="pages">挿入するページ</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void InsertPages(string owner, string scoreName, string parentVersionHash, int index, NewScoreV2Page[] pages)
        {
            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);

            if (false == _storage.ExistObject(headKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }

            CheckHeadBeforeUpdating(headKey, parentVersionHash);

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



            // update version object
            versionObject.Pages = pageList.ToArray();

            
            // register page objects
            foreach (var (data,hash) in newPages)
            {
                var objectKey = CreateObjectKey(owner, scoreName, hash);
                _storage.SetObjectBytes(objectKey, data);
            }


            // update version
            UpdateVersionObject(owner, scoreName, parentVersionHash, versionObject);
        }

        /// <summary>
        /// 指定したページを削除する
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <param name="scoreName">楽譜名</param>
        /// <param name="parentVersionHash">親となる Version の Hash</param>
        /// <param name="hashList">削除するページの Hash リスト</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void DeletePages(string owner, string scoreName, string parentVersionHash, string[] hashList)
        {
            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);

            if (false == _storage.ExistObject(headKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }

            CheckHeadBeforeUpdating(headKey, parentVersionHash);

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


            // update version object
            versionObject.Pages = pageList.ToArray();
            
            
            // update version
            UpdateVersionObject(owner, scoreName, parentVersionHash, versionObject);
        }

        /// <summary>
        /// 指定したページを更新する
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <param name="scoreName">楽譜名</param>
        /// <param name="parentVersionHash">親となる Version の Hash</param>
        /// <param name="pages">更新するページ</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void UpdatePages(string owner, string scoreName, string parentVersionHash, PatchScoreV2Page[] pages)
        {
            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);

            if (false == _storage.ExistObject(headKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }

            CheckHeadBeforeUpdating(headKey, parentVersionHash);

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


            // update version object
            versionObject.Pages = pageList;

            
            // register page objects
            foreach (var (_, newPageData, newPageHash) in newPageList)
            {
                var objectKey = CreateObjectKey(owner, scoreName, newPageHash);
                _storage.SetObjectBytes(objectKey, newPageData);
            }
            

            // update version
            UpdateVersionObject(owner, scoreName, parentVersionHash, versionObject);
        }

        /// <summary>
        /// 指定したページにコメントを追加する
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <param name="scoreName">楽譜名</param>
        /// <param name="parentVersionHash">親となる Version の Hash</param>
        /// <param name="comments">追加するコメント</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AddComments(string owner, string scoreName, string parentVersionHash, NewScoreV2Comment[] comments)
        {
            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);

            if (false == _storage.ExistObject(headKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }

            CheckHeadBeforeUpdating(headKey, parentVersionHash);

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

            // update version object
            versionObject.Comments = commentSet;

            
            // register page objects
            foreach (var (_, newCommentData, newCommentHash) in newComments)
            {
                var objectKey = CreateObjectKey(owner, scoreName, newCommentHash);
                _storage.SetObjectBytes(objectKey, newCommentData);
            }
            

            // update version
            UpdateVersionObject(owner, scoreName, parentVersionHash, versionObject);
        }

        /// <summary>
        /// 指定ページのコメントを削除する
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <param name="scoreName">楽譜名</param>
        /// <param name="parentVersionHash">親となる Version の Hash</param>
        /// <param name="targetPage">指定するページ</param>
        /// <param name="hashList">削除するコメントの Hash リスト</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void DeleteComments(string owner, string scoreName, string parentVersionHash,
            string targetPage, string[] hashList)
        {
            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);

            if (false == _storage.ExistObject(headKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }

            CheckHeadBeforeUpdating(headKey, parentVersionHash);

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

            // update version object
            versionObject.Comments = commentSet;
            

            // update version
            UpdateVersionObject(owner, scoreName, parentVersionHash, versionObject);
        }

        /// <summary>
        /// 指定したコメントを更新する
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <param name="scoreName">楽譜名</param>
        /// <param name="parentVersionHash">親となる Version の Hash</param>
        /// <param name="comments">更新するコメント</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void UpdateComments(string owner, string scoreName, string parentVersionHash, PatchScoreV2Comment[] comments)
        {
            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);

            if (false == _storage.ExistObject(headKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }

            CheckHeadBeforeUpdating(headKey, parentVersionHash);

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

            // update version object
            versionObject.Comments = commentSet;

            
            // register page objects
            foreach (var (_, _, newCommentData, newCommentHash) in newComments)
            {
                var objectKey = CreateObjectKey(owner, scoreName, newCommentHash);
                _storage.SetObjectBytes(objectKey, newCommentData);
            }

            
            // update version
            UpdateVersionObject(owner, scoreName, parentVersionHash, versionObject);
        }

        /// <summary>
        /// 指定した Hash のオブジェクトデータを JSON で取得する
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <param name="scoreName">楽譜名</param>
        /// <param name="hashList">Hash リスト</param>
        /// <returns>Key: Object Hash , Value: JSON</returns>
        /// <exception cref="InvalidOperationException"></exception>
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

        /// <summary>
        /// Version Ref Object を作成する
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <param name="scoreName">楽譜名</param>
        public ScoreV2Version CreateVersionRef(string owner, string scoreName)
        {
            var scoreRootKey = JoinKeys(RepositoriesRoot, owner, scoreName);

            var headKey = JoinKeys(scoreRootKey, HeadObjectName);
            if (false == _storage.ExistObject(headKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }

            var head = _storage.GetObjectString(headKey).TrimEnd('\n', '\r');

            var versionRefRootKey = JoinKeys(scoreRootKey, RefsDirectoryName, VersionRefsDirectoryName);
            var version = 0;
            if (_storage.ExistDirectory(versionRefRootKey))
            {
                var versionKeys = _storage.GetChildrenObjectNames(versionRefRootKey);

                foreach (var versionKey in versionKeys)
                {
                    var v = int.Parse(versionKey);
                    if (version < v)
                    {
                        version = v;
                    }
                }
            }

            version++;

            var versionRefKey = CreateVersionRefObjectKey(owner, scoreName, version);

            _storage.SetObjectString(versionRefKey, head);

            return new ScoreV2Version()
            {
                Hash = head,
                Version = version,
            };
        }

        /// <summary>
        /// Version Ref の一覧を取得する
        /// </summary>
        /// <param name="owner">所有者</param>
        /// <param name="scoreName">楽譜名</param>
        /// <returns></returns>
        public ScoreV2VersionSet GetVersions(string owner, string scoreName)
        {
            var scoreRootKey = JoinKeys(RepositoriesRoot, owner, scoreName);

            if (false == _storage.ExistDirectory(scoreRootKey))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' is not found.");
            }

            var versionRefRootKey = JoinKeys(scoreRootKey, RefsDirectoryName, VersionRefsDirectoryName);
            if (false == _storage.ExistDirectory(versionRefRootKey))
            {
                return new ScoreV2VersionSet();
            }

            var versionKeys= _storage.GetChildrenObjectNames(versionRefRootKey);

            var result = new ScoreV2VersionSet();
            foreach (var versionKey in versionKeys)
            {
                var version = int.Parse(versionKey);
                result[version] = _storage.GetObjectString(versionKey).TrimEnd('\n', '\r');
            }

            return result;
        }

        public void Commit(string owner, string scoreName, string parentVersionHash, CommitObject[] commitObjects)
        {

            if (string.IsNullOrWhiteSpace(owner))
                throw new ArgumentException(nameof(owner));
            if (string.IsNullOrWhiteSpace(scoreName))
                throw new ArgumentException(nameof(scoreName));
            if (string.IsNullOrWhiteSpace(parentVersionHash))
                throw new ArgumentException(nameof(parentVersionHash));
            if (commitObjects == null)
                throw new ArgumentNullException(nameof(commitObjects));
            if (commitObjects.Length == 0)
                throw new ArgumentException(nameof(commitObjects));


            // check HEAD
            var headKey = JoinKeys(RepositoriesRoot, owner, scoreName, HeadObjectName);
            var headHashFromStorage = _storage.GetObjectString(headKey).TrimEnd('\n', '\r');
            if (false == headHashFromStorage.Equals(parentVersionHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"'{parentVersionHash}' is old.");
            }


            // deserialize Version Object
            var versionObjectData = _storage.GetObjectBytes(parentVersionHash);
            var versionObjectObject = Deserialize<ScoreV2VersionObject>(versionObjectData);


            // update object
            foreach (var (commitObject, index) in commitObjects.Select((x, index) => (x, index)))
            {
                switch (commitObject.CommitType?.ToLowerInvariant())
                {
                    case AddCommentCommitObject.CommitType:
                    {
                        var addComment = commitObject.AddComment;

                        if (addComment == null)
                            throw new ArgumentException($"[{index}].{nameof(commitObject.AddComment)} == null");

                        if (string.IsNullOrWhiteSpace(addComment.Page))
                            throw new ArgumentException($"[{index}].{nameof(commitObject.AddComment)}.{addComment.Page} is empty.");


                        var commentHashSet = versionObjectObject.Comments ?? new Dictionary<string, string[]>();

                        if(false == commentHashSet.TryGetValue(addComment.Page, out var commentHashList))
                            throw new ArgumentException($"[{index}].{nameof(commitObject.AddComment)}.{addComment.Page} is not found. ({addComment.Page})");
                        var newCommentHashList = commentHashList?.ToList() ?? new List<string>();

                        var newCommentObject = new ScoreV2CommentObject()
                        {
                            CreateAt = _now,
                            Author = UserName,
                            Comment = addComment.Comment,
                        };

                        // save Comment Object
                        var newCommentData = Serialize(newCommentObject);
                        var newCommentHash = ComputeHash(CommentHashType, newCommentData);
                        var newCommentKey = CreateObjectKey(owner, scoreName, newCommentHash);
                        _storage.SetObjectBytes(newCommentKey, newCommentData);


                        // update Version Object
                        newCommentHashList.Add(newCommentKey);
                        commentHashSet[addComment.Page] = newCommentHashList.ToArray();
                        versionObjectObject.Comments = commentHashSet;

                        break;
                    }
                    case AddPageCommitObject.CommitType:
                    {
                        var addPage = commitObject.AddPage;

                        if (addPage == null)
                            throw new ArgumentException($"[{index}].{nameof(commitObject.AddPage)} == null");

                        var pageHashList = versionObjectObject.Pages?.ToList() ?? new List<string>();

                        var newPageObject = new ScoreV2PageObject()
                        {
                            CreateAt = _now,
                            Author = UserName,
                            Image = addPage.Image,
                            Number = addPage.Number,
                            Thumbnail = addPage.Thumbnail,
                        };

                        // save Page Object
                        var newPageData = Serialize(newPageObject);
                        var newPageHash = ComputeHash(PageHashType, newPageData);
                        var newPageKey = CreateObjectKey(owner, scoreName, newPageHash);
                        _storage.SetObjectBytes(newPageKey, newPageData);


                        // update Version Object
                        pageHashList.Add(newPageHash);
                        versionObjectObject.Pages = pageHashList.ToArray();

                        break;
                    }
                    case InsertCommentCommitObject.CommitType:
                    {
                        var insertComment = commitObject.InsertComment;

                        if (insertComment == null)
                            throw new ArgumentException($"[{index}].{nameof(commitObject.InsertComment)} == null");

                        if (string.IsNullOrWhiteSpace(insertComment.Page))
                            throw new ArgumentException($"[{index}].{nameof(commitObject.InsertComment)}.{insertComment.Page} is empty.");


                        var commentHashSet = versionObjectObject.Comments ?? new Dictionary<string, string[]>();

                        if (false == commentHashSet.TryGetValue(insertComment.Page, out var commentHashList))
                            throw new ArgumentException($"[{index}].{nameof(commitObject.InsertComment)}.{insertComment.Page} is not found. ({insertComment.Page})");

                        var newCommentHashList = commentHashList?.ToList() ?? new List<string>();

                        if(insertComment.Index < newCommentHashList.Count || newCommentHashList.Count < insertComment.Index)
                            throw new ArgumentException($"[{index}].{nameof(commitObject.InsertComment)}.{insertComment.Index} out of range. ({insertComment.Page})");

                        var newCommentObject = new ScoreV2CommentObject()
                        {
                            CreateAt = _now,
                            Author = UserName,
                            Comment = insertComment.Comment,
                        };

                        // save Comment Object
                        var newCommentData = Serialize(newCommentObject);
                        var newCommentHash = ComputeHash(CommentHashType, newCommentData);
                        var newCommentKey = CreateObjectKey(owner, scoreName, newCommentHash);
                        _storage.SetObjectBytes(newCommentKey, newCommentData);


                        // update Version Object
                        newCommentHashList.Insert(insertComment.Index, newCommentKey);
                        commentHashSet[insertComment.Page] = newCommentHashList.ToArray();
                        versionObjectObject.Comments = commentHashSet;

                        break;
                    }
                    case InsertPageCommitObject.CommitType:
                    {
                        var insertPage = commitObject.InsertPage;

                        if (insertPage == null)
                            throw new ArgumentException($"[{index}].{nameof(commitObject.InsertPage)} == null");


                        var pageHashList = versionObjectObject.Pages?.ToList() ?? new List<string>();

                        if (insertPage.Index < pageHashList.Count || pageHashList.Count < insertPage.Index)
                            throw new ArgumentException($"[{index}].{nameof(commitObject.InsertPage)}.{insertPage.Index} out of range.");

                        var newPageObject = new ScoreV2PageObject()
                        {
                            CreateAt = _now,
                            Author = UserName,
                            Number = insertPage.Number,
                            Image = insertPage.Image,
                            Thumbnail = insertPage.Thumbnail,
                        };

                        // save Comment Object
                        var newPageData = Serialize(newPageObject);
                        var newPageHash = ComputeHash(CommentHashType, newPageData);
                        var newPageKey = CreateObjectKey(owner, scoreName, newPageHash);
                        _storage.SetObjectBytes(newPageKey, newPageData);


                        // update Version Object
                        pageHashList.Insert(insertPage.Index, newPageKey);
                        versionObjectObject.Pages = pageHashList.ToArray();

                        break;
                    }
                    case DeleteCommentCommitObject.CommitType:
                    {
                        var deleteComment = commitObject.DeleteComment;

                        if (deleteComment == null)
                            throw new ArgumentException($"[{index}].{nameof(commitObject.DeleteComment)} == null");

                        if (string.IsNullOrWhiteSpace(deleteComment.Page))
                            throw new ArgumentException($"[{index}].{nameof(commitObject.DeleteComment)}.{deleteComment.Page} is empty.");


                        var commentHashSet = versionObjectObject.Comments ?? new Dictionary<string, string[]>();

                        if (false == commentHashSet.TryGetValue(deleteComment.Page, out var commentHashList))
                            throw new ArgumentException($"[{index}].{nameof(commitObject.DeleteComment)}.{deleteComment.Page} is not found. ({deleteComment.Page})");

                        var newCommentHashList = commentHashList?.ToList() ?? new List<string>();

                        if (deleteComment.Index < newCommentHashList.Count || newCommentHashList.Count <= deleteComment.Index)
                            throw new ArgumentException($"[{index}].{nameof(commitObject.DeleteComment)}.{deleteComment.Index} out of range. ({deleteComment.Page})");

                        
                        // update Version Object
                        newCommentHashList.RemoveAt(deleteComment.Index);
                        commentHashSet[deleteComment.Page] = newCommentHashList.ToArray();
                        versionObjectObject.Comments = commentHashSet;

                        break;
                    }
                    case DeletePageCommitObject.CommitType:
                    {
                        var deletePage = commitObject.DeletePage;

                        if (deletePage == null)
                            throw new ArgumentException($"[{index}].{nameof(commitObject.DeletePage)} == null");


                        var newPageHashList = versionObjectObject.Pages?.ToList() ?? new List<string>();
                        
                        if (deletePage.Index < newPageHashList.Count || newPageHashList.Count <= deletePage.Index)
                            throw new ArgumentException($"[{index}].{nameof(commitObject.DeletePage)}.{deletePage.Index} out of range.");


                        // update Version Object
                        newPageHashList.RemoveAt(deletePage.Index);
                        versionObjectObject.Pages = newPageHashList.ToArray();

                        break;
                    }
                    case UpdateCommentCommitObject.CommitType:
                    {
                        var updateComment = commitObject.UpdateComment;

                        if (updateComment == null)
                            throw new ArgumentException($"[{index}].{nameof(commitObject.UpdateComment)} == null");

                        if (string.IsNullOrWhiteSpace(updateComment.Page))
                            throw new ArgumentException($"[{index}].{nameof(commitObject.UpdateComment)}.{updateComment.Page} is empty.");


                        var commentHashSet = versionObjectObject.Comments ?? new Dictionary<string, string[]>();

                        if (false == commentHashSet.TryGetValue(updateComment.Page, out var commentHashList))
                            throw new ArgumentException($"[{index}].{nameof(commitObject.UpdateComment)}.{updateComment.Page} is not found. ({updateComment.Page})");

                        var newCommentHashList = commentHashList?.ToList() ?? new List<string>();

                        if (updateComment.Index < newCommentHashList.Count || newCommentHashList.Count <= updateComment.Index)
                            throw new ArgumentException($"[{index}].{nameof(commitObject.UpdateComment)}.{updateComment.Index} out of range. ({updateComment.Page})");

                        var oldCommentHash = newCommentHashList[updateComment.Index];
                        var oldCommentKey = CreateObjectKey(owner, scoreName, oldCommentHash);
                        var oldCommentData = _storage.GetObjectBytes(oldCommentKey);
                        var oldCommentObject = Deserialize<ScoreV2CommentObject>(oldCommentData);
                        var newCommentObject = new ScoreV2CommentObject()
                        {
                            CreateAt = _now,
                            Author = UserName,
                            Comment = updateComment.Comment ?? oldCommentObject.Comment,
                        };

                        // save Comment Object
                        var newCommentData = Serialize(newCommentObject);
                        var newCommentHash = ComputeHash(CommentHashType, newCommentData);
                        var newCommentKey = CreateObjectKey(owner, scoreName, newCommentHash);
                        _storage.SetObjectBytes(newCommentKey, newCommentData);


                        // update Version Object
                        newCommentHashList[updateComment.Index] = newCommentKey;
                        commentHashSet[updateComment.Page] = newCommentHashList.ToArray();
                        versionObjectObject.Comments = commentHashSet;
                        break;
                    }
                    case UpdatePageCommitObject.CommitType:
                    {
                        var updatePage = commitObject.UpdatePage;

                        if (updatePage == null)
                            throw new ArgumentException($"[{index}].{nameof(commitObject.UpdatePage)} == null");

                        var newPageHashList = versionObjectObject.Pages?.ToList() ?? new List<string>();

                        if (updatePage.Index < newPageHashList.Count || newPageHashList.Count <= updatePage.Index)
                            throw new ArgumentException($"[{index}].{nameof(commitObject.UpdatePage)}.{updatePage.Index} out of range.");

                        var oldPageHash = newPageHashList[updatePage.Index];
                        var oldPageKey = CreateObjectKey(owner, scoreName, oldPageHash);
                        var oldPageData = _storage.GetObjectBytes(oldPageKey);
                        var oldPageObject = Deserialize<ScoreV2PageObject>(oldPageData);
                        var newPageObject = new ScoreV2PageObject()
                        {
                            CreateAt = _now,
                            Author = UserName,
                            Image = updatePage.Image ?? oldPageObject.Image,
                            Number = updatePage.Number ?? oldPageObject.Number,
                            Thumbnail = updatePage.Thumbnail ?? oldPageObject.Thumbnail,
                        };

                        // save Comment Object
                        var newPageData = Serialize(newPageObject);
                        var newPageHash = ComputeHash(CommentHashType, newPageData);
                        var newPageKey = CreateObjectKey(owner, scoreName, newPageHash);
                        _storage.SetObjectBytes(newPageKey, newPageData);


                        // update Version Object
                        newPageHashList[updatePage.Index] = newPageKey;
                        versionObjectObject.Pages = newPageHashList.ToArray();

                        break;
                    }
                    case UpdatePropertyCommitObject.CommitType:
                    {
                        var property = commitObject.UpdateProperty;

                        if(property == null)
                            throw new ArgumentException($"'{nameof(commitObject.UpdateProperty)}' is null. Index '{index}'.");

                        var propertyItem = versionObjectObject.Property ?? new ScoreV2PropertyItem();

                        propertyItem.Title = property.Title ?? propertyItem.Title;
                        propertyItem.Description = property.Description ?? propertyItem.Description;

                        versionObjectObject.Property = propertyItem;
                        break;
                    }
                    default:
                    {
                        throw new InvalidOperationException($"commit type '{commitObject.CommitType}' is not found.");
                    }
                }
            }


            // update version
            UpdateVersionObject(owner, scoreName, parentVersionHash, versionObjectObject);
        }
    }
}
