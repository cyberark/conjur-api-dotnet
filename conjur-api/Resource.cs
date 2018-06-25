// <copyright file="Resource.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Base class representing a Conjur resource.
// </summary>

namespace Conjur
{
    using System;
    using System.Net;
    using System.Collections.Generic;

    /// <summary>
    /// Base class representing a Conjur resource.
    /// </summary>
    public class Resource
    {
        /// <summary>
        /// The Conjur client used to manipulate this resource.
        /// </summary>
        protected readonly Client m_client;

        /// <summary>
        /// Gets resource name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the identifier assmbled from account:kind:name.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; }

        private readonly ResourceKind m_kind;
        private string m_resourcePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="Conjur.Resource"/> class.
        /// </summary>
        /// <param name="client">Conjur client used to manipulate this resource.</param>
        /// <param name="kind">Resource kind.</param>
        /// <param name="name">Resource name.</param>
        internal Resource(Client client, ResourceKind kind, string name)
        {
            m_client = client;
            m_kind = kind;
            Name = name;
            Id = $"{client.GetAccountName()}:{kind}:{Name}";
        }
	
        /// <summary>
        /// Gets the resource path.
        /// </summary>
        /// <value>The resource path.</value>
        protected string ResourcePath
        {
            get
            {
                if (m_resourcePath == null)
                {
                    m_resourcePath = $"resources/{WebUtility.UrlEncode(m_client.GetAccountName())}/{m_kind}/{WebUtility.UrlEncode(Name)}";
                }
                return m_resourcePath;
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
            WebRequest req = m_client.AuthenticatedRequest($"{ResourcePath}/?check=true&privilege={WebUtility.UrlEncode(privilege)}");
            req.Method = WebRequestMethods.Http.Head;

            try
            {
                req.GetResponse().Close();
                return true;
            }
            catch (WebException exn)
            {
                HttpWebResponse hr = exn.Response as HttpWebResponse;
                if (hr != null && hr.StatusCode == HttpStatusCode.Forbidden)
                {
                    return false;
                }
                throw;
            }
        }

        internal static IEnumerable<T> ListResources<T, TResult>(Client client, ResourceKind kind, Func<TResult, T> newT, string query = null, uint limit = 1000, uint offset = 0)
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
            } while (resultList.Count > 0);
        }
    }
}
