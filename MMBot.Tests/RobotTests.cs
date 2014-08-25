using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Autofac;
using Common.Logging;
using Common.Logging.Simple;
using MMBot.Adapters;
using MMBot.Brains;
using MMBot.Scripts;
using Xunit;

using MMBot.XMPP;
using MMBot;
using System.Threading;

namespace MMBot.Tests
{
    
    public class RobotTests
    {
        [Fact]
        public void WhenConfiguredFromDictionary_GetConfigVariableReturnsValue()
        {
            var paramName = "param1";
            var paramValue = "param1Value";
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .DisablePluginDiscovery()
                        .WithConfiguration(new Dictionary<string, string>{{"param1", "param1Value"}})
                        .Build();
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
                var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                            .UseAdapter<StubAdapter>()
                            .DisablePluginDiscovery()
                            .Build();
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
                var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                            .UseAdapter<StubAdapter>()
                            .DisablePluginDiscovery()
                            .WithConfiguration(new Dictionary<string, string>{{paramName, newParamValue}})
                            .Build();
                Assert.Equal(robot.GetConfigVariable(paramName), newParamValue);
            }
        }

        [Fact]
        public void WhenInstantiatedWithoutDictionary_RobotIsConfigured()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .DisablePluginDiscovery()
                        .Build();

            Assert.Null(robot.GetConfigVariable("NothingExpected"));
        }

        [Fact]
        public async Task WhenMessageIsSentFromScript_AdapterSendIsInvoked()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .UseBrain<StubBrain>()
                        .DisablePluginDiscovery()
                        .DisableScriptDiscovery()
                        .Build();

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
        public void WhenSpeakIsCalledOnInvalidAdapterIdExceptionIsNotThrown()
        {
            // No Asserts.......argh!
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .UseBrain<StubBrain>()
                        .DisablePluginDiscovery()
                        .DisableScriptDiscovery()
                        .Build();

            robot.Speak("InvalidAdapter", "Room", "Foo");
        }

        [Fact]
        public async Task WhenRobotIsReset_ScriptCleanupIsInvoked()
        {
            var loggerConfigurator = new LoggerConfigurator(LogLevel.All);
            var builder = new RobotBuilder(loggerConfigurator)
                .UseAdapter<StubAdapter>()
                .UseBrain<StubBrain>()
                .DisablePluginDiscovery()
                .DisableScriptDiscovery();

            var scriptRunner = new ScriptRunner(loggerConfigurator.GetLogger());
            
            var robot = builder
                        .Build(c => c.Register<IScriptRunner>(scriptRunner));

            scriptRunner.Initialize(robot);

            robot.LoadScript<StubEchoScript>();

            bool isCleanedUp = false;
            using(scriptRunner.StartScriptProcessingSession(new ScriptSource("TestScript", string.Empty)))
            {
                robot.RegisterCleanup(() => isCleanedUp = true);
            }

            await robot.Reset();

            Assert.True(isCleanedUp);
        }

        [Fact]
        public async Task WhenMultipleAdaptersAreConfigured_ResponsesAreOnlySentToTheOriginatingAdapter()
        {
            var logConfig = new LoggerConfigurator(LogLevel.Trace);
            logConfig.ConfigureForConsole();
            using(var robot = new RobotBuilder(logConfig)
                        .DisablePluginDiscovery()
                        .DisableScriptDiscovery()
                        .UseAdapter<StubAdapter>()
                        .UseAdapter<StubAdapter2>()
                        .UseBrain<StubBrain>()
                        .Build()){
            
                robot.AutoLoadScripts = false;

                var adapter1 = robot.Adapters.Values.OfType<StubAdapter>().First();
                var adapter2 = robot.Adapters.Values.OfType<StubAdapter2>().First();

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
            };
        }

        public class StubAdapter2 : StubAdapter
        {
            public StubAdapter2(ILog logger, string adapterId) : base(logger, adapterId)
            {
            }
        }

        [Fact]
        public void WhenEmitInvokeOn()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .DisablePluginDiscovery()
                        .DisableScriptDiscovery()
                        .Build();
            robot.On<string>("Test", result =>
            {
                var data = result;
                Assert.NotNull(data);
                Assert.Equal(data, "Emitted");
            });
            
            robot.Emit("Test", "Emitted");
        }

        [Fact]
        public async Task WhenEmitReadyInvokeOn()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .DisablePluginDiscovery()
                        .DisableScriptDiscovery()
                        .Build();
            robot.AutoLoadScripts = false;
            bool onInvoked = false;
            robot.On<bool>("RobotReady", result =>
            {
                onInvoked = result;
            });

            await robot.Run();
            Assert.True(onInvoked);
        }

        [Fact]
        public async Task XmppRobot()
        {
            //enter config values to enable this test
            var config = new Dictionary<string, string>();         
            //config.Add("MMBOT_XMPP_HOST", "userver");
            //config.Add("MMBOT_XMPP_CONNECT_HOST", "userver");
            //config.Add("MMBOT_XMPP_USERNAME", "mmbot");
            //config.Add("MMBOT_XMPP_PASSWORD", "password");
            //config.Add("MMBOT_XMPP_CONFERENCE_SERVER", "conference.userver");
            //config.Add("MMBOT_XMPP_ROOMS", "testroom");
            //config.Add("MMBOT_XMPP_LOGROOMS", "logroom");

            if (config.Count() == 0)
                return;

            var logConfig = new LoggerConfigurator(LogLevel.Trace);
            logConfig.AddTraceListener();

            var robot = new RobotBuilder(logConfig)
                        .WithConfiguration(config)
                        .UseAdapter<XmppAdapter>()
                        .Build(); 
            
            robot.AutoLoadScripts = false;
            robot.LoadScript<CompiledScripts.Ping>();

            bool robotReady = false;
            robot.On<bool>("RobotReady", result =>
            {
                robotReady = result;
            });

            await robot.Run();

            Assert.True(robotReady);

            int cmdReceived = 0;
            robot.Hear("mmbot", msg => { cmdReceived++; });

            //will wait for two commands
            while (cmdReceived < 2)
                Thread.Sleep(1000);            
        }
    }
}

