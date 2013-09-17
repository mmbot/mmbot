using System.Collections.Generic;

namespace MMBot
{
    public class User
    {
        
        public User(string id, string name, IEnumerable<string> roles, string room)
        {
            Id = id;
            Roles = roles ?? new string[0];
            Name = name;
            Room = room;
        }

        public string Id { get; private set; }

        public IEnumerable<string> Roles { get; private set; }

        public string Name { get; private set; }
        public string Room { get; private set; }
    }
}