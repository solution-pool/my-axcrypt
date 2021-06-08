﻿#region Coypright and License

/*
 * AxCrypt - Copyright 2016, Svante Seleborg, All Rights Reserved
 *
 * This file is part of AxCrypt.
 *
 * AxCrypt is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * AxCrypt is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with AxCrypt.  If not, see <http://www.gnu.org/licenses/>.
 *
 * The source is maintained at http://bitbucket.org/axantum/axcrypt-net please visit for
 * updates, contributions and contact with the author. You may also visit
 * http://www.axcrypt.net for more information about the author.
*/

#endregion Coypright and License

using Axantum.AxCrypt.Abstractions;
using Axantum.AxCrypt.Core.Crypto;
using Axantum.AxCrypt.Core.Header;
using Axantum.AxCrypt.Core.IO;
using Axantum.AxCrypt.Core.Reader;
using Axantum.AxCrypt.Core.Runtime;
using Axantum.AxCrypt.Core.Test.Properties;
using Axantum.AxCrypt.Core.UI;
using Axantum.AxCrypt.Core.UI.ViewModel;
using Axantum.AxCrypt.Fake;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Axantum.AxCrypt.Abstractions.TypeResolve;

#pragma warning disable 3016 // Attribute-arguments as arrays are not CLS compliant. Ignore this here, it's how NUnit works.

namespace Axantum.AxCrypt.Core.Test
{
    [TestFixture(CryptoImplementation.Mono)]
    [TestFixture(CryptoImplementation.WindowsDesktop)]
    [TestFixture(CryptoImplementation.BouncyCastle)]
    public class TestFileOperationsController
    {
        private static readonly string _rootPath = Path.GetPathRoot(Environment.CurrentDirectory);
        private static readonly string _davidCopperfieldTxtPath = _rootPath.PathCombine("Users", "AxCrypt", "David Copperfield.txt");
        private static readonly string _uncompressedAxxPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Uncompressed.666");
        private static readonly string _helloWorldAxxPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HelloWorld.666");

        private CryptoImplementation _cryptoImplementation;

        public TestFileOperationsController(CryptoImplementation cryptoImplementation)
        {
            _cryptoImplementation = cryptoImplementation;
        }

        [SetUp]
        public void Setup()
        {
            SetupAssembly.AssemblySetup();
            SetupAssembly.AssemblySetupCrypto(_cryptoImplementation);

            FakeDataStore.AddFile(_davidCopperfieldTxtPath, FakeDataStore.TestDate4Utc, FakeDataStore.TestDate5Utc, FakeDataStore.TestDate6Utc, FakeDataStore.ExpandableMemoryStream(Encoding.GetEncoding(1252).GetBytes(Resources.david_copperfield)));
            FakeDataStore.AddFile(_uncompressedAxxPath, FakeDataStore.ExpandableMemoryStream(Resources.uncompressable_zip));
            FakeDataStore.AddFile(_helloWorldAxxPath, FakeDataStore.ExpandableMemoryStream(Resources.helloworld_key_a_txt));

            TypeMap.Register.Singleton<IUIThread>(() => new FakeUIThread());
        }

        [TearDown]
        public void Teardown()
        {
            SetupAssembly.AssemblyTeardown();
        }

        [Test]
        public async Task TestSimpleEncryptFile()
        {
            FileOperationsController controller = new FileOperationsController();
            string destinationPath = String.Empty;
            controller.QueryEncryptionPassphrase += (object sender, FileOperationEventArgs e) =>
                {
                    e.LogOnIdentity = new LogOnIdentity("allan");
                };
            controller.Completed += (object sender, FileOperationEventArgs e) =>
            {
                destinationPath = e.SaveFileFullName;
            };

            FileOperationContext status = await controller.EncryptFileAsync(New<IDataStore>(_davidCopperfieldTxtPath));
            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The status should indicate success.");

            IDataStore destinationInfo = New<IDataStore>(destinationPath);
            Assert.That(destinationInfo.IsAvailable, "After encryption the destination file should be created.");
            using (V2AxCryptDocument document = new V2AxCryptDocument())
            {
                using (Stream stream = destinationInfo.OpenRead())
                {
                    document.Load(new Passphrase("allan"), new V2Aes256CryptoFactory().CryptoId, stream);
                    Assert.That(document.PassphraseIsValid, "The encrypted document should be valid and encrypted with the passphrase given.");
                }
            }
        }

