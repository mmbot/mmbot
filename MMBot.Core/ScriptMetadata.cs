using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MMBot
{
    public class ScriptMetadata
    {
        public ScriptMetadata()
        {
            Commands = new List<string>();
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Configuration { get; set; }
        public List<string> Commands { get; set; }
        public string Notes { get; set; }
        public string Author { get; set; }

    }

}
