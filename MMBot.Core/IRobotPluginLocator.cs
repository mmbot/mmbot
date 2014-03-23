using System;

namespace MMBot
{
    public interface IRobotPluginLocator
    {
        Type[] GetAdapters();

        Type GetBrain(string name);

        Type GetRouter(string name);
    }
}