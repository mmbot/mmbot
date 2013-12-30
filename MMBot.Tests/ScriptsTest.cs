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
            robot.AutoLoadScripts = false;
            await robot.Run();

            adapter.SimulateMessage("test1", "mmbot ping");

            var firstMessage = (await adapter.GetEmittedMessages(1)).Select(i => i.Item2).First();
            Assert.Equal(1, firstMessage.Count());
            Assert.Equal("pong", firstMessage.First(), StringComparer.InvariantCultureIgnoreCase);
        }

        [TestMethod]
        public async Task Auth_CanAddRemoveUsernameToRole()
        {
            var robot = Robot.Create<StubAdapter>();
            var adapter = robot.Adapters.First().Value as StubAdapter;                        
            robot.AutoLoadScripts = false;
            robot.LoadScriptName("Auth");
            await robot.Run();

            Assert.IsTrue(robot.ScriptData.Any(d => d.Name == "Auth"));

            adapter.SimulateMessage("test1", "mmbot add test1 to the testgroup role");
            adapter.SimulateMessage("test1", "mmbot remove test1 from the testgroup role");

            var messages = adapter.Messages.Select(m => m.Item2);
            Assert.AreEqual(2, messages.Count());
            Assert.IsTrue(
                "Got it, test1 is now in the testgroup role" == messages.First().First() ||
                "test1 is already in the testgroup role" == messages.First().First());
            Assert.AreEqual("Got it, test1 is no longer in the testgroup role", messages.Last().First(), true);            
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