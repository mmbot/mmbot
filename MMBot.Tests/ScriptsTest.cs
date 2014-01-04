using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MMBot.Tests.CompiledScripts;

namespace MMBot.Tests
{
    [TestClass]
    public class ScriptsTest
    {
        [TestMethod]
        public void CanRegisterCompiledScripts()
        {
            var robot = Robot.Create<StubAdapter>();
            robot.LoadScripts(typeof(Ping).Assembly);
        }

        [TestMethod]
        public async Task WhenPing_ReceivePong()
        {
            var robot = Robot.Create<StubAdapter>();
            var adapter = robot.Adapters.First().Value as StubAdapter;
            robot.LoadScript<Ping>();
            robot.AutoLoadScripts = false;
            await robot.Run();

            adapter.SimulateMessage("test1", "mmbot ping");

            var messages = adapter.Messages.Select(m => m.Item2);
            Assert.AreEqual(1, messages.Count());
            Assert.AreEqual("pong", messages.First().First(), true);
        }

        #region Auth

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

        #endregion

        [TestMethod]
        public async Task CanCatchAnyMessage()
        {
            var robot = Robot.Create<StubAdapter>();
            var adapter = robot.Adapters.First().Value as StubAdapter;
            robot.LoadScript<CatchAllTest>();
            robot.AutoLoadScripts = false;
            await robot.Run();

            adapter.SimulateMessage("tester", "test message");

            var messages = adapter.Messages.Select(m => m.Item2);
            Assert.AreEqual(1, messages.Count());
            Assert.AreEqual("Caught msg test message from tester", messages.First().First(), true);

            adapter.SimulateEnter("tester");

            messages = adapter.Messages.Select(m => m.Item2);
            Assert.AreEqual(2, messages.Count());
            Assert.AreEqual("Caught msg tester joined testRoom from tester", messages.Skip(1).First().First(), true);

            adapter.SimulateLeave("tester");

            messages = adapter.Messages.Select(m => m.Item2);
            Assert.AreEqual(3, messages.Count());
            Assert.AreEqual("Caught msg tester left testRoom from tester", messages.Skip(2).First().First(), true);

            adapter.SimulateTopic("tester", "new topic");

            messages = adapter.Messages.Select(m => m.Item2);
            Assert.AreEqual(4, messages.Count());
            Assert.AreEqual("Caught msg new topic from tester", messages.Skip(3).First().First(), true);
        }

    }
}