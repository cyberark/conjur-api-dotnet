using System;
namespace Conjur
{
    /// <summary>
    /// Resource type.
    /// <see cref="https://www.conjur.org/api.html#header-kinds-of-resources"/>: Conjur API  resource kinds
    /// </summary>
    public enum ResourceType
    {
        User,
        Host,
        Layer,
        Group,
        Policy,
        Variable,
        Webservice
    }
}
