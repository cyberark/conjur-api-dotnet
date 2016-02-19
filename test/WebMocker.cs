using System;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ConjurTest
{
    public class WebMocker: IWebRequestCreate
    {
        private IDictionary<Uri, MockRequest> responses = new Dictionary<Uri, MockRequest>();

        public WebMocker()
        {
        }

        public MockRequest Mock(Uri uri, string content)
        {
            return responses[uri] = new MockRequest(uri, content);
        }

        #region IWebRequestCreate implementation

        public WebRequest Create(Uri uri)
        {
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
        }
    }
}

