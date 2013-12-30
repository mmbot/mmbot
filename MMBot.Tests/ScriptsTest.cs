using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MMBot.Tests.CompiledScripts;
using Xunit;

namespace MMBot.Tests
{
    public class ScriptsTest
    {
        [Fact]
        public void CanRegisterCompiledScripts()
        {
            var robot = Robot.Create<StubAdapter>();
            robot.LoadScripts(typeof(Ping).Assembly);
        }

        [Fact]
        public async Task WhenPing_ReceivePong()
        {
            var robot = Robot.Create<StubAdapter>();
            robot.AutoLoadScripts = false;
            var adapter = robot.Adapters.First().Value as StubAdapter;
            robot.LoadScript<Ping>();

            await robot.Run();

            adapter.SimulateMessage("test1", "mmbot ping");

            var firstMessage = (await adapter.GetEmittedMessages(1)).Select(i => i.Item2).First();
            Assert.Equal(1, firstMessage.Count());
            Assert.Equal("pong", firstMessage.First(), StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public async Task TestReplaySubject()
        {
            var subject = new ReplaySubject<string>();
            subject.OnNext("foo");

            var foo = await subject.FirstAsync();

            Assert.Equal("foo", foo);
        }
    }
}