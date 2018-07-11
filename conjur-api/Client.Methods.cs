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
        /// Lists the variables.
        /// </summary>
        /// <returns>The variabßles.</returns>
        /// <param name="query">Query.</param>
        public IEnumerable<Variable> ListVariables(string query = null)
        {
            return Conjur.Variable.List(this, query);
        }

        /// <summary>
        /// Create an onject representing the User with this specified name.
        /// </summary>
        /// <returns>The user.</returns>
        /// <param name="name">Name.</param>
        /// <returns>User object.</returns>
        /// <seealso cref="User()"/>
        public User User(string name)
        {
            return new User(this, name);
        }

        /// <summary>
        /// Lists the users.
        /// </summary>
        /// <returns>List of users.</returns>
        /// <param name="query">Query.</param>
        public IEnumerable<User> ListUsers(string query = null)
        {
            return Conjur.User.List(this, query);
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

        /// <summary>
        /// Actings as role is passed to new instanace of client.
        /// </summary>
        /// <returns>New instance of impersonated client with requestd role.</returns>
        /// <param name="role">Role.</param>
        public Client ActingAs(string role)
        {
            return new Client(this, role);
        }
    }
}