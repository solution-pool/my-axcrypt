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
using Axantum.AxCrypt.Common;
using Axantum.AxCrypt.Core.Crypto;
using Axantum.AxCrypt.Core.Extensions;
using Axantum.AxCrypt.Core.IO;
using Axantum.AxCrypt.Core.Runtime;
using Axantum.AxCrypt.Core.Session;
using Axantum.AxCrypt.Core.Test.Properties;
using Axantum.AxCrypt.Core.UI;
using Axantum.AxCrypt.Core.UI.ViewModel;
using Axantum.AxCrypt.Fake;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Axantum.AxCrypt.Abstractions.TypeResolve;

#pragma warning disable 3016 // Attribute-arguments as arrays are not CLS compliant. Ignore this here, it's how NUnit works.

namespace Axantum.AxCrypt.Core.Test
{
    [TestFixture(CryptoImplementation.Mono)]
    [TestFixture(CryptoImplementation.WindowsDesktop)]
    [TestFixture(CryptoImplementation.BouncyCastle)]
    public class TestMainViewModel
    {
        private CryptoImplementation _cryptoImplementation;

        public TestMainViewModel(CryptoImplementation cryptoImplementation)
        {
            _cryptoImplementation = cryptoImplementation;
        }

        [SetUp]
        public void Setup()
        {
            SetupAssembly.AssemblySetup();
            SetupAssembly.AssemblySetupCrypto(_cryptoImplementation);

            TypeMap.Register.Singleton<ParallelFileOperation>(() => new ParallelFileOperation());
        }

        [TearDown]
        public void Teardown()
        {
            SetupAssembly.AssemblyTeardown();
        }

        [Test]
        public void TestOpenSelectedFolderAction()
        {
            string filePath = @"C:\Folder\File.txt";

            var mock = new Mock<FakeLauncher>() { CallBase = true };
            TypeMap.Register.New<ILauncher>(() => mock.Object);

            using (MainViewModel mvm = New<MainViewModel>())
            {
                mvm.OpenSelectedFolder.Execute(filePath);
            }
            mock.Verify(r => r.Launch(filePath));
        }

        [Test]
        public void TestUpdateCheckWhenSigningIn()
        {
            AxCryptUpdateCheck mockedUpdateCheck = null;
            TypeMap.Register.New<AxCryptUpdateCheck>(() => mockedUpdateCheck = new Mock<AxCryptUpdateCheck>(new Version(2, 0, 200, 0)).Object);
            using (MainViewModel mvm = New<MainViewModel>())
            {
                mvm.LoggedOn = true;
            }

            Mock.Get<AxCryptUpdateCheck>(mockedUpdateCheck).Verify(x => x.CheckInBackgroundAsync(It.Is<DateTime>((d) => d == Resolve.UserSettings.LastUpdateCheckUtc), It.IsAny<string>(), It.IsAny<Uri>(), It.IsAny<string>()));
        }

        [Test]
        public async Task TestUpdateCheckExecutableWhenSignedIn()
        {
            Version ourVersion = New<IVersion>().Current;
            var mockUpdateCheck = new Mock<AxCryptUpdateCheck>(ourVersion);
            TypeMap.Register.New<AxCryptUpdateCheck>(() => mockUpdateCheck.Object);

            using (MainViewModel mvm = New<MainViewModel>())
            {
                mvm.LoggedOn = true;
                Assert.That(await mvm.AxCryptUpdateCheck.CanExecuteAsync(null), Is.True);
                await mvm.AxCryptUpdateCheck.ExecuteAsync(new DateTime(2001, 2, 3));
            }

            mockUpdateCheck.Verify(x => x.CheckInBackgroundAsync(It.Is<DateTime>(d => d == new DateTime(2001, 2, 3)), It.IsAny<string>(), It.IsAny<Uri>(), It.IsAny<string>()));
        }

