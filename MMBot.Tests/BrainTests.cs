using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MMBot.Tests
{
    [TestClass]
    public class BrainTests
    {
        [TestMethod]
        public async Task WhenValueIsAddedToBrain_CanBeRetrievedViaGet()
        {
            var robot = Robot.Create<StubAdapter>();
            var key = "test1";
            var value = "value1";
            await robot.Brain.Set(key, value);

            Assert.AreEqual(value, await robot.Brain.Get<string>(key));
        }

        [TestMethod]
        public async Task WhenValueIsRemovedToBrain_GetReturnsDefault()
        {
            var robot = Robot.Create<StubAdapter>();
            var key = "test1";
            var value = "value1";
            await robot.Brain.Set(key, value);
            await robot.Brain.Remove<string>(key);
            Assert.IsNull(await robot.Brain.Get<string>(key));
        }
    }
}