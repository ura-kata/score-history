using System.IO;

namespace ScoreHistoryApi.Tests.WithAws
{
    public static class ResourceUtils
    {
        public static Stream CreateResourceStream(string relativeResourceName)
        {
            var resourceName = "ScoreHistoryApi.Tests.WithAws." + relativeResourceName;
            return typeof(ResourceUtils).Assembly.GetManifestResourceStream(resourceName);
        }
    }
}
