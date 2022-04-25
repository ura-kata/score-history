using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace ScoreHistoryApi.Tests.WithFake.Utils.Extensions
{
    public static class ServiceCollectionExtensions
    {

        public static async Task InvokeAsync<T>(this ServiceCollection collection, Func<T, Task> action)
        {
            await using var provider = collection.BuildServiceProvider();
            var target = provider.GetRequiredService<T>();
            await action(target);
        }

        public static async Task InvokeAsync<T>(this ServiceCollection collection, Func<T, IServiceProvider, Task> action )
        {
            await using var provider = collection.BuildServiceProvider();
            var target = provider.GetRequiredService<T>();
            await action(target, provider);
        }
        public static async Task InvokeIgnoreErrorAsync<T>(this ServiceCollection collection, Func<T, Task> action, ITestOutputHelper helper = default)
        {
            await using var provider = collection.BuildServiceProvider();
            var target = provider.GetRequiredService<T>();

            try
            {
                await action(target);
            }
            catch (Exception ex)
            {
                helper?.WriteLine(ex.ToString());
            }
        }

        public static async Task InvokeIgnoreErrorAsync<T>(this ServiceCollection collection, Func<T, IServiceProvider, Task> action, ITestOutputHelper helper = default)
        {
            await using var provider = collection.BuildServiceProvider();
            var target = provider.GetRequiredService<T>();
            
            try
            {
                await action(target, provider);
            }
            catch (Exception ex)
            {
                helper?.WriteLine(ex.ToString());
            }
        }
    }
}
