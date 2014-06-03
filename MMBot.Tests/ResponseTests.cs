using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Common.Logging.Simple;
using Xunit;

using MMBot;

namespace MMBot.Tests
{
    public class ResponseTests
    {
        [Fact]
        public async Task WhenRandomIsCalledWithEmptyList_ADefaultIsReturned()
        {
            var response = CreateTestResponse();
            Assert.Equal(default(string), response.Random<string>(new List<string>()));
        }

        [Fact]
        public async Task WhenRandomIsCalledWithOneValue_TheValueCanBeReturned()
        {
            var response = CreateTestResponse();
            var msg = response.Random(new string[] {"One"});
            Assert.Equal("One", msg);
        }

        [Fact]
        public async Task WhenRandomIsCalled_AllValuesCanBeReturned()
        {
            var numRandoms = 50;
            var response = CreateTestResponse();
            var values = new string[] {"One", "Two"};

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
