using System.Collections.Generic;
using System.Linq;

namespace MMBot
{
    public class User
    {
        internal User()
        {

        }
        
        public User(string id, string name, IEnumerable<string> roles, string room, string adapterId)
        {
            Id = id;
            Roles = roles ?? new string[0];
            Name = name;
            Room = room;
            AdapterId = adapterId;
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public IEnumerable<string> Roles { get; set; }

        public string Room { get; private set; }

        public string AdapterId { get; set; }

    }

    public static class UserExtensions
    {

        public static User GetUser(this Robot robot, string id, string name, string room, string adapterId)
        {
            return new User(id, name, robot.GetUserRoles(name), room, adapterId);            
        }

        public static string[] GetUserRoles(this Robot robot, string userName)
        {
            userName = userName.ToLower();
            var roleStore = robot.Brain.Get<Dictionary<string, string>>("UserRoleStore").Result ?? new Dictionary<string, string>();
            return roleStore.ContainsKey(userName) ? roleStore[userName].Split(',') : new string[0];
        }

        public static void AddUserToRole(this Robot robot, string userName, string role)
        {
            AddUserToRole(robot, userName, new string[] { role });
        }

        public static void AddUserToRole(this Robot robot, string userName,  string[] roles)
        {
            userName = userName.ToLower();
            var roleStore = robot.Brain.Get<Dictionary<string, string>>("UserRoleStore").Result ?? new Dictionary<string, string>();
            var newRoles = (roleStore.ContainsKey(userName) ? roleStore[userName].Split(',') : new string[0])
                .Union(roles)
                .Distinct()
                .Select(d => d.Replace(",", ""));
            roleStore[userName] = string.Join(",", newRoles);
            robot.Brain.Set("UserRoleStore", roleStore);
        }

        public static void RemoveUserFromRole(this Robot robot, string userName, string role)
        {
            userName = userName.ToLower();
            var roleStore = robot.Brain.Get<Dictionary<string, string>>("UserRoleStore").Result ?? new Dictionary<string, string>();
            
            var roles = (roleStore.ContainsKey(userName) ? roleStore[userName].Split(',') : new string[0]);
            if (roles.Length > 0 && roles.Any(d => d.Equals(role, System.StringComparison.CurrentCultureIgnoreCase)))
            {
                roleStore[userName] = string.Join(",", roles.Where(d => d.Equals(role, System.StringComparison.CurrentCultureIgnoreCase)));
                robot.Brain.Set("UserRoleStore", roleStore);
            }
        }

        public static bool IsInRole(this User user, string role)
        {
            return user.Roles.Any(d => d.Equals(role, System.StringComparison.InvariantCultureIgnoreCase));
        }

        public static bool IsInRole(this Robot robot, string userName, string role)
        {
            userName = userName.ToLower();
            var roleStore = robot.Brain.Get<Dictionary<string, string>>("UserRoleStore").Result ?? new Dictionary<string, string>();
            return robot.GetUserRoles(userName).Any(d => d.Equals(role, System.StringComparison.CurrentCultureIgnoreCase));
        }

        public static bool IsAdmin(this User user, Robot robot)
        {
            return robot.IsAdmin(user.Name);
        }

        public static bool IsAdmin(this Robot robot, string userName)
        {
            return robot.Admins.Any(d => d.Equals(userName, System.StringComparison.InvariantCultureIgnoreCase));
        }
    }
}