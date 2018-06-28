// <copyright file="ResourceKind.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Available Conjur resource kind
// </summary>

namespace Conjur
{
    /// <summary>
    /// Resource kind.
    /// <see cref="https://www.conjur.org/api.html#header-kinds-of-resources"/>: Conjur API resource kinds.
    /// </summary>
    public enum ResourceKind
    {
        user,
        host,
        layer,
        group,
        policy,
        variable,
        webservice
    }
}
