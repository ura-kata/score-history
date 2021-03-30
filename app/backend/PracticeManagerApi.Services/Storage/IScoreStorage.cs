namespace PracticeManagerApi.Services.Storage
{
    public interface IScoreStorage
    {
        public byte[] GetObjectBytes(string key);
        public string GetObjectString(string key);

        public bool ExistObject(string key);

        public void SetObjectBytes(string key, byte[] data);
        public void SetObjectString(string key, string text);

        public bool DeleteObject(string key);

        public bool CreateDirectory(string key);
        public bool DeleteDirectory(string key);
        public bool ExistDirectory(string key);
        public string[] GetChildrenDirectoryNames(string key);
        public string[] GetChildrenObjectNames(string key);
    }
}
