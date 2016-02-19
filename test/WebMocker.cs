using System;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ConjurTest
{
    public class WebMocker: IWebRequestCreate
    {
        private IDictionary<Uri, string> responses = new Dictionary<Uri, string>();

        public WebMocker()
        {
        }

        public void Mock(Uri uri, string content)
        {
            responses[uri] = content;
        }

        #region IWebRequestCreate implementation

        public WebRequest Create(Uri uri)
        {
            return new MockRequest(responses[uri]);
        }

        #endregion

        public class MockRequest : WebRequest
        {
            private readonly string content;

            override public string Method
            {
                set
                {
                }
            }

            public MockRequest(string content)
            {
                this.content = content;
            }

            public override WebResponse GetResponse()
            {
                return new MockResponse(this.content);
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

