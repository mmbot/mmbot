using System;
using System.Collections.Generic;

namespace MMBot.Scripts
{
    public class TeamCity : IMMBotScript
    {
        private string _username;
        private string _password;
        private string _hostname;
        private string _scheme;
        private string _baseUrl;

        private Dictionary<string, string> GetHeaders()
        {
            return new Dictionary<string, string>
            {
                {"Authorization", string.Format("Basic {0}", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(string.Format("{0}:{1}", _username, _password))))},
                {"Accept", "application/json"}
            };
        }

        public void Register(Robot robot)
        {
            _username = robot.GetConfigVariable("MMBOT_TEAMCITY_USERNAME");
            _password = robot.GetConfigVariable("MMBOT_TEAMCITY_PASSWORD");
            _hostname = robot.GetConfigVariable("MMBOT_TEAMCITY_HOSTNAME");
            _scheme = robot.GetConfigVariable("MMBOT_TEAMCITY_SCHEME") ?? "http";
            _baseUrl = string.Format("{0}://{1}", _scheme, _hostname);

            if (_hostname == null)
            {
                return;
            }

            robot.Respond(@"what('?s| is) building$", async msg =>
            {
                var res = await msg.Http(string.Format("http://{0}/httpAuth/app/rest/builds/?locator=running:true", _hostname))
                          .Headers(GetHeaders())
                          .GetJson();
                if (res.count == 0)
                {
                    await msg.Send("No builds are currently running");
                }
                else
                {
                    
                }

            });

        }

        public IEnumerable<string> GetHelp()
        {
            return new[]
            {
                ""
                //"mmbot what is building - Show status of currently running builds",
                //"mmbot tc list projects - Show all available projects",
                //"mmbot tc list buildTypes - Show all available build types",
                //"mmbot tc list buildTypes of <project> - Show all available build types for the specified project",
                //"mmbot tc list builds <buildType> <number> - Show the status of the last <number> builds.  Number defaults to five.",
                //"mmbot tc list builds of <buildType> of <project> <number>- Show the status of the last <number> builds of the specified build type of the specified project. Number can only follow the last variable, so if project is not passed, number must follow buildType directly. <number> Defaults to 5",
                //"mmbot tc build start <buildType> - Adds a build to the queue for the specified build type",
                //"mmbot tc build start <buildType> of <project> - Adds a build to the queue for the specified build type of the specified project",
                //"mmbot tc build stop all <buildType> id <buildId> of <project> - Stops all currently running builds of a given buildType. Project parameter is optional. Please note that the special 'all' keyword will kill all currently running builds ignoring all further parameters. hubot tc build stop all all",
            };
        }
    }
}