using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting;
using System.Security.Policy;
using System.Text;
using System.Threading;
using log4net.Core;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

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

            robot.Respond(@"tc what('?s| is) building$", async msg =>
            {
                dynamic res = await msg.Http(string.Format("{0}/httpAuth/app/rest/builds/?locator=running:true", _baseUrl))
                          .Headers(GetHeaders())
                          .GetJson();
                if (res.count == 0 || res.build == null)
                {
                    await msg.Send("No builds are currently running");
                }
                else
                {
                    var buildsDescription = new StringBuilder();
                    foreach (var build in res.build)
                    {
                        if (build.percentageComplete < 100)
                        {
                            buildsDescription.AppendLine(
                                string.Format("{0} is currently at {1}% and is so far looking like a {2}",
                                    build.buildTypeId, build.percentageComplete, build.status));
                        }
                        else
                        {
                            buildsDescription.AppendLine(
                                string.Format("{0} completed with a status of {1} - {2}", build.buildTypeId, build.status, build.webUrl));
                        }
                    }

                    await msg.Send(buildsDescription.ToString());
                }

            });

            robot.Respond(@"tc list projects", async msg =>
            {
                
                dynamic res = await msg.Http(string.Format("{0}/httpAuth/app/rest/projects", _baseUrl))
                                 .Headers(GetHeaders())
                                 .GetJson();

                if (res.count == 0 || res.project == null)
                {
                    await msg.Send("No projects are defined");
                }
                else
                {
                    var projectsDescription = new StringBuilder();
                    foreach (var project in res.project)
                    {
                        projectsDescription.AppendLine((string)project.name);
                    }
                    await msg.Send(projectsDescription.ToString());
                }
            });

        }

        public IEnumerable<string> GetHelp()
        {
            return new[]
            {
                "mmbot what is building - Show status of currently running builds",
                "mmbot tc list projects - Show all available projects",
                //"mmbot tc list buildTypes - Show all available build types",
                //"mmbot tc list buildTypes of <project> - Show all available build types for the specified project",
                //"mmbot tc list builds <buildType> <number> - Show the status of the last <number> builds.  Number defaults to five.",
                //"mmbot tc list builds of <buildType> of <project> <number>- Show the status of the last <number> builds of the specified build type of the specified project. Number can only follow the last variable, so if project is not passed, number must follow buildType directly. <number> Defaults to 5",
                //"mmbot tc build start <buildType> - Adds a build to the queue for the specified build type",
                //"mmbot tc build start <buildType> of <project> - Adds a build to the queue for the specified build type of the specified project",
                //"mmbot tc build stop all <buildType> id <buildId> of <project> - Stops all currently running builds of a given buildType. Project parameter is optional. Please note that the special 'all' keyword will kill all currently running builds ignoring all further parameters. hubot tc build stop all all",
            };
        }


        private void GetBuildType(Robot robot, IResponse<TextMessage> msg, string type,
            Action<Exception, HttpResponseMessage, JObject> callback)
        {
            var url = string.Format("{0}/httpAuth/app/rest/buildTypes/{1}", _baseUrl, type);

            robot.Logger.Debug("sending request to #{url}");

            InvokeApiCallWithCallback(msg, callback, url);
        }


        private void GetCurrentBuilds(Robot robot, IResponse<TextMessage> msg, string type,
            Action<Exception, HttpResponseMessage, JObject> callback)
        {
            var url = string.IsNullOrEmpty(type)
                ? string.Format("http://{0}/httpAuth/app/rest/builds/?locator=running:true", _baseUrl)
                : string.Format("http://{0}/httpAuth/app/rest/builds/?locator=buildType:{1},running:true", _baseUrl, type);

            InvokeApiCallWithCallback(msg, callback, url);
        }

        private void GetBuildTypes(Robot robot, IResponse<TextMessage> msg, string project,
            Action<Exception, HttpResponseMessage, JObject> callback)
        {
            var projectSegment = string.IsNullOrWhiteSpace(project) ? string.Empty : string.Format("/projects/name:{0}", WebUtility.UrlEncode(project));
            var url = string.Format("{0}/httpAuth/app/rest{1}/buildTypes", _baseUrl, projectSegment);
            InvokeApiCallWithCallback(msg, (exception, message, res) =>
            {
                _buildTypes = res["buildTypes"].ToArray();
                callback(exception, message, res);
            }, url);
        }

        private void GetBuilds(Robot robot, IResponse<TextMessage> msg, string project, string configuration, int amount,
            Action<Exception, HttpResponseMessage, JObject> callback)
        {
            var projectSegment = string.IsNullOrWhiteSpace(project) ? string.Empty : string.Format("/projects/name:{0}", WebUtility.UrlEncode(project));
            var url = string.Format("{0}/httpAuth/app/rest{1}/buildTypes/name:{2}/builds", _baseUrl, projectSegment, WebUtility.UrlEncode(configuration));
            InvokeApiCallWithCallback(msg, callback, url, new{ locator= string.Format("count:{0},running:any", amount)});
        }

        private void MapNameToIdForBuildType(Robot robot, IResponse<TextMessage> msg, string name, string project,
            Action<IResponse<TextMessage>, JToken> callback)
        {
            Func<JToken, bool> filter = b => (string)b["name"] == name && (string.IsNullOrEmpty(project) || (string)b["projectName"] == project);
            var result = _buildTypes.FirstOrDefault(filter);

            if (result != null)
            {
                callback(msg, result);
                return;
            }

            GetBuildTypes(robot, msg, project, (exception, message, res) => callback(msg, res["buildTypes"].FirstOrDefault(filter)));
        }

        private void MapAndKillBuilds(Robot robot, IResponse<TextMessage> msg, string name, string id, string project)
        {

        }

        private void MapBuildToNameList(IResponse<TextMessage> msg, JToken build)
        {
            var id = (string)build["buildTypeId"];
            var url = string.Format("{0}/httpAuth/app/rest/buildTypes/id:{1}", _baseUrl, id);
            msg.Http(url)
                .Headers(GetHeaders())
                .GetJson((err, res, body) =>
                {
                    if (err != null || res.StatusCode != HttpStatusCode.OK)
                    {
                        return;
                    }
                    var buildName = body["name"].ToString();
                    var baseMessage = string.Format("#{0} of {1} {2}", build["number"].ToString(), buildName,
                        build["webUrl"].ToString());
                    string message;
                    if (build["running"].Value<bool>())
                    {
                        message = string.Format("{0} {1}% Complete :: {2}",
                            build["status"].Value<string>() == "SUCCESS" ? "***Winning***" : "__FAILING__",
                            build["percentageComplete"].Value<int>(), baseMessage);
                    }
                    else
                    {
                        message = string.Format("{0} :: {1}",
                            build["status"].Value<string>() == "SUCCESS" ? "OK!" : "__FAILED__", baseMessage);
                    }
                    msg.Send(message);
                });
        }

        private void CreateAndPublishBuildMap(IEnumerable<JToken> builds, IResponse<TextMessage> msg)
        {
            foreach (var build in builds)
            {
                MapBuildToNameList(msg, build);
            }
        }


        private JToken[] _buildTypes = new JToken[0];

        /*
        mapBuildToNameList = (build) ->
    id = build['buildTypeId']
    msg = build['messengerBot']
    url = "http://#{hostname}/httpAuth/app/rest/buildTypes/id:#{id}"
    msg.http(url)
      .headers(getAuthHeader())
      .get() (err, res, body) ->
        err = body unless res.statusCode = 200
        unless err
          buildName = JSON.parse(body).name
          baseMessage = "##{build.number} of #{buildName} #{build.webUrl}"
          if build.running
            status = if build.status == "SUCCESS" then "**Winning**" else "__FAILING__"
            message = "#{status} #{build.percentageComplete}% Complete :: #{baseMessage}"
          else
            status = if build.status == "SUCCESS" then "OK!" else "__FAILED__"
            message = "#{status} :: #{baseMessage}"
          msg.send message*/


        private void InvokeApiCallWithCallback(IResponse<TextMessage> msg, Action<Exception, HttpResponseMessage, JObject> callback, string url, object query = null)
        {
            msg.Http(url).Headers(GetHeaders())
                .Query(query)
                .GetJson((err, res, body) =>
                {
                    if (err == null && res.StatusCode != HttpStatusCode.OK)
                    {
                        err = new Exception(res.Content.ReadAsStringAsync().Result);
                    }
                    callback(err, res, body);
                });
        }
    }
}