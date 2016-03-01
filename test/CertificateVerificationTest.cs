using NUnit.Framework;
using System.Security.Cryptography.X509Certificates;

namespace Conjur.Test
{
    [TestFixture]
    public class CertificateVerificationTest
    {
        [Test]
        public void SelfSignedTest()
        {
            var chain = new X509Chain();
            var trusted = new X509Certificate2Collection();

            Assert.IsFalse(chain.Build(Certificates.SelfSigned));
            Assert.IsFalse(chain.VerifyWithExtraRoots(Certificates.SelfSigned, trusted));

            trusted.Add(Certificates.SelfSigned);
            Assert.IsTrue(chain.VerifyWithExtraRoots(Certificates.SelfSigned, trusted));
            Assert.IsFalse(chain.Build(Certificates.SelfSigned));

            trusted.Clear();
            Assert.IsFalse(chain.VerifyWithExtraRoots(Certificates.SelfSigned, trusted));
            Assert.IsFalse(chain.Build(Certificates.SelfSigned));
        }

        [Test]
        public void SelfSignedRootTest()
        {
            var chain = new X509Chain();
            var trusted = new X509Certificate2Collection();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            Assert.IsFalse(chain.Build(Certificates.SignedBySelfSigned));
            Assert.IsFalse(chain.VerifyWithExtraRoots(Certificates.SignedBySelfSigned, trusted));

            trusted.Add(Certificates.SelfSigned);
            Assert.IsTrue(chain.VerifyWithExtraRoots(Certificates.SignedBySelfSigned, trusted));
            Assert.IsFalse(chain.Build(Certificates.SignedBySelfSigned));

            trusted.Clear();
            Assert.IsFalse(chain.VerifyWithExtraRoots(Certificates.SignedBySelfSigned, trusted));
            Assert.IsFalse(chain.Build(Certificates.SignedBySelfSigned));
        }
    }
}

