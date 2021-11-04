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
    }
}
