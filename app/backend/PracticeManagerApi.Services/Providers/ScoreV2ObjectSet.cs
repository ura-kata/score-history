using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using PracticeManagerApi.Services.Objects;

namespace PracticeManagerApi.Services.Providers
{
    public class ScoreV2ObjectSet : Dictionary<string, string>
    {
        public string ToJson()
        {
            var sb = new StringBuilder();

            sb.Append("{");
            var list = this.ToArray();

            if (0 < list.Length)
            {
                var first = list[0];
                sb.Append('"');
                sb.Append(first.Key);
                sb.Append('"');
                sb.Append(':');
                sb.Append(first.Value);

                foreach (var (key,value) in list.Skip(1))
                {
                    sb.Append(',');
                    sb.Append('"');
                    sb.Append(key);
                    sb.Append('"');
                    sb.Append(':');
                    sb.Append(value);
                }
            }
            sb.Append("}");

            return sb.ToString();
        }
    }
}
