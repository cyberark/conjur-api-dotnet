// <copyright file="Resource.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2020 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Base class representing a Conjur resource.
// </summary>

using System;
using System.Collections.Generic;
using System.Net;

namespace Conjur
{
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
        internal Resource(Client client, string kind, string name)
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
                if (this.resourcePath == null) {
                    this.resourcePath = "resources/" +
                    Uri.EscapeDataString(this.Client.GetAccountName()) + "/" +
                    Uri.EscapeDataString(this.kind) + "/" + Uri.EscapeDataString(this.Name);
                }

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
            WebRequest req = this.Client.AuthenticatedRequest(this.ResourcePath
                          + "/?check=true&privilege=" + Uri.EscapeDataString(privilege));
            req.Method = "HEAD";

            try
            {
                req.GetResponse().Close();
                return true;
            }
            catch (WebException exn)
            {
                HttpWebResponse hr = exn.Response as HttpWebResponse;
                if (hr != null && hr.StatusCode == HttpStatusCode.Forbidden) {
                    return false;
                }

                throw;
            }
        }

        internal static IEnumerable<T> ListResources<T, TResult>(Client client, string kind, Func<TResult, T> newT,
         string query = null, uint limit = 10000, uint offset = 0)
        {
            List<TResult> resultList;
            do
            {
                string pathListResourceQuery = $"resources/{client.GetAccountName()}/{kind}?offset={offset}&limit={limit}"
                    + ((query != null) ? $"&search={query}" : string.Empty);

                resultList = JsonSerializer<List<TResult>>.Read(client.AuthenticatedRequest(pathListResourceQuery));
                foreach (TResult searchResult in resultList)
                {
                    yield return newT(searchResult);
                }

                offset += (uint)resultList.Count;
            } while (resultList.Count > 0 && offset % limit == 0);
        }

        internal static uint CountResources(Client client, string kind, string query = null)
        {
            string pathCountResourceQuery = $"resources/{client.GetAccountName()}/{kind}?count=true" + ((query != null) ? $"&search={query}" : string.Empty);
            CountResult countJsonObj = JsonSerializer<CountResult>.Read(client.AuthenticatedRequest(pathCountResourceQuery));
            return Convert.ToUInt32(countJsonObj.Count);
        }

        /// <summary>
        /// Parse Conjur id following format of acount:kind:name to extract name.
        /// </summary>
        /// <returns>Extracted name from id.</returns>
        /// <param name="id">Conjur Identifier.</param>
        /// <param name="account">Conjur Account.</param>
        /// <param name="kind">Conjur resource kind.</param>
        protected static string IdToName(string id, string account, string kind)
        {
            return id.Substring($"{account}:{kind}:".Length);
        }
    }
}
