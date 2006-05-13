/* CdrStreamTests.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 13.05.06  Dominic Ullmann (DUL), dul@elca.ch
 * 
 * Copyright 2006 Dominic Ullmann
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


#if UnitTest

namespace Ch.Elca.Iiop.Tests {
    
    using System.IO;
    using NUnit.Framework;
    using Ch.Elca.Iiop;
    using Ch.Elca.Iiop.Cdr;
    using omg.org.CORBA;
    
	/// <summary>
	/// Tests the CdrInputStream
	/// </summary>
	[TestFixture]
	public class CdrInputStreamTests {
	    
	    
	    private CdrInputStream PrepareStream(byte[] testData) {
	        MemoryStream testStream = new MemoryStream(testData);
	        CdrInputStreamImpl inputStream = new CdrInputStreamImpl(testStream);
	        inputStream.SetMaxLength((uint)testData.Length);
	        inputStream.ConfigStream(0, new GiopVersion(1, 2));	        
	        return inputStream;
	    }
	    
	    [Test]
	    public void TestReadStringCodeSetOk() {
	        byte[] testData = new byte[] { 0, 0, 0, 5, 65, 66, 67, 68, 0 };
	        CdrInputStream inputStream = PrepareStream(testData);
	        string result = inputStream.ReadString();
	        Assertion.AssertEquals("read string", "ABCD", result);
	    }
	    
	    [Test]
	    public void TestReadWStringCodeSetOk() {
	        byte[] testData = new byte[] { 0, 0, 0, 8, 0, 65, 0, 66, 0, 67, 0, 68 };
	        CdrInputStream inputStream = PrepareStream(testData);
	        string result = inputStream.ReadWString();
	        Assertion.AssertEquals("read string", "ABCD", result);
	    }
	    
	    // [Test]	    
	    public void TestCdrStreamWStringCodeSetNotSet() {
	        try {
	            
	            Assertion.Fail("no exception, although no wchar code set set");
	        } catch (INV_OBJREF iEx) {
	            Assertion.AssertEquals("minor code", 1, iEx.Minor);
	        }
	    }
		
	}
		
}

#endif
