using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Conjur.Test
{
    public class WebMocker
    {
        private readonly IDictionary<Uri, MockResponse> responses =
            new Dictionary<Uri, MockResponse>();

        public WebMocker()
        {
        }

        public MockResponse Mock(Uri uri, string content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return responses[uri] = new MockResponse(content, statusCode);
        }

        public void Clear()
        {
            responses.Clear();
        }

        public HttpClient GetMockHttpClient()
        {
            return new HttpClient(new MockHttpMessageHandler(responses));
        }

        public class MockResponse
        {
            public string Content { get; }
            public HttpStatusCode StatusCode { get; }
            public Action<HttpRequestMessage> Verifier { get; set; }

            public MockResponse(string content, HttpStatusCode statusCode)
            {
                this.Content = content;
                this.StatusCode = statusCode;
            }
        }

        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly IDictionary<Uri, MockResponse> _responses;

            public MockHttpMessageHandler(IDictionary<Uri, MockResponse> responses)
            {
                _responses = responses;
            }

            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (!_responses.TryGetValue(request.RequestUri, out MockResponse response))
                {
                    throw new KeyNotFoundException(request.RequestUri.ToString());
                }

                response.Verifier?.Invoke(request);

                var httpResponseMessage = new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    RequestMessage = request
                };

                return httpResponseMessage;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(Send(request, cancellationToken));
            }
        }

    }
}
