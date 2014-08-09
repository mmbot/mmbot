using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NuGet;

namespace MMBot.Scripts
{
    class NuGetScripts : IMMBotScript
    {
        private const string NuGetRepositoriesSetting = "MMBOT_NUGET_REPOS";
        private const string NuGetPackageAliasesSetting = "MMBOT_NUGET_PACKAGE_ALIASES";
        private const string NuGetResetAfterUpdateSetting = "MMBOT_NUGET_RESET";

        const string Add = "add|remember";
        const string Remove = "remove|delete|del|rem|forget";
        const string Package = "pkg|package";
        const string Source = "source|src|sources";
        const string Alias = "alias|aliases";
        const string List = "list";
        const string ParamWithNoSpaces = @"[^\s]+";
        const string ParamInQuotes = @"""[^""]+""";
        const string Update = "update";
        const string Restart = "restart";

        #region Package Sources
        private void RememberConfiguredSources(Robot robot)
        {
	        var configuredSources = robot.GetConfigVariable(NuGetRepositoriesSetting) ?? string.Empty;
	        foreach(var source in configuredSources.Split(','))
	        {
		        AddSource(source, robot);
	        }
        }

        private List<string> GetRememberedSources(Robot robot)
        {
	        var sources = robot.Brain.Get<List<string>>(NuGetRepositoriesSetting).Result;
	        if(sources == null)
	        {
		        sources = new List<string>();
		        Remember(NuGetRepositoriesSetting, sources, robot);
	        }

	        return sources;
        }

        private void Remember(string key, object value, Robot robot)
        {
	        robot.Brain.Set<object>(key, value);
        }

        private bool AddSource(string source, Robot robot)
        {
	        var sources = GetRememberedSources(robot);
	        if (sources.Contains(source))
	        {
		        return false;
	        }
	        else
	        {
		        sources.Add(source);
		        Remember(NuGetRepositoriesSetting, sources, robot);
		        return true;
	        }
        }

        private bool RemoveSource(string source, Robot robot)
        {
	        var sources = GetRememberedSources(robot);
	        if (sources.Contains(source))
	        {
		        sources.Remove(source);
		        Remember(NuGetRepositoriesSetting, sources, robot);
		        return true;
	        }
	        else
	        {
		        return false;
	        }
        }
        #endregion

        #region Package Aliases
        private void RememberConfiguredAliases(Robot robot)
        {
	        var configuredAliases = robot.GetConfigVariable(NuGetPackageAliasesSetting) ?? string.Empty;
            foreach (var alias in configuredAliases.Split(new []{','}, StringSplitOptions.RemoveEmptyEntries))
            {
                AddAlias(alias, robot);
            }
        }

        private Dictionary<string,string> GetRememberedAliases(Robot robot)
        {
	        var aliases = robot.Brain.Get<Dictionary<string,string>>(NuGetPackageAliasesSetting).Result;
	        if(aliases == null)
	        {
		        aliases = new Dictionary<string,string>();
		        Remember(NuGetPackageAliasesSetting, aliases, robot);
	        }
	        return aliases;
        }

        private void AddAlias(string alias, Robot robot)
        {
	        var aliases = GetRememberedAliases(robot);
	        var parts = alias.Split('=');
	
	        alias = parts[0].ToLower();
	        var packageName = parts[1];
	
	        aliases[alias] = packageName;

	        Remember(NuGetPackageAliasesSetting, aliases, robot);
        }

        private void RemoveAlias(string alias, Robot robot)
        {
	        var aliases = GetRememberedAliases(robot);
	        alias = alias.Split(',')[0];
	        aliases.Remove(alias);
	        Remember(NuGetPackageAliasesSetting, aliases, robot);
        }
        #endregion

        #region Auto-Reset
        private void RememberConfiguredAutoReset(Robot robot)
        {
            var autoReset = robot.GetConfigVariable(NuGetResetAfterUpdateSetting) ?? string.Empty;
            bool autoResetValue;
            if (!bool.TryParse(autoReset, out autoResetValue))
            {
                autoResetValue = false;
            }
            Remember(NuGetResetAfterUpdateSetting, autoResetValue, robot);
        }

        private bool ShouldAutoResetAfterUpdate(Robot robot)
        {
            return bool.Parse(robot.Brain.Get<string>(NuGetResetAfterUpdateSetting).Result);
        }
        #endregion

        private string BuildCommand(string[] parts, IEnumerable<int> optionalParams = null)
        {
	        bool[] optional = new bool[parts.Count()];
	        optionalParams = optionalParams ?? new int[0];
	        foreach(var optionalParam in optionalParams)
	        {
		        optional[optionalParam] = true;
	        }

	        var sb = new StringBuilder();
	        for(var i = 0; i < parts.Length; i++)
	        {
		        if(i > 0)
		        {
			        sb.Append(string.Format(@"\s{0}", optional[i] ? "*" : string.Empty));
		        }
		        sb.Append(string.Format("({0}){1}", parts[i], optional[i] ? "?" : string.Empty));
	        }
	        return sb.ToString();
        }

