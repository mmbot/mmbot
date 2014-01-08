using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace MMBot.Tests
{
    public class HttpWrapperTests
    {
        [Fact]
        public async Task WhenQueryParametersAreAddedByAnonymousType_UrlIsCorrectlyConstructed()
        {
            var stubHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var http = new HttpWrapper("http://foo.com/",
                new TestLogger(), 
                new Envelope(new TextMessage(CreateTestUser(), "test", "id")),
                stubHandler);

            http.Query(new
            {
                foo = "Foo",
                bar = "Bar"
            });

            await http.Get();

            Assert.Equal("http://foo.com/?foo=Foo&bar=Bar", stubHandler.LastRequest.RequestUri.ToString());
        }

        private static User CreateTestUser()
        {
            return new User("foo", "foo", new string[0], "testRoom", "stubAdapter");
        }

        [Fact]
        public async Task WhenQueryParametersAreAddedindividually_UrlIsCorrectlyConstructed()
        {
            var stubHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var http = new HttpWrapper("http://foo.com/",
                new TestLogger(),
                new Envelope(new TextMessage(CreateTestUser(), "test", "id")),
                stubHandler);

            http.Query("foo", "Foo");
            http.Query("bar", "Bar");

            await http.Get();

            Assert.Equal("http://foo.com/?foo=Foo&bar=Bar", stubHandler.LastRequest.RequestUri.ToString());
        }

        [Fact]
        public async Task WhenQueryParametersAreAddedViaDictionary_UrlIsCorrectlyConstructed()
        {
            var stubHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var http = new HttpWrapper("http://foo.com/",
                new TestLogger(),
                new Envelope(new TextMessage(CreateTestUser(), "test", "id")),
                stubHandler);

            http.Query(new Dictionary<string, string>{{"foo", "Foo"}, {"bar", "Bar"}});
            
            await http.Get();

            Assert.Equal("http://foo.com/?foo=Foo&bar=Bar", stubHandler.LastRequest.RequestUri.ToString());
        }


        [Fact]
        public async Task WhenGetJsonIsCalled_ResponseContentIsDeserialized()
        {
            var expectedString = JsonConvert.SerializeObject(new {Id=4, Foo = "Foo", Bar = "Bar", Date = DateTime.Now});
            var expected = JsonConvert.DeserializeObject(expectedString);

            var stubHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(expectedString)
            });
            var http = new HttpWrapper("http://foo.com/",
                new TestLogger(),
                new Envelope(new TextMessage(CreateTestUser(), "test", "id")),
                stubHandler);

            var actual = await http.GetJson();


            Assert.True(new JTokenEqualityComparer().Equals((JToken)expected, (JToken)actual));
        }


        [Fact]
        public async Task WhenGetJsonWithCallbackReturnsErrorHttpStatusCode_CodeIsAccessibleViaResponseParameter()
        {
            var expectedString = JsonConvert.SerializeObject(new { Id = 4, Foo = "Foo", Bar = "Bar", Date = DateTime.Now });
            var expected = JsonConvert.DeserializeObject(expectedString);

            var stubHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(expectedString)
            });
            var http = new HttpWrapper("http://foo.com/",
                new TestLogger(),
                new Envelope(new TextMessage(CreateTestUser(), "test", "id")),
                stubHandler);

            var callback = false;
            await http.GetJson((err, res, body) =>
            {
                callback = true;
                Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
            });
            Assert.True(callback);

        }

        [Fact]
        public async Task WhenGetJson_AndContentContainsGarbage_ThrowsException()
        {
            var expected = "dfsgsdf%#@$%^&*()";

            var stubHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(expected)
            });
            var http = new HttpWrapper("http://foo.com/",
                new TestLogger(),
                new Envelope(new TextMessage(CreateTestUser(), "test", "id")),
                stubHandler);

            Assert.Throws<AggregateException>(() => http.GetJson().Result);
        }

        [Fact]
        public async Task WhenGetJsonWithCallback_AndContentContainsGarbage_ThrowsException()
        {
            var expected = "dfsgsdf%#@$%^&*()";

            var stubHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(expected)
            });
            var http = new HttpWrapper("http://foo.com/",
                new TestLogger(),
                new Envelope(new TextMessage(CreateTestUser(), "test", "id")),
                stubHandler);

            var callback = false;
            await http.GetJson((err, res, body) =>
            {
                callback = true;
                Assert.NotNull(err);
                Assert.IsType<JsonReaderException>(err);
                
            });

            Assert.True(callback);
        }


        [Fact]
        public async Task WhenGetXmlIsCalled_ResponseContentIsDeserialized()
        {
            var expectedString = "<root><foo>Foo</foo><bar>Bar</bar></root>";
            var expected = new XmlDocument();
            expected.LoadXml(expectedString);

            var stubHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(expectedString)
            });
            var http = new HttpWrapper("http://foo.com/",
                new TestLogger(),
                new Envelope(new TextMessage(CreateTestUser(), "test", "id")),
                stubHandler);

            var actual = await http.GetXml();

            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public async Task WhenGetXmlWithCallbackReturnsErrorHttpStatusCode_CodeIsAccessibleViaResponseParameter()
        {
            var expectedString = "<root><foo>Foo</foo><bar>Bar</bar></root>";
            var expected = new XmlDocument();
            expected.LoadXml(expectedString);

            var stubHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(expectedString)
            });
            var http = new HttpWrapper("http://foo.com/",
                new TestLogger(),
                new Envelope(new TextMessage(CreateTestUser(), "test", "id")),
                stubHandler);

            var callback = false;
            await http.GetXml((err, res, body) =>
            {
                callback = true;
                Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
            });
            Assert.True(callback);

        }

        [Fact]
        public async Task WhenGetXmlIsCalled_AndContentIsGarbage_ExceptionIsThrown()
        {
            var expectedString = "!@#$%^&*()_+<root><foo@#$%^&*()_>$%^&*(OP)_Foo</foo><bar>Bar</bar></root>";
            
            var stubHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(expectedString)
            });
            var http = new HttpWrapper("http://foo.com/",
                new TestLogger(),
                new Envelope(new TextMessage(CreateTestUser(), "test", "id")),
                stubHandler);

            Assert.Throws<AggregateException>(() => http.GetXml().Result);
        }

        [Fact]
        public async Task WhenGetXmlWithCallback_AndContentIsGarbage_ErrContainsException()
        {
            var expectedString = "!@#$%^&*()_+<root><foo@#$%^&*()_>$%^&*(OP)_Foo</foo><bar>Bar</bar></root>";

            var stubHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(expectedString)
            });
            var http = new HttpWrapper("http://foo.com/",
                new TestLogger(),
                new Envelope(new TextMessage(CreateTestUser(), "test", "id")),
                stubHandler);

            var callback = false;
            await http.GetXml((err, res, body) =>
            {
                callback = true;
                Assert.NotNull(err);
                Assert.IsType<HttpRequestException>(err);
            });
            Assert.True(callback);

        }
    }
}