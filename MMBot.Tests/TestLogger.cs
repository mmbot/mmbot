using Common.Logging;
using Common.Logging.Simple;

namespace MMBot.Tests
{
    public class TestLogger : ConsoleOutLogger
    {
        public TestLogger() : base("test logger", LogLevel.All, true, true, false, null)
        {

        }
    }
}