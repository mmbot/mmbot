using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common.Logging;
using Microsoft.Owin.Testing;
using MMBot.Brains;
using MMBot.Router.Nancy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Owin;
using Xunit;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace MMBot.Tests
{
    public class RouterTests
    {
        [Fact]
        public async Task WhenSimpleGetRoute_WithStringResult_ResponseBodyIsString()
        {
            string expected = "Yo!";

            using (var router = await SetupRoute(robot => robot.Router.Get("/test/", context => expected)))
            {

                var response = await router.Client.GetAsync("/test/");

                Assert.Equal(expected, await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task WhenReturnJson_WithString_ResponseIsJson()
        {
            JToken token = new JObject(new JProperty("foo", "The Foo"));
            using(var router = await SetupRoute(robot => robot.Router.Get("/json/test/", context => context.ReturnJson(JsonConvert.SerializeObject(token)))))
            {

                var response = await router.Client.GetAsync("/json/test/");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);

                var body = await response.Content.ReadAsStringAsync();
                Assert.Equal(token.ToString(), JToken.Parse(body).ToString());
            }
        }

        [Fact]
        public async Task WhenRequestIsJson_CanReadAsJsonWithHelper()
        {
            JToken expected = new JObject(new JProperty("foo", "The Foo"));
            JToken actual = null;
            using (var router = await SetupRoute(robot => robot.Router.Post("/json/post/test/", context =>
            {
                var requestBody = context.ReadBodyAsJson();
                actual = requestBody;
                context.Response.StatusCode = 200;
            })))
            {

                var response =
                    await
                        router.Client.PostAsync("/json/post/test",
                            new StringContent(JsonConvert.SerializeObject(expected), Encoding.UTF8, "application/json"));

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public async Task WhenRouteDefinitionHasParameter_CanReadParameterFromContext()
        {
            string expectedRoom = "theroom";
            string expectedMessage = "This is my message";
            JToken expected = new JObject(new JProperty("foo", "The Foo"));
            string actualRoom = null;
            string actualMessage = null;
            using (var router = await SetupRoute(robot => robot.Router.Post("/route/test/{room}", context =>
            {
                var requestBody = context.ReadBodyAsJson();
                actualRoom = context.Request.Params()["room"];
                actualMessage = context.Request.Query["message"];
                context.Response.StatusCode = 200;
            })))
            {

                var response =
                    await
                        router.Client.PostAsync("/route/test/" + expectedRoom + "?message=" + expectedMessage,
                            new StringContent(JsonConvert.SerializeObject(expected), Encoding.UTF8, "application/json"));

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(expectedRoom, actualRoom);
                Assert.Equal(expectedMessage, actualMessage);
            }
        }

        [Fact]
        public void TestStringParseing()
        {
            var results = Regex.Matches("This {is} the {test} string", @"[^{}]+(?=\})");
        }

        [Fact]
        public async Task WhenGithubWebHook_BodyIsParsed()
        {
            JToken actualPayload = null;
            string eventType = null;
            using (var router = await SetupRoute(robot => robot.Router.Post("/github/webhook/test/", context =>
            {
                actualPayload = context.Form()["payload"].ToJson();
                eventType = context.Request.Headers["X-GitHub-Event"];
                context.Response.StatusCode = 200;
            })))
            {

                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"payload", Resources.GithubWebHookJson}
                });

                content.Headers.Add("X-GitHub-Event", new[] {"push"});

                var response =
                    await
                        router.Client.PostAsync("/github/webhook/test",
                            content);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(3, actualPayload["commits"].Count());
                Assert.Equal("push", eventType);

            }
        }

        [Fact]
        public async Task WhenRouteCreatedAfterStartup_RouteExistsAfterDelay()
        {
            string expected = "Yo!";

            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .UseRouter<TestNancyRouter>()
                        .DisablePluginDiscovery()
                        .DisableScriptDiscovery()
                        .Build();

            robot.AutoLoadScripts = false;

            using (var testNancyRouter = (robot.Router as TestNancyRouter))
            {

                await robot.Run();

                await testNancyRouter.Started.Take(1);

                robot.Router.Get("/test/", context => expected);

                await testNancyRouter.Started.Take(2);

                var server = testNancyRouter.Server;

                var response = await server.HttpClient.GetAsync("/test/");

                Assert.Equal(expected, await response.Content.ReadAsStringAsync());
            }
        }

        private async Task<TestNancyRouter> SetupRoute(Action<Robot> setup)
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .UseRouter<TestNancyRouter>()
                        .DisablePluginDiscovery()
                        .DisableScriptDiscovery()
                        .Build();

            robot.AutoLoadScripts = false;
            
            setup(robot);

            await robot.Run();

            return (robot.Router as TestNancyRouter);
        }

        
        public class TestNancyRouter : NancyRouter, IDisposable, IMustBeInitializedWithRobot
        {
            
            private readonly ReplaySubject<Unit> _started = new ReplaySubject<Unit>();

            public TestNancyRouter() : base(TimeSpan.FromSeconds(0))
            {
                
            }

            public void Initialize(Robot robot)
            {
                __robot = robot;
            }

            private TestServer _server;
            private Robot __robot;

            public TestServer Server
            {
                get { return _server; }
                set { _server = value; }
            }

            public HttpClient Client {
                get { return Server.HttpClient; }
            }

            public IObservable<Unit> Started
            {
                get { return _started; }
            }

            public override void Start()
            {
                Server = TestServer.Create(app => app.UseNancy(options => options.Bootstrapper = new Bootstrapper(this)));
                IsStarted = true;

                _started.OnNext(Unit.Default);
            }


            public void Dispose()
            {
                Server.Dispose();
                __robot.Shutdown().Wait();
            }
        }

    }
}