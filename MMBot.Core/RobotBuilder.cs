using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Logging;
using MMBot.Router;

namespace MMBot
{
    public class RobotBuilder
    {
        public RobotBuilder(ILog logger)
        {

        }

        public RobotBuilder(LoggerConfigurator logConfig)
        {

        }
        
        public Robot Build()
        {
            throw new NotImplementedException();
        }
    }

    public class RobotServicesBuilder
    {
        public RobotServicesBuilder(IEnumerable<Type> adapterTypes, Type routerType, Type brainType)
        {
        }
    }

    public class RobotServices
    {
        public IRouter Router { get; set; }

        public IDictionary<string, Adapter> Adapters { get; set; }

        //public IBrain Brain { get; set; }

    }

    public interface IRobotConfiguration
    {
        string Get(string key);
    }

    // This may cause issues in the way we deal with scriptcs. 
    // It wants a script file or a script text, not sure of the implications of each right now
    public interface IScriptStore
    {
        Task SaveScriptAsync();

    }

    public interface IFileSystem
    {

    }
}