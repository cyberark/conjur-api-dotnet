// <copyright file="Role.cs" company="Conjur Inc.">
//     Copyright (c) 2018 Cyberark Ltd. All rights reserved.
// </copyright>
// <summary>
//     Role manipulation routines.
// </summary>

namespace Conjur
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    /// <summary>
    /// Conjur Role reference.
    /// </summary>
    /// A role is an actor in the system, in the classical sense of role-based access control. Roles are the entities which receive permission grants.
    public class Role : Resource
    {
        private readonly string path;

        /// <summary>
        /// Initializes a new instance of the <see cref="Conjur.Role"/> class.
        /// </summary>
        /// <param name="client">Conjur client to use to connect.</param>
        /// <param name="kind">Kind of the role, for example 'group' or 'layer'.</param>
        /// <param name="name">The Role name.</param>
        internal Role(Client client, string kind, string name)
            : base(client, kind, name)
        {
            this.path = $"authz/{client.GetAccountName()}/roles/{WebUtility.UrlEncode(kind)}/{WebUtility.UrlEncode(name)}";
        }

        /// <summary>
        /// Check for the existence of a role.
        /// </summary>
        /// <returns>True if role exists otherwice false.</returns>
        /// Only roles that you have read permission on will be searched.
        public bool Exists()
        {
            WebRequest webRequest = this.Client.AuthenticatedRequest(this.path);
            webRequest.Method = WebRequestMethods.Http.Head;
            try
            {
                webRequest.GetResponse().Close();                
            }
            catch (WebException ex)
            {
                HttpWebResponse responce = ex.Response as HttpWebResponse;
                if (responce != null && responce.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                throw;
            }

            return true;
        }

        /// <summary>
        /// Lists the roles that have been the recipient of a role grant.
        /// </summary>
        /// <returns>Returns IEnumerable RoleMember.</returns>
        /// The creator of the role is always a role member and role administrator.
        public IEnumerable<RoleMember> ListMembers()
        {
            return JsonSerializer<List<RoleMember>>.Read(this.Client.AuthenticatedRequest($"{this.path}?members"));
        }

        /// <summary>
        /// List the roles a role is a member of.
        /// </summary>
        /// <returns>Returns IEnumerable role ids.</returns>
        public IEnumerable<string> ListMemberships()
        {
            return JsonSerializer<List<string>>.Read(this.Client.AuthenticatedRequest($"{this.path}?all"));
        }
    }
}
