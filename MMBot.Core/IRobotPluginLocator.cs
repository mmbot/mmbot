using System;
using System.Collections;
using System.Collections.Generic;
using MMBot.Scripts;

namespace MMBot
{
    public interface IRobotPluginLocator
    {
        Type[] GetAdapters();

        Type GetBrain(string name);

        Type GetRouter(string name);

        IEnumerable<IScript> GetPluginScripts();
    }
}