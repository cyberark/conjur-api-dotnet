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
<<<<<<< HEAD
        /// Lists Conjur resource of kind variable.
        /// </summary>
        /// <param name="query">Additional Query parameters, not required.</param>
        /// <returns>A list of variables objects.</returns>
=======
        /// Lists Conjur variables.
        /// </summary>
        /// <returns>A list of variables</returns>
        /// <param name="query">Query.</param>
>>>>>>> 9b1fc3a577d209f9dc2470af980fdd7e44a95d22
        public IEnumerable<Variable> ListVariables(string query = null)
        {
            return Conjur.Variable.List(this, query);
        }

        /// <summary>
<<<<<<< HEAD
        /// Create an object representing a Conjur ressource of kind user corresponding with the specifiy name.
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
        /// <returns>A list of users objects.</returns>
        public IEnumerable<User> ListUsers(string query = null)
        {
            return Conjur.User.List(this, query);
        }

        /// <summary>
=======
>>>>>>> 9b1fc3a577d209f9dc2470af980fdd7e44a95d22
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
        /// <param name="kind">Resource kind. @<see cref="ResourceKind"/></param>
        /// <param name="name">Resource Name.</param>
        /// <returns>Object representing the specified resource.</returns>
<<<<<<< HEAD
        public Resource Resource(ResourceKind kind, string name)
=======
        public Resource Resource(string kind, string name)
>>>>>>> 9b1fc3a577d209f9dc2470af980fdd7e44a95d22
        {
            return new Resource(this, kind, name);
        }
    }
}