        [Test]
        public void TestSimpleEncryptFileOnThreadWorker()
        {
            FileOperationsController controller = new FileOperationsController();
            controller.QueryEncryptionPassphrase += (object sender, FileOperationEventArgs e) =>
            {
                e.LogOnIdentity = new LogOnIdentity("allan");
            };
            string destinationPath = String.Empty;
            FileOperationContext status = new FileOperationContext(String.Empty, ErrorStatus.Unknown);
            controller.Completed += (object sender, FileOperationEventArgs e) =>
            {
                destinationPath = e.SaveFileFullName;
                status = e.Status;
            };

            controller.EncryptFileAsync(New<IDataStore>(_davidCopperfieldTxtPath));
            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The status should indicate success.");

            IDataStore destinationInfo = New<IDataStore>(destinationPath);
            Assert.That(destinationInfo.IsAvailable, "After encryption the destination file should be created.");
            using (V2AxCryptDocument document = new V2AxCryptDocument())
            {
                using (Stream stream = destinationInfo.OpenRead())
                {
                    document.Load(new Passphrase("allan"), new V2Aes256CryptoFactory().CryptoId, stream);
                    Assert.That(document.PassphraseIsValid, "The encrypted document should be valid and encrypted with the passphrase given.");
                }
            }
        }

        [Test]
        public async Task TestEncryptFileWithDefaultEncryptionKey()
        {
            TypeMap.Register.New<ICryptoPolicy>(() => new LegacyCryptoPolicy());
            await Resolve.KnownIdentities.SetDefaultEncryptionIdentity(new LogOnIdentity("default"));
            FileOperationsController controller = new FileOperationsController();
            bool queryEncryptionPassphraseWasCalled = false;
            controller.QueryEncryptionPassphrase += (object sender, FileOperationEventArgs e) =>
                {
                    queryEncryptionPassphraseWasCalled = true;
                };
            string destinationPath = String.Empty;
            controller.Completed += (object sender, FileOperationEventArgs e) =>
                {
                    destinationPath = e.SaveFileFullName;
                };

            FileOperationContext status = await controller.EncryptFileAsync(New<IDataStore>(_davidCopperfieldTxtPath));
            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The status should indicate success.");
            Assert.That(!queryEncryptionPassphraseWasCalled, "No query of encryption passphrase should be needed since there is a default set.");

            IDataStore destinationInfo = New<IDataStore>(destinationPath);
            Assert.That(destinationInfo.IsAvailable, "After encryption the destination file should be created.");
            using (V1AxCryptDocument document = new V1AxCryptDocument())
            {
                using (Stream stream = destinationInfo.OpenRead())
                {
                    document.Load(new Passphrase("default"), new V1Aes128CryptoFactory().CryptoId, stream);
                    Assert.That(document.PassphraseIsValid, "The encrypted document should be valid and encrypted with the default passphrase given.");
                }
            }
        }

