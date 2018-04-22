// <copyright file="Client.Methods.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Conjur Client methods delegating to entity-specific classes.
// </summary>

using System.Collections.Generic;

namespace Conjur
{
    /// <summary>
    /// Entity-specific methods for the Client facade.
    /// </summary>
    public partial class Client
    {
        private uint limitSearchVarListsReturned = 1000;

        /// <summary>
        /// Creates an object representing the named variable.
        /// </summary>
        /// Note the existence of the variable is not verified.
        /// <param name="name">The variable name.</param>
        /// <returns>Variable object.</returns>
        /// <seealso cref="Variable()"/>
        public Variable Variable(string name)
        {
            return new Variable(this, name);
        }

        /// <summary>
        /// Search for variables
        /// </summary>
        /// <param name="query">Query for search.</param>
        /// <param name="actingAs">Fully-qualified Conjur ID of a role to act as.</param>
        /// <returns>Returns IEnumerable to VariableInfo.</returns>
        public IEnumerable<Variable> SearchVariables(string query = null, string actingAs = null)
        {
            uint offset = 0;
            List<SearchVariableResult> searchVarsResult;
            do
            {
                string urlWithParams = $"authz/{GetAccountName()}/resources/variable?offset={offset}"
                                      + $"&limit={limitSearchVarListsReturned}"
                                      + ((query != null) ? $"&search={query}" : string.Empty)
                                      + ((actingAs != null) ? $"&acting_as={actingAs}" : string.Empty);

                searchVarsResult = JsonSerializer<List<SearchVariableResult>>.Read(this.AuthenticatedRequest(urlWithParams));
                foreach (SearchVariableResult searchVarResult in searchVarsResult)
                {
                    yield return new Variable(this, searchVarResult.Id);
                }

                offset += (uint)searchVarsResult.Count;
            } while (searchVarsResult.Count > 0);
        }

        /// <summary>
        /// Creates a host using a host factory token.
        /// </summary>
        /// <returns>The created host.</returns>
        /// <param name="name">Name of the host to create.</param>
        /// <param name="hostFactoryToken">Host factory token.</param>
        public Host CreateHost(string name, string hostFactoryToken)
        {
            return new HostFactoryToken(this, hostFactoryToken)
                .CreateHost(name);
        }

        /// <summary>
        /// Creates an object representing a Conjur resource.
        /// </summary>
        /// <param name="kind">Resource kind.</param>
        /// <param name="id">Resource identifier.</param>
        /// <returns>Object representing the specified resource.</returns>
        public Resource Resource(string kind, string id)
        {
            return new Resource(this, kind, id);
        }
    }
}