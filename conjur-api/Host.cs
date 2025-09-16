// <copyright file="Host.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Host objects.
// </summary>

using System.Runtime.Serialization;

namespace Conjur;

/// <summary>
/// Conjur host resource and role.
/// </summary>
[DataContract]
public class Host
{
    // these data members are assigned by a deserializer
#pragma warning disable 169, 0649
    [DataMember]
    private string id;

    [DataMember]
    private string api_key;
#pragma warning restore 169, 0649

    private NetworkCredential credential;

    private Host()
    {
    }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    /// <value>The identifier.</value>
    public string Id => id;

    /// <summary>
    /// Gets the API key.
    /// </summary>
    /// <value>The API key, or null if unknown.</value>
    public string ApiKey => api_key;

    /// <summary>
    /// Gets the authn username corresponding to the host.
    /// </summary>
    /// <value>The authn username.</value>
    public string UserName => "host/" + id;

    /// <summary>
    /// Gets the credential.
    /// </summary>
    /// This will be authn username and API key of the host.
    /// <value>The credential.</value>
    public NetworkCredential Credential
    {
        get
        {
            if (credential is null)
            {
                if (ApiKey is null)
                {
                    throw new InvalidOperationException("Unknown host API key");
                }

                credential = new NetworkCredential(UserName, ApiKey);
            }

            return credential;
        }
    }
}
