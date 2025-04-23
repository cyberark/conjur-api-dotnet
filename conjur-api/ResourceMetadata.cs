// <copyright file="ResourceMetadata.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Class representing resource metadata returned from web request.
// </summary>

using System.Runtime.Serialization;

namespace Conjur;

[DataContract]
public class ResourceMetadata
{
    [DataMember(Name = "id")]
    public string Id { get; set; }
    [DataMember(Name = "policy")]
    public string Policy { get; set; }
    [DataMember(Name = "created_at")]
    public string CreatedAt { get; set; }
    [DataMember(Name = "owner")]
    public string Owner { get; set; }
    [DataMember(Name = "permissions")]
    public Permission[] Permissions { get; set; }
    [DataMember(Name = "annotations")]
    public Annotation[] Annotations { get; set; }
    [DataMember(Name = "secrets")]
    public Secrets[] Secrets { get; set; }
}

[DataContract]
public class Permission
{
    [DataMember(Name = "privilege")]
    public string Privilege { get; set; }
    [DataMember(Name = "grant_option")]
    public string GrantOption { get; set; }
    [DataMember(Name = "resource")]
    public string Resource { get; set; }
    [DataMember(Name = "role")]
    public string Role { get; set; }
    [DataMember(Name = "grantor")]
    public string Grantor { get; set; }
}

[DataContract]
public class Annotation
{
    [DataMember(Name = "name")]
    public string Name { get; set; }
    [DataMember(Name = "value")]
    public string Value { get; set; }
    [DataMember(Name = "policy")]
    public string Policy { get; set; }
}

[DataContract]
public class Secrets
{
    [DataMember(Name = "version")]
    public string Version { get; set; }
    [DataMember(Name = "expires_at")]
    public string ExpiresAt { get; set; }
}

[DataContract]
internal class CountResult
{
    [DataMember(Name = "count")]
    public uint Count { get; set; }
}
