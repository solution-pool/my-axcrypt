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
using Axantum.AxCrypt.Abstractions.Algorithm;
using Axantum.AxCrypt.Api.Model;
using Axantum.AxCrypt.Core.Crypto;
using Axantum.AxCrypt.Core.Crypto.Asymmetric;
using Axantum.AxCrypt.Core.Extensions;
using Axantum.AxCrypt.Core.IO;
using Axantum.AxCrypt.Core.Runtime;
using Axantum.AxCrypt.Core.Service;
using Axantum.AxCrypt.Core.UI;
using Axantum.AxCrypt.Fake;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

using static Axantum.AxCrypt.Abstractions.TypeResolve;

namespace Axantum.AxCrypt.Core.Test
{
    [TestFixture]
    public static class TestExtensions
    {
        [SetUp]
        public static void Setup()
        {
            SetupAssembly.AssemblySetup();
        }

        [TearDown]
        public static void Teardown()
        {
            SetupAssembly.AssemblyTeardown();
        }

        [Test]
        public static void TestExtensions_StringInvariantFormat()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");
            DateTime dateTime = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);

            string dateTimeSwedishFormatted = String.Format(CultureInfo.CurrentCulture, "Formatted: {0}", dateTime);
            Assert.That(dateTimeSwedishFormatted, Is.EqualTo("Formatted: 2001-02-03 04:05:06"));

