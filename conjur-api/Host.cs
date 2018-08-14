// <copyright file="Host.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Host objects.
// </summary>

namespace Conjur
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;

    /// <summary>
    /// Conjur host resource and role.
    /// </summary>
    [DataContract]
    public class Host
    {
        // these data members are assigned by a deserializer
        #pragma warning disable 169
        [DataMember]
        private string id;
        [DataMember]
        private string api_key;
        #pragma warning restore 169

        private NetworkCredential credential;

        private Host()
        {
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id
        {
            get
            {
                return this.id;
            }
        }

        /// <summary>
        /// Gets the API key.
        /// </summary>
        /// <value>The API key, or null if unknown.</value>
        public string ApiKey
        {
            get
            {
                return this.api_key;
            }
        }

        /// <summary>
        /// Gets the authn username corresponding to the host.
        /// </summary>
        /// <value>The authn username.</value>
        public string UserName
        {
            get
            {
                return "host/" + this.id;
            }
        }

        /// <summary>
        /// Gets the credential.
        /// </summary>
        /// This will be authn username and API key of the host.
        /// <value>The credential.</value>
        public NetworkCredential Credential
        {
            get
            {
                if (this.credential == null)
                {
                    if (this.ApiKey == null)
                        throw new InvalidOperationException("Unknown host API key");
                    
                    this.credential = new NetworkCredential(
                        this.UserName, this.ApiKey);
                }

                return this.credential;
            }
        }
    }
}