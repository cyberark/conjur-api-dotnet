// <copyright file="Client.Methods.cs "company="CyberArk Software Ltd.">
//     Copyright (c) 2020 CyberArk Software Ltd. All rights reserved.
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
        public uint CountResources(string kind, string query = null)
        {
            return Conjur.Resource.CountResources(this, kind, query);
        }

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
        /// Lists Conjur resource of kind variable.
        /// </summary>
        /// <param name="query">Additional Query parameters, not required.</param>
        /// <param name="limit">Additional limit parameters, not required.</param>
        /// <param name="offset">Additional offset parameters, not required.</param>
        /// <returns>A list of variables objects.</returns>
        public IEnumerable<Variable> ListVariables(string query = null, uint limit = 10000, uint offset = 0)
        {
            return Conjur.Variable.List(this, query, limit, offset);
        }

        /// <summary>
        /// Count Conjur resource of kind variable.
        /// </summary>
        /// <param name="query">Additional Query parameters, not required.</param>
        /// <returns>A number represent the number of Variables records.</returns>
        public uint CountVariables(string query = null)
        {
            return Conjur.Variable.Count(this, query);
        }

        /// <summary>
        /// Create an object representing a Conjur resource of kind "user" corresponding with the specific name.
        /// </summary>
        /// <param name="name">A Name for the requested user.</param>
        /// <returns>An Object respresenting a user.</returns>
        /// <seealso cref="User()"/>
        public User User(string name)
        {
            return new User(this, name);
        }

        /// <summary>
        /// Lists Conjur resources of kind user.
        /// </summary>
        /// <param name="query">Additional Query parameters, not required.</param>
        /// <param name="limit">Additional limit parameters, not required.</param>
        /// <param name="offset">Additional offset parameters, not required.</param>
        /// <returns>A list of users objects.</returns>
        public IEnumerable<User> ListUsers(string query = null, uint limit = 10000, uint offset = 0)
        {
            return Conjur.User.List(this, query, limit, offset);
        }

        /// <summary>
        /// Create Conjur policy object, however not loading it to Conjur
        /// In order to load it use LoadPolicy(Stream policyContent) method.
        /// </summary>
        /// <param name="policyName">Name of policy.</param>
        /// <seealso cref="Policy()"/>
        /// <returns>Policy entity.</returns>
        public Policy Policy(string policyName)
        {
            return new Policy(this, policyName);
        }

        /// <summary>
        /// Creates a host using a host factory token.
        /// </summary>
        /// <param name="name">Name of the host to create.</param>
        /// <param name="hostFactoryToken">Host factory token.</param>
        /// <returns>The created host.</returns>
        public Host CreateHost(string name, string hostFactoryToken)
        {
            return new HostFactoryToken(this, hostFactoryToken)
                .CreateHost(name);
        }

        /// <summary>
        /// Creates an object representing a Conjur general resource.
        /// </summary>
        /// <param name="kind">Resource kind.</param>
        /// <param name="name">Resource Name.</param>
        /// <returns>Object representing the specified resource.</returns>
        public Resource Resource(string kind, string name)
        {
            return new Resource(this, kind, name);
        }

        /// <summary>
        /// Actings as role is passed to new instanace of client.
        /// </summary>
        /// <returns>New instance of impersonated client with requested role.</returns>
        /// <param name="role">Conjur Role.</param>
        public Client ActingAs(string role)
        {
            return new Client(this, role);
        }
    }
}
