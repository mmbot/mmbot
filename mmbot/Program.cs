using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using MMBot;
using System.ServiceProcess;

namespace mmbot
{
    class Program
    {
        

        static void Main(string[] args)
        {
            Bootstrapper bootstrapper = new Bootstrapper();
            bootstrapper.Bootstrap(args);

            while (true)
            {
                // sit and spin?
                Thread.Sleep(2000);
            }       
        }
    }
}
