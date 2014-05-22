using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            userName = robot.GetUserNameByAlias(userName).ToLower();
            var roleStore = robot.Brain.Get<Dictionary<string, string>>("UserRoleStore").Result ?? new Dictionary<string, string>();
            return roleStore.ContainsKey(userName) ? roleStore[userName].Split(',') : new string[0];
        }

        public static void AddUserToRole(this Robot robot, string userName, string role)
        {
            AddUserToRole(robot, userName, new[] { role });
        }

        public static void AddUserToRole(this Robot robot, string userName,  string[] roles)
        {
            userName = robot.GetUserNameByAlias(userName).ToLower();
            var roleStore = robot.Brain.Get<Dictionary<string, string>>("UserRoleStore").Result ?? new Dictionary<string, string>();
            var newRoles = (roleStore.ContainsKey(userName) ? roleStore[userName].Split(',') : new string[0])
                .Union(roles)
                .Distinct()
                .Select(d => d.Replace(",", ""));
            roleStore[userName] = string.Join(",", newRoles);
            robot.Brain.Set("UserRoleStore", roleStore).Wait();
        }

        public static void RemoveUserFromRole(this Robot robot, string userName, string role)
        {
            userName = robot.GetUserNameByAlias(userName).ToLower();
            var roleStore = robot.Brain.Get<Dictionary<string, string>>("UserRoleStore").Result ?? new Dictionary<string, string>();
            var roles = (roleStore.ContainsKey(userName) ? roleStore[userName].Split(',') : new string[0]);
            
            if (roles.Length > 0 && roles.Any(d => d.Equals(role, System.StringComparison.CurrentCultureIgnoreCase)))
            {
                var newRoles = roles.Where(d => !d.Equals(role, System.StringComparison.CurrentCultureIgnoreCase)).ToArray();

                if (newRoles.Any())
                {
                    roleStore[userName] = string.Join(",", newRoles);
                }
                else
                {
                    roleStore.Remove(userName);
                }

                robot.Brain.Set("UserRoleStore", roleStore).Wait();
            }
        }

        public static bool IsInRole(this User user, string role)
        {
            return user.Roles.Any(d => d.Equals(role, System.StringComparison.InvariantCultureIgnoreCase));
        }

        public static bool IsInRole(this Robot robot, string userName, string role)
        {
            userName = userName.ToLower();
            return robot.GetUserRoles(userName).Any(d => d.Equals(role, System.StringComparison.CurrentCultureIgnoreCase));
        }

        public static bool IsAdmin(this User user, Robot robot)
        {
            return robot.IsAdmin(robot.GetUserNameByAlias(user.Name));
        }

        public static bool IsAdmin(this Robot robot, string userName)
        {
            return robot.Admins.Any(d => d.Equals(robot.GetUserNameByAlias(userName), StringComparison.InvariantCultureIgnoreCase));
        }


        public static string[] GetUserAliases(this Robot robot, string userNameOrAlias)
        {
            var userName = robot.GetUserNameByAlias(userNameOrAlias);
            var allUserAliases = robot.GetAllUserAliases();
            if (!allUserAliases.ContainsKey(userName))
            {
                return new string[0];
            }
            return (allUserAliases[userName] ?? new Collection<string>()).Concat(new[] { userName }).Except(new[] { userNameOrAlias }).Distinct().ToArray();
        }

        private static string GetUserNameByAlias(this Robot robot, string alias)
        {
            return robot
                .GetAllUserAliases()
                .Where(kvp => string.Equals(kvp.Key, alias, StringComparison.InvariantCultureIgnoreCase) 
                    || kvp.Value.Contains(alias, StringComparer.InvariantCultureIgnoreCase))
                .Select(kvp => kvp.Key)
                .FirstOrDefault() ?? alias;
        }

        public static void RegisterAliasForUser(this Robot robot, string userName, string alias)
        {
            var aliases = robot.GetAllUserAliases();
            var key = robot.GetUserNameByAlias(userName);
            if (key != null)
            {
                if (string.Equals(key, alias, StringComparison.InvariantCultureIgnoreCase) ||
                    (aliases.ContainsKey(key) && aliases[key].Contains(alias, StringComparer.InvariantCultureIgnoreCase)))
                {
                    return;
                }
                
                var newAliases = aliases.ContainsKey(key) ? aliases[key] : new Collection<string>();
                newAliases.Add(alias);
                aliases[key] = newAliases;
            }
            else
            {
                aliases.Add(userName, new Collection<string>(new []{alias}));
            }
            robot.SaveUserAliasStore(aliases);
        }

        public static Dictionary<string, ICollection<string>> GetAllUserAliases(this Robot robot)
        {
            return robot.Brain.Get<Dictionary<string, ICollection<string>>>("UserAliasStore").Result ?? new Dictionary<string, ICollection<string>>();
        }

        public static void SaveUserAliasStore(this Robot robot, Dictionary<string, ICollection<string>> aliases)
        {
            robot.Brain.Set("UserAliasStore", aliases).Wait();
        }
    }
}