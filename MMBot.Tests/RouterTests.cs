using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Testing;
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

            var client = await SetupRoute(robot => robot.Router.Get("/test/", context => expected));
            
            var response = await client.GetAsync("/test/");

            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task WhenReturnJson_WithString_ResponseIsJson()
        {
            JToken token = new JObject(new JProperty("foo", "The Foo"));
            var client = await SetupRoute(robot => robot.Router.Get("/json/test/", context => context.ReturnJson(JsonConvert.SerializeObject(token))));

            var response = await client.GetAsync("/json/test/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(token, JToken.Parse(body));
        }

        [Fact]
        public async Task WhenRequestIsJson_CanReadAsJsonWithHelper()
        {
            JToken expected = new JObject(new JProperty("foo", "The Foo"));
            JToken actual = null;
            var client = await SetupRoute(robot => robot.Router.Post("/json/post/test/", context =>
            {
                var requestBody = context.ReadBodyAsJson();
                actual = requestBody;
                context.Response.StatusCode = 200;
            }));

            var response =
                await client.PostAsync("/json/post/test", new StringContent(JsonConvert.SerializeObject(expected), Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, actual);
        }

        public async Task WhenGithubWebHook_BodyIsParsed()
        {
            JToken actualPayload = null;
            string eventType = null;
            var client = await SetupRoute(robot => robot.Router.Post("/github/webhook/test/", context =>
            {
                actualPayload = context.Form()["payload"].ToJson();
                eventType = context.Request.Headers["X-GitHub-Event"];
                context.Response.StatusCode = 200;
            }));

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"payload", Resources.GithubWebHookJson}
            });

            content.Headers.Add("X-GitHub-Event", new[]{"push"});

            var response =
                await
                    client.PostAsync("/github/webhook/test",
                        content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(3, actualPayload["commits"].Count());
            Assert.Equal("push", eventType);
        }


        private async Task<HttpClient> SetupRoute(Action<Robot> setup)
        {
            Robot robot = Robot.Create<StubAdapter>();
            robot.AutoLoadScripts = false;
            robot.ConfigureRouter(typeof(TestNancyRouter));

            setup(robot);

            await robot.Run();

            var server = (robot.Router as TestNancyRouter).Server;

            return server.HttpClient;
        }


        public class TestNancyRouter : NancyRouter
        {
            private TestServer _server;

            public TestServer Server
            {
                get { return _server; }
                set { _server = value; }
            }

            public override void Start()
            {
                Server = TestServer.Create(app => app.UseNancy(options => options.Bootstrapper = new Bootstrapper(this)));
            }
        }
    }
}