namespace MMBot
{
    public class ScriptSource
    {
        public ScriptSource(string name, string description)
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// This is the unique name for the script source. Scripts will overwrite previous entries with the same name.
        /// </summary>
        public string Name { get; set; }
        
        public string Description { get; set; }
    }
}