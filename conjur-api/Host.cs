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
        [DataMember]
        private string id;
        [DataMember]
        private string api_key;

        private NetworkCredential credential;

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
                    if (this.api_key == null)
                        throw new InvalidOperationException("Unknown host API key");
                    
                    this.credential = new NetworkCredential(
                        this.UserName, this.api_key);
                }

                return this.credential;
            }
        }
    }
}