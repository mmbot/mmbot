using System;

namespace MMBot
{
    public class AdapterNotConfiguredException : Exception
    {
        public AdapterNotConfiguredException() : base("You must configure the adapter first before you can use it. You can do this either via Environment variables or the config parameter. See http://github.com/mmbot/mmbot")
        {

        }
    }
}