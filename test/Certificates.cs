using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Conjur.Test;

public static class Certificates
{
    internal static Stream GetStream(string fileName)
    {
        return Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Conjur.Test.Certificates." + fileName);
    }

    internal static byte[] GetBytes(string fileName)
    {
        Stream stream = GetStream(fileName);
        MemoryStream mem = new MemoryStream((int)stream.Length);
        stream.CopyTo(mem);
        return mem.ToArray();
    }

    public static X509Certificate2 ReadCertificate(string fileName)
    {
        return new X509Certificate2(GetBytes(fileName));
    }

    public static X509Certificate2
        SelfSigned = ReadCertificate("SelfSigned.pem"),
        SignedBySelfSigned = ReadCertificate("SignedBySelfSigned.pem");
}
