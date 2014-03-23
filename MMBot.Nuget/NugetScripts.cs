using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MMBot.Scripts;
using NuGet;

namespace MMBot.Nuget
{
    public class NugetScripts : IMMBotScript
    {
        private List<string> _packageSources = new List<string>();
        public const string NuGetRepositoriesSetting = "MMBOT_NUGET_REPOS";

        public void Register(Robot robot)
        {
            robot.Respond(@"list package|pkg sources?", msg => msg.Send(GetAllPackageSource(robot).ToArray()));

            robot.Respond(@"add|remember package|pkg source ([^\s]+)", msg =>
            {
                var source = msg.Matches[1].ToString();
                if (_packageSources.Contains(source))
                {
                    msg.Send("I already know about this one.");
                }
                else
                {
                    _packageSources.Add(source);
                    msg.Send("Consider it done.");
                }
            });

            robot.Respond(@"remove|delete|del|rem|forget package|pkg source ([^\s]+)", msg =>
            {
                var source = msg.Matches[1].ToString();
                if (_packageSources.Contains(source))
                {
                    _packageSources.Remove(source);
                    msg.Send("I'll forget it immediately.");
                }
                else
                {
                    msg.Send("It's easy to forget what you never knew.");
                }
            });

            robot.Respond(@"nuget update ([^\s]+)", msg =>
            {
                //ID of the package to be looked up
                string packageID = msg.Matches[1].ToString();

                msg.Send("Building repositories...");
                IPackageRepository repo = BuildPackagesRepository(robot);

                //Get the list of all NuGet packages with ID 'EntityFramework'   
                msg.Send("Finding package...");
                List<IPackage> packages = repo.FindPackagesById(packageID).ToList();

                if (packageID.Any())
                {
                    msg.Send("I found it! Downloading...");
                }
                else
                {
                    msg.Send("I couldn't find it...sorry!");
                }

                //Initialize the package manager
                string path = GetPackagesPath();
                PackageManager packageManager = new PackageManager(repo, path);

                //Download and unzip the package
                packageManager.InstallPackage(packageID);
                msg.Send("Finished downloading...");
                msg.Send("Reloading scripts...");

                robot.LoadScripts("RELOADPACKAGES");
            });
        }

        private IEnumerable<string> GetAllPackageSource(Robot robot)
        {
            var sources = _packageSources.Union(robot.GetConfigVariable(NuGetRepositoriesSetting)
                .Split(','))
                .Distinct();
            return sources;
        }

        private AggregateRepository BuildPackagesRepository(Robot robot)
        {
            return new AggregateRepository(GetAllPackageSource(robot)
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
    }
}
