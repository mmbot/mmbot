using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMBot.Bootstrap.Interfaces
{
    public interface IRobotContainer
    {
        void Run(string[] args, AppDomain parentDomain, CrossAppDomainDelegate target);
        void Shutdown();
        bool IsRunning { get; }
    }
}
