// <copyright file="Resource.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Base class representing a Conjur resource.
// </summary>

namespace Conjur
{
    using System;
    using System.Collections.Generic;
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

<<<<<<< HEAD
        private readonly ResourceKind kind;
=======
        private readonly string kind;
>>>>>>> 9b1fc3a577d209f9dc2470af980fdd7e44a95d22
        private string resourcePath;

        /// <summary>
        /// Gets resource name.
        /// </summary>
        /// <value>The name of the resource.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the resource identifier, in format of account:kind:name.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Conjur.Resource"/> class.
        /// </summary>
        /// <param name="client">Conjur client used to manipulate this resource.</param>
        /// <param name="kind">Resource kind.</param>
        /// <param name="name">Resource name.</param>
<<<<<<< HEAD
        internal Resource(Client client, ResourceKind kind, string name)
=======
        internal Resource(Client client, string kind, string name)
>>>>>>> 9b1fc3a577d209f9dc2470af980fdd7e44a95d22
        {
            this.Client = client;
            this.kind = kind;
            this.Name = name;
            this.Id = $"{client.GetAccountName()}:{kind}:{Name}";
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
                    this.resourcePath = "resources/" +
                    WebUtility.UrlEncode(this.Client.GetAccountName()) + "/" +
<<<<<<< HEAD
                    WebUtility.UrlEncode(this.kind.ToString()) + "/" + WebUtility.UrlEncode(this.Name);
=======
                    WebUtility.UrlEncode(this.kind) + "/" + WebUtility.UrlEncode(this.Name);
>>>>>>> 9b1fc3a577d209f9dc2470af980fdd7e44a95d22
                return this.resourcePath;
            }
        }

        /// <summary>
        /// Determines whether the authenticated user holds the specified privilege
        /// on this resource.
        /// </summary>
        /// <returns><c>true</c> if the authenticated user holds the specified
        /// privilege; otherwise, <c>false</c>.</returns>
        /// <param name="privilege">Privilege to check.</param>
        public bool Check(string privilege)
        {
            var req = this.Client.AuthenticatedRequest(this.ResourcePath
                          + "/?check=true&privilege=" + WebUtility.UrlEncode(privilege));
            req.Method = "HEAD";

            try
            {
                req.GetResponse().Close();
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

<<<<<<< HEAD
        internal static IEnumerable<T> ListResources<T, TResult>(Client client, ResourceKind kind, Func<TResult, T> newT, string query = null, uint limit = 1000, uint offset = 0)
=======
        internal static IEnumerable<T> ListResources<T, TResult>(Client client, string kind, Func<TResult, T> newT, string query = null, uint limit = 1000, uint offset = 0)
>>>>>>> 9b1fc3a577d209f9dc2470af980fdd7e44a95d22
        {
            List<TResult> resultList;
            do
            {
                string pathListResourceQuery = $"resources/{client.GetAccountName()}?{kind}&offset={offset}&limit={limit}"
                    + ((query != null) ? $"&search={query}" : string.Empty);

                resultList = JsonSerializer<List<TResult>>.Read(client.AuthenticatedRequest(pathListResourceQuery));
                foreach (TResult searchResult in resultList) 
                {
                    yield return newT(searchResult);
                }

                offset += (uint)resultList.Count;
            } while(resultList.Count > 0);
        }

<<<<<<< HEAD
        protected static string IdToName(string id, string account, ResourceKind kind)
        {
            return id.Substring(id.IndexOf($"{account}:{kind}:", StringComparison.CurrentCulture) + 1);
=======
        protected static string IdToName(string id, string account, string kind)
        {
            return id.Substring ($"{account}:{kind}:".Length);
>>>>>>> 9b1fc3a577d209f9dc2470af980fdd7e44a95d22
        }
    }
}
