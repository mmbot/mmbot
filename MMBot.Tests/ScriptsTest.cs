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

        [Fact]
        public async Task Auth_CanAddRemoveUsernameToRole()
        {
            var robot = Robot.Create<StubAdapter>();
            var adapter = robot.Adapters.First().Value as StubAdapter;                        
            robot.AutoLoadScripts = false;
            robot.LoadScriptName("Auth");
            await robot.Run();

            Assert.True(robot.ScriptData.Any(d => d.Name == "Auth"));

            adapter.SimulateMessage("test1", "mmbot add test1 to the testgroup role");
            adapter.SimulateMessage("test1", "mmbot remove test1 from the testgroup role");

            var messages = await adapter.GetEmittedMessages(2);
            Assert.Equal(2, messages.Count());
            Assert.True(
                "Got it, test1 is now in the testgroup role" == messages.First().Item2.First() ||
                "test1 is already in the testgroup role" == messages.First().Item2.First());
            Assert.Equal("Got it, test1 is no longer in the testgroup role", messages.Last().Item2.First(), StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public async Task CanCatchAnyMessage()
        {
            var robot = Robot.Create<StubAdapter>();
            var adapter = robot.Adapters.First().Value as StubAdapter;
            robot.LoadScript<CatchAllTest>();
            robot.AutoLoadScripts = false;
            await robot.Run();

            adapter.SimulateMessage("tester", "test message");

            var messages = adapter.Messages.Select(m => m.Item2);
            Assert.Equal(1, messages.Count());
            Assert.Equal("Caught msg test message from tester", messages.First().First(), StringComparer.InvariantCultureIgnoreCase);

            adapter.SimulateEnter("tester");

            messages = adapter.Messages.Select(m => m.Item2);
            Assert.Equal(2, messages.Count());
            Assert.Equal("Caught msg tester joined testRoom from tester", messages.Skip(1).First().First(), StringComparer.InvariantCultureIgnoreCase);

            adapter.SimulateLeave("tester");

            messages = adapter.Messages.Select(m => m.Item2);
            Assert.Equal(3, messages.Count());
            Assert.Equal("Caught msg tester left testRoom from tester", messages.Skip(2).First().First(), StringComparer.InvariantCultureIgnoreCase);

            adapter.SimulateTopic("tester", "new topic");

            messages = adapter.Messages.Select(m => m.Item2);
            Assert.Equal(4, messages.Count());
            Assert.Equal("Caught msg new topic from tester", messages.Skip(3).First().First(), StringComparer.InvariantCultureIgnoreCase);
        }

    }
}