namespace Conjur.Test;

public class WebMocker
{
    private readonly Dictionary<Uri, MockResponse> responses = new();

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

    public class MockResponse(string content, HttpStatusCode statusCode)
    {
        public string Content { get; } = content;

        public HttpStatusCode StatusCode { get; } = statusCode;

        public Action<HttpRequestMessage> Verifier { get; set; }

        public Func<HttpRequestMessage, Task> VerifierAsync { get; set; }
    }

    private class MockHttpMessageHandler(IDictionary<Uri, MockResponse> responses) : HttpMessageHandler
    {
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!responses.TryGetValue(request.RequestUri!, out var response))
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

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!responses.TryGetValue(request.RequestUri!, out var response))
            {
                throw new KeyNotFoundException(request.RequestUri.ToString());
            }

            if (response.VerifierAsync is not null)
            {
                await response.VerifierAsync(request);
            }
            else
            {
                response.Verifier?.Invoke(request);
            }

            var httpResponseMessage = new HttpResponseMessage(response.StatusCode)
            {
                Content = new StringContent(response.Content),
                RequestMessage = request
            };

            return httpResponseMessage;
        }
    }
}
