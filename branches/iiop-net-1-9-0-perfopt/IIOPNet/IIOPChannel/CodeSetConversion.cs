/* CodeSetConversion.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 28.01.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
 * 
 * Copyright 2003 Dominic Ullmann
 *
 * Copyright 2003 ELCA Informatique SA
 * Av. de la Harpe 22-24, 1000 Lausanne 13, Switzerland
 * www.elca.ch
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System;
using System.Collections;
using System.Text;
using Ch.Elca.Iiop.Services;
using omg.org.CORBA;

namespace Ch.Elca.Iiop.CodeSet {
    
    /// <summary>
    /// stores the mapping between codesets and encodings for realising the conversion to/from .NET chars
    /// </summary>
    internal class CodeSetConversionRegistry {

        #region IFields
        
        /// <summary>stores the encodings</summary>
        private Hashtable m_codeSetsEndianIndep = new Hashtable();
        private Hashtable m_codeSetsBigEndian = new Hashtable();
        private Hashtable m_codeSetsLittleEndian = new Hashtable();
        
        #endregion IFields
        #region IConstructors
        
        internal CodeSetConversionRegistry() { 
        }

        #endregion IConstructors
        #region IMethods


        /// <summary>
        /// adds an encoding for both endians (endian independant)
        /// </summary>
        internal void AddEncodingAllEndian(int id, System.Text.Encoding encoding) {
            m_codeSetsEndianIndep.Add(id,encoding);
            AddEncodingBigEndian(id, encoding);
            AddEncodingLittleEndian(id, encoding);
        }

        /// <summary>
        /// adds an encoding for only big endian
        /// </summary>            
        internal void AddEncodingBigEndian(int id, System.Text.Encoding encoding) {
            m_codeSetsBigEndian.Add(id, encoding);
        }            

        /// <summary>
        /// adds an encoding for little endian
        /// </summary>                        
        internal void AddEncodingLittleEndian(int id, System.Text.Encoding encoding) {
            m_codeSetsLittleEndian.Add(id, encoding);
        }            

        /// <summary>
        /// gets an endian independant encoding
        /// </summary>                                    
        internal System.Text.Encoding GetEncodingEndianIndependant(int id) {
            return (System.Text.Encoding)m_codeSetsEndianIndep[id];
        }

        /// <summary>
        /// gets an encoding usable with big endian
        /// </summary>                                    
        internal System.Text.Encoding GetEncodingBigEndian(int id) {
            return (System.Text.Encoding)m_codeSetsBigEndian[id];
        }
            
        /// <summary>
        /// gets an encoding usable with little endian
        /// </summary>                                    
        internal System.Text.Encoding GetEncodingLittleEndian(int id) {
            return (System.Text.Encoding)m_codeSetsLittleEndian[id];
        }

        #endregion IMethods
    }
                


    public class Latin1Encoding : Encoding {
        
        #region IMethods
        
        public override int GetByteCount(char[] chars, int index, int count) {
            // one char results in one byte
            return count;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount,
                                     byte[] bytes, int byteIndex) {
            if ((bytes.Length - byteIndex) < charCount) { 
                // bytes array is too small
                throw new INTERNAL(9965, CompletionStatus.Completed_MayBe);
            }
            
            // mapping for latin-1: latin-1 value = unicode-value, for unicode values 0 - 0xFF, other values: exception, non latin-1
            for (int i = charIndex; i < charIndex + charCount; i++) {
                byte lowbits = (byte)(chars[i] & 0x00FF);
                byte highbits = (byte) ((chars[i] & 0xFF00) >> 8);
                if (highbits != 0) { 
                    // character : chars[i]
                    // can't be encoded, because it's a non-latin1 character
                    throw new BAD_PARAM(1919, CompletionStatus.Completed_MayBe);
                }
                bytes[byteIndex + (i - charIndex)] = lowbits;
            }
            return charCount;
        }

        public override int GetCharCount(byte[] bytes, int index, int count) {
            // one byte results in one char
            return count;
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) {
            if ((chars.Length - charIndex) < byteCount) { 
                // chars array is too small
                throw new INTERNAL(9965, CompletionStatus.Completed_MayBe);
            }
            // mapping for latin-1: unicode-value = latin-1 value
            for (int i = byteIndex; i < byteIndex + byteCount; i++) {
                chars[charIndex + (i  - byteIndex)] = (char) bytes[i];
            }
            return byteCount;
        }

        public override int GetMaxByteCount(int charCount) {
            // one char results in one byte
            return charCount;
        }

        public override int GetMaxCharCount(int byteCount) {
            // one byte results in one char
            return byteCount;
        }

        #endregion IMethods

    }


    /// <summary>
    /// This class is an extended version of the unicode-encoder:
    /// it encodes a byte-order-mark for little endian, removes a byte order mark on decoding
    /// </summary>
    /// <remarks>This class implements the
    /// rules in CORBA 2.6 Chapter 15.3.1.6 releated to UTF 16</remarks>
    public class UnicodeEncodingExt : Encoding {

        #region SFields
        
        private static UnicodeEncoding s_unicodeEncodingBe =
            new UnicodeEncoding(true, false);
        
        private static UnicodeEncoding s_unicodeEncodingLe =
            new UnicodeEncoding(false, false);
        
        #endregion SFields
        #region IFields

        private bool m_encodeAsBigEndian = true;        
        private bool m_encodeBom = false;
        /// <summary>
        /// the encoding instance used to convert char[] to byte[], for the reverse,
        /// the decision is based on bom in byte[].
        /// </summary>
        private UnicodeEncoding m_encoderToUse;

        #endregion IFields
        #region IConstructors
        
        public UnicodeEncodingExt(bool encodeAsBigEndian) {
            m_encodeAsBigEndian = encodeAsBigEndian;            
            m_encodeBom = !encodeAsBigEndian; // for little endian, a bom is required, because default is big endian.
            if (m_encodeAsBigEndian) {
                m_encoderToUse = s_unicodeEncodingBe;
            } else {
                m_encoderToUse = s_unicodeEncodingLe;
            }
        }

        #endregion IConsturctors
        #region IMethods

        public override int GetByteCount(char[] chars, int index, int count) {
            int result = m_encoderToUse.GetByteCount(chars, index, count);
            if (m_encodeBom) {
                result += 2;
            }
            return result;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount,
                                     byte[] bytes, int byteIndex) {
            if (m_encodeBom) {
                if (m_encodeAsBigEndian) {
                    bytes[byteIndex] = 254;
                    bytes[byteIndex+1] = 255;
                } else {
                    bytes[byteIndex] = 255;
                    bytes[byteIndex+1] = 254;
                }
                int result = m_encoderToUse.GetBytes(chars, charIndex, charCount,
                                                     bytes, byteIndex+2);
                return result + 2;
            } else {
                return m_encoderToUse.GetBytes(chars, charIndex, charCount,
                                               bytes, byteIndex);
            }
        }

        public override int GetCharCount(byte[] bytes, int index, int count) {
            // no endian mark possible if array too small, default is big endian
            if (bytes.Length <= 1) { 
                return s_unicodeEncodingBe.GetCharCount(bytes, index, count); 
            }
            // check for endian mark, select correct encoding.                           
            if (bytes[index] == 254 && (bytes[index+1] == 255)) {
                return s_unicodeEncodingBe.GetCharCount(bytes, index+2, count-2);
            } else if (bytes[index] == 255 && (bytes[index+1] == 254)) {
                return s_unicodeEncodingLe.GetCharCount(bytes, index+2, count-2);
            } else {
                return s_unicodeEncodingBe.GetCharCount(bytes, index, count); // no endian mark present
            }
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                     char[] chars, int charIndex) {
            // no big/little endian tag in byte array possible if array too small
            if (bytes.Length <= 1) { 
                return s_unicodeEncodingBe.GetChars(bytes, byteIndex, byteCount, chars, charIndex); 
            }
            // check for endian mark, select correct encoding.                           
            if (bytes[byteIndex] == 254 && (bytes[byteIndex+1] == 255)) {
                return s_unicodeEncodingBe.GetChars(bytes, byteIndex+2, byteCount-2,
                                                    chars, charIndex);
            } else if (bytes[byteIndex] == 255 && (bytes[byteIndex+1] == 254)) {
                return s_unicodeEncodingLe.GetChars(bytes, byteIndex+2, byteCount-2,
                                                    chars, charIndex);
            } else {
                // no endian mark present
                return s_unicodeEncodingBe.GetChars(bytes, byteIndex, byteCount,
                                                    chars, charIndex);
            }            
        }
        
        public override int GetMaxByteCount(int charCount) {
            // one char results in two byte; if a bom is encoded, add 2.
            int result = m_encoderToUse.GetMaxByteCount(charCount);            
            if (m_encodeBom) {
                result += 2;
            }
            return result;
        }

        public override int GetMaxCharCount(int byteCount) {
            // two bytes results in one char; if a bom is encoded, this method returns too much,
            // but only a maximum is requested -> therefore ok.
            return m_encoderToUse.GetMaxCharCount(byteCount);
        }        

        #endregion IMethods

    }

}

#if UnitTest

namespace Ch.Elca.Iiop.Tests {
    
    using System.IO;
    using NUnit.Framework;
    using Ch.Elca.Iiop;
    using Ch.Elca.Iiop.Cdr;
    
    /// <summary>
    /// test the encoding/decoding of UTF 16 strings
    /// </summary>
    [TestFixture]
    public class TestUtf16StringsGiop1_2 {
        
        private CdrInputStreamImpl CreateInputStream(byte[] content, bool isLittleEndian) {
            MemoryStream stream = new MemoryStream(content);
            CdrInputStreamImpl cdrStream = new CdrInputStreamImpl(stream);
            byte endianFlag = 0;
            if (isLittleEndian) {
                endianFlag = 1;
            }
            cdrStream.ConfigStream(endianFlag, new GiopVersion(1, 2));
            cdrStream.SetMaxLength((uint)content.Length);
            return cdrStream;
        }
                        
        /// <summary>
        /// check, that an utf 16 encoded string can be read correctly from a 
        /// big endian stream, if no bom is placed. If no bom is placed, big endian
        /// is assumed. See CORBA 2.6, chapter 15.3.1
        /// </summary>
        [Test]
        public void TestDecodeNoBomBeStream() {
            byte[] encoded = new byte[] { 0, 0, 0, 8, 0, 84, 0, 101, 0, 115, 0, 116 }; // Test
            CdrInputStream cdrStream = CreateInputStream(encoded, false);
            Assertion.AssertEquals("wrongly decoded", "Test", cdrStream.ReadWString());            
        }
        
        /// <summary>
        /// check, that an utf 16 encoded string can be read correctly from a 
        /// little endian stream, if no bom is placed. If no bom is placed, big endian
        /// is assumed. See CORBA 2.6, chapter 15.3.1
        /// </summary>
        [Test]
        public void TestDecodeNoBomLeStream() {
            byte[] encoded = new byte[] { 8, 0, 0, 0, 0, 84, 0, 101, 0, 115, 0, 116 }; // Test
            CdrInputStream cdrStream = CreateInputStream(encoded, true);
            Assertion.AssertEquals("wrongly decoded", "Test", cdrStream.ReadWString());            
        }
        
        /// <summary>
        /// check, that an utf 16 encoded string can be read correctly
        /// from a big endian stream, if a little endian bom is placed.
        /// See CORBA 2.6, chapter 15.3.1
        /// </summary>
        [Test]
        public void TestDecodeLeBomBeStream() {
            byte[] encoded = new byte[] { 0, 0, 0, 10, 0xFF, 0xFE, 84, 0, 101, 0, 115, 0, 116, 0 }; // Test
            CdrInputStream cdrStream = CreateInputStream(encoded, false);
            Assertion.AssertEquals("wrongly decoded", "Test", cdrStream.ReadWString());
        }
        
        /// <summary>
        /// check, that an utf 16 encoded string can be read correctly
        /// from a little endian stream, if a little endian bom is placed.
        /// See CORBA 2.6, chapter 15.3.1
        /// </summary>
        [Test]
        public void TestDecodeLeBomLeStream() {
            byte[] encoded = new byte[] { 10, 0, 0, 0, 0xFF, 0xFE, 84, 0, 101, 0, 115, 0, 116, 0 }; // Test
            CdrInputStream cdrStream = CreateInputStream(encoded, true);
            Assertion.AssertEquals("wrongly decoded", "Test", cdrStream.ReadWString());            
        }
        
        /// <summary>
        /// check, that an utf 16 encoded string can be read correctly
        /// from a big endian stream, if a big endian bom is placed.
        /// See CORBA 2.6, chapter 15.3.1
        /// </summary>
        [Test]
        public void TestDecodeBeBomBeStream() {
            byte[] encoded = new byte[] { 0, 0, 0, 10, 0xFE, 0xFF, 0, 84, 0, 101, 0, 115, 0, 116 }; // Test
            CdrInputStream cdrStream = CreateInputStream(encoded, false);
            Assertion.AssertEquals("wrongly decoded", "Test", cdrStream.ReadWString());            
        }
        
        /// <summary>
        /// check, that an utf 16 encoded string can be read correctly
        /// from a little endian stream, if a big endian bom is placed.
        /// See CORBA 2.6, chapter 15.3.1
        /// </summary>
        [Test]
        public void TestDecodeBeBomLeStream() {
            byte[] encoded = new byte[] { 10, 0, 0, 0, 0xFE, 0xFF, 0, 84, 0, 101, 0, 115, 0, 116 }; // Test
            CdrInputStream cdrStream = CreateInputStream(encoded, true);
            Assertion.AssertEquals("wrongly decoded", "Test", cdrStream.ReadWString());            
        }
        
        /// <summary>
        /// check, that a wstring is encoded as big-endian with big endian bom for a big endian stream.
        /// </summary>
        [Test]
        public void TestEncodeBeStream() {
            MemoryStream outStream = new MemoryStream();
            CdrOutputStreamImpl cdrStream = new CdrOutputStreamImpl(outStream, 0, new GiopVersion(1, 2));
            cdrStream.WriteWString("Test");
            AssertByteArrayEquals(new byte[] { 0, 0, 0, 8, 0, 84, 0, 101, 0, 115, 0, 116 },
                                  outStream.ToArray());

        }
        
        /// <summary>
        /// check, that a wstring is encoded as little-endian with little endian bom for a little endian stream.
        /// </summary>        
        [Test]
        public void TestEncodeLeStream() {
            MemoryStream outStream = new MemoryStream();
            CdrOutputStreamImpl cdrStream = new CdrOutputStreamImpl(outStream, 1, new GiopVersion(1, 2));
            cdrStream.WriteWString("Test");
            AssertByteArrayEquals(new byte[] { 10, 0, 0, 0, 0xFF, 0xFE, 84, 0, 101, 0, 115, 0, 116, 0 },
                                  outStream.ToArray());            
        }   
        
        private void AssertByteArrayEquals(byte[] arg1, byte[] arg2) {
            Assertion.AssertEquals("Array length", arg1.Length, arg2.Length);            
            for (int i = 0; i < arg1.Length; i++) {
                Assertion.AssertEquals("array element number: " + i, arg1[i], arg2[i]);
            }            
        }
       
        
    }
    
    /// <summary>
    /// test the encoding/decoding of UTF 16 strings
    /// </summary>
    [TestFixture]    
    public class TestUtf16StringsGiop1_1 {
        
        private CdrInputStreamImpl CreateInputStream(byte[] content, bool isLittleEndian) {
            MemoryStream stream = new MemoryStream(content);
            CdrInputStreamImpl cdrStream = new CdrInputStreamImpl(stream);
            byte endianFlag = 0;
            if (isLittleEndian) {
                endianFlag = 1;
            }
            cdrStream.ConfigStream(endianFlag, new GiopVersion(1, 1));
            cdrStream.SetMaxLength((uint)content.Length);
            return cdrStream;
        }
                        
        /// <summary>
        /// check, that an utf 16 encoded string can be read correctly from a 
        /// big endian stream, if no bom is placed. If no bom is placed, big endian
        /// is assumed. See CORBA 2.6, chapter 15.3.1
        /// </summary>
        [Test]
        public void TestDecodeNoBomBeStream() {
            byte[] encoded = new byte[] { 0, 0, 0, 5, 0, 84, 0, 101, 0, 115, 0, 116, 0, 0 }; // Test
            CdrInputStream cdrStream = CreateInputStream(encoded, false);
            Assertion.AssertEquals("wrongly decoded", "Test", cdrStream.ReadWString());            
        }
        
        /// <summary>
        /// check, that an utf 16 encoded string can be read correctly from a 
        /// little endian stream, if no bom is placed. If no bom is placed, big endian
        /// is assumed. See CORBA 2.6, chapter 15.3.1
        /// </summary>
        [Test]
        public void TestDecodeNoBomLeStream() {
            byte[] encoded = new byte[] { 5, 0, 0, 0, 0, 84, 0, 101, 0, 115, 0, 116, 0, 0 }; // Test
            CdrInputStream cdrStream = CreateInputStream(encoded, true);
            Assertion.AssertEquals("wrongly decoded", "Test", cdrStream.ReadWString());            
        }
        
        /// <summary>
        /// check, that an utf 16 encoded string can be read correctly
        /// from a big endian stream, if a little endian bom is placed.
        /// See CORBA 2.6, chapter 15.3.1
        /// </summary>
        [Test]
        public void TestDecodeLeBomBeStream() {
            byte[] encoded = new byte[] { 0, 0, 0, 6, 0xFF, 0xFE, 84, 0, 101, 0, 115, 0, 116, 0, 0, 0 }; // Test
            CdrInputStream cdrStream = CreateInputStream(encoded, false);
            Assertion.AssertEquals("wrongly decoded", "Test", cdrStream.ReadWString());
        }
        
        /// <summary>
        /// check, that an utf 16 encoded string can be read correctly
        /// from a little endian stream, if a little endian bom is placed.
        /// See CORBA 2.6, chapter 15.3.1
        /// </summary>
        [Test]
        public void TestDecodeLeBomLeStream() {
            byte[] encoded = new byte[] { 6, 0, 0, 0, 0xFF, 0xFE, 84, 0, 101, 0, 115, 0, 116, 0, 0, 0 }; // Test
            CdrInputStream cdrStream = CreateInputStream(encoded, true);
            Assertion.AssertEquals("wrongly decoded", "Test", cdrStream.ReadWString());            
        }
        
        /// <summary>
        /// check, that an utf 16 encoded string can be read correctly
        /// from a big endian stream, if a big endian bom is placed.
        /// See CORBA 2.6, chapter 15.3.1
        /// </summary>
        [Test]
        public void TestDecodeBeBomBeStream() {
            byte[] encoded = new byte[] { 0, 0, 0, 6, 0xFE, 0xFF, 0, 84, 0, 101, 0, 115, 0, 116, 0, 0 }; // Test
            CdrInputStream cdrStream = CreateInputStream(encoded, false);
            Assertion.AssertEquals("wrongly decoded", "Test", cdrStream.ReadWString());            
        }
        
        /// <summary>
        /// check, that an utf 16 encoded string can be read correctly
        /// from a little endian stream, if a big endian bom is placed.
        /// See CORBA 2.6, chapter 15.3.1
        /// </summary>
        [Test]
        public void TestDecodeBeBomLeStream() {
            byte[] encoded = new byte[] { 6, 0, 0, 0, 0xFE, 0xFF, 0, 84, 0, 101, 0, 115, 0, 116, 0, 0 }; // Test
            CdrInputStream cdrStream = CreateInputStream(encoded, true);
            Assertion.AssertEquals("wrongly decoded", "Test", cdrStream.ReadWString());            
        }
        
        /// <summary>
        /// check, that a wstring is encoded as big-endian with big endian bom for a big endian stream.
        /// </summary>
        [Test]
        public void TestEncodeBeStream() {
            MemoryStream outStream = new MemoryStream();
            CdrOutputStreamImpl cdrStream = new CdrOutputStreamImpl(outStream, 0, new GiopVersion(1, 1));
            cdrStream.WriteWString("Test");
            AssertByteArrayEquals(new byte[] { 0, 0, 0, 5, 0, 84, 0, 101, 0, 115, 0, 116, 0, 0 },
                                  outStream.ToArray());

        }
        
        /// <summary>
        /// check, that a wstring is encoded as little-endian with little endian bom for a little endian stream.
        /// </summary>        
        [Test]
        public void TestEncodeLeStream() {
            MemoryStream outStream = new MemoryStream();
            CdrOutputStreamImpl cdrStream = new CdrOutputStreamImpl(outStream, 1, new GiopVersion(1, 1));
            cdrStream.WriteWString("Test");
            AssertByteArrayEquals(new byte[] { 6, 0, 0, 0, 0xFF, 0xFE, 84, 0, 101, 0, 115, 0, 116, 0, 0, 0 },
                                  outStream.ToArray());            
        }   
        
        private void AssertByteArrayEquals(byte[] arg1, byte[] arg2) {
            Assertion.AssertEquals("Array length", arg1.Length, arg2.Length);            
            for (int i = 0; i < arg1.Length; i++) {
                Assertion.AssertEquals("array element number: " + i, arg1[i], arg2[i]);
            }            
        }
       
        
    }
    
    
}

#endif