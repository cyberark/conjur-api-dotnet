// <copyright file="Client.Methods.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Conjur Client methods delegating to entity-specific classes.
// </summary>
namespace Conjur
{
    using System.Collections.Generic;

    /// <summary>
    /// Entity-specific methods for the Client facade.
    /// </summary>
    public partial class Client
    {
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
        /// Lists Conjur variables.
        /// </summary>
        /// <returns>A list of variables</returns>
        /// <param name="query">Query.</param>
        public IEnumerable<Variable> ListVariables(string query = null)
        {
            return Conjur.Variable.List(this, query);
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
        /// <param name="kind">Resource kind. @<see cref="ResourceKind"/></param>
        /// <param name="name">Resource Name.</param>
        /// <returns>Object representing the specified resource.</returns>
        public Resource Resource(string kind, string name)
        {
            return new Resource(this, kind, name);
        }
    }
}