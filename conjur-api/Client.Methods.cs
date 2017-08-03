// <copyright file="Client.Methods.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Conjur Client methods delegating to entity-specific classes.
// </summary>

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