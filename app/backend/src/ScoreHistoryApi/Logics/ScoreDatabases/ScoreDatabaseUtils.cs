using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    public static class ScoreDatabaseUtils
    {
        /// <summary>
        /// UUID を Base64 エンコードで変換する
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string ConvertToBase64(Guid id) =>
            Convert.ToBase64String(id.ToByteArray());

        /// <summary>
        /// Base64 エンコードされた id を UUID に変換する
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Guid ConvertToGuid(string id) =>
            new Guid(Convert.FromBase64String(id));

        /// <summary>
        /// データベースのデータからハッシュ値を計算する
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string CalcHash(DynamoDbScoreDataV1 data)
        {
            var option = new JsonSerializerOptions()
            {
                AllowTrailingCommas = false,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
                IgnoreReadOnlyProperties = true,
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Disallow,
                WriteIndented = false,
                Encoder = JavaScriptEncoder.Default,
            };
            var json = JsonSerializer.SerializeToUtf8Bytes(data, option);
            return Convert.ToBase64String(MD5.Create().ComputeHash(json));
        }

        /// <summary>
        /// コンテンツのハッシュ値を計算する
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string CalcContentHash(string content) =>
            Convert.ToBase64String(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(content)));

        /// <summary>
        /// <see cref="DateTimeOffset"/> から Unix millisecond の16進数表記に変換する
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public static string ConvertToUnixTimeMilli(DateTimeOffset datetime) =>
            datetime.ToUnixTimeMilliseconds().ToString("X");

        /// <summary>
        /// Unix millisecond の16進数表記から <see cref="DateTimeOffset"/> に変換する
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public static DateTimeOffset ConvertFromUnixTimeMilli(string datetime) =>
            DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(datetime, NumberStyles.HexNumber, CultureInfo.InvariantCulture));

        /// <summary>
        /// 最小単位が Unix millisecond の現時間を取得する
        /// </summary>
        /// <returns></returns>
        public static DateTimeOffset UnixTimeMillisecondsNow() =>
            DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.Now.ToUnixTimeMilliseconds());

        /// <summary>
        /// アクセスを文字に変換する
        /// </summary>
        /// <param name="access"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string ConvertFromScoreAccess(ScoreAccesses access) => access switch
        {
            ScoreAccesses.Private => ScoreDatabaseConstant.ScoreAccessPrivate,
            ScoreAccesses.Public => ScoreDatabaseConstant.ScoreAccessPublic,
            _ => throw new InvalidOperationException()
        };

        /// <summary>
        /// 文字をアクセスに変換する
        /// </summary>
        /// <param name="access"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static ScoreAccesses ConvertToScoreAccess(string access) => access.ToLower(CultureInfo.InvariantCulture) switch
        {
            ScoreDatabaseConstant.ScoreAccessPrivate => ScoreAccesses.Private,
            ScoreDatabaseConstant.ScoreAccessPublic => ScoreAccesses.Public,
            _ => throw new InvalidOperationException()
        };
    }
}
