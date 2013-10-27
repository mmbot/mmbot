using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MMBot.Tests
{
    [TestClass]
    public class HttpWrapperTests
    {
        [TestMethod]
        public async Task WhenQueryParametersAreAddedByAnonymousType_UrlIsCorrectlyConstructed()
        {
            var stubHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var http = new HttpWrapper("http://foo.com/",
                new TestLogger(), 
                new Envelope(new TextMessage(new User("foo"), "test", "id")),
                stubHandler);

            http.Query(new
            {
                foo = "Foo",
                bar = "Bar"
            });

            await http.Get();

            Assert.AreEqual("http://foo.com/?foo=Foo&bar=Bar", stubHandler.LastRequest.RequestUri.ToString());
        }

        [TestMethod]
        public async Task WhenQueryParametersAreAddedindividually_UrlIsCorrectlyConstructed()
        {
            var stubHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var http = new HttpWrapper("http://foo.com/",
                new TestLogger(),
                new Envelope(new TextMessage(new User("foo"), "test", "id")),
                stubHandler);

            http.Query("foo", "Foo");
            http.Query("bar", "Bar");

            await http.Get();

            Assert.AreEqual("http://foo.com/?foo=Foo&bar=Bar", stubHandler.LastRequest.RequestUri.ToString());
        }

        [TestMethod]
        public async Task WhenQueryParametersAreAddedViaDictionary_UrlIsCorrectlyConstructed()
        {
            var stubHandler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var http = new HttpWrapper("http://foo.com/",
                new TestLogger(),
                new Envelope(new TextMessage(new User("foo"), "test", "id")),
                stubHandler);

            http.Query(new Dictionary<string, string>{{"foo", "Foo"}, {"bar", "Bar"}});
            
            await http.Get();

            Assert.AreEqual("http://foo.com/?foo=Foo&bar=Bar", stubHandler.LastRequest.RequestUri.ToString());
        }


        [TestMethod]
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
                new Envelope(new TextMessage(new User("foo"), "test", "id")),
                stubHandler);

            var actual = await http.GetJson();


            Assert.IsTrue(new JTokenEqualityComparer().Equals((JToken)expected, (JToken)actual));
        }

        [TestMethod]
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
                new Envelope(new TextMessage(new User("foo"), "test", "id")),
                stubHandler);

            var actual = await http.GetXml();

            Assert.AreEqual(expected.ToString(), actual.ToString());
        }

    }
}