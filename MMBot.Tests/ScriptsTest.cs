using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Common.Logging;
using MMBot.Brains;
using MMBot.Tests.CompiledScripts;
using Xunit;

namespace MMBot.Tests
{
    public class ScriptsTest
    {
        [Fact]
        public void CanRegisterCompiledScripts()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .DisablePluginDiscovery()
                        .DisableScriptDiscovery()
                        .Build();
            robot.LoadScripts(typeof(Ping).Assembly);
        }

        [Fact]
        public async Task WhenPing_ReceivePong()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .UseBrain<StubBrain>()
                        .DisablePluginDiscovery()
                        .DisableScriptDiscovery()
                        .Build();
            
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
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .UseBrain<StubBrain>()
                        .DisablePluginDiscovery()
                        .DisableScriptDiscovery()
                        .Build();
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
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .UseBrain<StubBrain>()
                        .DisablePluginDiscovery()
                        .DisableScriptDiscovery()
                        .Build();

            var adapter = robot.Adapters.First().Value as StubAdapter;
            robot.LoadScript<CatchAllTest>();
            robot.AutoLoadScripts = false;
            await robot.Run();

            adapter.SimulateMessage("tester", "test message");

            var messages = await adapter.GetEmittedMessages(1);
            Assert.Equal(1, messages.Count());
            Assert.Equal("Caught msg test message from tester", messages.First().Item2.First(), StringComparer.InvariantCultureIgnoreCase);

            adapter.SimulateEnter("tester");

            messages = await adapter.GetEmittedMessages(2);
            Assert.Equal(2, messages.Count());
            Assert.Equal("Caught msg tester joined testRoom from tester", messages.Skip(1).First().Item2.First(), StringComparer.InvariantCultureIgnoreCase);

            adapter.SimulateLeave("tester");

            messages = await adapter.GetEmittedMessages(3);
            Assert.Equal(3, messages.Count());
            Assert.Equal("Caught msg tester left testRoom from tester", messages.Skip(2).First().Item2.First(), StringComparer.InvariantCultureIgnoreCase);

            adapter.SimulateTopic("tester", "new topic");

            messages = await adapter.GetEmittedMessages(4);
            Assert.Equal(4, messages.Count());
            Assert.Equal("Caught msg new topic from tester", messages.Skip(3).First().Item2.First(), StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public async Task CanListen()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
            .UseAdapter<StubAdapter>()
            .UseBrain<StubBrain>()
            .WithName("mmbot")
            .DisablePluginDiscovery()
            .DisableScriptDiscovery()
            .Build();

            var adapter = robot.Adapters.First().Value as StubAdapter;
            robot.LoadScript<ListenerTest>();
            robot.LoadScript<TextListenerTest>();
            robot.AutoLoadScripts = false;
            await robot.Run();

            Assert.IsType<Listener<TextMessage>>(robot.Listeners[0]);
            Assert.IsType<Listener<TextMessage>>(robot.Listeners[1]);
            Assert.IsType<Listener<TextMessage>>(robot.Listeners[2]);
            Assert.IsType<TextListener>(robot.Listeners[3]);

            adapter.SimulateMessage("tester", "test message");

            var messages = await adapter.GetEmittedMessages(2);
            Assert.Equal(2, messages.Count());
            Assert.Equal("Handled TextMessage without regex", messages.First().Item2.First(), StringComparer.InvariantCultureIgnoreCase);
            Assert.Equal("Handled TextMessage with no other handlers", messages.Skip(1).First().Item2.First(), StringComparer.InvariantCultureIgnoreCase);

            adapter.SimulateMessage("tester", "testregex test message");

            messages = await adapter.GetEmittedMessages(5);
            Assert.Equal(5, messages.Count());
            Assert.Equal("Handled TextMessage with regex", messages.Skip(2).First().Item2.First(), StringComparer.InvariantCultureIgnoreCase);
            Assert.Equal("Handled TextMessage without regex", messages.Skip(3).First().Item2.First(), StringComparer.InvariantCultureIgnoreCase);
            Assert.Equal("Handled TextMessage with no other handlers", messages.Skip(4).First().Item2.First(), StringComparer.InvariantCultureIgnoreCase);

            adapter.SimulateMessage("tester", "mmbot gif me cat");

            messages = await adapter.GetEmittedMessages(7);
            Assert.Equal(7, messages.Count());
            Assert.Equal("Handled TextMessage without regex", messages.Skip(5).First().Item2.First(), StringComparer.InvariantCultureIgnoreCase);
            Assert.Equal("cat", messages.Skip(6).First().Item2.First(), StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public async Task WhenScriptStringIsRun_ActionRespondsToMessage()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
            .UseAdapter<StubAdapter>()
            .UseBrain<StubBrain>()
            .DisablePluginDiscovery()
            .DisableScriptDiscovery()
            .Build();

            var expectedFirst = "Bad";
            var expectedSecond = "Good";

            robot.AutoLoadScripts = false;
            var adapter = robot.Adapters.First().Value as StubAdapter;
            var scriptPath = Path.GetFullPath(Path.Combine("ScriptFiles", "ScriptRespondBad.csx"));
            robot.LoadScriptFile(scriptPath);
            robot.AutoLoadScripts = false;
            await robot.Run();

            adapter.SimulateMessage("test1", "mmbot test");

            var firstMessage = (await adapter.GetEmittedMessages(1)).Select(i => i.Item2).First();
            Assert.Equal(1, firstMessage.Count());
            Assert.Equal(expectedFirst, firstMessage.First(), StringComparer.InvariantCultureIgnoreCase);

            robot.LoadScriptFile(Path.GetFullPath(Path.Combine("ScriptFiles", "ScriptRespondGood.csx")));

            adapter.SimulateMessage("test1", "mmbot test");

            var secondMessage = (await adapter.GetEmittedMessages(10)).Select(i => i.Item2).Last();
            Assert.Equal(1, secondMessage.Count());
            Assert.Equal(expectedSecond, secondMessage.First(), StringComparer.InvariantCultureIgnoreCase);
        }
    }
}