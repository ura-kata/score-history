using System.IO;

namespace ScoreHistoryApi.Tests.WithFake
{
    public static class ResourceUtils
    {
        public static Stream CreateResourceStream(string relativeResourceName)
        {
            var resourceName = "ScoreHistoryApi.Tests.WithFake." + relativeResourceName;
            return typeof(ResourceUtils).Assembly.GetManifestResourceStream(resourceName);
        }
    }
}
