using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Common.Logging;
using log4net.Core;
using MMBot.Adapters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml;

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

        public async Task<dynamic> GetJson()
        {
            try
            {
                var response = await GetResponseMessage();
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
                response = await GetResponseMessage();

                string result = await response.Content.ReadAsStringAsync();

                JToken body;

                if (result != null && result.StartsWith("["))
                {
                    body = await JsonConvert.DeserializeObjectAsync<JArray>(result);
                }
                else
                {
                    body = await JsonConvert.DeserializeObjectAsync<JObject>(result);
                }
                
                callback(null, response, body);
            }
            catch (Exception e)
            {
                _logger.Error("Http GetJson error", e);
                callback(e, response, null);
            }
        }

        private async Task<HttpResponseMessage> GetResponseMessage()
        {
            var uri = BuildUri();
            var client = new HttpClient(_httpMessageHandler);
            _headers.ForEach(h => client.DefaultRequestHeaders.Add(h.Key, h.Value));

            var response = await client.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            return response;
        }

        public async Task<XmlDocument> GetXml()
        {
            var response = await GetResponseMessage();

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
                response = await GetResponseMessage();

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

        public async Task<HttpResponseMessage> Get()
        {
            try
            {
                return await GetResponseMessage();
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
                response = await GetResponseMessage();
                response.EnsureSuccessStatusCode();
                callback(null, response);
            }
            catch (Exception e)
            {
                _logger.Error("Http Get error", e);
                callback(e, response);
            }
            
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