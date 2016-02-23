using System.Runtime.Serialization.Json;
using System.Net;

namespace Conjur
{
    static class JsonSerializer<T> where T: class
    {
        // Analysis disable once StaticFieldInGenericType
        // (The behaviour is exactly what I want: one instance per T.)
        static readonly DataContractJsonSerializer instance = 
            new DataContractJsonSerializer(typeof(T));

        static public T Read(WebRequest request)
        {
            return instance.ReadObject(
                request.GetResponse().GetResponseStream()) as T;
        }
    }
}