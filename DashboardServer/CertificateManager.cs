using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DashboardServer
{
    static class CertificateManager
    {
        #region Generation
        public static X509Certificate2 GenerateCACertificate(string subjectName)
        {
            const int keyStrength = 2048;

            // Generating Random Numbers
            CryptoApiRandomGenerator randomGenerator = new CryptoApiRandomGenerator();
            SecureRandom random = new SecureRandom(randomGenerator);

            // The Certificate Generator
            X509V3CertificateGenerator certificateGenerator = new X509V3CertificateGenerator();

            // Serial Number
            BigInteger serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            // Issuer and Subject Name
            X509Name subjectDN = new X509Name(subjectName);
            X509Name issuerDN = subjectDN;
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            // Valid For
            DateTime notBefore = DateTime.UtcNow.Date;
            DateTime notAfter = notBefore.AddYears(2);

            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            certificateGenerator.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(true));
            certificateGenerator.AddExtension(X509Extensions.KeyUsage, true, new KeyUsage(KeyUsage.KeyCertSign));
            certificateGenerator.AddExtension(X509Extensions.ExtendedKeyUsage, true, new ExtendedKeyUsage(new[] { KeyPurposeID.IdKPServerAuth }));
            certificateGenerator.AddExtension(X509Extensions.NameConstraints, true, new NameConstraints(new ArrayList() { new GeneralSubtree(new GeneralName(GeneralName.DnsName, "localhost")) }, new ArrayList()));

            // Subject Public Key
            KeyGenerationParameters keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
            RsaKeyPairGenerator keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            AsymmetricCipherKeyPair subjectKeyPair = keyPairGenerator.GenerateKeyPair();

            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            // selfsign certificate
            Org.BouncyCastle.X509.X509Certificate certificate = certificateGenerator.Generate(new Asn1SignatureFactory("SHA256WITHRSA", subjectKeyPair.Private, random));

            X509Certificate2 x509 = new X509Certificate2(certificate.GetEncoded());
            x509.SetBouncyPrivateKey(subjectKeyPair.Private);
            return x509;
        }

        public static X509Certificate2 GenerateSelfSignedCertificate(string subjectName, string issuerName, X509Certificate2 cacert)
        {
            const int keyStrength = 2048;

            // Generating Random Numbers
            CryptoApiRandomGenerator randomGenerator = new CryptoApiRandomGenerator();
            SecureRandom random = new SecureRandom(randomGenerator);

            // The Certificate Generator
            X509V3CertificateGenerator certificateGenerator = new X509V3CertificateGenerator();

            // Serial Number
            BigInteger serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            // Issuer and Subject Name
            X509Name subjectDN = new X509Name("CN=" + subjectName);
            X509Name issuerDN = new X509Name("CN=" + issuerName);
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            // Valid For
            DateTime notBefore = DateTime.UtcNow.Date;
            DateTime notAfter = notBefore.AddYears(2);

            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            // Subject Public Key
            AsymmetricCipherKeyPair subjectKeyPair;
            var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            subjectKeyPair = keyPairGenerator.GenerateKeyPair();

            certificateGenerator.SetPublicKey(subjectKeyPair.Public);
            
            certificateGenerator.AddExtension(X509Extensions.SubjectAlternativeName, true, 
                new DerSequence(new Asn1Encodable[] { new GeneralName(GeneralName.DnsName, subjectName) }));

            // selfsign certificate
            Org.BouncyCastle.X509.X509Certificate certificate = certificateGenerator.Generate(new Asn1SignatureFactory("SHA256WITHRSA", cacert.PrivateKey.ToBouncyKey(), random));

            // merge into X509Certificate2
            X509Certificate2 x509 = new System.Security.Cryptography.X509Certificates.X509Certificate2(certificate.GetEncoded());
            x509.SetBouncyPrivateKey(subjectKeyPair.Private);
            return x509;
        }
        #endregion
        #region ExtensionMethods
        public static RsaPrivateCrtKeyParameters ToBouncyKey(this AsymmetricAlgorithm privateKey)
        {
            RSACryptoServiceProvider prov = privateKey as RSACryptoServiceProvider;
            RSAParameters parameters = prov.ExportParameters(true);

            return new RsaPrivateCrtKeyParameters(
                new BigInteger(1, parameters.Modulus),
                new BigInteger(1, parameters.Exponent),
                new BigInteger(1, parameters.D),
                new BigInteger(1, parameters.P),
                new BigInteger(1, parameters.Q),
                new BigInteger(1, parameters.DP),
                new BigInteger(1, parameters.DQ),
                new BigInteger(1, parameters.InverseQ));
        }

        public static AsymmetricAlgorithm ToDotNetKey(this RsaPrivateCrtKeyParameters privateKey)
        {
            var cspParams = new CspParameters
            {
                KeyContainerName = Guid.NewGuid().ToString(),
                KeyNumber = (int)KeyNumber.Exchange,
                Flags = CspProviderFlags.UseMachineKeyStore
            };

            var rsaProvider = new RSACryptoServiceProvider(cspParams);
            var parameters = new RSAParameters
            {
                Modulus = privateKey.Modulus.ToByteArrayUnsigned(),
                P = privateKey.P.ToByteArrayUnsigned(),
                Q = privateKey.Q.ToByteArrayUnsigned(),
                DP = privateKey.DP.ToByteArrayUnsigned(),
                DQ = privateKey.DQ.ToByteArrayUnsigned(),
                InverseQ = privateKey.QInv.ToByteArrayUnsigned(),
                D = privateKey.Exponent.ToByteArrayUnsigned(),
                Exponent = privateKey.PublicExponent.ToByteArrayUnsigned()
            };

            rsaProvider.ImportParameters(parameters);
            return rsaProvider;
        }

        public static void SetBouncyPrivateKey(this X509Certificate2 cert, AsymmetricKeyParameter key)
        {
            PrivateKeyInfo info = PrivateKeyInfoFactory.CreatePrivateKeyInfo(key);
            Asn1Sequence seq = (Asn1Sequence)Asn1Object.FromByteArray(info.ParsePrivateKey().GetDerEncoded());
            if (seq.Count != 9)
            {
                //throw new PemException("malformed sequence in RSA private key");
            }

            RsaPrivateKeyStructure rsa = RsaPrivateKeyStructure.GetInstance(seq);
            RsaPrivateCrtKeyParameters rsaparams = new RsaPrivateCrtKeyParameters(
                rsa.Modulus, rsa.PublicExponent, rsa.PrivateExponent, rsa.Prime1, rsa.Prime2, rsa.Exponent1, rsa.Exponent2, rsa.Coefficient);

            cert.PrivateKey = rsaparams.ToDotNetKey();
        }
        #endregion

        public static bool AddCertToStore(X509Certificate2 cert, StoreName st, StoreLocation sl)
        {
            bool bRet = false;

            try
            {
                X509Store store = new X509Store(st, sl);
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);

                store.Close();
            }
            catch
            {

            }

            return bRet;
        }

        public static void StoreCertificate(string basename, X509Certificate2 cert)
        {
            File.WriteAllBytes(basename + ".pfx", cert.Export(X509ContentType.Pkcs12, (string)null));
        }

        public static X509Certificate2 TryLoadCertificate(string basename)
        {
            try
            {
                if (File.Exists(basename + ".pfx"))
                {
                    var cert = new X509Certificate2(File.ReadAllBytes(basename + ".pfx"));
                    return cert;
                }
            }catch(Exception) {
            }
            return null;
        }

        public static X509Certificate2 LoadCertificates()
        {
            var caCert = TryLoadCertificate("cacert");
            if (caCert == null)
            {
                caCert = GenerateCACertificate("CN=DashboardSystemServerCA");
                AddCertToStore(caCert, StoreName.Root, StoreLocation.CurrentUser);
                StoreCertificate("cacert", caCert);
            }
            var clientCert = TryLoadCertificate("clientcert");
            if (clientCert == null)
            {
                clientCert = GenerateSelfSignedCertificate("localhost", "DashboardSystemServerCA", caCert);
                StoreCertificate("clientcert", clientCert);
            }

            return clientCert;
        }
    }
}