        [Test]
        public void TestVersionUpdate()
        {
            Version ourVersion = New<IVersion>().Current;
            var mockUpdateCheck = new Mock<AxCryptUpdateCheck>(ourVersion);
            TypeMap.Register.New<AxCryptUpdateCheck>(() => mockUpdateCheck.Object);

            using (MainViewModel mvm = New<MainViewModel>())
            {
                mvm.LoggedOn = true;

                mockUpdateCheck.Raise(m => m.AxCryptUpdate += null, new VersionEventArgs(new DownloadVersion(new Uri("http://localhost/"), new Version(2, 0, 9999, 0)), DateTime.MinValue));
                Assert.That(mvm.VersionUpdateStatus, Is.EqualTo(VersionUpdateStatus.NewerVersionIsAvailable));
            }
        }

        [Test]
        public void TestDragAndDropFilesPropertyBindSetsDragAndDropFileTypes()
        {
            using (MainViewModel mvm = New<MainViewModel>())
            {
                string encryptedFilePath = @"C:\Folder\File-txt.666";
                mvm.DragAndDropFiles = new string[] { encryptedFilePath, };
                Assert.That(mvm.DragAndDropFilesTypes, Is.EqualTo(FileInfoTypes.None));

                string decryptedFilePath = @"C:\Folder\File.txt";
                mvm.DragAndDropFiles = new string[] { decryptedFilePath, };
                Assert.That(mvm.DragAndDropFilesTypes, Is.EqualTo(FileInfoTypes.None));

                FakeDataStore.AddFile(encryptedFilePath, null);
                mvm.DragAndDropFiles = new string[] { encryptedFilePath, decryptedFilePath, };
                Assert.That(mvm.DragAndDropFilesTypes, Is.EqualTo(FileInfoTypes.EncryptedFile));

                FakeDataStore.AddFile(decryptedFilePath, null);
                mvm.DragAndDropFiles = new string[] { encryptedFilePath, decryptedFilePath, };
                Assert.That(mvm.DragAndDropFilesTypes, Is.EqualTo(FileInfoTypes.EncryptableFile | FileInfoTypes.EncryptedFile));

                string folderPath = @"C:\Folder\";
                mvm.DragAndDropFiles = new string[] { encryptedFilePath, decryptedFilePath, folderPath };
                Assert.That(mvm.DragAndDropFilesTypes, Is.EqualTo(FileInfoTypes.EncryptableFile | FileInfoTypes.EncryptedFile));

                FakeDataStore.AddFolder(folderPath);
                mvm.DragAndDropFiles = new string[] { encryptedFilePath, decryptedFilePath, folderPath };
                Assert.That(mvm.DragAndDropFilesTypes, Is.EqualTo(FileInfoTypes.EncryptableFile | FileInfoTypes.EncryptedFile));
            }
        }

