using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using MMBot;
using MMBot.Bootstrap;
using MMBot.Bootstrap.Interfaces;

namespace mmbot
{
    [Serializable]
    public class Bootstrapper : IBootstrapper
    {
        private AppDomain _robotAppDomain;
        private string[] _args;
        private IRobotContainer _robotContainer;

        const string BootstrapAssemblyName = "MMBot.Bootstrap, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        public Bootstrapper()
        {

        }

        public void Bootstrap(string[] args)
        {
            _args = args;
            InitializeRobotDomain();
            RunInRobotDomain();
        }

        private void DestroyRobotDomain()
        {
            try
            {
                AppDomain.Unload(_robotAppDomain);
            }
            catch (ThreadAbortException exception)
            {
                //The code running in the other AppDomain wasn't finished executing.
                //I'm pretty sure that there's nothing we can do now, we're just going to die. 
            }
            
        }

        private void InitializeRobotDomain()
        {
            //Create a new AppDomain just for the robot.
            _robotAppDomain = AppDomain.CreateDomain("MMBotDomain" + Guid.NewGuid().ToString("N"));
        }

        private void RunInRobotDomain()
        {   
            //Fire up a new robot in another AppDomain.
            _robotContainer = (IRobotContainer)_robotAppDomain.CreateInstanceAndUnwrap(BootstrapAssemblyName, "MMBot.Bootstrap.RobotContainer");
            _robotContainer.Run(_args, AppDomain.CurrentDomain, DoHardReset);
        }

        public async void DoHardReset()
        {
            Console.WriteLine("Beginning reset...waiting for shutdown to complete...");
            //Start the shutdown process for the RobotContainer.
            _robotContainer.Shutdown();

            //We don't know how long it will take for the RobotContainer to fully shut down,
            //but until it does we can't unload the AppDomain.
            //So we spin up a new task that will monitor the RobotContainer to find out when 
            //the shutdown is complete so that we can continue.
            await Task.Run(() =>
            {
                bool exitAfterNextLoop = false;
                while (true)
                {
                    Thread.Sleep(1000);

                    if (exitAfterNextLoop)
                    {
                        Console.Write("Shutdown complete...");
                        return;
                    }

                    if (!_robotContainer.IsRunning)
                    {
                        //Don't exit just yet, give it one more second to wrap up,
                        //just to be safe.
                        exitAfterNextLoop = true;
                    }
                }
            });

            //Destory the domain.
            Console.WriteLine("Unloading...");
            DestroyRobotDomain();

            //Make a new one.
            InitializeRobotDomain();

            //Run a new bot.
            Console.WriteLine("Restarting...");
            RunInRobotDomain();
        }
    }
}
