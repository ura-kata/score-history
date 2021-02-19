using System;
using System.IO;
using System.Linq;
using PracticeManagerApi.Services.Storage;

namespace PracticeManagerApi.Mock
{
    public class ScoreFileSystemStorage : IScoreStorage
    {
        public string BaseDirectory { get; }

        public ScoreFileSystemStorage(string baseDirectory)
        {
            BaseDirectory = baseDirectory;
        }

        public byte[] GetObjectBytes(string key)
        {
            var path = Path.Join(BaseDirectory, key);

            if (false == File.Exists(path))
            {
                throw new InvalidOperationException($"'{key}' object is not found.");
            }

            try
            {
                return File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"'{key}' object read error.", ex);
            }
        }

        public string GetObjectString(string key)
        {
            var path = Path.Join(BaseDirectory, key);

            if (false == File.Exists(path))
            {
                throw new InvalidOperationException($"'{key}' object is not found.");
            }

            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"'{key}' object read error.", ex);
            }
        }

        public bool ExistObject(string key)
        {
            var path = Path.Join(BaseDirectory, key);

            return File.Exists(path);
        }

        public void SetObjectBytes(string key, byte[] data)
        {
            var path = Path.Join(BaseDirectory, key);

            try
            {
                var dir = Path.GetDirectoryName(path);
                if (false == Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllBytes(path, data);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"'{key}' object write error.", ex);
            }
        }

        public void SetObjectString(string key, string text)
        {
            var path = Path.Join(BaseDirectory, key);

            try
            {
                var dir = Path.GetDirectoryName(path);
                if (false == Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(path, text);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"'{key}' object write error.", ex);
            }
        }

        public bool DeleteObject(string key)
        {
            var path = Path.Join(BaseDirectory, key);

            if (false == File.Exists(path))
            {
                return false;
            }

            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"'{key}' object delete error.", ex);
            }
        }

        public bool CreateDirectory(string key)
        {
            var path = Path.Join(BaseDirectory, key);

            if (File.Exists(path) || Directory.Exists(path))
            {
                return false;
            }

            Directory.CreateDirectory(path);
            return true;
        }

        public bool DeleteDirectory(string key)
        {
            var path = Path.Join(BaseDirectory, key);

            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    return true;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"'{key}' object delete error.", ex);
                }
            }

            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, true);
                    return true;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"'{key}' directory delete error.", ex);
                }
            }

            return false;
        }

        public bool ExistDirectory(string key)
        {
            var path = Path.Join(BaseDirectory, key);

            return File.Exists(path) || Directory.Exists(path);
        }

        public string[] GetChildrenDirectoryNames(string key)
        {
            var path = Path.Join(BaseDirectory, key);

            if (false == Directory.Exists(path))
            {
                throw new InvalidOperationException($"'{key}' directory is not found.");
            }

            return Directory
                .GetDirectories(path)
                .Select(dir => dir.TrimEnd('/', '\\').Split('/', '\\').Last())
                .ToArray();
        }

        public string[] GetChildrenObjectNames(string key)
        {
            var path = Path.Join(BaseDirectory, key);

            if (false == Directory.Exists(path))
            {
                throw new InvalidOperationException($"'{key}' directory is not found.");
            }

            return Directory
                .GetFiles(path)
                .Select(file => file.TrimEnd('/', '\\').Split('/', '\\').Last())
                .ToArray();
        }
    }
}
