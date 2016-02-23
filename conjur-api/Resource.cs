// <copyright file="Resource.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Base class representing a Conjur resource.
// </summary>

namespace Conjur
{
    using System.Net;

    /// <summary>
    /// Base class representing a Conjur resource.
    /// </summary>
    public class Resource
    {
        /// <summary>
        /// The Conjur client used to manipulate this resource.
        /// </summary>
        protected readonly Client Client;

        private readonly string kind;
        private readonly string id;
        private string resourcePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="Conjur.Resource"/> class.
        /// </summary>
        /// <param name="client">Conjur client used to manipulate this resource.</param>
        /// <param name="kind">Resource kind.</param>
        /// <param name="id">Resource identifier.</param>
        public Resource(Client client, string kind, string id)
        {
            this.Client = client;
            this.kind = kind;
            this.id = id;
        }

        /// <summary>
        /// Gets the resource path.
        /// </summary>
        /// <value>The resource path.</value>
        protected string ResourcePath
        {
            get
            {
                if (this.resourcePath == null)
                    this.resourcePath = "authz/" +
                    WebUtility.UrlEncode(this.Client.GetAccountName()) + "/resources/" +
                    WebUtility.UrlEncode(this.kind) + "/" + WebUtility.UrlEncode(this.id);
                return this.resourcePath;
            }
        }

        /// <summary>
        /// Determines whether the authenticated user has the specified privilege
        /// on this resource.
        /// </summary>
        /// <returns><c>true</c> if this instance has the specified privilege; otherwise, <c>false</c>.</returns>
        /// <param name="privilege">Privilege to check.</param>
        public bool CanI(string privilege)
        {
            var req = this.Client.AuthenticatedRequest(this.ResourcePath
                          + "/?check=true&privilege=" + WebUtility.UrlEncode(privilege));
            try
            {
                req.GetResponse();
                return true;
            }
            catch (WebException exn)
            {
                var hr = exn.Response as HttpWebResponse;
                if (hr != null && hr.StatusCode == HttpStatusCode.Forbidden)
                    return false;
                throw;
            }
        }
    }
}