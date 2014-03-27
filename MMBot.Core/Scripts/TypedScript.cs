using System;

namespace MMBot.Scripts
{
    public class TypedScript : IScript
    {
        public static TypedScript Create<T>() where T : IMMBotScript, new()
        {
            return Create(typeof (T));
        }

        public static TypedScript Create(Type scriptType)
        {
            return new TypedScript
            {
                Type = scriptType,
                Name = scriptType.Name
            };
        }

        protected TypedScript()
        {
        }

        public string Name { get; set; }

        public string DisplayName {
            get { return Name; }
        }

        public Type Type { get; set; }

    }
}