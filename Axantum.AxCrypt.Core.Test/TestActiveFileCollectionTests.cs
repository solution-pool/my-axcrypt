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
using Axantum.AxCrypt.Core.IO;
using Axantum.AxCrypt.Core.Session;
using Axantum.AxCrypt.Core.Test.Properties;
using Axantum.AxCrypt.Fake;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;

using static Axantum.AxCrypt.Abstractions.TypeResolve;

namespace Axantum.AxCrypt.Core.Test
{
    [TestFixture]
    public static class TestActiveFileCollectionTests
    {
        private static readonly string _rootPath = Path.GetPathRoot(Environment.CurrentDirectory);
        private static readonly string _testTextPath = Path.Combine(_rootPath, "test.txt");
        private static readonly string _davidCopperfieldTxtPath = _rootPath.PathCombine("Users", "AxCrypt", "David Copperfield.txt");
        private static readonly string _uncompressedAxxPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Uncompressed.666");
        private static readonly string _helloWorldAxxPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HelloWorld.666");

        [SetUp]
        public static void Setup()
        {
            SetupAssembly.AssemblySetup();

            FakeDataStore.AddFile(_testTextPath, FakeDataStore.TestDate1Utc, FakeDataStore.TestDate2Utc, FakeDataStore.TestDate1Utc, FakeDataStore.ExpandableMemoryStream(Encoding.UTF8.GetBytes("This is a short file")));
            FakeDataStore.AddFile(_davidCopperfieldTxtPath, FakeDataStore.TestDate4Utc, FakeDataStore.TestDate5Utc, FakeDataStore.TestDate6Utc, FakeDataStore.ExpandableMemoryStream(Encoding.GetEncoding(1252).GetBytes(Resources.david_copperfield)));
            FakeDataStore.AddFile(_uncompressedAxxPath, FakeDataStore.ExpandableMemoryStream(Resources.uncompressable_zip));
            FakeDataStore.AddFile(_helloWorldAxxPath, FakeDataStore.ExpandableMemoryStream(Resources.helloworld_key_a_txt));
        }

        [TearDown]
        public static void Teardown()
        {
            SetupAssembly.AssemblyTeardown();
        }

        [Test]
        public static void TestActiveFileCollectionSimpleConstructor()
        {
            ActiveFileCollection collection = new ActiveFileCollection(new ActiveFile[0]);

            IDataStore decryptedFileInfo = New<IDataStore>(_testTextPath);
            IDataStore encryptedFileInfo = New<IDataStore>(_helloWorldAxxPath);
            ActiveFile activeFile = new ActiveFile(encryptedFileInfo, decryptedFileInfo, new LogOnIdentity("new"), ActiveFileStatus.None, new V1Aes128CryptoFactory().CryptoId);

            collection.Add(activeFile);

            Assert.That(collection.Count, Is.EqualTo(1), "There should be one entry in the collection.");

            foreach (ActiveFile member in collection)
            {
                Assert.That(member, Is.EqualTo(activeFile), "The one we added should now be in the collection.");
            }
        }

        [Test]
        public static void TestActiveFileCollectionEnumerationConstructor()
        {
            IDataStore decryptedFileInfo1 = New<IDataStore>(Path.Combine(_rootPath, "test1.txt"));
            IDataStore encryptedFileInfo1 = New<IDataStore>(Path.Combine(_rootPath, "test1-txt.666"));
            IDataStore decryptedFileInfo2 = New<IDataStore>(Path.Combine(_rootPath, "test2.txt"));
            IDataStore encryptedFileInfo2 = New<IDataStore>(Path.Combine(_rootPath, "test2-text.666"));
            ActiveFile activeFile1 = new ActiveFile(encryptedFileInfo1, decryptedFileInfo1, new LogOnIdentity("newA"), ActiveFileStatus.None, new V1Aes128CryptoFactory().CryptoId);
            ActiveFile activeFile2 = new ActiveFile(encryptedFileInfo2, decryptedFileInfo2, new LogOnIdentity("newB"), ActiveFileStatus.None, new V1Aes128CryptoFactory().CryptoId);

            ActiveFileCollection collection = new ActiveFileCollection(new ActiveFile[] { activeFile1, activeFile2 });

            Assert.That(collection.Count, Is.EqualTo(2), "There should be two entries in the collection.");

            Assert.That(collection.First(), Is.EqualTo(activeFile1), "This should be the first in the collection.");
            Assert.That(collection.Last(), Is.EqualTo(activeFile2), "This should be the last in the collection.");
        }
    }
}