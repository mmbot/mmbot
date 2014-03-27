using System.Collections.Generic;

namespace MMBot.Scripts
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
