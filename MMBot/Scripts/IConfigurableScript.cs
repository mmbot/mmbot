using System.Collections.Generic;

namespace MMBot.Scripts
{
    public interface IConfigurableScript
    {
        void Configure(IDictionary<string, string> config);
    }
}