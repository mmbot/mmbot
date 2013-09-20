using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MMBot.Adapters;
using Newtonsoft.Json;

namespace MMBot
{
    public class HttpWrapper
    {
        private Uri _baseUrl;
        Dictionary<string, string> _headers= new Dictionary<string, string>();
        NameValueCollection _queries = new NameValueCollection();

        public HttpWrapper(string baseUrl)
        {
            _baseUrl = new Uri(baseUrl);
        }

        public HttpWrapper Query(string name, string value)
        {
            _queries.Add(name, value);
            return this;
        }

        public HttpWrapper Query(object queryConfig)
        {
            foreach (var prop in queryConfig.GetType().GetProperties())
            {
                var value = prop.GetValue(queryConfig, null);
                if(value != null)
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
            foreach(var header in headers)
            {
                _headers.Add(header.Key, header.Value);
            }
            return this;
        }

        private Uri BuildUri()
        {
            if (_queries.Count ==0)
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

        public async Task<dynamic> Get()
        {
            var uri = BuildUri();
            var client = new HttpClient();
            _headers.ForEach(h => client.DefaultRequestHeaders.Add(h.Key, h.Value));
            
            var result = await client.GetStringAsync(uri);
            return await JsonConvert.DeserializeObjectAsync<dynamic>(result);
        }
    }
}