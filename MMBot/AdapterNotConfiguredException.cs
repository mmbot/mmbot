using System;

namespace MMBot
{
    public class AdapterNotConfiguredException : Exception
    {
        public AdapterNotConfiguredException() : base("You must configure the adapter first before you can use it")
        {

        }
    }
}