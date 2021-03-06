using Axantum.AxCrypt.Abstractions;
using Axantum.AxCrypt.Abstractions.Algorithm;
using Axantum.AxCrypt.Api.Implementation;
using Axantum.AxCrypt.Core.Algorithm;
using Axantum.AxCrypt.Core.Crypto;
using Axantum.AxCrypt.Core.Crypto.Asymmetric;
using Axantum.AxCrypt.Core.Header;
using Axantum.AxCrypt.Core.Portable;
using Axantum.AxCrypt.Core.Runtime;
using Axantum.AxCrypt.Core.UI;
using Axantum.AxCrypt.Fake;
using Axantum.AxCrypt.Mono;
using Axantum.AxCrypt.Mono.Portable;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

using static Axantum.AxCrypt.Abstractions.TypeResolve;

namespace Axantum.AxCrypt.Core.Test
{
    [TestFixture]
    public static class TestV2AsymmetricRecipientsEncryptedHeaderBlock
    {
        [SetUp]
        public static void Setup()
        {
            TypeMap.Register.Singleton<IAsymmetricFactory>(() => new FakeAsymmetricFactory("MD5"));
            TypeMap.Register.Singleton<IPortableFactory>(() => new PortableFactory());
            TypeMap.Register.New<IStringSerializer>(() => new StringSerializer(New<IAsymmetricFactory>().GetSerializers()));
            TypeMap.Register.Singleton<IRandomGenerator>(() => new FakeRandomGenerator());
            TypeMap.Register.Singleton<IRuntimeEnvironment>(() => new FakeRuntimeEnvironment());
            TypeMap.Register.Singleton<IEmailParser>(() => new EmailParser());
            TypeMap.Register.New<Aes>(() => PortableFactory.AesManaged());
        }

        [TearDown]
        public static void Teardown()
        {
            TypeMap.Register.Clear();
        }

        [Test]
        public static void TestGetSetRecipientsAndClone()
        {
            V2AsymmetricRecipientsEncryptedHeaderBlock headerBlock = new V2AsymmetricRecipientsEncryptedHeaderBlock(new V2AesCrypto(SymmetricKey.Zero256, SymmetricIV.Zero128, 0));
            IAsymmetricKeyPair aliceKeyPair = New<IAsymmetricFactory>().CreateKeyPair(512);
            IAsymmetricKeyPair bobKeyPair = New<IAsymmetricFactory>().CreateKeyPair(512);

            List<UserPublicKey> publicKeys = new List<UserPublicKey>();
            publicKeys.Add(new UserPublicKey(EmailAddress.Parse("alice@email.com"), aliceKeyPair.PublicKey));
            publicKeys.Add(new UserPublicKey(EmailAddress.Parse("bob@email.com"), bobKeyPair.PublicKey));
            Recipients recipients = new Recipients(publicKeys);
            headerBlock.Recipients = recipients;
            Assert.That(headerBlock.Recipients.PublicKeys.ToList()[0].Email, Is.EqualTo(EmailAddress.Parse("alice@email.com")));
            Assert.That(headerBlock.Recipients.PublicKeys.ToList()[1].Email, Is.EqualTo(EmailAddress.Parse("bob@email.com")));

            V2AsymmetricRecipientsEncryptedHeaderBlock clone = (V2AsymmetricRecipientsEncryptedHeaderBlock)headerBlock.Clone();
            Assert.That(clone.Recipients.PublicKeys.ToList()[0].Email, Is.EqualTo(EmailAddress.Parse("alice@email.com")));
            Assert.That(clone.Recipients.PublicKeys.ToList()[0].PublicKey.ToString(), Is.EqualTo(aliceKeyPair.PublicKey.ToString()));
            Assert.That(clone.Recipients.PublicKeys.ToList()[1].Email, Is.EqualTo(EmailAddress.Parse("bob@email.com")));
            Assert.That(clone.Recipients.PublicKeys.ToList()[1].PublicKey.ToString(), Is.EqualTo(bobKeyPair.PublicKey.ToString()));
        }
    }
}