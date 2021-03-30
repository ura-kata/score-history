using System;

namespace PracticeManagerApi.Services.Providers
{
    public interface IDateTimeProvider
    {
        DateTimeOffset Now { get; }
    }
}
