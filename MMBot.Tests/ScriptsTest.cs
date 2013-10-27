using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MMBot.CompiledScripts;

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
            var adapter = robot.Adapter as StubAdapter;
            robot.LoadScript<Ping>();

            await robot.Run();

            adapter.SimulateMessage("test1", "mmbot ping");

            var messages = adapter.Messages.Select(m => m.Item2);
            Assert.AreEqual(1, messages.Count());
            Assert.AreEqual("pong", messages.First().First(), true);
        }

    }
}