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
        private const uint LIMIT_SEARCH_VAR_LIST_RETURNED = 1000;

        private readonly string kind;
        private string resourcePath;
        

        public string Id { get; }


        /// <summary>
        /// Initializes a new instance of the <see cref="Conjur.Resource"/> class.
        /// </summary>
        /// <param name="client">Conjur client used to manipulate this resource.</param>
        /// <param name="kind">Resource kind.</param>
        /// <param name="id">Resource identifier.</param>
        internal Resource(Client client, string kind, string id)
        {
            this.Client = client;
            this.kind = kind;
            this.Id = id;
        }

        /// <summary>
        /// List or Search for resources. Generic method that can be adapted to various kinds of resources.
        /// To reduce overhead, list is retrieved in chunks behind the scenes.
        /// </summary>
        /// <param name="newT">A method that gets as arguments TResult and returns new instance of type T.</param>
        /// <param name="client">Conjur client to query.</param>
        /// <param name="kind">Resource kind to query.</param>
        /// <param name="query">Query for search.</param>
        /// <returns>Returns IEnumerable<T>.</returns>
        internal static IEnumerable<T> ListResources<T, TResult>(Client client, string kind, Func<TResult, T> newT, string query = null) 
        {
            uint offset = 0;
            List<TResult> resultList;
            do
            {
                string urlWithParams = $"authz/{client.GetAccountName()}/resources/{kind}?offset={offset}"
                                      + $"&limit={LIMIT_SEARCH_VAR_LIST_RETURNED}"
                                      + ((query != null) ? $"&search={query}" : string.Empty)
                                      + ((client.ActingAs != null) ? $"&acting_as={client.ActingAs}" : string.Empty);

                resultList = JsonSerializer<List<TResult>>.Read(client.AuthenticatedRequest(urlWithParams));
                foreach (TResult searchVarResult in resultList)
                {
                    yield return newT(searchVarResult);
                }

                offset += (uint)resultList.Count;
            } while (resultList.Count > 0);
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
                    WebUtility.UrlEncode(this.kind) + "/" + WebUtility.UrlEncode(this.Id);
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
    }
}