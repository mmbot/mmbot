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
        private AppDomain RobotAppDomain;
        private string[] Args;
        private IRobotContainer _robotContainer;
        private List<AppDomain> _retiredDomains = new List<AppDomain>(); 

        public Bootstrapper()
        {

        }

        public void Bootstrap(string[] args)
        {
            Args = args;
            InitializeRobotDomain();
            RunInRobotDomain();
        }

        private async Task DestroyRobotDomain()
        {
            PrintCurrentDomain("DestroyRobotDomain");
            try
            {
                Task.Run(() =>
                {

                });
                Console.WriteLine("---> Unloading AppDomain");
                _retiredDomains.Add(RobotAppDomain);
                //await _robotContainer.Shutdown();
                AppDomain.Unload(RobotAppDomain);
                Console.WriteLine("---> AppDomain Unloaded");
            }
            catch (CannotUnloadAppDomainException e)
            {
                int i = 0;
                i++;
            }
            catch (AppDomainUnloadedException e2)
            {
                int i = 0;
                i++;
            }
            catch (ThreadAbortException e3)
            {
                Console.WriteLine(e3.StackTrace);
                Thread.ResetAbort();
            }
            
        }

        private int initCount = 0;

        private void InitializeRobotDomain()
        {
            initCount++;
            RobotAppDomain = AppDomain.CreateDomain("MMBotDomain" + initCount);
        }

        private void RunInRobotDomain()
        {
            PrintCurrentDomain("RunInRobotDomain");
            var assemblyName = "MMBot.Bootstrap, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
            _robotContainer = (IRobotContainer)RobotAppDomain.CreateInstanceAndUnwrap(assemblyName, "MMBot.Bootstrap.RobotContainer");
            _robotContainer.Run(Args, AppDomain.CurrentDomain, DoHardReset);
        }

        public async void DoHardReset()
        {
            PrintCurrentDomain("DoHardReset");
            _robotContainer.Shutdown();
            await Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(3000);
                    if (!_robotContainer.IsRunning)
                    {
                        return;
                    }
                }
            });
            await DestroyRobotDomain();
            InitializeRobotDomain();
            RunInRobotDomain();
        }

        private void PrintCurrentDomain(string name)
        {
            Console.WriteLine(string.Format("{0}: {1}", name, AppDomain.CurrentDomain.FriendlyName));
        }
    }
}