        [Test]
        public async Task TestDragAndDropFilesPropertyBindSetsDroppableAsRecent()
        {
            using (MainViewModel mvm = New<MainViewModel>())
            {
                string encryptedFilePath = @"C:\Folder\File-txt.666";
                mvm.DragAndDropFiles = new string[] { encryptedFilePath, };
                Assert.That(mvm.DroppableAsRecent, Is.False, "An encrypted file that does not exist is not a candidate for recent.");

                LogOnIdentity id = new LogOnIdentity("passphrase1");
                Resolve.FileSystemState.KnownPassphrases.Add(id.Passphrase);
                await Resolve.KnownIdentities.SetDefaultEncryptionIdentity(id);
                mvm.DragAndDropFiles = new string[] { encryptedFilePath, };
                Assert.That(mvm.DroppableAsRecent, Is.False, "An encrypted file that does not exist, even when logged on, is not droppable as recent.");

                FakeDataStore.AddFile(encryptedFilePath, null);
                await Resolve.KnownIdentities.SetDefaultEncryptionIdentity(LogOnIdentity.Empty);
                mvm.DragAndDropFiles = new string[] { encryptedFilePath, };
                Assert.That(mvm.DroppableAsRecent, Is.True, "An encrypted file that exist is droppable as recent even when not logged on.");

                string decryptedFilePath = @"C:\Folder\File.txt";
                mvm.DragAndDropFiles = new string[] { decryptedFilePath, };
                Assert.That(mvm.DroppableAsRecent, Is.False, "An encryptable file that does not exist is not droppable as recent.");

                FakeDataStore.AddFile(decryptedFilePath, null);
                mvm.DragAndDropFiles = new string[] { decryptedFilePath, };
                Assert.That(mvm.DroppableAsRecent, Is.False, "An encrpytable file without a valid log on is not droppable as recent.");

                id = new LogOnIdentity("passphrase");
                Resolve.FileSystemState.KnownPassphrases.Add(id.Passphrase);
                await Resolve.KnownIdentities.SetDefaultEncryptionIdentity(id);
                mvm.DragAndDropFiles = new string[] { decryptedFilePath, };
                Assert.That(mvm.DroppableAsRecent, Is.True, "An encryptable existing file with a valid log on should be droppable as recent.");
            }
        }

        [Test]
        public void TestDragAndDropFilesPropertyBindSetsDroppableAsWatchedFolder()
        {
            using (MainViewModel mvm = New<MainViewModel>())
            {
                string folder1Path = @"C:\Folder1\FilesFolder\".NormalizeFilePath();
                mvm.DragAndDropFiles = new string[] { folder1Path, };
                Assert.That(mvm.DroppableAsWatchedFolder, Is.False, "A folder that does not exist is not a candidate for watched folders.");

                FakeDataStore.AddFolder(folder1Path);
                mvm.DragAndDropFiles = new string[] { folder1Path, };
                Assert.That(mvm.DroppableAsWatchedFolder, Is.True, "This is a candidate for watched folders.");

                New<FileFilter>().AddUnencryptable(new Regex(@"^C:\{0}Folder1\{0}".InvariantFormat(Path.DirectorySeparatorChar)));
                mvm.DragAndDropFiles = new string[] { folder1Path, };
                Assert.That(mvm.DroppableAsWatchedFolder, Is.False, "A folder that matches a path filter is not a candidate for watched folders.");

                string folder2Path = @"C:\Folder1\FilesFolder2\".NormalizeFilePath();
                FakeDataStore.AddFolder(folder2Path);
                TypeMap.Register.Singleton<FileFilter>(() => new FileFilter());

                mvm.DragAndDropFiles = new string[] { folder1Path, folder2Path, };
                Assert.That(mvm.DroppableAsWatchedFolder, Is.False, "Although both folders are ok, only a single folder is a candidate for watched folders.");
            }
        }

