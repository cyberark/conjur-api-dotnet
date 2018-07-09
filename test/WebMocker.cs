using System;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;

namespace Conjur.Test
{
    public class WebMocker: IWebRequestCreate
    {
        private readonly IDictionary<Uri, MockRequest> responses = 
            new Dictionary<Uri, MockRequest>();

        public WebMocker()
        {
        }

        public MockRequest Mock(Uri uri, string content)
        {
            return responses[uri] = new MockRequest(uri, content);
        }

        public void Clear()
        {
            responses.Clear();
        }

        #region IWebRequestCreate implementation

        public WebRequest Create(Uri uri)
        {
            if (!responses.ContainsKey(uri))
                throw new KeyNotFoundException(uri.ToString());
            return responses[uri];
        }

        #endregion

        public class MockRequest : WebRequest
        {
            private readonly string content;
            private string method = "GET";
            private ICredentials credentials;
            private Uri uri;
            MemoryStream requestStream;

            public override WebHeaderCollection Headers { get; set; }

            public override string ContentType { get; set; }

            public override long ContentLength { get; set; }

            public override Uri RequestUri
            {
                get
                {
                    return uri;
                }
            }

            public override string Method
            {
                get
                {
                    return this.method;
                }
                set
                {
                    this.method = value;
                }
            }

            public override bool PreAuthenticate
            {
                get
                {
                    return base.PreAuthenticate;
                }
                set
                {
                    base.PreAuthenticate = value;
                }
            }

            public override ICredentials Credentials
            {
                get
                {
                    return credentials;
                }
                set
                {
                    credentials = value;
                }
            }

            public Action<WebRequest> Verifier;

            public MockRequest(Uri uri, string content)
            {
                this.uri = uri;
                this.content = content;
                Headers = new WebHeaderCollection();
            }

            public override WebResponse GetResponse()
            {
                if (Verifier != null)
                    Verifier(this);
                return new MockResponse(this.content);
            }

            public override Stream GetRequestStream()
            {
                return requestStream = new MemoryStream();
            }

            public string Body
            {
                get
                {
                    return Encoding.UTF8.GetString(requestStream.ToArray());
                }
            }
        }

        public class MockResponse : WebResponse
        {
            private readonly string content;

            public MockResponse(string content)
            {
                this.content = content;
            }

            public override System.IO.Stream GetResponseStream()
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(content));
            }

            public override void Close()
            {
                // noop
            }
        }

        public class MockResponseException : WebException
        {
            public HttpStatusCode Code;

            public MockResponseException(HttpStatusCode code, string message)
                : base(Message(code, message), null, 
                       WebExceptionStatus.ProtocolError, Response(code, message))
            {
                this.Code = code;
            }

            new static string Message(HttpStatusCode code, string message)
            {
                return "The remote server returned an error: (" + (int)code + ") "
                + message;
            }

            new private static HttpWebResponse Response(HttpStatusCode code, string description)
            {
                // HACK: there is no public way of creating an HttpWebResponse, but
                // we use it to get to the status code. So synthesize one using
                // reflection that's just good enough.
                object[] cargs =
                    { 
                        null, // uri
                        null, // method
                        new WebConnectionData()
                            .Set("StatusCode", code)
                            .Set("StatusDescription", description)
                            .Unwrap(),
                        null // CookieContainer
                    };
                return Activator.CreateInstance(typeof(HttpWebResponse), 
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null, cargs, null) as HttpWebResponse;
            }

            private class WebConnectionData
            {
                private readonly object data;
                private readonly Type type;

                public WebConnectionData()
                {
                    this.data = Activator.CreateInstance("System", "System.Net.WebConnectionData").Unwrap();
                    this.type = this.data.GetType();
                    Set("Headers", new WebHeaderCollection());
                }

                public WebConnectionData Set(string field, object value)
                {
                    type.GetField(field).SetValue(data, value);
                    return this;
                }

                public object Unwrap()
                {
                    return data;
                }
            }

        }
    }
}
