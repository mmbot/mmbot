using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MMBot
{
    public static class OwinContextExtensions
    {
        public static void ReturnJson(this IOwinContext context, string json)
        {
            context.Response.ContentType = "application/json";
            context.Response.WriteAsync(json);
        }

        public static void ReturnJson(this IOwinContext context, object model)
        {
            context.Response.ContentType = "application/json";
            context.Response.WriteAsync(JsonConvert.SerializeObject(model));
        }

        public static JToken ReadBodyAsJson(this IOwinContext context)
        {
            return ReadBodyAsJsonAsync(context).Result;
        }

        public static async Task<JToken> ReadBodyAsJsonAsync(this IOwinContext context)
        {
            var body = await context.ReadBodyAsStringAsync();
            return await body.ToJsonAsync();
        }
        
        public static string ReadBodyAsString(this IOwinContext context)
        {
            return context.ReadBodyAsStringAsync().Result;
        }

        public static async Task<string> ReadBodyAsStringAsync(this IOwinContext context)
        {
            var sb = new StringBuilder();
            var buffer = new byte[8000];
            var read = 0;

            read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            while (read > 0)
            {
                sb.Append(Encoding.UTF8.GetString(buffer));
                buffer = new byte[8000];
                read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            }

            return sb.ToString();
        }

        public static async Task<IFormCollection> FormAsync(this IOwinContext context)
        {
            return await context.Request.ReadFormAsync();
        }

        public static IFormCollection Form(this IOwinContext context)
        {
            return context.FormAsync().Result;
        }
    }
}