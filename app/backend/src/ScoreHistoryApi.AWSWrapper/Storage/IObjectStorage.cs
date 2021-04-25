namespace ScoreHistoryApi.AWSWrapper.Storage
{
    public interface IObjectStorage
    {
        /// <summary>
        /// オブジェクトを保存する
        /// </summary>
        /// <param name="path">object のパス</param>
        /// <param name="data"></param>
        /// <returns></returns>
        void SaveObject(string path, byte[] data);

        /// <summary>
        /// 指定したパスのオブジェクトデータを削除する
        /// </summary>
        /// <param name="path">object のパス</param>
        void DeleteObject(string path);
    }
}
