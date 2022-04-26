using System;

namespace Db.V1.Diagnostics
{
    public interface IUtility
    {
        /// <summary>
        /// Guid を DynamoDB に保存するために文字列に変換する
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        string ConvertIdFromGuid(Guid id);

        /// <summary>
        /// DynamoDB に保存した ID を Guid に変換する
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Guid ConvertFromDbId(string id);

        /// <summary>
        /// 現在時間
        /// </summary>
        DateTimeOffset Now { get; }

        /// <summary>
        /// 新しい Guid を作成する
        /// </summary>
        Guid NewGuid();

        /// <summary>
        /// 新しいロック文字列を作成する
        /// </summary>
        /// <returns></returns>
        string NewLock();

        /// <summary>
        /// DB に保存する時間に変換する
        /// </summary>
        /// <returns></returns>
        string ConvertToDbTime(DateTimeOffset time);

        /// <summary>
        /// DB に保存する時間から <see cref="DateTimeOffset"/> に変換する
        /// </summary>
        /// <returns></returns>
        DateTimeOffset ConvertFromDbTime(string dbTime);
    }

    /// <summary>
    /// 共通処理
    /// </summary>
    public class Utility : IUtility
    {
        /// <summary>
        /// Guid を DynamoDB に保存するために文字列に変換する
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string ConvertIdFromGuid(Guid id) => Convert.ToBase64String(id.ToByteArray()).Substring(0, 22);

        /// <summary>
        /// DynamoDB に保存した ID を Guid に変換する
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Guid ConvertFromDbId(string id) => new Guid(Convert.FromBase64String(id + "=="));

        /// <summary>
        /// 現在時間
        /// </summary>
        public DateTimeOffset Now => DateTimeOffset.Now;

        /// <summary>
        /// 新しい Guid を作成する
        /// </summary>
        /// <returns></returns>
        public Guid NewGuid() => Guid.NewGuid();

        /// <summary>
        /// 新しいロック文字列を作成する
        /// </summary>
        /// <returns></returns>
        public string NewLock()
        {
            var id = Guid.NewGuid();
            return ConvertIdFromGuid(id);
        }

        /// <summary>
        /// DB に保存する時間に変換する
        /// </summary>
        /// <returns></returns>
        public string ConvertToDbTime(DateTimeOffset time)
        {
            return time.ToUnixTimeMilliseconds().ToString();
        }

        /// <summary>
        /// DB に保存する時間から <see cref="DateTimeOffset"/> に変換する
        /// </summary>
        /// <returns></returns>
        public DateTimeOffset ConvertFromDbTime(string dbTime)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(dbTime));
        }
    }
}