        [Test]
        public void TestSetRecentFilesComparer()
        {
            string file1 = @"C:\Folder\File3-txt.666";
            string decrypted1 = @"C:\Folder\File2.txt";
            string file2 = @"C:\Folder\File2-txt.666";
            string decrypted2 = @"C:\Folder\File1.txt";
            string file3 = @"C:\Folder\File1-txt.666";
            string decrypted3 = @"C:\Folder\File3.txt";

            ActiveFile activeFile;

            ((FakeNow)New<INow>()).TimeFunction = () => new DateTime(2001, 1, 1);
            FakeDataStore.AddFile(file1, new MemoryStream(Resources.helloworld_key_a_txt));
            activeFile = new ActiveFile(New<IDataStore>(file1), New<IDataStore>(decrypted1), new LogOnIdentity("passphrase1"), ActiveFileStatus.NotDecrypted, new V1Aes128CryptoFactory().CryptoId);
            Resolve.FileSystemState.Add(activeFile);

            ((FakeNow)New<INow>()).TimeFunction = () => new DateTime(2002, 2, 2);
            FakeDataStore.AddFile(file2, new MemoryStream(Resources.helloworld_key_a_txt));
            activeFile = new ActiveFile(New<IDataStore>(file2), New<IDataStore>(decrypted2), new LogOnIdentity("passphrase2"), ActiveFileStatus.NotDecrypted, new V1Aes128CryptoFactory().CryptoId);
            Resolve.FileSystemState.Add(activeFile);

            ((FakeNow)New<INow>()).TimeFunction = () => new DateTime(2003, 3, 3);
            FakeDataStore.AddFile(file3, new MemoryStream(Resources.helloworld_key_a_txt));
            activeFile = new ActiveFile(New<IDataStore>(file3), New<IDataStore>(decrypted3), new LogOnIdentity("passphrase"), ActiveFileStatus.NotDecrypted, new V1Aes128CryptoFactory().CryptoId);
            Resolve.FileSystemState.Add(activeFile);

            ActiveFileComparer comparer;
            List<ActiveFile> recentFiles;

            comparer = ActiveFileComparer.DateComparer;
            using (MainViewModel mvm = New<MainViewModel>())
            {
                mvm.RecentFiles = Resolve.FileSystemState.ActiveFiles;
                mvm.RecentFilesComparer = comparer;
                recentFiles = mvm.RecentFiles.ToList();

                Assert.That(recentFiles[0].EncryptedFileInfo.FullName, Is.EqualTo(file1.NormalizeFilePath()), "Sorted by Date, this should be number 1.");
                Assert.That(recentFiles[1].EncryptedFileInfo.FullName, Is.EqualTo(file2.NormalizeFilePath()), "Sorted by Date, this should be number 2.");
                Assert.That(recentFiles[2].EncryptedFileInfo.FullName, Is.EqualTo(file3.NormalizeFilePath()), "Sorted by Date, this should be number 3.");

                comparer = ActiveFileComparer.DateComparer;
                comparer.ReverseSort = true;
                mvm.RecentFilesComparer = comparer;
                recentFiles = mvm.RecentFiles.ToList();
            }

            Assert.That(recentFiles[0].EncryptedFileInfo.FullName, Is.EqualTo(file3.NormalizeFilePath()), "Sorted by Date in reverse, this should be number 1.");
            Assert.That(recentFiles[1].EncryptedFileInfo.FullName, Is.EqualTo(file2.NormalizeFilePath()), "Sorted by Date, this should be number 2.");
            Assert.That(recentFiles[2].EncryptedFileInfo.FullName, Is.EqualTo(file1.NormalizeFilePath()), "Sorted by Date, this should be number 3.");
        }

        [Test]
        public async Task TestOpenFiles()
        {
            string file1 = @"C:\Folder\File3-txt.666";
            string decrypted1 = @"C:\Folder\File2.txt";
            string file2 = @"C:\Folder\File2-txt.666";
            string decrypted2 = @"C:\Folder\File1.txt";
            string file3 = @"C:\Folder\File1-txt.666";
            string decrypted3 = @"C:\Folder\File3.txt";

            ActiveFile activeFile;

            using (MainViewModel mvm = New<MainViewModel>())
            {
                ((FakeNow)New<INow>()).TimeFunction = () => new DateTime(2001, 1, 1);
                FakeDataStore.AddFile(file1, new MemoryStream(Resources.helloworld_key_a_txt));
                activeFile = new ActiveFile(New<IDataStore>(file1), New<IDataStore>(decrypted1), new LogOnIdentity("passphrase1"), ActiveFileStatus.AssumedOpenAndDecrypted, new V1Aes128CryptoFactory().CryptoId);
                Resolve.FileSystemState.Add(activeFile);

                ((FakeNow)New<INow>()).TimeFunction = () => new DateTime(2002, 2, 2);
                FakeDataStore.AddFile(file2, new MemoryStream(Resources.helloworld_key_a_txt));
                activeFile = new ActiveFile(New<IDataStore>(file2), New<IDataStore>(decrypted2), new LogOnIdentity("passphrase2"), ActiveFileStatus.NotDecrypted, new V1Aes128CryptoFactory().CryptoId);
                Resolve.FileSystemState.Add(activeFile);

                ((FakeNow)New<INow>()).TimeFunction = () => new DateTime(2003, 3, 3);
                FakeDataStore.AddFile(file3, new MemoryStream(Resources.helloworld_key_a_txt));
                activeFile = new ActiveFile(New<IDataStore>(file3), New<IDataStore>(decrypted3), new LogOnIdentity("passphrase3"), ActiveFileStatus.NotDecrypted, new V1Aes128CryptoFactory().CryptoId);
                Resolve.FileSystemState.Add(activeFile);
                await Resolve.FileSystemState.Save();

                Assert.That(mvm.FilesArePending, Is.True);

                activeFile = new ActiveFile(New<IDataStore>(file1), New<IDataStore>(decrypted1), new LogOnIdentity("passphrase"), ActiveFileStatus.NotDecrypted, new V1Aes128CryptoFactory().CryptoId);
                Resolve.FileSystemState.Add(activeFile);
                await Resolve.FileSystemState.Save();

                Assert.That(mvm.FilesArePending, Is.False);
            }
        }