            string dateTimeInvariantFormatted = "Formatted: {0}".InvariantFormat(dateTime);
            Assert.That(dateTimeInvariantFormatted, Is.EqualTo("Formatted: 02/03/2001 04:05:06"));
        }

        [Test]
        public static void TestExtensions_StringFormatWith()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");
            DateTime dateTime = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);

            string dateTimeSwedishFormatted = "Formatted: {0}".FormatWith(CultureInfo.CurrentCulture, dateTime);
            Assert.That(dateTimeSwedishFormatted, Is.EqualTo("Formatted: 2001-02-03 04:05:06"));

            string dateTimeInvariantFormatted = "Formatted: {0}".FormatWith(CultureInfo.InvariantCulture, dateTime);
            Assert.That(dateTimeInvariantFormatted, Is.EqualTo("Formatted: 02/03/2001 04:05:06"));
        }

        [Test]
        public static void TestExtensions_ByteArrayLocate()
        {
            byte[] testArray = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x09, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, 0x00 };

            int locationSingleByteFirstInArray = testArray.Locate(new byte[] { 0x01 }, 0, testArray.Length);
            Assert.That(locationSingleByteFirstInArray, Is.EqualTo(0), "Looking for a single byte that is first in the array.");

            int locationSingleByteLastInArray = testArray.Locate(new byte[] { 0x00 }, 0, testArray.Length);
            Assert.That(locationSingleByteLastInArray, Is.EqualTo(testArray.Length - 1), "Looking for a single byte that is last in the array.");

            int locationSingleByteNotInArray = testArray.Locate(new byte[] { 0xff }, 0, testArray.Length);
            Assert.That(locationSingleByteNotInArray, Is.EqualTo(-1), "Looking for a single byte that is not part of the array.");

            int locationSingleByteNotInPartOfArray = testArray.Locate(new byte[] { 0x01 }, 1, testArray.Length - 3);
            Assert.That(locationSingleByteNotInPartOfArray, Is.EqualTo(-1), "Looking for a single byte that is not in a selected part of the array.");

            int locationMultipleBytesFirstInArray = testArray.Locate(new byte[] { 0x01, 0x02, 0x03 }, 0, testArray.Length);
            Assert.That(locationMultipleBytesFirstInArray, Is.EqualTo(0), "Looking for multiple bytes that are first in the array.");

            int locationMultipleBytesLastInArray = testArray.Locate(new byte[] { 0x02, 0x01, 0x00 }, 0, testArray.Length);
            Assert.That(locationMultipleBytesLastInArray, Is.EqualTo(testArray.Length - 3), "Looking for multiple bytes that are last in the array.");

            int locationMultipleBytesMiddleInArray = testArray.Locate(new byte[] { 0x09, 0x09, 0x08 }, 0, testArray.Length);
            Assert.That(locationMultipleBytesMiddleInArray, Is.EqualTo(8), "Looking for multiple bytes that are in the middle of the array.");

            int locationMultipleBytesNotInArray = testArray.Locate(new byte[] { 0x02, 0x00, 0x01 }, 0, testArray.Length);
            Assert.That(locationMultipleBytesNotInArray, Is.EqualTo(-1), "Looking for multiple bytes that are not in the array.");

            int locationMultipleBytesLastInPartOfArray = testArray.Locate(new byte[] { 0x03, 0x02, 0x01 }, 2, testArray.Length - 3);
            Assert.That(locationMultipleBytesLastInPartOfArray, Is.EqualTo(testArray.Length - 4), "Looking for multiple bytes that are last in part of the array.");

            int locationMultipleBytesNotInPartOfArray = testArray.Locate(new byte[] { 0x03, 0x02, 0x01 }, 2, testArray.Length - 4);
            Assert.That(locationMultipleBytesNotInPartOfArray, Is.EqualTo(-1), "Looking for multiple bytes that are not in part of the array.");
        }

        [Test]
        public static void TestExtensions_ByteArrayXor()
        {
            byte[] testArray = new byte[] { 0x01, 0x02, 0x04, 0x08, 0x00, 0xff };

            byte[] nullArray = null;

            TestDelegate bufferArgumentNullException = () => nullArray.Xor(testArray);
            Assert.Throws<ArgumentNullException>(bufferArgumentNullException, "Calling with a null-reference buffer.");

            TestDelegate otherArgumentNullException = () => testArray.Xor(nullArray);
            Assert.Throws<ArgumentNullException>(otherArgumentNullException, "Calling with a null-reference other argument.");
            Assert.That(testArray, Is.EqualTo(new byte[] { 0x01, 0x02, 0x04, 0x08, 0x00, 0xff }), "Assuring that the array is still unmodified.");

            byte[] xorArray = (byte[])testArray.Clone();
            xorArray.Xor(testArray);
            Assert.That(xorArray, Is.EqualTo(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }), "Xor with all of itself should yield all zeros.");

            xorArray.Xor(new byte[] { 0x01, 0x02, 0x04 });
            Assert.That(xorArray, Is.EqualTo(new byte[] { 0x01, 0x02, 0x04, 0x00, 0x00, 0x00 }), "Xor with a short array should only affect those bytes.");

            xorArray.Xor(new byte[] { 0x00, 0x00, 0x00, 0x08, 0x00, 0xff, 0x12, 0x34, 0x56 });
            Assert.That(xorArray, Is.EqualTo(testArray), "Xor with a longer array should just ignore the extra bytes in other.");

            TestDelegate bufferArgumentWithAllParametersNullException = () => nullArray.Xor(0, testArray, 0, testArray.Length);
            Assert.Throws<ArgumentNullException>(bufferArgumentWithAllParametersNullException, "Calling all parameters overload with null-reference buffer.");

            TestDelegate otherArgumentWithAllParametersNullException = () => testArray.Xor(0, nullArray, 0, testArray.Length);
            Assert.Throws<ArgumentNullException>(otherArgumentWithAllParametersNullException, "Calling all parameters overload with a null-reference other argument.");
            Assert.That(testArray, Is.EqualTo(new byte[] { 0x01, 0x02, 0x04, 0x08, 0x00, 0xff }), "Assuring that the array is still unmodified.");

            TestDelegate bufferLengthArgumentOutOfRangeException = () => testArray.Xor(1, testArray, 0, testArray.Length);
            Assert.Throws<ArgumentOutOfRangeException>(bufferLengthArgumentOutOfRangeException, "Calling with an offset into buffer causing length to be too long.");
            Assert.That(testArray, Is.EqualTo(new byte[] { 0x01, 0x02, 0x04, 0x08, 0x00, 0xff }), "Assuring that the array is still unmodified.");

            TestDelegate otherLengthArgumentOutOfRangeException = () => testArray.Xor(0, testArray, 1, testArray.Length);
            Assert.Throws<ArgumentOutOfRangeException>(otherLengthArgumentOutOfRangeException, "Calling with an offset into other causing length to be too long.");
            Assert.That(testArray, Is.EqualTo(new byte[] { 0x01, 0x02, 0x04, 0x08, 0x00, 0xff }), "Assuring that the array is still unmodified.");

            xorArray.Xor(2, testArray, 4, 2);
            Assert.That(xorArray, Is.EqualTo(new byte[] { 0x01, 0x02, 0x04, 0xf7, 0x00, 0xff }), "Xor part of the buffer with part of the other byte array.");

            Assert.That(xorArray, Is.Not.EqualTo(new byte[] { 0x01, 0x02, 0x04, 0xf7, 0xff, 0xff }), "Sanity check to ensure that we're really testing what we think.");
        }

        [Test]
        public static void TestExtensions_ByteArrayAppend()
        {
            byte[] one = new byte[] { 0x01 };
            byte[] two = new byte[] { 0x02, 0x02 };
            byte[] three = new byte[] { 0x03, 0x03, 0x03 };

            byte[] noAppend = one.Append();
            Assert.That(noAppend, Is.EqualTo(one), "Append with zero arrays should yield the same.");
            Assert.IsFalse(Object.ReferenceEquals(noAppend, one), "Append with zero arrays should yield a copy, not the same instance.");

            byte[] oneTwoAppend = one.Append(two);
            Assert.That(oneTwoAppend, Is.EqualTo(new byte[] { 0x01, 0x02, 0x02 }), "Append of one with two should yield the appended result.");

            byte[] oneTwoThreeAppend = one.Append(two, three);
            Assert.That(oneTwoThreeAppend, Is.EqualTo(new byte[] { 0x01, 0x02, 0x02, 0x03, 0x03, 0x03 }), "Append of one with two and three should yield the appended result.");

            byte[] oneTwoAppendThreeAppend = one.Append(two).Append(three);
            Assert.That(oneTwoAppendThreeAppend, Is.EqualTo(new byte[] { 0x01, 0x02, 0x02, 0x03, 0x03, 0x03 }), "Append of one with two and append again three should yield the appended result (just less efficiently).");
        }

        [Test]
        public static void TestExtensions_ByteArrayIsEquivalentTo()
        {
            byte[] one = new byte[] { 0x01 };
            byte[] two = new byte[] { 0x02, 0x01 };
            byte[] three = new byte[] { 0x02, 0x01, 0x03 };
            byte[] nullArray = null;
            bool isEquivalent = false;

            Assert.Throws<ArgumentNullException>(() => { isEquivalent = nullArray.IsEquivalentTo(0, one, 0, 1); });
            Assert.Throws<ArgumentNullException>(() => { isEquivalent = one.IsEquivalentTo(0, nullArray, 0, 1); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { isEquivalent = one.IsEquivalentTo(one.Length, one, 0, one.Length); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { isEquivalent = two.IsEquivalentTo(0, one, 1, one.Length); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { isEquivalent = three.IsEquivalentTo(0, two, 0, two.Length + 1); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { isEquivalent = three.IsEquivalentTo(-1, three, 0, three.Length); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { isEquivalent = two.IsEquivalentTo(0, three, -1, two.Length); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { isEquivalent = one.IsEquivalentTo(0, one, 0, -1); });

            isEquivalent = one.IsEquivalentTo(0, one, 0, 1);
            Assert.IsTrue(isEquivalent, "An array should be equivalent to itself.");

            isEquivalent = one.IsEquivalentTo(0, two, 1, 1);
            Assert.IsTrue(isEquivalent, "'one' should be equivalent to second byte of 'two'.");

            isEquivalent = three.IsEquivalentTo(1, one, 0, 1);
            Assert.IsTrue(isEquivalent, "the second byte of 'three' should be equivalent to 'one'");

            isEquivalent = two.IsEquivalentTo(0, three, 1, two.Length);
            Assert.IsFalse(isEquivalent, "'two' should not be equivalent to 'three' with offset one.");

            Assert.Throws<ArgumentNullException>(() => { isEquivalent = nullArray.IsEquivalentTo(one); });
            Assert.Throws<ArgumentNullException>(() => { isEquivalent = one.IsEquivalentTo(nullArray); });

            byte[] threeAgain = new byte[] { 0x02, 0x01, 0x03 };
            isEquivalent = threeAgain.IsEquivalentTo(three);
            Assert.IsTrue(isEquivalent, "'threeAgain' should be equivalent to 'three'.");

            isEquivalent = threeAgain.IsEquivalentTo(two);
            Assert.IsFalse(isEquivalent, "'threeAgain' should be not be equivalent to 'two'.");
        }

        [Test]
        public static void TestExtensions_EndianOptimization()
        {
            TypeMap.Register.Singleton<IRuntimeEnvironment>(() => new FakeRuntimeEnvironment(Endian.Reverse));
            OS.Current.IsLittleEndian.SetLittleEndian();
            if (BitConverter.IsLittleEndian)
            {
                byte[] actuallyLittleEndianBytes = 0x0102030405060708L.GetBigEndianBytes();
                Assert.That(actuallyLittleEndianBytes, Is.EqualTo(new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 }), "Getting big endian long, thinking we are big endian but in fact are not, will get us little endian bytes.");

                byte[] actuallyIntLittleEndianBytes = 0x01020304.GetBigEndianBytes();
                Assert.That(actuallyIntLittleEndianBytes, Is.EqualTo(new byte[] { 0x04, 0x03, 0x02, 0x01 }), "Getting big endian int, thinking we are big endian but in fact are not, will get us little endian bytes.");

                byte[] actuallyStillLittleEndianBytes = 0x0102030405060708L.GetLittleEndianBytes();
                Assert.That(actuallyStillLittleEndianBytes, Is.EqualTo(new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 }), "Getting little endian long, thinking we are big endian but in fact are not, will still get us little endian.");

                byte[] actuallyIntStillLittleEndianBytes = 0x01020304.GetLittleEndianBytes();
                Assert.That(actuallyIntStillLittleEndianBytes, Is.EqualTo(new byte[] { 0x04, 0x03, 0x02, 0x01 }), "Getting little endian int, thinking we are big endian but in fact are not, will still get us little endian.");
            }
            else
            {
                byte[] actuallyStillBigEndianBytes = 0x0102030405060708L.GetBigEndianBytes();
                Assert.That(actuallyStillBigEndianBytes, Is.EqualTo(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 }), "Getting big endian long, thinking we are little endian but in fact are not, will still get us big endian bytes.");

                byte[] actuallyIntStillBigEndianBytes = 0x01020304.GetBigEndianBytes();
                Assert.That(actuallyIntStillBigEndianBytes, Is.EqualTo(new byte[] { 0x01, 0x02, 0x03, 0x04 }), "Getting big endian int, thinking we are little endian but in fact are not, will still get us big endian bytes.");

                byte[] actuallyBigEndianBytes = 0x0102030405060708L.GetLittleEndianBytes();
                Assert.That(actuallyBigEndianBytes, Is.EqualTo(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 }), "Getting little endian long, thinking we are big endian but in fact are not, will get us big endian bytes.");

                byte[] actuallyIntBigEndianBytes = 0x01020304.GetLittleEndianBytes();
                Assert.That(actuallyIntBigEndianBytes, Is.EqualTo(new byte[] { 0x01, 0x02, 0x03, 0x04 }), "Getting little endian int, thinking we are big endian but in fact are not, will get us big endian bytes.");
            }
            TypeMap.Register.Singleton<IRuntimeEnvironment>(() => new FakeRuntimeEnvironment(Endian.Actual));
            OS.Current.IsLittleEndian.SetLittleEndian();
        }

        [Test]
        public static void TestExtensions_EndianConversion()
        {
            byte[] actuallyLittleEndianBytes = 0x0102030405060708L.GetBigEndianBytes();
            Assert.That(actuallyLittleEndianBytes, Is.EqualTo(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 }), "Getting big endian long.");

            byte[] actuallyStillLittleEndianBytes = 0x0102030405060708L.GetLittleEndianBytes();
            Assert.That(actuallyStillLittleEndianBytes, Is.EqualTo(new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 }), "Getting little endian long.");

            byte[] actuallyIntStillLittleEndianBytes = 0x01020304.GetLittleEndianBytes();
            Assert.That(actuallyIntStillLittleEndianBytes, Is.EqualTo(new byte[] { 0x04, 0x03, 0x02, 0x01 }), "Getting little endian int.");
        }

        [Test]
        public static void TestExtensions_CreateEncryptedName()
        {
            string rootPath = Path.GetPathRoot(Environment.CurrentDirectory);
            string fileName = rootPath.PathCombine("Users", "Axantum", "A Documents Folder", "My Document.docx");
            string encryptedFileName = New<IDataStore>(fileName).CreateEncryptedName().FullName;
            Assert.That(encryptedFileName, Is.EqualTo(rootPath.PathCombine("Users", "Axantum", "A Documents Folder", "My Document-docx.666")), "Standard conversion of file name to encrypted form.");

            Assert.Throws<InternalErrorException>(() =>
                 {
                     string encryptedEncryptedFileName = New<IDataStore>(encryptedFileName).CreateEncryptedName().FullName;

                     // Use the instance to avoid FxCop errors.
                     Object.Equals(encryptedEncryptedFileName, null);
                 });

            fileName = rootPath.PathCombine("Users", "Axantum", "A Documents Folder", "My Extensionless File");
            encryptedFileName = New<IDataStore>(fileName).CreateEncryptedName().FullName;
            Assert.That(encryptedFileName, Is.EqualTo(rootPath.PathCombine("Users", "Axantum", "A Documents Folder", "My Extensionless File.666")), "Conversion of file name without extension to encrypted form.");

            Assert.Throws<InternalErrorException>(() =>
            {
                string encryptedEncryptedFileName = New<IDataStore>(encryptedFileName).CreateEncryptedName().FullName;

                // Use the instance to avoid FxCop errors.
                Object.Equals(encryptedEncryptedFileName, null);
            });
        }

        [Test]
        public static void TestExtensions_TrimLogMessage()
        {
            string trimmed, untrimmed;

            untrimmed = "This text should not be trimmed.";
            trimmed = untrimmed.TrimLogMessage();
            Assert.That(trimmed, Is.EqualTo(untrimmed), "The text does not contain anything that should be trimmed.");

            untrimmed = "Information: Should not be trimmed.";
            trimmed = untrimmed.TrimLogMessage();
            Assert.That(trimmed, Is.EqualTo(untrimmed), "The text does not contain anything that should be trimmed.");

            untrimmed = "blah blah Warning - should be trimmed!";
            trimmed = untrimmed.TrimLogMessage();
            Assert.That(trimmed, Is.EqualTo("Warning - should be trimmed!"), "The text should be trimmed at the start.");

            untrimmed = " Warning - should be trimmed!";
            trimmed = untrimmed.TrimLogMessage();
            Assert.That(trimmed, Is.EqualTo("Warning - should be trimmed!"), "The text should be trimmed at the start, even just a single space.");

            untrimmed = "18:30 Debug - should be trimmed!";
            trimmed = untrimmed.TrimLogMessage();
            Assert.That(trimmed, Is.EqualTo("Debug - should be trimmed!"), "The text should be trimmed at the start.");

            untrimmed = "Information Error Error - should be trimmed!";
            trimmed = untrimmed.TrimLogMessage();
            Assert.That(trimmed, Is.EqualTo("Error Error - should be trimmed!"), "The text should be trimmed at the start, but only to the first occurrence.");

            untrimmed = "Prefix Fatal";
            trimmed = untrimmed.TrimLogMessage();
            Assert.That(trimmed, Is.EqualTo("Fatal"), "The text should be trimmed at the start.");
        }

        [Test]
        public static void TestExtensions_CopyToArgumentExceptions()
        {
            Stream nullStream = null;
            using (Stream stream = new MemoryStream())
            {
                Assert.Throws<ArgumentNullException>(() => { StreamExtensions.CopyTo(nullStream, stream, 100); });
                Assert.Throws<ArgumentNullException>(() => { StreamExtensions.CopyTo(stream, nullStream, 100); });
                Assert.Throws<ArgumentOutOfRangeException>(() => { StreamExtensions.CopyTo(stream, stream, 0); });
                Assert.Throws<ArgumentOutOfRangeException>(() => { StreamExtensions.CopyTo(stream, stream, -1); });
            }
        }

        [Test]
        public static void TestExtensions_CreateUniqueFileFirstIsOk()
        {
            using (FileLock lockedUnique = @"C:\temp\test.txt".CreateUniqueFile())
            {
                Assert.That(lockedUnique.DataStore.FullName, Is.EqualTo(@"C:\temp\test.txt".NormalizeFilePath()));
            }
        }

        [Test]
        public static void TestExtensions_CreateUniqueFileFirstIsNotOk()
        {
            IDataStore fileInfo = New<IDataStore>(@"C:\temp\test.txt");
            using (Stream stream = fileInfo.OpenWrite())
            {
            }
            using (FileLock lockedUnique = @"C:\temp\test.txt".CreateUniqueFile())
            {
                Assert.That(lockedUnique.DataStore.FullName, Is.EqualTo(@"C:\temp\test.1.txt".NormalizeFilePath()));
            }
        }

        [Test]
        public static void TestExtensions_CreateUniqueFileReallyCreates()
        {
            using (FileLock lockedUnique = @"C:\temp\test.txt".CreateUniqueFile())
            {
                Assert.That(lockedUnique.DataStore.FullName, Is.EqualTo(@"C:\temp\test.txt".NormalizeFilePath()));
            }

            using (FileLock lockedUnique = @"C:\temp\test.txt".CreateUniqueFile())
            {
                Assert.That(lockedUnique.DataStore.FullName, Is.EqualTo(@"C:\temp\test.1.txt".NormalizeFilePath()));
            }
        }

        [Test]
        public static void TestExtensions_CreateUniqueFileUnexpectedAxCryptException()
        {
            EventHandler handler = delegate (object sender, EventArgs e)
            {
                if (((FakeDataStore)sender).TestTag == "CreateNewFile")
                {
                    throw new InternalErrorException("An unexpected exception.", ErrorStatus.InternalError);
                }
            };
            FakeDataStore.ExceptionHook += handler;
            try
            {
                Assert.Throws<InternalErrorException>(() => @"C:\temp\test.txt".CreateUniqueFile());
            }
            finally
            {
                FakeDataStore.ExceptionHook -= handler;
            }
        }

        [Test]
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "TestIs")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Encryptable")]
        public static void TestExtensions_IsEncryptable()
        {
            New<FileFilter>().AddUnencryptable(new Regex(@"^C:\{0}Windows\{0}(?!Temp$)".InvariantFormat(Path.DirectorySeparatorChar)));

            Assert.That(New<FileFilter>().IsEncryptable(New<IDataStore>(@"C:\Temp\test.txt")), Is.True);
            Assert.That(New<FileFilter>().IsEncryptable(New<IDataStore>(@"C:\Windows\test.txt")), Is.False);
            Assert.That(New<FileFilter>().IsEncryptable(New<IDataStore>(@"C:\Temp\test-txt.666")), Is.False);

            IDataStore nullFileInfo = null;
            Assert.Throws<ArgumentNullException>(() => New<FileFilter>().IsEncryptable(nullFileInfo));
        }

        [Test]
        public static void TestExtensions_NormalizeFolder()
        {
            string expected = @"C:\Documents\".Replace('\\', Path.DirectorySeparatorChar);
            Assert.That(New<IDataContainer>(@"C:\Documents\").FullName, Is.EqualTo(expected));
            Assert.That(New<IDataContainer>(@"C:/Documents\").FullName, Is.EqualTo(expected));
            Assert.That(New<IDataContainer>(@"C:\Documents").FullName, Is.EqualTo(expected));
            Assert.That(New<IDataContainer>(@"C:\Documents\\//").FullName, Is.EqualTo(expected));
        }

        [Test]
        public static void TestExtensions_FolderFromEnvironment()
        {
            string nullString = null;
            Assert.Throws<ArgumentNullException>(() => nullString.FolderFromEnvironment());

            string expected = @"C:\Windows\".Replace('\\', Path.DirectorySeparatorChar);

            SetupAssembly.FakeRuntimeEnvironment.EnvironmentVariables.Add("VARIABLE", expected);

            Assert.That("VARIABLE".FolderFromEnvironment().FullName, Is.EqualTo(expected));
            Assert.That("UNKNOWN".FolderFromEnvironment(), Is.Null);
        }

        [Test]
        public static void TestExtensions_FallbackExtension()
        {
            Assert.That(default(int).Fallback<int>(2), Is.EqualTo(2));
            Assert.That(3.Fallback<int>(3), Is.EqualTo(3));
        }

        [Test]
        public static void TestExtensions_FileInfoTypeExtension()
        {
            FakeDataStore.AddFile(@"c:\test.txt", null);
            IDataStore fileInfo = New<IDataStore>(@"c:\test.txt");
            Assert.That(fileInfo.Type(), Is.EqualTo(FileInfoTypes.EncryptableFile));

            FakeDataStore.AddFile(@"c:\test-txt.666", null);
            fileInfo = New<IDataStore>(@"c:\test-txt.666");
            Assert.That(fileInfo.Type(), Is.EqualTo(FileInfoTypes.EncryptedFile));

            FakeDataStore.AddFolder(@"c:\test\");
            IDataContainer folderInfo = New<IDataContainer>(@"c:\test\");
            Assert.That(folderInfo.Type(), Is.EqualTo(FileInfoTypes.Folder));

            fileInfo = New<IDataStore>(@"c:\not-there.txt");
            Assert.That(fileInfo.Type(), Is.EqualTo(FileInfoTypes.NonExisting));

            New<FileFilter>().AddUnencryptable(new Regex(@"^C:\{0}Windows\{0}".InvariantFormat(Path.DirectorySeparatorChar)));
            FakeDataStore.AddFile(@"C:\Windows\System.drv", null);
            fileInfo = New<IDataStore>(@"C:\Windows\System.drv");
            Assert.That(fileInfo.Type(), Is.EqualTo(FileInfoTypes.OtherFile));
        }

        [Test]
        public static void TestExtensions_ReduceByteArrayTooShort()
        {
            byte[] big = new byte[5];
            byte[] reduced = null;

            Assert.Throws<ArgumentException>(() => reduced = big.Reduce(6));
            Assert.That(reduced, Is.Null);
        }

        [Test]
        public static void TestExtensions_FromHexBadArguments()
        {
            string nullString = null;
            Assert.Throws<ArgumentNullException>(() => nullString.FromHex());

            Assert.Throws<ArgumentException>(() => "01A".FromHex());
        }

        [Test]
        public static void TestExtensions_CopyToWithBufferSize()
        {
            using (Stream input = new MemoryStream())
            {
                input.Write(new byte[] { 1, 2, 3, 4 }, 0, 4);
                input.Position = 0;
                using (MemoryStream output = new MemoryStream())
                {
                    StreamExtensions.CopyTo(input, output, 2);

                    byte[] copy = output.ToArray();
                    Assert.That(copy.IsEquivalentTo(new byte[] { 1, 2, 3, 4 }));
                }
            }
        }

        [TestCase(CryptoImplementation.Mono)]
        [TestCase(CryptoImplementation.WindowsDesktop)]
        [TestCase(CryptoImplementation.BouncyCastle)]
        public static void TestExtensions_DecryptToBadArgumentsCausingEarlyException(CryptoImplementation cryptoImplementation)
        {
            SetupAssembly.AssemblySetupCrypto(cryptoImplementation);

            Stream nullStream = null;
            ICryptoTransform nullEncryptor = null;
            ICryptoTransform encryptor = new V2AesCrypto(SymmetricKey.Zero256, SymmetricIV.Zero128, 0).DecryptingTransform();

            Assert.Throws<ArgumentNullException>(() => nullStream.DecryptTo(Stream.Null, encryptor, true));
            Assert.Throws<ArgumentNullException>(() => Stream.Null.DecryptTo(nullStream, encryptor, true));
            Assert.Throws<ArgumentNullException>(() => Stream.Null.DecryptTo(Stream.Null, nullEncryptor, true));
        }

        [TestCase(CryptoImplementation.Mono)]
        [TestCase(CryptoImplementation.WindowsDesktop)]
        [TestCase(CryptoImplementation.BouncyCastle)]
        public static void TestExtensions_UserAsymmetricKeysToAccountKeyAndBack(CryptoImplementation cryptoImplementation)
        {
            SetupAssembly.AssemblySetupCrypto(cryptoImplementation);

            UserKeyPair originalKeys = new UserKeyPair(EmailAddress.Parse("svante@axcrypt.net"), 512);
            AccountKey accountKey = originalKeys.ToAccountKey(new Passphrase("password"));
            UserKeyPair roundtripKeys = accountKey.ToUserKeyPair(new Passphrase("password"));

            Assert.That(originalKeys, Is.EqualTo(roundtripKeys));
        }

        [TestCase(CryptoImplementation.Mono)]
        [TestCase(CryptoImplementation.WindowsDesktop)]
        [TestCase(CryptoImplementation.BouncyCastle)]
        public static void TestExtensions_UserAsymmetricKeysToAccountKeyAndBackUsingDataProtection(CryptoImplementation cryptoImplementation)
        {
            SetupAssembly.AssemblySetupCrypto(cryptoImplementation);

            UserKeyPair originalKeys = new UserKeyPair(EmailAddress.Parse("svante@axcrypt.net"), 512);
            AccountKey accountKey = originalKeys.ToAccountKey(Passphrase.Empty);
            UserKeyPair roundtripKeys = accountKey.ToUserKeyPair(Passphrase.Empty);

            Assert.That(accountKey.KeyPair.PrivateEncryptedPem.Length, Is.GreaterThan(0));
            Assert.That(originalKeys, Is.EqualTo(roundtripKeys));
        }

        [TestCase(CryptoImplementation.Mono)]
        [TestCase(CryptoImplementation.WindowsDesktop)]
        [TestCase(CryptoImplementation.BouncyCastle)]
        public static void TestExtensions_AccountKeyToUserAsymmetricKeysWithWrongPassphrase(CryptoImplementation cryptoImplementation)
        {
            SetupAssembly.AssemblySetupCrypto(cryptoImplementation);

            UserKeyPair originalKeys = new UserKeyPair(EmailAddress.Parse("svante@axcrypt.net"), 512);
            AccountKey accountKey = originalKeys.ToAccountKey(new Passphrase("password"));
            UserKeyPair roundtripKeys = accountKey.ToUserKeyPair(new Passphrase("wrong password"));

            Assert.That(roundtripKeys, Is.Null);
        }

        [TestCase(CryptoImplementation.Mono)]
        [TestCase(CryptoImplementation.WindowsDesktop)]
        [TestCase(CryptoImplementation.BouncyCastle)]
        public static void TestExtensions_AccountKeyToUserAsymmericKeysWithOnlyPublicKey(CryptoImplementation cryptoImplementation)
        {
            SetupAssembly.AssemblySetupCrypto(cryptoImplementation);

            UserKeyPair originalKeys = new UserKeyPair(EmailAddress.Parse("svante@axcrypt.net"), 512);
            IAsymmetricKeyPair partialKeyPair = Resolve.AsymmetricFactory.CreateKeyPair(originalKeys.KeyPair.PublicKey.ToString(), String.Empty);
            UserKeyPair originalPartialKeys = new UserKeyPair(originalKeys.UserEmail, originalKeys.Timestamp, partialKeyPair);

            AccountKey accountKey = originalPartialKeys.ToAccountKey(Passphrase.Empty);
            UserKeyPair roundtripKeys = accountKey.ToUserKeyPair(Passphrase.Empty);

            Assert.That(roundtripKeys, Is.EqualTo(originalPartialKeys));
        }

        [Test]
        public static void TestExtensions_UrlUtilityIsValidTopLevelDomain()
        {
            string value = null;
            Assert.Throws<ArgumentNullException>(() => value.IsValidTopLevelDomain());

            Assert.That(string.Empty.IsValidTopLevelDomain(), Is.False, nameof(string.Empty));
            Assert.That(".se".IsValidTopLevelDomain(), Is.False, ".se");
            Assert.That("se".IsValidTopLevelDomain(), Is.True, "se");
            Assert.That(".xyzx".IsValidTopLevelDomain(), Is.False, ".xyzx");
            Assert.That("xyzz".IsValidTopLevelDomain(), Is.False, "xyzz");
            Assert.That("xyz".IsValidTopLevelDomain(), Is.True, "xyz");
            Assert.That("local".IsValidTopLevelDomain(), Is.True, "local");
        }

        [Test]
        public static void TestExtensions_BetweenDate()
        {
            DateTime utcNow = New<INow>().Utc;
            ((FakeNow)New<INow>()).TimeFunction = () => utcNow;

            DateTime utcFuture = utcNow.AddDays(10);
            DateTime utc10DaysPast = utcNow.AddDays(-10);
            DateTime utc29DaysPast = utcNow.AddDays(-29);
            DateTime utcAlmost30DaysPast = utcNow.AddDays(-30).AddSeconds(1);
            DateTime utc30DaysPast = utcNow.AddDays(-30);
            DateTime utcJustMoreThan30DaysPast = utcNow.AddDays(-30).AddSeconds(-1);
            DateTime utc45daysPast = utcNow.AddDays(-45);
            DateTime utc59DaysPast = utcNow.AddDays(-59);
            DateTime utcAlmost60daysPast = utcNow.AddDays(-60).AddSeconds(1);
            DateTime utc60daysPast = utcNow.AddDays(-60);
            DateTime utcJustMoreThan60daysPast = utcNow.AddDays(-60).AddSeconds(-1);
            DateTime utc100DaysPast = utcNow.AddDays(-100);

            TimeSpan days30 = TimeSpan.FromDays(30);
            TimeSpan days60 = TimeSpan.FromDays(60);

            Assert.That(!utcFuture.IsBetween(days30, days60), nameof(utcFuture));
            Assert.That(!utc10DaysPast.IsBetween(days30, days60), nameof(utc10DaysPast));
            Assert.That(!utc29DaysPast.IsBetween(days30, days60), nameof(utc29DaysPast));
            Assert.That(!utcAlmost30DaysPast.IsBetween(days30, days60), nameof(utcAlmost30DaysPast));
            Assert.That(!utc30DaysPast.IsBetween(days30, days60), nameof(utc30DaysPast));
            Assert.That(utcJustMoreThan30DaysPast.IsBetween(days30, days60), nameof(utcJustMoreThan30DaysPast));
            Assert.That(utc45daysPast.IsBetween(days30, days60), nameof(utc45daysPast));
            Assert.That(utc59DaysPast.IsBetween(days30, days60), nameof(utc59DaysPast));
            Assert.That(utcAlmost60daysPast.IsBetween(days30, days60), nameof(utcAlmost60daysPast));
            Assert.That(!utc60daysPast.IsBetween(days30, days60), nameof(utc60daysPast));
            Assert.That(!utcJustMoreThan60daysPast.IsBetween(days30, days60), nameof(utcJustMoreThan60daysPast));
            Assert.That(!utc100DaysPast.IsBetween(days30, days60), nameof(utc100DaysPast));
        }

        [Test]
        public static void TestExtensions_FileFilter()
        {
            FakeDataStore.AddFile(@"c:\test.txt", null);
            IDataStore fileInfo = New<IDataStore>(@"c:\test.txt");
            Assert.That(New<FileFilter>().IsEncryptable(fileInfo), Is.True);

            New<FileFilter>().AddUnencryptableExtension("txt");

            Assert.That(New<FileFilter>().IsEncryptable(fileInfo), Is.False);
        }

        [Test]
        public static void TestExtensions_WildcardMatch()
        {
            Assert.That("*deux*".WildcardMatch("-@-.de"), Is.False, "'*deux*' does not match '-@-.de'");
            Assert.That("*deux*".WildcardMatch("-@-.deux"), Is.True, "'*deux*' does match '-@-.deux'");
            Assert.That("*deux*".WildcardMatch("-@-.deuxxxx"), Is.True, "'*deux*' does match '-@-.deuxxxx'");
            Assert.That("*deux".WildcardMatch("-@-.deux"), Is.True, "'*deux' does match '-@-.deux'");
            Assert.That("test?".WildcardMatch("testx"), Is.True, "'test?' does match 'testx'");
            Assert.That("test?".WildcardMatch("test"), Is.False, "'test?' does not match 'test'");
            Assert.That("*test?".WildcardMatch("something-testx"), Is.True, "'*test?' does match 'something-testx'");
            Assert.That("*test?".WildcardMatch("something-test"), Is.False, "'*test?' does not match 'something-test'");
            Assert.That("my*te?t".WildcardMatch("my-something-test"), Is.True, "'my*te?t' does match 'my-something-test'");
            Assert.That("".WildcardMatch(""), Is.True, "'' does match ''");
            Assert.That("".WildcardMatch("x"), Is.False, "'' does not match 'x'");
            Assert.That("x".WildcardMatch(""), Is.False, "'x' does not match ''");
            Assert.That("*".WildcardMatch(""), Is.True, "'*' does match ''");
            Assert.That("?".WildcardMatch(""), Is.False, "'?' does not match ''");
        }

        [Test]
        public static void TestExtensions_UpdateList()
        {
            List<object> existing;
            List<object> updated;

            existing = new List<object>(new object[0]);
            updated = new List<object>(new object[] { "A", });

            existing.UpdateListTo(updated);
            Assert.That(existing.Count, Is.EqualTo(1), "Update empty with single element");
            Assert.That(existing[0], Is.EqualTo("A"), "Update empty with single element");

            existing = new List<object>(new object[] { "A", });
            updated = new List<object>(new object[] { "A", });
            existing.UpdateListTo(updated);
            Assert.That(existing.Count, Is.EqualTo(1), "Update single with same single element");
            Assert.That(existing[0], Is.EqualTo("A"), "Update single with same single element");

            updated = new List<object>(new object[] { "A", });
            updated = new List<object>(new object[0]);
            existing.UpdateListTo(updated);
            Assert.That(existing.Count, Is.EqualTo(0), "Update single element with empty");

            updated = new List<object>(new object[] { "A", });
            updated = new List<object>(new object[] { "X" });
            existing.UpdateListTo(updated);
            Assert.That(existing.Count, Is.EqualTo(1), "Update single element with different single element");
            Assert.That(existing[0], Is.EqualTo("X"), "Update single element with different single element");

            existing = new List<object>(new object[0]);
            updated = new List<object>(new object[0]);
            existing.UpdateListTo(updated);
            Assert.That(existing.Count, Is.EqualTo(0), "Update empty with empty");

            existing = new List<object>(new object[] { "A", "B", "C", });
            updated = new List<object>(new object[] { "A", "B", "C", });
            existing.UpdateListTo(updated);
            Assert.That(existing.Count, Is.EqualTo(3), "Update multiple with same multiple");
            Assert.That(existing[0], Is.EqualTo("A"), "Update multiple with same multiple");
            Assert.That(existing[1], Is.EqualTo("B"), "Update multiple with same multiple");
            Assert.That(existing[2], Is.EqualTo("C"), "Update multiple with same multiple");

            existing = new List<object>(new object[] { "A", "B", "C", });
            updated = new List<object>(new object[] { "X", "A", "B", "C", });
            existing.UpdateListTo(updated);
            Assert.That(existing.Count, Is.EqualTo(4), "Update multiple with additional first");
            Assert.That(existing[0], Is.EqualTo("X"), "Update multiple with additional first");
            Assert.That(existing[1], Is.EqualTo("A"), "Update multiple with additional first");
            Assert.That(existing[2], Is.EqualTo("B"), "Update multiple with additional first");
            Assert.That(existing[3], Is.EqualTo("C"), "Update multiple with additional first");

            existing = new List<object>(new object[] { "A", "B", "C", });
            updated = new List<object>(new object[] { "A", "X", "B", "C", });
            existing.UpdateListTo(updated);
            Assert.That(existing.Count, Is.EqualTo(4), "Update multiple with additional between first and second");
            Assert.That(existing[0], Is.EqualTo("A"), "Update multiple with additional between first and second");
            Assert.That(existing[1], Is.EqualTo("X"), "Update multiple with additional between first and second");
            Assert.That(existing[2], Is.EqualTo("B"), "Update multiple with additional between first and second");
            Assert.That(existing[3], Is.EqualTo("C"), "Update multiple with additional between first and second");

            existing = new List<object>(new object[] { "A", "B", "C", });
            updated = new List<object>(new object[] { "A", "B", "C", "X", });
            existing.UpdateListTo(updated);
            Assert.That(existing.Count, Is.EqualTo(4), "Update multiple with additional appended");
            Assert.That(existing[0], Is.EqualTo("A"), "Update multiple with additional appended");
            Assert.That(existing[1], Is.EqualTo("B"), "Update multiple with additional appended");
            Assert.That(existing[2], Is.EqualTo("C"), "Update multiple with additional appended");
            Assert.That(existing[3], Is.EqualTo("X"), "Update multiple with additional appended");

            existing = new List<object>(new object[] { "A", "B", "C", });
            updated = new List<object>(new object[] { "X", "A", "Y", "B", "Z", "C", "+", });
            existing.UpdateListTo(updated);
            Assert.That(existing.Count, Is.EqualTo(7), "Update multiple with additional first, in between, and appended");
            Assert.That(existing[0], Is.EqualTo("X"), "Update multiple with additional first, in between, and appended");
            Assert.That(existing[1], Is.EqualTo("A"), "Update multiple with additional first, in between, and appended");
            Assert.That(existing[2], Is.EqualTo("Y"), "Update multiple with additional first, in between, and appended");
            Assert.That(existing[3], Is.EqualTo("B"), "Update multiple with additional first, in between, and appended");
            Assert.That(existing[4], Is.EqualTo("Z"), "Update multiple with additional first, in between, and appended");
            Assert.That(existing[5], Is.EqualTo("C"), "Update multiple with additional first, in between, and appended");
            Assert.That(existing[6], Is.EqualTo("+"), "Update multiple with additional first, in between, and appended");

            existing = new List<object>(new object[] { "A", "B", "C", });
            updated = new List<object>(new object[] { "X", "Y", "Z", });
            existing.UpdateListTo(updated);
            Assert.That(existing.Count, Is.EqualTo(3), "Update with all new elements");
            Assert.That(existing[0], Is.EqualTo("X"), "Update with all new elements");
            Assert.That(existing[1], Is.EqualTo("Y"), "Update with all new elements");
            Assert.That(existing[2], Is.EqualTo("Z"), "Update with all new elements");

            existing = new List<object>(new object[] { "A", "B", "C", "D", "E", "F", });
            updated = new List<object>(new object[] { "F", "E", "D", "C", "B", "A", });
            existing.UpdateListTo(updated);
            Assert.That(existing.Count, Is.EqualTo(6), "Update with reversed elements");
            Assert.That(existing[0], Is.EqualTo("F"), "Update with reversed elements");
            Assert.That(existing[1], Is.EqualTo("E"), "Update with reversed elements");
            Assert.That(existing[2], Is.EqualTo("D"), "Update with reversed elements");
            Assert.That(existing[3], Is.EqualTo("C"), "Update with reversed elements");
            Assert.That(existing[4], Is.EqualTo("B"), "Update with reversed elements");
            Assert.That(existing[5], Is.EqualTo("A"), "Update with reversed elements");
        }
    }
}