namespace MMBot.Scripts
{
    public class ScriptCsScriptFile : IScript
    {
        public string Name { get; set; }

        public string DisplayName {
            get { return string.Concat(Name, ".csx"); }
        }

        public string Path { get; set; }

        protected bool Equals(ScriptCsScriptFile other)
        {
            return string.Equals(Name, other.Name) && string.Equals(Path, other.Path);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ScriptCsScriptFile)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Path != null ? Path.GetHashCode() : 0);
            }
        }

    }
}