        [Test]
        public async Task TestRemoveRecentFiles()
        {
            var mockFileSystemState = new Mock<FileSystemState>() { CallBase = true };
            mockFileSystemState.Setup(x => x.Save()).Returns(Constant.CompletedTask);

            TypeMap.Register.Singleton<FileSystemState>(() => mockFileSystemState.Object);

            string file1 = @"C:\Folder\File3-txt.666";
            string decrypted1 = @"C:\Folder\File2.txt";
            string file2 = @"C:\Folder\File2-txt.666";
            string decrypted2 = @"C:\Folder\File1.txt";
            string file3 = @"C:\Folder\File1-txt.666";
            string decrypted3 = @"C:\Folder\File3.txt";

            ActiveFile activeFile;

            FakeDataStore.AddFile(file1, new MemoryStream(Resources.helloworld_key_a_txt));
            activeFile = new ActiveFile(New<IDataStore>(file1), New<IDataStore>(decrypted1), new LogOnIdentity("passphrase1"), ActiveFileStatus.NotDecrypted, new V1Aes128CryptoFactory().CryptoId);
            Resolve.FileSystemState.Add(activeFile);

            FakeDataStore.AddFile(file2, new MemoryStream(Resources.helloworld_key_a_txt));
            activeFile = new ActiveFile(New<IDataStore>(file2), New<IDataStore>(decrypted2), new LogOnIdentity("passphrase2"), ActiveFileStatus.NotDecrypted, new V1Aes128CryptoFactory().CryptoId);
            Resolve.FileSystemState.Add(activeFile);

            FakeDataStore.AddFile(file3, new MemoryStream(Resources.helloworld_key_a_txt));
            activeFile = new ActiveFile(New<IDataStore>(file3), New<IDataStore>(decrypted3), new LogOnIdentity("passphrase"), ActiveFileStatus.NotDecrypted, new V1Aes128CryptoFactory().CryptoId);
            Resolve.FileSystemState.Add(activeFile);

            using (MainViewModel mvm = New<MainViewModel>())
            {
                Assert.That(await mvm.RemoveRecentFiles.CanExecuteAsync(null), Is.True, "RemoveRecentFiles should be executable by default.");

                await mvm.RemoveRecentFiles.ExecuteAsync(new string[] { file2 });
                mockFileSystemState.Verify(x => x.RemoveActiveFile(It.IsAny<ActiveFile>()), Times.Once, "Exactly one recent file should be removed.");

                mockFileSystemState.ResetCalls();
                await mvm.RemoveRecentFiles.ExecuteAsync(new string[] { file2 });
            }
            mockFileSystemState.Verify(x => x.RemoveActiveFile(It.IsAny<ActiveFile>()), Times.Never, "There is no longer any matching file, so no call to remove should happen.");
        }

