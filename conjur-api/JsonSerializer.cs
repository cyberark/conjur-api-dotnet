// <copyright file="JsonSerializer.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
// JSON utilities.
// </summary>

namespace Conjur
{
    using System.Net;
    using System.Runtime.Serialization.Json;

    internal static class JsonSerializer<T> where T : class
    {
        // Analysis disable once StaticFieldInGenericType
        // (The behaviour is exactly what I want: one instance per T.)
        private static readonly DataContractJsonSerializer Instance =
            new DataContractJsonSerializer(typeof(T));

        public static T Read(WebRequest request)
        {
            using (var stream = request.GetResponse().GetResponseStream())
                return Instance.ReadObject(stream) as T;
        }
    }
}