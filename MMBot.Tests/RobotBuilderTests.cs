using Common.Logging;
using Xunit;

namespace MMBot.Tests
{
    public class RobotBuilderTests
    {
        [Fact]
        public void WhenUnconfiguredBuildConstructsNewRobot()
        {
            var builder = new RobotBuilder(new LoggerConfigurator(LogLevel.All));
            var robot = builder.Build();
            
            Assert.IsType<Robot>(robot);
        }
    }
}