using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MMBot.Bootstrap.Interfaces
{
    public static class ThreadDebugHelper
    {
        public static void PrintCurrentDomain(string message)
        {
            Console.WriteLine(string.Format("{0}: ", AppDomain.CurrentDomain.FriendlyName));
        }

        public static void PrintCurrentDomainAssemblies(string message)
        {
            Console.WriteLine("=== {0} Loaded Assemblies ===");

            foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Console.WriteLine(assembly.FullName);
            }
        }
    }
}
