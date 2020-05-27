// <copyright file="JsonSerializer.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2020 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
// JSON utilities.
// </summary>

using System.Net;
using System.Runtime.Serialization.Json;

namespace Conjur
{
    internal static class JsonSerializer<T> where T : class
    {
        // Analysis disable once StaticFieldInGenericType
        // (The behaviour is exactly what I want: one instance per T.)
        private static readonly DataContractJsonSerializer Instance =
            new DataContractJsonSerializer(typeof(T));

        public static T Read(WebRequest request)
        {
            using (System.IO.Stream stream = request.GetResponse().GetResponseStream()) {
                return Instance.ReadObject(stream) as T;
            }
        }
    }
}
