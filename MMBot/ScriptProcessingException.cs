using System;

namespace MMBot
{
    public class ScriptProcessingException : Exception
    {
        public ScriptProcessingException(string message) : base(message)
        {
        }

        public ScriptProcessingException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}