        [Test]
        public async Task TestEncryptFileWhenDestinationExists()
        {
            IDataStore sourceInfo = New<IDataStore>(_davidCopperfieldTxtPath);
            IDataStore expectedDestinationInfo = New<IDataStore>(AxCryptFile.MakeAxCryptFileName(sourceInfo));
            using (Stream stream = expectedDestinationInfo.OpenWrite())
            {
            }

            FileOperationsController controller = new FileOperationsController();
            string destinationPath = String.Empty;
            LogOnIdentity logOnIdentity = null;
            controller.QueryEncryptionPassphrase += (object sender, FileOperationEventArgs e) =>
            {
                e.LogOnIdentity = new LogOnIdentity("allan");
            };
            controller.QuerySaveFileAs += (object sender, FileOperationEventArgs e) =>
            {
                e.SaveFileFullName = Path.Combine(Path.GetDirectoryName(e.SaveFileFullName), "alternative-name.666");
            };
            Guid cryptoId = Guid.Empty;
            controller.Completed += (object sender, FileOperationEventArgs e) =>
            {
                destinationPath = e.SaveFileFullName;
                logOnIdentity = e.LogOnIdentity;
                cryptoId = e.CryptoId;
            };

            FileOperationContext status = await controller.EncryptFileAsync(New<IDataStore>(_davidCopperfieldTxtPath));
            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The status should indicate success.");

            Assert.That(Path.GetFileName(destinationPath), Is.EqualTo("alternative-name.666"), "The alternative name should be used, since the default existed.");
            IDataStore destinationInfo = New<IDataStore>(destinationPath);
            Assert.That(destinationInfo.IsAvailable, "After encryption the destination file should be created.");

            EncryptionParameters encryptionParameters = new EncryptionParameters(cryptoId, logOnIdentity);
            await encryptionParameters.AddAsync(logOnIdentity.PublicKeys);

            Headers headers = new Headers();
            AxCryptReaderBase reader = headers.CreateReader(new LookAheadStream(destinationInfo.OpenRead()));
            using (IAxCryptDocument document = AxCryptReaderBase.Document(reader))
            {
                document.Load(logOnIdentity.Passphrase, cryptoId, headers);
                Assert.That(document.PassphraseIsValid, "The encrypted document should be valid and encrypted with the passphrase given.");
            }
        }

