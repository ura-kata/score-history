using System;

namespace PracticeManagerApi.Services.Providers
{
    public class DateTimeProvider: IDateTimeProvider
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}
