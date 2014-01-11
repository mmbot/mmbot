using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Common.Logging;
using log4net.Core;
using MMBot.Adapters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml;
using HtmlAgilityPack;

namespace MMBot
{
    public class HttpWrapper
    {
        private readonly ILog _logger;
        private readonly Envelope _envelope;
        private Uri _baseUrl;
        Dictionary<string, string> _headers = new Dictionary<string, string>();
        NameValueCollection _queries = new NameValueCollection();
        private HttpMessageHandler _httpMessageHandler;

        public HttpWrapper(string baseUrl, ILog logger, Envelope envelope) : this(baseUrl, logger, envelope, null){}

        public HttpWrapper(string baseUrl, ILog logger, Envelope envelope, HttpMessageHandler httpMessageHandler)
        {
            _logger = logger;
            _envelope = envelope;
            _baseUrl = new Uri(baseUrl);
            _httpMessageHandler = httpMessageHandler ?? new HttpClientHandler();            
        }

        public HttpWrapper Query(string name, string value)
        {
            _queries.Add(name, value);
            return this;
        }

        public HttpWrapper Query(object queryConfig)
        {
            if (queryConfig == null)
            {
                return this;
            }
            foreach (var prop in queryConfig.GetType().GetProperties())
            {
                var value = prop.GetValue(queryConfig, null);
                if (value != null)
                {
                    Query(prop.Name, value.ToString());
                }
            }
            return this;
        }

        public HttpWrapper Query(Dictionary<string, string> queryParameters)
        {
            foreach (var kvp in queryParameters)
            {
                Query(kvp.Key, kvp.Value);
            }
            return this;
        }

        public HttpWrapper Headers(Dictionary<string, string> headers)
        {
            foreach (var header in headers)
            {
                _headers.Add(header.Key, header.Value);
            }
            return this;
        }

        private Uri BuildUri()
        {
            if (_queries.Count == 0)
            {
                return _baseUrl;
            }
            var array = (from key in _queries.AllKeys
                         from value in _queries.GetValues(key)
                         select string.Format("{0}={1}", System.Net.WebUtility.UrlEncode(key), System.Net.WebUtility.UrlEncode(value))).ToArray();

            string newUri = _baseUrl.ToString();

            newUri = newUri + (string.IsNullOrWhiteSpace(_baseUrl.Query) ? "?" : "&");

            return new Uri(newUri + string.Join("&", array));
        }

        public async Task<HtmlDocument> GetHtml()
        {
            try
            {
                var response = await DoGet();
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(result);
                return doc;
            }
            catch (Exception e)
            {
                _logger.Error("Http GetHtml error", e);
                throw;
            }
        }

        public async Task GetHtml(Action<Exception, HttpResponseMessage, HtmlDocument> callback)
        {
            HttpResponseMessage response = null;
            try
            {
                response = await DoGet();

                response.EnsureSuccessStatusCode();

                string result = await response.Content.ReadAsStringAsync();
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(result);                
                callback(null, response, doc);
            }
            catch (Exception e)
            {
                _logger.Error("Http GetHtml error", e);
                callback(e, response, null);
            }
        }

        public async Task<dynamic> GetJson()
        {
            try
            {
                var response = await DoGet();
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                return await JsonConvert.DeserializeObjectAsync<dynamic>(result);
            }
            catch (Exception e)
            {
                _logger.Error("Http GetJson error", e);
                throw;
            }
        }

        public async Task GetJson(Action<Exception, HttpResponseMessage, JToken> callback)
        {
            HttpResponseMessage response = null;
            try
            {
                response = await DoGet();

                response.EnsureSuccessStatusCode();

                string result = await response.Content.ReadAsStringAsync();

                var body = await result.ToJsonAsync();
                
                callback(null, response, body);
            }
            catch (Exception e)
            {
                _logger.Error("Http GetJson error", e);
                callback(e, response, null);
            }
        }

        private async Task<HttpResponseMessage> DoGet()
        {
            var uri = BuildUri();
            var client = new HttpClient(_httpMessageHandler);
            _headers.ForEach(h => client.DefaultRequestHeaders.Add(h.Key, h.Value));

            var response = await client.GetAsync(uri);

            return response;
        }

        public async Task<XmlDocument> GetXml()
        {
            var response = await DoGet();

            response.EnsureSuccessStatusCode();

            string result = await response.Content.ReadAsStringAsync();

            var xDoc = new XmlDocument();
            xDoc.LoadXml(result);
            return xDoc;
        }

        public async Task GetXml(Action<Exception, HttpResponseMessage, XmlDocument> callback)
        {
            HttpResponseMessage response = null;
            try
            {
                response = await DoGet();

                response.EnsureSuccessStatusCode();

                string result = await response.Content.ReadAsStringAsync();

                var xDoc = new XmlDocument();
                xDoc.LoadXml(result);

                callback(null, response, xDoc);
            }
            catch (Exception e)
            {
                callback(e, response, null);
            }
        }

        public async Task GetString(Action<Exception, HttpResponseMessage, string> callback)
        {
            HttpResponseMessage response = null;
            try
            {
                response = await DoGet();
                response.EnsureSuccessStatusCode();

                string result = await response.Content.ReadAsStringAsync();

                callback(null, response, result);
            }
            catch (Exception e)
            {
                callback(e, response, null);
            }
        }

        public async Task<HttpResponseMessage> Get()
        {
            try
            {
                return await DoGet();
            }
            catch (Exception e)
            {
                _logger.Error("Http Get error", e);
                throw;
            }
        }

        public async Task Get(Action<Exception, HttpResponseMessage> callback)
        {
            HttpResponseMessage response = null;
            try
            {
                response = await DoGet();
                response.EnsureSuccessStatusCode();
                callback(null, response);
            }
            catch (Exception e)
            {
                _logger.Error("Http Get error", e);
                callback(e, response);
            }
        }

        public async Task Post(JToken json, Action<Exception, HttpResponseMessage> callback)
        {
            HttpResponseMessage response = null;
            try
            {
                response = await DoPost(json);
                response.EnsureSuccessStatusCode();
                callback(null, response);
            }
            catch (Exception e)
            {
                _logger.Error("Http Post error", e);
                callback(e, response);
            }
        }

        public async Task<HttpResponseMessage> Post(object json)
        {
            try
            {
                return await DoPost(json);
            }
            catch (Exception e)
            {
                _logger.Error("Http Post error", e);
                throw;
            }
        }

        private async Task<HttpResponseMessage> DoPost(object json)
        {
            var uri = BuildUri();
            var client = new HttpClient(_httpMessageHandler);
            _headers.ForEach(h => client.DefaultRequestHeaders.Add(h.Key, h.Value));

            return await
                client.PostAsync(uri,
                new StringContent(json is string ? (string)json : JsonConvert.SerializeObject(json), Encoding.UTF8, "application/json"));
        }
    }

    public static class HttpClientExtensions
    {
        public static async Task<dynamic> Json(this HttpResponseMessage response)
        {
            return await JsonConvert.DeserializeObjectAsync<dynamic>(await response.Content.ReadAsStringAsync());
        }

        public static async Task Json(this HttpResponseMessage response, Action<JToken> callback)
        {
            var result = await JsonConvert.DeserializeObjectAsync<JToken>(await response.Content.ReadAsStringAsync());
            callback(result);
        }
    }

}