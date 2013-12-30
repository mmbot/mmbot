using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Common.Logging;
using MMBot.Adapters;
using Xunit;

namespace MMBot.Tests
{
    
    public class RobotTests
    {
        [Fact]
        public void WhenConfiguredFromDictionary_GetConfigVariableReturnsValue()
        {
            var paramName = "param1";
            var paramValue = "param1Value";
            var robot = Robot.Create<StubAdapter>("mmbot", new Dictionary<string, string>{{"param1", "param1Value"}});
            Assert.Equal(robot.GetConfigVariable(paramName), paramValue);
        }

        [Fact]
        public void WhenConfiguredFromEnvironmentVariable_GetConfigVariableReturnsValue()
        {
            var paramName = "param1";
            var paramValue = "param1Value";
            Environment.SetEnvironmentVariable(paramName, paramValue);
            using(Disposable.Create(() => Environment.SetEnvironmentVariable(paramName, null)))
            {
                var robot = Robot.Create<StubAdapter>("mmbot", new Dictionary<string, string>());
                Assert.Equal(robot.GetConfigVariable(paramName), paramValue);
            }
        }

        [Fact]
        public void WhenConfiguredFromDictionary_EnvironmentVariableIsOverriden()
        {
            var paramName = "param1";
            var paramValue = "param1Value";
            var newParamValue = "param1Value_new";
            Environment.SetEnvironmentVariable(paramName, paramValue);
            using (Disposable.Create(() => Environment.SetEnvironmentVariable(paramName, null)))
            {
                var robot = Robot.Create<StubAdapter>("mmbot", new Dictionary<string, string>{{paramName, newParamValue}});
                Assert.Equal(robot.GetConfigVariable(paramName), newParamValue);
            }
        }

        [Fact]
        public void WhenInstantiatedWithoutDictionary_RobotIsConfigured()
        {
            var robot = Robot.Create<StubAdapter>();

            Assert.Null(robot.GetConfigVariable("NothingExpected"));
        }

        [Fact]
        public async Task WhenMessageIsSentFromScript_AdapterSendIsInvoked()
        {
            var robot = Robot.Create<StubAdapter>();
            var adapter = robot.Adapters.First().Value as StubAdapter;
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

            Assert.True(expectedMessagesValues.SequenceEqual(actualMessagesValues));
        }

        [Fact]
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

            Assert.True(isCleanedUp);
        }

        [Fact]
        public async Task WhenMultipleAdaptersAreConfigured_ResponsesAreOnlySentToTheOriginatingAdapter()
        {
            var robot = Robot.Create("mmbot", new Dictionary<string, string>(), new TestLogger(), new[]{typeof(StubAdapter), typeof(StubAdapter2)});
            robot.AutoLoadScripts = false;

            var adapter1 = robot.Adapters.First().Value as StubAdapter;
            var adapter2 = robot.Adapters.Last().Value as StubAdapter2;

            robot.LoadScript<StubEchoScript>();

            var expectedMessages = new[]
            {
                Tuple.Create("test1", "Hello Test 1"),
                Tuple.Create("test2", "Hello Test 2"),
                Tuple.Create("test3", "Hello Test 3")
            };
            await robot.Run();

            Console.WriteLine("Testing Adapter 1");
            expectedMessages.ForEach(t => adapter1.SimulateMessage(t.Item1, "mmbot " + t.Item2));

            var expectedMessagesValues = expectedMessages.Select(t => string.Concat(t.Item1, t.Item2));
            Console.WriteLine("Expected:");
            Console.WriteLine(string.Join(Environment.NewLine, expectedMessagesValues));
            var actualMessagesValues = adapter1.Messages.Select(t => string.Concat(t.Item1.User.Name, t.Item2.FirstOrDefault()));
            Console.WriteLine("Actual:");
            Console.WriteLine(string.Join(Environment.NewLine, actualMessagesValues));

            Assert.True(expectedMessagesValues.SequenceEqual(actualMessagesValues));
            Assert.Equal(0, adapter2.Messages.Count());

            Console.WriteLine("Testing Adapter 2");
            expectedMessages.ForEach(t => adapter2.SimulateMessage(t.Item1, "mmbot " + t.Item2));

            
            Console.WriteLine("Expected:");
            Console.WriteLine(string.Join(Environment.NewLine, expectedMessagesValues));
            actualMessagesValues = adapter2.Messages.Select(t => string.Concat(t.Item1.User.Name, t.Item2.FirstOrDefault()));
            Console.WriteLine("Actual:");
            Console.WriteLine(string.Join(Environment.NewLine, actualMessagesValues));

            Assert.True(expectedMessagesValues.SequenceEqual(actualMessagesValues));
            Assert.Equal(3, adapter1.Messages.Count());
        }

        public class StubAdapter2 : StubAdapter
        {
            public StubAdapter2(Robot robot, ILog logger, string adapterId) : base(robot, logger, adapterId)
            {
            }
        }

        [TestMethod]
        public void WhenEmitInvokeOn()
        {
            var robot = Robot.Create<StubAdapter>();
            robot.On<string>("Test", result =>
            {
                var data = result;
                Assert.IsNotNull(data);
                Assert.AreEqual(data, "Emitted");
            });
            
            robot.Emit("Test", "Emitted");
        }
    }
}

