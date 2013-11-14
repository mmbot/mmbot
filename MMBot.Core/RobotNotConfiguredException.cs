using System;

namespace MMBot
{
    public class RobotNotConfiguredException : Exception
    {
        public RobotNotConfiguredException()
            : base("You must call Configure<TAdapter>() on the robot before running it.")
        {
        }
    }
}