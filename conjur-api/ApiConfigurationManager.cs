// <copyright file="ApiConfigurationManager.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Configuration class for this API project
// </summary>
namespace Conjur
{
    using System;
    using System.Configuration;

    public sealed class ApiConfigurationManager
    {
        private ApiConfigurationManager()
        {
        }

        public static ApiConfigurationManager GetInstance()
        {
            return Nested.Instance;
        }

        /// <summary>
        /// Gets the global http request timeout configuration.
        /// </summary>
        /// <returns>Request timeout configuration in milliseconds, default is: 100000.</returns>
        public int GetHttpRequestTimeout()
        {
            string value = ConfigurationManager.AppSettings.Get("HTTP_REQUEST_TIMEOUT");
            int returnValue = 100000;
            if (!string.IsNullOrWhiteSpace(value)) 
            {
                returnValue = Convert.ToInt32(ConfigurationManager.AppSettings.Get("HTTP_REQUEST_TIMEOUT"));
            }

            return returnValue;
        }

        private class Nested
        {
            internal static readonly ApiConfigurationManager Instance = new ApiConfigurationManager();

            static Nested()
            {
            }
        }
    }
}