using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;
using MMBot;

namespace mmbot
{
    public class Options
    {
        
        [Option('v', "verbose", HelpText = "Display logging in verbose mode")]
        public bool Verbose { get; set; }

        [Option('n', "noconfig", HelpText = "Do not process the mmbot.ini config file if present.")]
        public bool SkipConfiguration { get; set; }

        [Option('t', "test", HelpText = "Starts a test console to evaluate the specified scripts")]
        public bool Test { get; set; }
        
        [ValueList(typeof(List<string>))]
        public IList<string> ScriptFiles { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }


        [OptionArray('p', "parameters", HelpText = "A list of configuration parameters and their values e.g. -p PARAM1=VALUE1 PARAM2=VALUE2", DefaultValue = new string[0])]
        public string[] Parameters { get; set; }



        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("Usage: mmbot [-t] [-v] [script files]");
            help.AddOptions(this);
            return string.Concat(Program.IntroText, help);

        }

    }
}