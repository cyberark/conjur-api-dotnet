// <copyright file="Client.Methods.cs "company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Conjur Client methods delegating to entity-specific classes.
// </summary>

namespace Conjur;

/// <summary>
/// Entity-specific methods for the Client facade.
/// </summary>
public partial class Client
{
    public uint CountResources(string kind, string query = null)
    {
        return Conjur.Resource.CountResources(this, kind, query);
    }

    public Task<uint> CountResourcesAsync(string kind, string query = null, CancellationToken cancellationToken = default)
    {
        return Conjur.Resource.CountResourcesAsync(this, kind, query, cancellationToken);
    }

    /// <summary>
    /// Creates an object representing the named variable.
    /// </summary>
    /// Note the existence of the variable is not verified.
    /// <param name="name">The variable name.</param>
    /// <returns>Variable object.</returns>
    /// <seealso cref="Variable(string)"/>
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
    /// Lists Conjur resource of kind variable.
    /// </summary>
    /// <param name="query">Additional Query parameters, not required.</param>
    /// <param name="limit">Additional limit parameters, not required.</param>
    /// <param name="offset">Additional offset parameters, not required.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A list of variables objects.</returns>
    public IAsyncEnumerable<Variable> ListVariablesAsync(string query = null, uint limit = 10000, uint offset = 0, CancellationToken cancellationToken = default)
    {
        return Conjur.Variable.ListAsync(this, query, limit, offset, cancellationToken);
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
    /// Count Conjur resource of kind variable.
    /// </summary>
    /// <param name="query">Additional Query parameters, not required.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A number represent the number of Variables records.</returns>
    public Task<uint> CountVariablesAsync(string query = null, CancellationToken cancellationToken = default)
    {
        return Conjur.Variable.CountAsync(this, query, cancellationToken);
    }

    /// <summary>
    /// Create an object representing a Conjur resource of kind "user" corresponding with the specific name.
    /// </summary>
    /// <param name="name">A Name for the requested user.</param>
    /// <returns>An Object representing a user.</returns>
    /// <seealso cref="User(string)"/>
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
    /// Lists Conjur resources of kind user.
    /// </summary>
    /// <param name="query">Additional Query parameters, not required.</param>
    /// <param name="limit">Additional limit parameters, not required.</param>
    /// <param name="offset">Additional offset parameters, not required.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A list of users objects.</returns>
    public IAsyncEnumerable<User> ListUsersAsync(string query = null, uint limit = 10000, uint offset = 0, CancellationToken cancellationToken = default)
    {
        return Conjur.User.ListAsync(this, query, limit, offset, cancellationToken);
    }

    /// <summary>
    /// Create Conjur policy object, however not loading it to Conjur
    /// In order to load it use LoadPolicy(Stream policyContent) method.
    /// </summary>
    /// <param name="policyName">Name of policy.</param>
    /// <seealso cref="Policy(string)"/>
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
        return new HostFactoryToken(this, hostFactoryToken).CreateHost(name);
    }

    /// <summary>
    /// Creates a host using a host factory token.
    /// </summary>
    /// <param name="name">Name of the host to create.</param>
    /// <param name="hostFactoryToken">Host factory token.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The created host.</returns>
    public Task<Host> CreateHostAsync(string name, string hostFactoryToken, CancellationToken cancellationToken = default)
    {
        return new HostFactoryToken(this, hostFactoryToken).CreateHostAsync(name, cancellationToken);
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
    /// Acting as role is passed to new instance of client.
    /// </summary>
    /// <returns>New instance of impersonated client with requested role.</returns>
    /// <param name="role">Conjur Role.</param>
    public Client ActingAs(string role)
    {
        return new Client(this, role);
    }
}
