using System.Collections.Generic;
using Common.Logging;
using Xunit;

namespace MMBot.Tests
{
    public class ResponseTests
    {
        [Fact]
        public void WhenRandomIsCalledWithEmptyList_ADefaultIsReturned()
        {
            var response = CreateTestResponse();
            Assert.Equal(default(string), response.Random<string>(new List<string>()));
        }

        [Fact]
        public void WhenRandomIsCalledWithOneValue_TheValueCanBeReturned()
        {
            var response = CreateTestResponse();
            var msg = response.Random(new string[] { "One" });
            Assert.Equal("One", msg);
        }

        [Fact]
        public void WhenRandomIsCalled_AllValuesCanBeReturned()
        {
            var numRandoms = 50;
            var response = CreateTestResponse();
            var values = new string[] { "One", "Two" };

            var msgs = new List<string>(numRandoms);
            for (var i = 0; i < numRandoms; i++) msgs.Add(response.Random(values));

            foreach (var value in values) Assert.Contains<string>(value, msgs);
        }

        private static Response<TextMessage> CreateTestResponse()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .UseBrain<StubBrain>()
                        .DisablePluginDiscovery()
                        .DisableScriptDiscovery()
                        .Build();
            var message = new TextMessage(new User("foo", "foo", new string[0], "testRoom", "stubAdapter"), "");
            var matchResult = new MatchResult(false, null);
            return new Response<TextMessage>(robot, message, matchResult);
        }
    }
}