        private AggregateRepository BuildPackagesRepository(Robot robot)
        {
            var packageSources = GetRememberedSources(robot).Where(s => !string.IsNullOrWhiteSpace(s));
	        return new AggregateRepository(packageSources
		        .Select(s => PackageRepositoryFactory.Default.CreateRepository(s)));

        }

        private string GetPackagesPath()
        {
	        return Path.Combine(Directory.GetCurrentDirectory(), "packages");
        }

        public void Register(Robot robot)
        {
            RememberConfiguredSources(robot);
            RememberConfiguredAliases(robot);
            RememberConfiguredAutoReset(robot);

            robot.Respond(BuildCommand(new[] {List, Package, Source}), msg =>
            {
                msg.Send(GetRememberedSources(robot).ToArray());
            });

            robot.Respond(BuildCommand(new []{Add, Package, Source, ParamWithNoSpaces}), msg =>
            {
	            var source = msg.Match[4].ToString();
	            if (!AddSource(source, robot))
	            {
		            msg.Send("I already know about this one.");
	            }
	            else
	            {
		            msg.Send("Consider it done.");
	            }
            });

            robot.Respond(BuildCommand(new []{Remove, Package, Source, ParamWithNoSpaces}), msg =>
            {
	            var source = msg.Match[4].ToString();
	            if (RemoveSource(source, robot))
	            {
		            msg.Send("I'll forget it immediately.");
	            }
	            else
	            {
		            msg.Send("It's easy to forget what you never knew.");
	            }
            });

            robot.Respond(BuildCommand(new[] { Update, ParamWithNoSpaces, Restart}, new[] {2}), msg =>
            {
	            //ID of the package to be looked up
	            string packageId = msg.Match[2].ToString();
	            string unaliasedPackageId;
	
	            var knownAliases = GetRememberedAliases(robot);
	            if(!knownAliases.TryGetValue(packageId.ToLower(), out unaliasedPackageId))
	            {
		            unaliasedPackageId = packageId;
	            }

	            msg.Send("Building repositories...");
	            IPackageRepository repo = BuildPackagesRepository(robot);

	            //Get the list of all NuGet packages with ID 'EntityFramework'   
	            msg.Send("Finding package...");
	            List<IPackage> packages = repo.FindPackagesById(unaliasedPackageId).ToList();

	            if (packages.Any())
	            {
		            msg.Send("Found it! Downloading...");
	            }
	            else
	            {
		            msg.Send("I couldn't find it...sorry!");
		            return;
	            }

	            //Initialize the package manager
	            string path = GetPackagesPath();
	            PackageManager packageManager = new PackageManager(repo, path);

	            //Download and unzip the package
	            packageManager.InstallPackage(unaliasedPackageId);
	            msg.Send("Finished downloading...");

                if (ShouldAutoResetAfterUpdate(robot) || msg.Match.Length == 4)
                {
                    //They submitted the reset parameter or auto-reset is on.
                    msg.Send("Resetting...please wait.");
                    robot.Reset();
                }
            });

            robot.Respond(BuildCommand(new []{List, Package, Alias}), msg => {
	            msg.Send(GetRememberedAliases(robot).Select(kvp => string.Format("{0} = {1}", kvp.Key, kvp.Value)).ToArray());
            });

            robot.Respond(BuildCommand(new []{Add, Package, Alias, ParamWithNoSpaces}), msg =>
            {
	            var alias = msg.Match[4].ToString();
	            AddAlias(alias, robot);
	            msg.Send("I'll be sure to remember that.");
            });

            robot.Respond(BuildCommand(new []{Remove, Package, Alias, ParamWithNoSpaces}), msg =>
            {
	            var alias = msg.Match[4].ToString();
	            RemoveAlias(alias, robot);
	            msg.Send("As you wish.");
            });
        }

        public IEnumerable<string> GetHelp()
        {
            return new List<string>
            {
                "mmbot add package source (package source url) - adds a package source to use when downloading packages",
                "mmbot remove package source (package source url) - removes a package source",
                "mmbot list package sources - lists the currently in-use package sources",
                "mmbot add package alias (alias name)=(actual package name) - adds an alias to a package name for convenience",
                "mmbot remove package alias (alias name) - removes an alias",
                "mmbot list package aliases - lists the currently in-use package aliases",
                "mmbot update (package name or alias) [restart] - updates the specified package and optionally restarts the robot to load updated packages"
            };
        }
    }
}