        [Test]
        public async Task TestPurgeRecentFiles()
        {
            var mockActiveFileAction = new Mock<ActiveFileAction>();

            TypeMap.Register.New<ActiveFileAction>(() => mockActiveFileAction.Object);

            Mock<IStatusChecker> mockStatusChecker = new Mock<IStatusChecker>();

            TypeMap.Register.New<SessionNotificationHandler>(() => new SessionNotificationHandler(Resolve.FileSystemState, Resolve.KnownIdentities, New<ActiveFileAction>(), New<AxCryptFile>(), mockStatusChecker.Object));
            Resolve.SessionNotify.AddCommand(async (notification) => await New<SessionNotificationHandler>().HandleNotificationAsync(notification));

            using (MainViewModel mvm = New<MainViewModel>())
            {
                Assert.That(await mvm.EncryptPendingFiles.CanExecuteAsync(null), Is.True, "PurgeRecentFiles should be executable by default.");

                await mvm.EncryptPendingFiles.ExecuteAsync(null);
            }
            New<IProgressBackground>().WaitForIdle();
#pragma warning disable 4014
            mockActiveFileAction.Verify(x => x.PurgeActiveFiles(It.IsAny<IProgressContext>()), Times.Once, "Purge should be called.");
#pragma warning restore 4014
        }

        [Test]
        public async Task TestClearPassphraseMemory()
        {
            string file1 = @"C:\Folder\File3-txt.666";
            string decrypted1 = @"C:\Folder\File2.txt";

            ActiveFile activeFile;

            FakeDataStore.AddFile(file1, new MemoryStream(Resources.helloworld_key_a_txt));
            activeFile = new ActiveFile(New<IDataStore>(file1), New<IDataStore>(decrypted1), new LogOnIdentity("passphrase1"), ActiveFileStatus.NotDecrypted, new V1Aes128CryptoFactory().CryptoId);
            Resolve.FileSystemState.Add(activeFile);

            await Resolve.KnownIdentities.AddAsync(new LogOnIdentity("passphrase2"));
            LogOnIdentity id = new LogOnIdentity("passphrase");
            Resolve.FileSystemState.KnownPassphrases.Add(id.Passphrase);
            await Resolve.KnownIdentities.SetDefaultEncryptionIdentity(id);

            Assert.That(Resolve.FileSystemState.ActiveFileCount, Is.EqualTo(1), "One ActiveFile is expected.");
            Assert.That(Resolve.KnownIdentities.Identities.Count(), Is.EqualTo(2), "Two known keys are expected.");
            Assert.That(Resolve.KnownIdentities.DefaultEncryptionIdentity != LogOnIdentity.Empty, "There should be a non-null default encryption key");

            var sessionNotificationMonitorMock = new Mock<SessionNotify>();
            TypeMap.Register.Singleton<SessionNotify>(() => sessionNotificationMonitorMock.Object);
            using (MainViewModel mvm = New<MainViewModel>())
            {
                await mvm.ClearPassphraseMemory.ExecuteAsync(null);
            }

            Assert.That(Resolve.FileSystemState.ActiveFileCount, Is.EqualTo(0));
            Assert.That(Resolve.KnownIdentities.Identities.Count(), Is.EqualTo(0));
            Assert.That(Resolve.KnownIdentities.DefaultEncryptionIdentity == LogOnIdentity.Empty);

            sessionNotificationMonitorMock.Verify(x => x.NotifyAsync(It.Is<SessionNotification>(sn => sn.NotificationType == SessionNotificationType.SessionStart)), Times.Once);
        }

