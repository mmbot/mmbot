using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MMBot.Tests
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public HttpRequestMessage LastRequest { get; set; }

        public FakeHttpMessageHandler(HttpResponseMessage response)
        {
            this._response = response;
        }

        protected override Task<HttpResponseMessage>
            SendAsync(HttpRequestMessage request,
                CancellationToken cancellationToken)
        {
            LastRequest = request;
            var responseTask =
                new TaskCompletionSource<HttpResponseMessage>();
            responseTask.SetResult(_response);

            return responseTask.Task;
        }
    }
}