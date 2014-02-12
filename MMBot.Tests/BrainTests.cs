using System.Threading.Tasks;
using MMBot.Brains;
using Xunit;

namespace MMBot.Tests
{

    public class BrainTests
    {
        [Fact]
        public async Task WhenValueIsAddedToBrain_CanBeRetrievedViaGet()
        {
            var robot = Robot.Create<StubAdapter>();
            robot.ConfigureBrain(typeof(StubBrain));
            var key = "test1";
            var value = "value1";
            await robot.Brain.Set(key, value);

            Assert.Equal(value, await robot.Brain.Get<string>(key));
        }

        [Fact]
        public async Task WhenValueIsRemovedToBrain_GetReturnsDefault()
        {
            var robot = Robot.Create<StubAdapter>();
            robot.ConfigureBrain(typeof(StubBrain));
            var key = "test1";
            var value = "value1";
            await robot.Brain.Set(key, value);
            await robot.Brain.Remove<string>(key);
            Assert.Null(await robot.Brain.Get<string>(key));
        }
    }
}