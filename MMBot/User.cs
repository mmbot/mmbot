using System.Collections.Generic;

namespace MMBot
{
    public class User
    {
        
        public User(string id, IEnumerable<string> roles)
        {
            Id = id;
            Roles = roles ?? new string[0];
        }

        public string Id { get; private set; }

        public IEnumerable<string> Roles { get; private set; }
    }
}