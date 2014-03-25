using System.Collections.Generic;
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

        [Fact]
        public void WhenWithNameCalledBuildNamesNewRobotWithName()
        {
            string name = "my-shiny-robot";
            
            var builder = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                .WithName(name);

            var robot = builder.Build();

            Assert.IsType<Robot>(robot);
            Assert.Equal(name, robot.Name);
        }

        [Fact]
        public void WhenNameInConfigurationBuildNamesNewRobotFromConfiguration()
        {
            string name = "my-shiny-robot";

            var builder = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                .WithConfiguration(new Dictionary<string, string>{{"MMBOT_ROBOT_NAME", name}});

            var robot = builder.Build();

            Assert.IsType<Robot>(robot);
            Assert.Equal(name, robot.Name);
        }

        [Fact]
        public void WhenNameInConfigurationAndWithNameCalledBuildNamesNewRobotWithName()
        {
            string withName = "my-shiny-robot";
            string configuredName = "my-not-so-shint-robot";

            var builder = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                .WithName(withName)
                .WithConfiguration(new Dictionary<string, string> {{"MMBOT_ROBOT_NAME", configuredName}});

            var robot = builder.Build();

            Assert.IsType<Robot>(robot);
            Assert.Equal(withName, robot.Name);
            Assert.NotEqual(configuredName, robot.Name);
        }

        [Fact]
        public void WhenNameNotDefinedBuildNamesNewRobotWithDefaultName()
        {
            string name = "mmbot";

            var builder = new RobotBuilder(new LoggerConfigurator(LogLevel.All));
            var robot = builder.Build();

            Assert.IsType<Robot>(robot);
            Assert.Equal(name, robot.Name);
        }
    }
}