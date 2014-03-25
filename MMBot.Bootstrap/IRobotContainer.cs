using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMBot.Bootstrap
{
    public interface IRobotContainer
    {
        void Run(string[] args);
        IBootstrapper OwningBootstrapper { get; set; }
    }
}
