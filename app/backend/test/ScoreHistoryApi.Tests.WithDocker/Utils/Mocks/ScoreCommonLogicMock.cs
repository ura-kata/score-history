using System;
using Moq;
using ScoreHistoryApi.Logics;

namespace ScoreHistoryApi.Tests.WithDocker.Utils.Mocks
{
    public class ScoreCommonLogicMock: Mock<IScoreCommonLogic> 
    {
        private readonly ScoreCommonLogic _logic = new();

        public DateTimeOffset DefaultNow { get; set; } = DateTimeOffset.Parse("2021-04-01T00:00:00+09:00");
        public Guid DefaultNewGuid { get; set; } = Guid.Empty;

        public ScoreCommonLogicMock()
        {
            Setup(x => x.ConvertIdFromDynamo(It.IsAny<string>()))
                .Returns((string id) => _logic.ConvertIdFromDynamo(id));
            Setup(x => x.ConvertIdFromGuid(It.IsAny<Guid>()))
                .Returns((Guid id) => _logic.ConvertIdFromGuid(id));
            Setup(x => x.Now)
                .Returns(()=>DefaultNow);
            Setup(x => x.NewGuid())
                .Returns(() => DefaultNewGuid);
        }

        public void SetupNewGuidSequential()
        {
            int i = 0;

            Setup(x => x.NewGuid())
                .Returns(() => ConvertTo(i++));
        }

        public static Guid ConvertTo(int value)
        {
            var id = value.ToString("x32")
                .Insert(20, "-")
                .Insert(16, "-")
                .Insert(12, "-")
                .Insert(8, "-");
            return new Guid(id);
        }
    }
}
