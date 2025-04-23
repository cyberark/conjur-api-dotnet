// <copyright file="ApiConfigurationManager.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Configuration class for this API project
// </summary>

using System.Configuration;

namespace Conjur;

public sealed class ApiConfigurationManager
{
    public const string HTTP_REQUEST_TIMEOUT = "HTTP_REQUEST_TIMEOUT";
    public const string TOKEN_REFRESH_TIMEOUT = "TOKEN_REFRESH_TIMEOUT";

    private readonly object locker = new();
    private int? httpRequestTimeout;
    private uint? tokenRefreshTimeout;

    private ApiConfigurationManager()
    {
    }

    public static ApiConfigurationManager GetInstance()
    {
        return Nested.Instance;
    }

    /// <summary>
    /// Gets/Sets the global http request timeout configuration.
    /// Request timeout configuration in milliseconds, default is: 100000.
    /// </summary>
    public int HttpRequestTimeout
    {
        get
        {
            if (httpRequestTimeout == null)
            {
                lock (locker)
                {
                    if (httpRequestTimeout == null)
                    {
                        string value = ConfigurationManager.AppSettings.Get(HTTP_REQUEST_TIMEOUT);
                        httpRequestTimeout = 100000; //100 seconds; WebRequest default timeout
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            httpRequestTimeout = Convert.ToInt32(value);
                        }
                    }
                }
            }
            return httpRequestTimeout.Value;
        }
        set => httpRequestTimeout = value;
    }


    /// <summary>
    /// Gets/Sets the global token refresh timeout configuration.
    /// Token refresh timeout configuration in milliseconds, default is: 420000 (7 minutes).
    /// </summary>
    public uint TokenRefreshTimeout
    {
        get
        {
            if (tokenRefreshTimeout == null)
            {
                lock (locker)
                {
                    if (tokenRefreshTimeout == null)
                    {
                        string value = ConfigurationManager.AppSettings.Get(TOKEN_REFRESH_TIMEOUT);
                        tokenRefreshTimeout = 420000; //7 minutes
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            tokenRefreshTimeout = Convert.ToUInt32(value);
                        }
                    }
                }
            }
            return tokenRefreshTimeout.Value;
        }
        set => tokenRefreshTimeout = value;
    }

    private class Nested
    {
        internal static readonly ApiConfigurationManager Instance = new();

        static Nested()
        {
        }
    }
}