        [Test]
        public async Task TestRemoveWatchedFolders()
        {
            var mockFileSystemState = new Mock<FileSystemState>() { CallBase = true };
            mockFileSystemState.Setup(x => x.Save()).Returns(Constant.CompletedTask);

            TypeMap.Register.Singleton<FileSystemState>(() => mockFileSystemState.Object);

            using (MainViewModel mvm = New<MainViewModel>())
            {
                LogOnIdentity id = new LogOnIdentity("passphrase");
                mockFileSystemState.Object.KnownPassphrases.Add(id.Passphrase);
                await Resolve.KnownIdentities.SetDefaultEncryptionIdentity(id);
                mockFileSystemState.ResetCalls();

                await mvm.DecryptWatchedFolders.ExecuteAsync(new string[] { "File1.txt", "file2.txt" });
            }

            mockFileSystemState.Verify(x => x.RemoveAndDecryptWatchedFolder(It.IsAny<IDataContainer>()), Times.Exactly(2));
            mockFileSystemState.Verify(x => x.Save(), Times.Once);
        }

        [Test]
        public async Task TestAddAndRemoveWatchedFolderState()
        {
            var fileSystemStateMock = new Mock<FileSystemState>() { CallBase = true };
            fileSystemStateMock.Setup(x => x.Save()).Returns(Constant.CompletedTask);

            TypeMap.Register.Singleton<FileSystemState>(() => fileSystemStateMock.Object);
            using (MainViewModel mvm = New<MainViewModel>())
            {
                Assert.ThrowsAsync<InvalidOperationException>(async () => await mvm.DecryptWatchedFolders.ExecuteAsync(new string[] { }));

                LogOnIdentity id = new LogOnIdentity("passphrase");
                fileSystemStateMock.Object.KnownPassphrases.Add(id.Passphrase);
                await Resolve.KnownIdentities.SetDefaultEncryptionIdentity(id);
                fileSystemStateMock.ResetCalls();

                await mvm.DecryptWatchedFolders.ExecuteAsync(new string[] { });

                fileSystemStateMock.Verify(x => x.RemoveAndDecryptWatchedFolder(It.IsAny<IDataContainer>()), Times.Never);
                fileSystemStateMock.Verify(x => x.Save(), Times.Never);

                fileSystemStateMock.ResetCalls();
                await mvm.AddWatchedFolders.ExecuteAsync(new string[] { });
                fileSystemStateMock.Verify(x => x.AddWatchedFolderAsync(It.IsAny<WatchedFolder>()), Times.Never);
                fileSystemStateMock.Verify(x => x.Save(), Times.Never);

                await mvm.AddWatchedFolders.ExecuteAsync(new string[] { @"C:\Folder1\", @"C:\Folder2\" });

                fileSystemStateMock.Verify(x => x.AddWatchedFolderAsync(It.IsAny<WatchedFolder>()), Times.Exactly(2));
                fileSystemStateMock.Verify(x => x.Save(), Times.Once);

                fileSystemStateMock.ResetCalls();
                await mvm.DecryptWatchedFolders.ExecuteAsync(new string[] { @"C:\Folder1\" });
            }

            fileSystemStateMock.Verify(x => x.RemoveAndDecryptWatchedFolder(It.IsAny<IDataContainer>()), Times.Exactly(1));
            fileSystemStateMock.Verify(x => x.Save(), Times.Once);
        }

        [Test]
        public async Task TestSetDefaultEncryptionKeyWithoutIdentity()
        {
            var fileSystemStateMock = new Mock<FileSystemState>() { CallBase = true };
            fileSystemStateMock.Setup(x => x.Save());

            TypeMap.Register.Singleton<FileSystemState>(() => fileSystemStateMock.Object);
            using (MainViewModel mvm = New<MainViewModel>())
            {
                LogOnIdentity identity = new LogOnIdentity("passphrase");
                Assert.That(!Resolve.FileSystemState.KnownPassphrases.Any(kp => kp.Thumbprint == identity.Passphrase.Thumbprint));
                await Resolve.KnownIdentities.SetDefaultEncryptionIdentity(new LogOnIdentity("passphrase"));
                Assert.That(Resolve.FileSystemState.KnownPassphrases.Any(kp => kp.Thumbprint == identity.Passphrase.Thumbprint));
            }
        }

        [Test]
        public void TestSetDebugMode()
        {
            var fileSystemStateMock = new Mock<FileSystemState>();
            var logMock = new Mock<ILogging>();

            TypeMap.Register.Singleton<FileSystemState>(() => fileSystemStateMock.Object);
            TypeMap.Register.Singleton<ILogging>(() => logMock.Object);

            using (MainViewModel mvm = New<MainViewModel>())
            {
                logMock.ResetCalls();

                mvm.DebugMode = true;
                logMock.Verify(x => x.SetLevel(It.Is<LogLevel>(ll => ll == LogLevel.Debug)));
                Assert.That(FakeRuntimeEnvironment.Instance.IsDebugModeEnabled, Is.True);

                logMock.ResetCalls();

                mvm.DebugMode = false;
            }
            logMock.Verify(x => x.SetLevel(It.Is<LogLevel>(ll => ll == LogLevel.Error)));
            Assert.That(FakeRuntimeEnvironment.Instance.IsDebugModeEnabled, Is.False);
        }

        [Test]
        public void TestSelectedRecentFiles()
        {
            using (MainViewModel mvm = New<MainViewModel>())
            {
                Assert.That(mvm.SelectedRecentFiles.Any(), Is.False);

                mvm.SelectedRecentFiles = new string[] { @"C:\Folder\Test1.666", @"C:\Folder\Test2.666" };

                Assert.That(mvm.SelectedRecentFiles.Count(), Is.EqualTo(2));
            }
        }

        [Test]
        public void TestSelectedWatchedFolders()
        {
            using (MainViewModel mvm = New<MainViewModel>())
            {
                Assert.That(mvm.SelectedWatchedFolders.Any(), Is.False);

                mvm.SelectedWatchedFolders = new string[] { @"C:\Folder1\", @"C:\Folder2\" };

                Assert.That(mvm.SelectedWatchedFolders.Count(), Is.EqualTo(2));
            }
        }

        [Test]
        public async Task TestNotifyWatchedFolderAdded()
        {
            await Resolve.KnownIdentities.SetDefaultEncryptionIdentity(new LogOnIdentity("passphrase"));
            FakeDataStore.AddFolder(@"C:\MyFolders\Folder1");
            using (MainViewModel mvm = New<MainViewModel>())
            {
                Assert.That(mvm.WatchedFolders.Count(), Is.EqualTo(0));

                await Resolve.FileSystemState.AddWatchedFolderAsync(new WatchedFolder(@"C:\MyFolders\Folder1", Resolve.KnownIdentities.DefaultEncryptionIdentity.Tag));

                Assert.That(mvm.WatchedFolders.Count(), Is.EqualTo(1));
                Assert.That(mvm.WatchedFolders.First(), Is.EqualTo(@"C:\MyFolders\Folder1".NormalizeFolderPath()));
            }
        }

        [Test]
        public async Task TestSetFilesArePending()
        {
            await Resolve.KnownIdentities.SetDefaultEncryptionIdentity(new LogOnIdentity("passphrase"));
            FakeDataStore.AddFolder(@"C:\MyFolders\Folder1");
            using (MainViewModel mvm = New<MainViewModel>())
            {
                Assert.That(mvm.FilesArePending, Is.False);
                await Resolve.FileSystemState.AddWatchedFolderAsync(new WatchedFolder(@"C:\MyFolders\Folder1", Resolve.KnownIdentities.DefaultEncryptionIdentity.Tag));
                FakeDataStore.AddFile(@"C:\MyFolders\Folder1\Encryptable.txt", Stream.Null);
                Assert.That(mvm.FilesArePending, Is.True);
            }
        }
    }
}