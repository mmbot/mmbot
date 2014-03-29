/**
* <description>
*     Provides NuGet package management capabilities.
* </description>
*
* <commands>
*     mmbot list package sources - Displays all of the package sources that mmbot knows about.;
* </commands>
* 
* <author>
*     Anthony Compton
* </author>
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MMBot.Scripts;
using NuGet;

var robot = Require<Robot>();

string NuGetRepositoriesSetting = "MMBOT_NUGET_REPOS";
string NuGetPackageAliasesSetting = "MMBOT_NUGET_PACKAGE_ALIASES";
string NuGetResetAfterUpdate = "MMBOT_NUGET_RESET";

const string Add = "add|remember";
const string Remove = "remove|delete|del|rem|forget";
const string Package = "pkg|package";
const string Source = "source|src|sources";
const string Alias = "alias|aliases";
const string List = "list";
const string Param = @"[^\s]+";
const string Update = "update";
const string Restart = "restart";

//Package Sources
private void RememberConfiguredSources()
{
	var configuredSources = robot.GetConfigVariable(NuGetRepositoriesSetting) ?? string.Empty;
	foreach(var source in configuredSources.Split(','))
	{
		AddSource(source);
	}
}

private List<string> GetRememberedSources()
{
	var sources = robot.Brain.Get<List<string>>(NuGetRepositoriesSetting).Result;
	if(sources == null)
	{
		sources = new List<string>();
		Remember(NuGetRepositoriesSetting, sources);
	}

	return sources;
}

private void Remember(string key, object value)
{
	robot.Brain.Set<object>(key, value);
}

private bool AddSource(string source)
{
	var sources = GetRememberedSources();
	if (sources.Contains(source))
	{
		return false;
	}
	else
	{
		sources.Add(source);
		Remember(NuGetRepositoriesSetting, sources);
		return true;
	}
}

private bool RemoveSource(string source)
{
	var sources = GetRememberedSources();
	if (sources.Contains(source))
	{
		sources.Remove(source);
		Remember(NuGetRepositoriesSetting, sources);
		return true;
	}
	else
	{
		return false;
	}
}

robot.Respond(BuildCommand(new []{List, Package, Source}), msg => {
	msg.Send(GetRememberedSources().ToArray());
});

robot.Respond(BuildCommand(new []{Add, Package, Source, Param}), msg =>
{
	var source = msg.Match[4].ToString();
	if (!AddSource(source))
	{
		msg.Send("I already know about this one.");
	}
	else
	{
		msg.Send("Consider it done.");
	}
});

robot.Respond(BuildCommand(new []{Remove, Package, Source, Param}), msg =>
{
	var source = msg.Match[4].ToString();
	if (RemoveSource(source))
	{
		msg.Send("I'll forget it immediately.");
	}
	else
	{
		msg.Send("It's easy to forget what you never knew.");
	}
});

//Package Aliases
private void RememberConfiguredAliases()
{
	var configuredSources = robot.GetConfigVariable(NuGetPackageAliasesSetting) ?? string.Empty;
	configuredSources.Split(',').ForEach(AddAlias);
}

private Dictionary<string,string> GetRememberedAliases()
{
	var aliases = robot.Brain.Get<Dictionary<string,string>>(NuGetPackageAliasesSetting).Result;
	if(aliases == null)
	{
		aliases = new Dictionary<string,string>();
		Remember(NuGetPackageAliasesSetting, aliases);
	}
	return aliases;
}

private void AddAlias(string alias)
{
	var aliases = GetRememberedAliases();
	var parts = alias.Split('=');
	
	alias = parts[0].ToLower();
	var packageName = parts[1];
	
	aliases[alias] = packageName;

	Remember(NuGetPackageAliasesSetting, aliases);
}

private void RemoveAlias(string alias)
{
	var aliases = GetRememberedAliases();
	alias = alias.Split(',')[0];
	aliases.Remove(alias);
	Remember(NuGetPackageAliasesSetting, aliases);
}

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

robot.Respond(BuildCommand(new []{List, Package, Alias}), msg => {
	msg.Send(GetRememberedAliases().Select(kvp => string.Format("{0} = {1}", kvp.Key, kvp.Value)).ToArray());
});

robot.Respond(BuildCommand(new []{Add, Package, Alias, Param}), msg =>
{
	var alias = msg.Match[4].ToString();
	AddAlias(alias);
	msg.Send("I'll be sure to remember that.");
});

robot.Respond(BuildCommand(new []{Remove, Package, Alias, Param}), msg =>
{
	var alias = msg.Match[4].ToString();
	RemoveAlias(alias);
	msg.Send("As you wish.");
});

private bool ShouldAutoResetAfterUpdate()
{
	return bool.Parse(robot.Brain.Get<string>(NuGetResetAfterUpdate).Result);
}

//TODO: Once reset changes go in, uncomment the commented code in this method to enbale automatic restarts after package installs.
//robot.Respond(BuildCommand(new []{Update, Param /*, Restart*/}, new []{/*2*/}), msg =>
robot.Respond(BuildCommand(new []{Update, Param}), msg =>
{
	//ID of the package to be looked up
	string packageId = msg.Match[2].ToString();
	string unaliasedPackageId;
	
	var knownAliases = GetRememberedAliases();
	if(!knownAliases.TryGetValue(packageId.ToLower(), out unaliasedPackageId))
	{
		unaliasedPackageId = packageId;
	}

	msg.Send("Building repositories...");
	IPackageRepository repo = BuildPackagesRepository();

	//Get the list of all NuGet packages with ID 'EntityFramework'   
	msg.Send("Finding package...");
	List<IPackage> packages = repo.FindPackagesById(unaliasedPackageId).ToList();

	if (packages.Any())
	{
		msg.Send("feafeafeafeafea");
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
	
	//if(ShouldAutoResetAfterUpdate() || msg.Match.Length == 4)
	//{
	//	//They submitted the reset parameter or auto-reset is on.
	//	msg.Send("Resetting...please wait.");
	//	robot.Reset();
	//}
});

RememberConfiguredSources();
RememberConfiguredAliases();

private AggregateRepository BuildPackagesRepository()
{
	return new AggregateRepository(GetRememberedSources()
		.Select(s => PackageRepositoryFactory.Default.CreateRepository(s)));

}

private string GetPackagesPath()
{
	return Path.Combine(Directory.GetCurrentDirectory(), "packages");
}

public IEnumerable<string> GetHelp()
{
	return new String[] { };
}