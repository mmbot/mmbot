using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Common.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MMBot.Adapters;
using MMBot.Scripts;

namespace MMBot.Tests
{
    [TestClass]
    public class RobotTests
    {
        [TestMethod]
        public void WhenConfiguredFromDictionary_GetConfigVariableReturnsValue()
        {
            var paramName = "param1";
            var paramValue = "param1Value";
            var robot = Robot.Create<StubAdapter>("mmbot", new Dictionary<string, string>{{"param1", "param1Value"}});
            Assert.AreEqual(robot.GetConfigVariable(paramName), paramValue);
        }

        [TestMethod]
        public void WhenConfiguredFromEnvironmentVariable_GetConfigVariableReturnsValue()
        {
            var paramName = "param1";
            var paramValue = "param1Value";
            Environment.SetEnvironmentVariable(paramName, paramValue);
            using(Disposable.Create(() => Environment.SetEnvironmentVariable(paramName, null)))
            {
                var robot = Robot.Create<StubAdapter>("mmbot", new Dictionary<string, string>());
                Assert.AreEqual(robot.GetConfigVariable(paramName), paramValue);
            }
        }

        [TestMethod]
        public void WhenConfiguredFromDictionary_EnvironmentVariableIsOverriden()
        {
            var paramName = "param1";
            var paramValue = "param1Value";
            var newParamValue = "param1Value_new";
            Environment.SetEnvironmentVariable(paramName, paramValue);
            using (Disposable.Create(() => Environment.SetEnvironmentVariable(paramName, null)))
            {
                var robot = Robot.Create<StubAdapter>("mmbot", new Dictionary<string, string>{{paramName, newParamValue}});
                Assert.AreEqual(robot.GetConfigVariable(paramName), newParamValue);
            }
        }

        [TestMethod]
        public void WhenInstantiatedWithoutDictionary_RobotIsConfigured()
        {
            var robot = Robot.Create<StubAdapter>();

            Assert.IsNull(robot.GetConfigVariable("NothingExpected"));
        }

        [TestMethod]
        public async Task WhenMessageIsSentFromScript_AdapterSendIsInvoked()
        {
            var robot = Robot.Create<StubAdapter>();
            var adapter = robot.Adapter as StubAdapter;
            robot.LoadScript<StubEchoScript>();
            
            var expectedMessages = new[]
            {
                Tuple.Create("test1", "Hello Test 1"),
                Tuple.Create("test2", "Hello Test 2"),
                Tuple.Create("test3", "Hello Test 3")
            };
            await robot.Run();
            expectedMessages.ForEach(t => adapter.SimulateMessage(t.Item1, "mmbot " +  t.Item2));

            var expectedMessagesValues = expectedMessages.Select(t => string.Concat(t.Item1, t.Item2));
            Console.WriteLine("Expected:");
            Console.WriteLine(string.Join(Environment.NewLine, expectedMessagesValues));
            var actualMessagesValues = adapter.Messages.Select(t => string.Concat(t.Item1.User.Name, t.Item2.FirstOrDefault()));
            Console.WriteLine("Actual:");
            Console.WriteLine(string.Join(Environment.NewLine, actualMessagesValues));

            Assert.IsTrue(expectedMessagesValues.SequenceEqual(actualMessagesValues));
        }

        [TestMethod]
        public async Task WhenRobotIsReset_ScriptCleanupIsInvoked()
        {
            var robot = Robot.Create<StubAdapter>();
            robot.LoadScript<StubEchoScript>();

            bool isCleanedUp = false;
            using(robot.StartScriptProcessingSession(new ScriptSource("TestScript", string.Empty)))
            {
                robot.RegisterCleanup(() => isCleanedUp = true);
            }

            await robot.Reset();

            Assert.IsTrue(isCleanedUp);
        }
    }

    public class StubAdapter : Adapter
    {
        private readonly List<Tuple<Envelope, string[]>> _messages = new List<Tuple<Envelope, string[]>>();

        public StubAdapter(Robot robot, ILog logger) : base(robot, logger)
        {
        }

        public IEnumerable<Tuple<Envelope, string[]>> Messages
        {
            get { return _messages; }
        }

        public override async Task Run()
        {
            
        }

        public override async Task Close()
        {
            
        }

        public void SimulateMessage(string user, string message)
        {
            Robot.Receive(new TextMessage(new User(user), message, null));
        }

        public override Task Send(Envelope envelope, params string[] messages)
        {
            _messages.Add(Tuple.Create(envelope, messages));
            return base.Send(envelope, messages);
        }
    }

    public class StubEchoScript : IMMBotScript
    {

        public void Register(Robot robot)
        {
            robot.Respond("(.*)", msg => msg.Send(msg.Match[1]));
        }

        public IEnumerable<string> GetHelp()
        {
            return new[] {"type anything"};
        }
    }
}

