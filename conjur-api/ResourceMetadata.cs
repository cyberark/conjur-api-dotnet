// <copyright file="VariableInfo.cs" company="Cyberark Inc.">
//     Copyright (c) 2018 Cyberark Inc. All rights reserved.
// </copyright>
// <summary>
//     Resource metadata for deserialization, returned from List\Search Resource
// </summary>


namespace Conjur
{
    using System.Runtime.Serialization;

    [DataContract]
    public class ResourceMetadata
    {
        // these data members are assigned by a deserializer
#pragma warning disable 169
        [DataMember(Name = "id")]
        public string Id { get; private set; }
        [DataMember(Name = "created_at")]
        public string CreatedAt { get; private set; }
        [DataMember(Name = "owner")]
        public string Owner { get; private set; }
        [DataMember(Name = "created_by")]
        public string CreatedBy { get; private set; }
        [DataMember(Name = "permissions")]
        public Permission[] Permissions { get; private set; }
        [DataMember(Name = "annotations")]
        public Annoatation[] Annoatations { get; private set; }
    }

    [DataContract]
    public class Permission
    {
        [DataMember(Name = "privilege")]
        public string Privilege { get; private set; }
        [DataMember(Name = "grant_option")]
        public string GrantOption { get; private set; }
        [DataMember(Name = "resource")]
        public string Resource { get; private set; }
        [DataMember(Name = "role")]
        public string Role { get; private set; }
        [DataMember(Name = "grantor")]
        public string Grantor { get; private set; }
    }

    [DataContract]
    public class Annoatation
    {
        [DataMember(Name = "resource_id")]
        public string ResourceId { get; private set; }
        [DataMember(Name = "name")]
        public string Name { get; private set; }
        [DataMember(Name = "value")]
        public string Value { get; private set; }
        [DataMember(Name = "created_at")]
        public string CreatedAt { get; private set; }
        [DataMember(Name = "updated_at")]
        public string UpdatedAt { get; private set; }
    }

    [DataContract]
    public class RoleMember
    {
        [DataMember(Name = "admin_option")]
        public bool Admin { get; set; }
        [DataMember(Name = "grantor")]
        public string Grantor { get; set; }
        [DataMember(Name = "member")]
        public string Member { get; set; }
        [DataMember(Name = "role")]
        public string Role { get; set; }
    }
}