        [Test]
        public async Task TestEncryptFileWhenCanceledDuringQuerySaveAs()
        {
            IDataStore sourceInfo = New<IDataStore>(_davidCopperfieldTxtPath);
            IDataStore expectedDestinationInfo = New<IDataStore>(AxCryptFile.MakeAxCryptFileName(sourceInfo));
            using (Stream stream = expectedDestinationInfo.OpenWrite())
            {
            }

            FileOperationsController controller = new FileOperationsController();
            controller.QuerySaveFileAs += (object sender, FileOperationEventArgs e) =>
            {
                e.Cancel = true;
            };

            FileOperationContext status = await controller.EncryptFileAsync(New<IDataStore>(_davidCopperfieldTxtPath));
            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Canceled), "The status should indicate cancellation.");
        }

        [Test]
        public async Task TestEncryptFileWhenCanceledDuringQueryPassphrase()
        {
            FileOperationsController controller = new FileOperationsController();
            controller.QueryEncryptionPassphrase += (object sender, FileOperationEventArgs e) =>
            {
                e.Cancel = true;
            };

            FileOperationContext status = await controller.EncryptFileAsync(New<IDataStore>(_davidCopperfieldTxtPath));
            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Canceled), "The status should indicate cancellation.");
        }

        [Test]
        public async Task TestSimpleDecryptFile()
        {
            FileOperationsController controller = new FileOperationsController();
            controller.QueryDecryptionPassphrase = (FileOperationEventArgs e) =>
                {
                    e.LogOnIdentity = new LogOnIdentity("a");
                    return Task.FromResult<object>(null);
                };
            bool knownKeyWasAdded = false;
            controller.KnownKeyAdded = new AsyncDelegateAction<FileOperationEventArgs>((FileOperationEventArgs e) =>
                {
                    knownKeyWasAdded = e.LogOnIdentity.Equals(new LogOnIdentity("a"));
                    return Constant.CompletedTask;
                });
            string destinationPath = String.Empty;
            controller.Completed += (object sender, FileOperationEventArgs e) =>
                {
                    destinationPath = e.SaveFileFullName;
                };
            FileOperationContext status = await controller.DecryptFileAsync(New<IDataStore>(_helloWorldAxxPath));

            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The status should indicate success.");
            Assert.That(knownKeyWasAdded, "A new known key was used, so the KnownKeyAdded event should have been raised.");
            IDataStore destinationInfo = New<IDataStore>(destinationPath);
            Assert.That(destinationInfo.IsAvailable, "After decryption the destination file should be created.");

            string fileContent;
            using (Stream stream = destinationInfo.OpenRead())
            {
                fileContent = new StreamReader(stream).ReadToEnd();
            }
            Assert.That(fileContent.Contains("Hello"), "A file named Hello World should contain that text when decrypted.");
        }

        [Test]
        public void TestSimpleDecryptFileOnThreadWorker()
        {
            FileOperationsController controller = new FileOperationsController();
            controller.QueryDecryptionPassphrase = (FileOperationEventArgs e) =>
            {
                e.LogOnIdentity = new LogOnIdentity("a");
                return Task.FromResult<object>(null);
            };
            bool knownKeyWasAdded = false;
            controller.KnownKeyAdded = new AsyncDelegateAction<FileOperationEventArgs>((FileOperationEventArgs e) =>
            {
                knownKeyWasAdded = e.LogOnIdentity.Equals(new LogOnIdentity("a"));
                return Constant.CompletedTask;
            });
            string destinationPath = String.Empty;
            FileOperationContext status = new FileOperationContext(String.Empty, ErrorStatus.Unknown);
            controller.Completed += (object sender, FileOperationEventArgs e) =>
            {
                destinationPath = e.SaveFileFullName;
                status = e.Status;
            };

            controller.DecryptFileAsync(New<IDataStore>(_helloWorldAxxPath));

            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The status should indicate success.");
            Assert.That(knownKeyWasAdded, "A new known key was used, so the KnownKeyAdded event should have been raised.");
            IDataStore destinationInfo = New<IDataStore>(destinationPath);
            Assert.That(destinationInfo.IsAvailable, "After decryption the destination file should be created.");

            string fileContent;
            using (Stream stream = destinationInfo.OpenRead())
            {
                fileContent = new StreamReader(stream).ReadToEnd();
            }
            Assert.That(fileContent.Contains("Hello"), "A file named Hello World should contain that text when decrypted.");
        }

        [Test]
        public async Task TestDecryptWithCancelDuringQueryDecryptionPassphrase()
        {
            FileOperationsController controller = new FileOperationsController();
            controller.QueryDecryptionPassphrase = (FileOperationEventArgs e) =>
            {
                e.Cancel = true;
                return Task.FromResult<object>(null);
            };
            FileOperationContext status = await controller.DecryptFileAsync(New<IDataStore>(_helloWorldAxxPath));

            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Canceled), "The status should indicate cancellation.");
        }

        [Test]
        public async Task TestDecryptWithSkipDuringQueryDecryptionPassphrase()
        {
            IDataStore expectedDestinationInfo = New<IDataStore>(Path.Combine(Path.GetDirectoryName(_helloWorldAxxPath), "HelloWorld-Key-a.txt"));
            using (Stream stream = expectedDestinationInfo.OpenWrite())
            {
            }

            FileOperationsController controller = new FileOperationsController();
            controller.QueryDecryptionPassphrase = (FileOperationEventArgs e) =>
            {
                e.Skip = true;
                return Task.FromResult<object>(null);
            };
            bool saveAs = false;
            controller.QuerySaveFileAs += (sender, e) => saveAs = true;
            FileOperationContext status = await controller.DecryptFileAsync(New<IDataStore>(_helloWorldAxxPath));

            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The status should indicate success.");
            Assert.That(saveAs, Is.False, "No Save As should happen, since skip was indicated.");
        }

        [Test]
        public async Task TestDecryptWithCancelDuringQuerySaveAs()
        {
            IDataStore expectedDestinationInfo = New<IDataStore>(Path.Combine(Path.GetDirectoryName(_helloWorldAxxPath), "HelloWorld-Key-a.txt"));
            using (Stream stream = expectedDestinationInfo.OpenWrite())
            {
            }

            FileOperationsController controller = new FileOperationsController();
            controller.QueryDecryptionPassphrase = (FileOperationEventArgs e) =>
                {
                    e.LogOnIdentity = new LogOnIdentity("a");
                    return Task.FromResult<object>(null);
                };
            controller.QuerySaveFileAs += (object sender, FileOperationEventArgs e) =>
                {
                    e.Cancel = true;
                };
            FileOperationContext status = await controller.DecryptFileAsync(New<IDataStore>(_helloWorldAxxPath));

            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Canceled), "The status should indicate cancellation.");
        }

        [Test]
        public async Task TestDecryptWithAlternativeDestinationName()
        {
            IDataStore expectedDestinationInfo = New<IDataStore>(Path.Combine(Path.GetDirectoryName(_helloWorldAxxPath), "HelloWorld-Key-a.txt"));
            using (Stream stream = expectedDestinationInfo.OpenWrite())
            {
            }

            FileOperationsController controller = new FileOperationsController();
            controller.QueryDecryptionPassphrase = (FileOperationEventArgs e) =>
            {
                e.LogOnIdentity = new LogOnIdentity("a");
                return Task.FromResult<object>(null);
            };
            controller.QuerySaveFileAs += (object sender, FileOperationEventArgs e) =>
            {
                e.SaveFileFullName = Path.Combine(Path.GetDirectoryName(e.SaveFileFullName), "Other Hello World.txt");
            };
            string destinationPath = String.Empty;
            controller.Completed += (object sender, FileOperationEventArgs e) =>
            {
                destinationPath = e.SaveFileFullName;
            };
            FileOperationContext status = await controller.DecryptFileAsync(New<IDataStore>(_helloWorldAxxPath));

            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The status should indicate success.");

            IDataStore destinationInfo = New<IDataStore>(destinationPath);
            string fileContent;
            using (Stream stream = destinationInfo.OpenRead())
            {
                fileContent = new StreamReader(stream).ReadToEnd();
            }
            Assert.That(fileContent.Contains("Hello"), "A file named 'Other Hello World.txt' should contain that text when decrypted.");
        }

        [Test]
        public async Task TestSimpleDecryptAndLaunch()
        {
            FakeLauncher launcher = new FakeLauncher();
            bool called = false;
            TypeMap.Register.New<ILauncher>(() => { called = true; return launcher; });

            FileOperationsController controller = new FileOperationsController();
            controller.QueryDecryptionPassphrase = (FileOperationEventArgs e) =>
            {
                e.LogOnIdentity = new LogOnIdentity("a");
                return Task.FromResult<object>(null);
            };
            FileOperationContext status = await controller.DecryptAndLaunchAsync(New<IDataStore>(_helloWorldAxxPath));

            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The status should indicate success.");

            Assert.That(called, Is.True, "There should be a call to launch.");
            Assert.That(Path.GetFileName(launcher.Path), Is.EqualTo("HelloWorld-Key-a.txt"), "The file should be decrypted and the name should be the original from the encrypted headers.");

            IDataStore destinationInfo = New<IDataStore>(launcher.Path);
            Assert.That(destinationInfo.IsAvailable, "After decryption the destination file should be created.");

            string fileContent;
            using (Stream stream = destinationInfo.OpenRead())
            {
                fileContent = new StreamReader(stream).ReadToEnd();
            }

            Assert.That(fileContent.Contains("Hello"), "A file named Hello World should contain that text when decrypted.");
        }

        [Test]
        public async Task TestSimpleDecryptAndLaunchOnThreadWorker()
        {
            FakeLauncher launcher = new FakeLauncher();
            bool called = false;
            TypeMap.Register.New<ILauncher>(() => { called = true; return launcher; });

            FileOperationsController controller = new FileOperationsController();
            controller.QueryDecryptionPassphrase = (FileOperationEventArgs e) =>
            {
                e.LogOnIdentity = new LogOnIdentity("a");
                return Task.FromResult<object>(null);
            };
            FileOperationContext status = new FileOperationContext(String.Empty, ErrorStatus.Unknown);
            controller.Completed += (object sender, FileOperationEventArgs e) =>
            {
                status = e.Status;
            };

            await controller.DecryptAndLaunchAsync(New<IDataStore>(_helloWorldAxxPath));

            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The status should indicate success.");

            Assert.That(called, Is.True, "There should be a call to launch.");
            Assert.That(Path.GetFileName(launcher.Path), Is.EqualTo("HelloWorld-Key-a.txt"), "The file should be decrypted and the name should be the original from the encrypted headers.");

            IDataStore destinationInfo = New<IDataStore>(launcher.Path);
            Assert.That(destinationInfo.IsAvailable, "After decryption the destination file should be created.");

            string fileContent;
            using (Stream stream = destinationInfo.OpenRead())
            {
                fileContent = new StreamReader(stream).ReadToEnd();
            }

            Assert.That(fileContent.Contains("Hello"), "A file named Hello World should contain that text when decrypted.");
        }

        [Test]
        public async Task TestCanceledDecryptAndLaunch()
        {
            FileOperationsController controller = new FileOperationsController();
            controller.QueryDecryptionPassphrase = (FileOperationEventArgs e) =>
            {
                e.Cancel = true;
                return Task.FromResult<object>(null);
            };
            FileOperationContext status = await controller.DecryptAndLaunchAsync(New<IDataStore>(_helloWorldAxxPath));
            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Canceled), "The status should indicate cancellation.");
        }

        [Test]
        public async Task TestDecryptWithKnownKey()
        {
            FileOperationsController controller = new FileOperationsController();
            await Resolve.KnownIdentities.AddAsync(new LogOnIdentity("b"));
            await Resolve.KnownIdentities.AddAsync(new LogOnIdentity("c"));
            await Resolve.KnownIdentities.AddAsync(new LogOnIdentity("a"));
            await Resolve.KnownIdentities.AddAsync(new LogOnIdentity("e"));
            bool passphraseWasQueried = false;
            controller.QueryDecryptionPassphrase = (FileOperationEventArgs e) =>
            {
                passphraseWasQueried = true;
                return Task.FromResult<object>(null);
            };
            string destinationPath = String.Empty;
            controller.Completed += (object sender, FileOperationEventArgs e) =>
            {
                destinationPath = e.SaveFileFullName;
            };
            bool knownKeyWasAdded = false;
            controller.KnownKeyAdded = new AsyncDelegateAction<FileOperationEventArgs>((FileOperationEventArgs e) =>
            {
                knownKeyWasAdded = true;
                return Constant.CompletedTask;
            });
            FileOperationContext status = await controller.DecryptFileAsync(New<IDataStore>(_helloWorldAxxPath));

            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The status should indicate success.");
            Assert.That(!knownKeyWasAdded, "An already known key was used, so the KnownKeyAdded event should not have been raised.");
            Assert.That(!passphraseWasQueried, "An already known key was used, so the there should be no need to query for a passphrase.");
            IDataStore destinationInfo = New<IDataStore>(destinationPath);
            Assert.That(destinationInfo.IsAvailable, "After decryption the destination file should be created.");

            string fileContent;
            using (Stream stream = destinationInfo.OpenRead())
            {
                fileContent = new StreamReader(stream).ReadToEnd();
            }
            Assert.That(fileContent.Contains("Hello"), "A file named Hello World should contain that text when decrypted.");
        }

        [Test]
        public async Task TestDecryptFileWithRepeatedPassphraseQueries()
        {
            FileOperationsController controller = new FileOperationsController();
            int passphraseTry = 0;
            controller.QueryDecryptionPassphrase = (FileOperationEventArgs e) =>
            {
                switch (++passphraseTry)
                {
                    case 1:
                        e.LogOnIdentity = new LogOnIdentity("b");
                        break;

                    case 2:
                        e.LogOnIdentity = new LogOnIdentity("d");
                        break;

                    case 3:
                        e.LogOnIdentity = new LogOnIdentity("a");
                        break;

                    case 4:
                        e.LogOnIdentity = new LogOnIdentity("e");
                        break;
                };
                return Task.FromResult<object>(null);
            };
            string destinationPath = String.Empty;
            controller.Completed += (object sender, FileOperationEventArgs e) =>
            {
                destinationPath = e.SaveFileFullName;
            };
            bool knownKeyWasAdded = false;
            controller.KnownKeyAdded = new AsyncDelegateAction<FileOperationEventArgs>((FileOperationEventArgs e) =>
            {
                knownKeyWasAdded = e.LogOnIdentity.Equals(new LogOnIdentity("a"));
                return Constant.CompletedTask;
            });
            FileOperationContext status = await controller.DecryptFileAsync(New<IDataStore>(_helloWorldAxxPath));

            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The status should indicate success.");
            Assert.That(knownKeyWasAdded, "A new known key was used, so the KnownKeyAdded event should have been raised.");
            Assert.That(passphraseTry, Is.EqualTo(3), "The third key was the correct one.");
            IDataStore destinationInfo = New<IDataStore>(destinationPath);
            Assert.That(destinationInfo.IsAvailable, "After decryption the destination file should be created.");

            string fileContent;
            using (Stream stream = destinationInfo.OpenRead())
            {
                fileContent = new StreamReader(stream).ReadToEnd();
            }
            Assert.That(fileContent.Contains("Hello"), "A file named Hello World should contain that text when decrypted.");
        }

        [Test]
        public void TestDecryptFileWithExceptionBeforeStartingDecryption()
        {
            FileOperationsController controller = new FileOperationsController();
            controller.QueryDecryptionPassphrase = (FileOperationEventArgs e) =>
                {
                    e.LogOnIdentity = new LogOnIdentity("a");
                    return Task.FromResult<object>(null);
                };
            controller.KnownKeyAdded = new AsyncDelegateAction<FileOperationEventArgs>((FileOperationEventArgs e) =>
            {
                throw new FileNotFoundException("Just kidding, but we're faking...", e.OpenFileFullName);
            });
            string destinationPath = String.Empty;
            AsyncDelegateAction<FileOperationEventArgs> previous = controller.KnownKeyAdded;
            controller.KnownKeyAdded = new AsyncDelegateAction<FileOperationEventArgs>(async (FileOperationEventArgs e) =>
            {
                await previous.ExecuteAsync(e);
                destinationPath = e.SaveFileFullName;
            });
            FileOperationContext status = new FileOperationContext(String.Empty, ErrorStatus.Unknown);
            Assert.DoesNotThrowAsync(async () => { status = await controller.DecryptFileAsync(New<IDataStore>(_helloWorldAxxPath)); });

            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.FileDoesNotExist), "The status should indicate an exception occurred.");
            Assert.That(String.IsNullOrEmpty(destinationPath), "Since an exception occurred, the destination file should not be created.");
        }

        [Test]
        public async Task TestEncryptFileThatIsAlreadyEncrypted()
        {
            FileOperationsController controller = new FileOperationsController();
            FileOperationContext status = await controller.EncryptFileAsync(New<IDataStore>("test" + OS.Current.AxCryptExtension));

            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.FileAlreadyEncrypted), "The status should indicate that it was already encrypted.");
        }

        [Test]
        public void TestDecryptWithCancelDuringQueryDecryptionPassphraseOnThreadWorker()
        {
            FileOperationsController controller = new FileOperationsController();
            controller.QueryDecryptionPassphrase = (FileOperationEventArgs e) =>
                {
                    e.Cancel = true;
                    return Task.FromResult<object>(null);
                };
            FileOperationContext status = new FileOperationContext(String.Empty, ErrorStatus.Unknown);
            controller.Completed += (object sender, FileOperationEventArgs e) =>
                {
                    status = e.Status;
                };

            controller.DecryptFileAsync(New<IDataStore>(_helloWorldAxxPath));

            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Canceled), "The status should indicate cancellation.");
        }

        [Test]
        public async Task TestSimpleWipe()
        {
            FileOperationsController controller = new FileOperationsController();
            controller.WipeQueryConfirmation += (object sender, FileOperationEventArgs e) =>
            {
                e.Cancel = false;
                e.Skip = false;
                e.ConfirmAll = false;
            };
            FileOperationContext status = await controller.WipeFileAsync(New<IDataStore>(_helloWorldAxxPath));
            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The wipe should indicate success.");

            IDataStore fileInfo = New<IDataStore>(_helloWorldAxxPath);
            Assert.That(!fileInfo.IsAvailable, "The file should not exist after wiping.");
        }

        [Test]
        public void TestSimpleWipeOnThreadWorker()
        {
            FileOperationsController controller = new FileOperationsController();
            controller.WipeQueryConfirmation += (object sender, FileOperationEventArgs e) =>
            {
                e.Cancel = false;
                e.Skip = false;
                e.ConfirmAll = false;
            };

            string destinationPath = String.Empty;
            FileOperationContext status = new FileOperationContext(String.Empty, ErrorStatus.Unknown);
            controller.Completed += (object sender, FileOperationEventArgs e) =>
            {
                destinationPath = e.SaveFileFullName;
                status = e.Status;
            };

            controller.WipeFileAsync(New<IDataStore>(_davidCopperfieldTxtPath));
            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The status should indicate success.");

            IDataStore destinationInfo = New<IDataStore>(destinationPath);
            Assert.That(!destinationInfo.IsAvailable, "After wiping the destination file should not exist.");
        }

        [Test]
        public async Task TestWipeWithCancel()
        {
            FileOperationsController controller = new FileOperationsController();
            controller.WipeQueryConfirmation += (object sender, FileOperationEventArgs e) =>
            {
                e.Cancel = true;
            };
            FileOperationContext status = await controller.WipeFileAsync(New<IDataStore>(_helloWorldAxxPath));
            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Canceled), "The wipe should indicate cancellation.");

            IDataStore fileInfo = New<IDataStore>(_helloWorldAxxPath);
            Assert.That(fileInfo.IsAvailable, "The file should still exist after wiping that was canceled during confirmation.");
        }

        [Test]
        public async Task TestWipeWithSkip()
        {
            FileOperationsController controller = new FileOperationsController();
            controller.WipeQueryConfirmation += (object sender, FileOperationEventArgs e) =>
            {
                e.Skip = true;
            };
            FileOperationContext status = await controller.WipeFileAsync(New<IDataStore>(_helloWorldAxxPath));
            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The wipe should indicate success even when skipping.");

            IDataStore fileInfo = New<IDataStore>(_helloWorldAxxPath);
            Assert.That(fileInfo.IsAvailable, "The file should still exist after wiping that was skipped during confirmation.");
        }

        [Test]
        public async Task TestWipeWithConfirmAll()
        {
            ProgressContext progress = new ProgressContext();
            FileOperationsController controller = new FileOperationsController(progress);
            int confirmationCount = 0;
            controller.WipeQueryConfirmation += (object sender, FileOperationEventArgs e) =>
            {
                if (confirmationCount++ > 0)
                {
                    throw new InvalidOperationException("The event should not be raised a second time.");
                }
                e.ConfirmAll = true;
            };
            progress.NotifyLevelStart();
            FileOperationContext status = await controller.WipeFileAsync(New<IDataStore>(_helloWorldAxxPath));
            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The wipe should indicate success.");

            IDataStore fileInfo = New<IDataStore>(_helloWorldAxxPath);
            Assert.That(!fileInfo.IsAvailable, "The file should not exist after wiping.");

            Assert.DoesNotThrowAsync(async () => { status = await controller.WipeFileAsync(New<IDataStore>(_davidCopperfieldTxtPath)); });
            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success), "The wipe should indicate success.");
            progress.NotifyLevelFinished();

            fileInfo = New<IDataStore>(_davidCopperfieldTxtPath);
            Assert.That(!fileInfo.IsAvailable, "The file should not exist after wiping.");
        }

        [Test]
        public async Task TestVerifyEncrypted()
        {
            FileOperationsController controller = new FileOperationsController();
            bool passphraseWasQueried = false;
            controller.QueryDecryptionPassphrase = (FileOperationEventArgs e) =>
            {
                passphraseWasQueried = true;
                e.Cancel = true;
                return Task.FromResult<object>(null);
            };
            bool knownKeyWasAdded = false;
            controller.KnownKeyAdded = new AsyncDelegateAction<FileOperationEventArgs>((FileOperationEventArgs e) =>
            {
                knownKeyWasAdded = true;
                return Constant.CompletedTask;
            });

            FileOperationContext status = await controller.VerifyEncryptedAsync(New<IDataStore>(_helloWorldAxxPath));
            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Canceled));
            Assert.That(knownKeyWasAdded, Is.False);
            Assert.That(passphraseWasQueried, Is.True);

            controller = new FileOperationsController();
            controller.QueryDecryptionPassphrase = (FileOperationEventArgs e) =>
            {
                e.LogOnIdentity = new LogOnIdentity("a");
                return Task.FromResult<object>(null);
            };
            controller.KnownKeyAdded = new AsyncDelegateAction<FileOperationEventArgs>((FileOperationEventArgs e) =>
            {
                knownKeyWasAdded = true;
                return Constant.CompletedTask;
            });

            await Resolve.KnownIdentities.AddAsync(new LogOnIdentity("b"));
            await Resolve.KnownIdentities.AddAsync(new LogOnIdentity("c"));

            status = await controller.VerifyEncryptedAsync(New<IDataStore>(_helloWorldAxxPath));
            Assert.That(status.ErrorStatus, Is.EqualTo(ErrorStatus.Success));
            Assert.That(knownKeyWasAdded, Is.True, "A known key should have been added.");
        }
    }
}