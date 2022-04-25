using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace ScoreHistoryApi.Tests.WithFake.Utils
{
    public static class ExceptionUtils
    {
        public static void IgnoreError(Action action, ITestOutputHelper helper = default)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                helper?.WriteLine(ex.ToString());
            }
        }

        public static async Task IgnoreErrorAsync(Func<Task> action, ITestOutputHelper helper = default)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                helper?.WriteLine(ex.ToString());
            }
        }
    }
}
