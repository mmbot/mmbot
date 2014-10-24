using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MMBot.Scripts;

namespace MMBot.ScriptIt
{
    public class ScriptsScripts : IMMBotScript
    {
        public void Register(Robot robot)
        {
            robot.Respond(@"scriptthis (.*):(.*)", async msg =>
            {
                if (!msg.Message.User.IsAdmin(robot))
                {
                    await msg.Send("You must be an admin to run this command");
                    return;
                }

                string name = msg.Match[1].Trim();
                string script = msg.Match[2].Trim();

                //Save script to file
                var filePath = Path.Combine(Environment.CurrentDirectory, Path.Combine("scripts", string.Format("{0}.csx", name)));
                File.WriteAllText(filePath, script);
                //try to load file
                try
                {
                    robot.LoadScriptFile("test1", filePath);
                    await msg.Send(string.Format("Successfully added script: {0}", name));
                }
                catch (Exception scriptEx)
                {
                    if (File.Exists(filePath))
                    {
                        //clean up
                        try
                        {
                            File.Delete(filePath);
                        }
                        catch { }
                    }
                    msg.Send(string.Format("Failed to load script: ({0}) - {1}", name, scriptEx.Message)).Wait();
                }
                //???
                //profit
            });

            robot.Respond(@"scriptthat (.*)", async msg =>
            {
                if (!msg.Message.User.IsAdmin(robot))
                {
                    await msg.Send("You must be an admin to run this command");
                    return;
                }

                string url = msg.Match[1].Trim();

                Uri uri;
                try
                {
                    uri = new Uri(url);
                }
                catch (Exception ex)
                {
                    msg.Send("Invalid Uri: " + ex.Message).Wait();
                    return;
                }

                if (!uri.Host.Equals("gist.github.com", StringComparison.OrdinalIgnoreCase))
                {
                    await msg.Send("Only accepting Github Gists, try again later...");
                    return;
                }

                var gistId = url.Substring(url.LastIndexOf("/") + 1);

                await msg.Http(string.Format("https://api.github.com/gists/{0}", gistId))
                    .GetJson((ex, response, body) =>
                    {
                        if (ex != null)
                        {
                            msg.Send("That's a bad one...");
                            return;
                        }
                        foreach (var gistFile in body["files"])
                        {
                            var script = (string)gistFile.Children().First()["content"];
                            var name = (string)gistFile.Children().First()["filename"];

                            if (!name.EndsWith(".csx"))
                            {
                                name += ".csx";
                            }

                            var filePath = Path.Combine(Environment.CurrentDirectory, Path.Combine("scripts", name));
                            File.WriteAllText(filePath, script);

                            try
                            {
                                robot.LoadScriptFile(name, filePath);
                                msg.Send(string.Format("Successfully added script: {0}", name));
                            }
                            catch (Exception scriptEx)
                            {
                                if (File.Exists(filePath))
                                {
                                    //clean up
                                    try
                                    {
                                        File.Delete(filePath);
                                    }
                                    catch { }
                                }
                                msg.Send(string.Format("Failed to load script: ({0}) - {1}", name, scriptEx.Message));
                            }
                        }
                    });
            });
        }

        public IEnumerable<string> GetHelp()
        {
            return new[]
            {
                "mmbot scriptthis <name>:<script> - creates a new mmbot script",
                "mmbot scriptthis <gist url> - creates a new mmbot script from a github gist"
            };
        }
    }
}