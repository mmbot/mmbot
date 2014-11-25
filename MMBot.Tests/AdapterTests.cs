using System;
using System.Linq;
using Common.Logging;
using MMBot.Adapters;
using MMBot.HipChat;
using MMBot.Jabbr;
using MMBot.Slack;
using MMBot.XMPP;
using Newtonsoft.Json.Bson;
using Xunit;

namespace MMBot.Tests
{
    public class AdapterTests
    {
        [Fact]
        public void WhenConfiguredWithHipChatAdapter_CanInstantiateRobot()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
            .UseAdapter<HipChatAdapter>()
            .DisablePluginDiscovery()
            .DisableScriptDiscovery()
            .Build();

            Assert.True(robot.Adapters.Values.OfType<HipChatAdapter>().Any());
        }

        [Fact]
        public void WhenConfiguredWithJabbrAdapter_CanInstantiateRobot()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
            .UseAdapter<JabbrAdapter>()
            .DisablePluginDiscovery()
            .DisableScriptDiscovery()
            .Build();

            Assert.True(robot.Adapters.Values.OfType<JabbrAdapter>().Any());
        }

        [Fact]
        public void WhenConfiguredWithSlackAdapter_CanInstantiateRobot()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
            .UseAdapter<SlackAdapter>()
            .DisablePluginDiscovery()
            .DisableScriptDiscovery()
            .Build();

            Assert.True(robot.Adapters.Values.OfType <SlackAdapter>().Any());
        }

        [Fact]
        public void WhenConfiguredWithXMPPAdapter_CanInstantiateRobot()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
            .UseAdapter<XmppAdapter>()
            .DisablePluginDiscovery()
            .DisableScriptDiscovery()
            .Build();

            Assert.True(robot.Adapters.Values.OfType<XmppAdapter>().Any());
        }

        [Fact]
        public void WhenConfiguredWithConsoleAdapter_CanInstantiateRobot()
        {
			// This stinks but the test runners in TeamCity and AppVeyor 
			// will cause this test to fail as the Console Adapter is removed
			// when not in an interactive session.
	        if (Environment.UserInteractive)
	        {
		        var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
			        .UseAdapter<ConsoleAdapter>()
			        .DisablePluginDiscovery()
			        .DisableScriptDiscovery()
			        .Build();

		        Assert.True(robot.Adapters.Values.OfType<ConsoleAdapter>().Any());
	        }
        }
    }

}