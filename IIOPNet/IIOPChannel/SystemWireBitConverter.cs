/* SystemWireBitConverter.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 21.06.06  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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

using System;
using Ch.Elca.Iiop;

namespace Ch.Elca.Iiop.Cdr {
	
	/// <summary>
	/// Convertes between bytes on the wire with a specific endian and values.	
	/// </summary>
	/// <remarks>This class is only intended for internal use.
	/// It does assume to be called correctly, to achieve better speed.</remarks>
	internal class SystemWireBitConverter {
		
		private static void Reverse2ForBCIfNeeded(byte[] wireVal, bool wireIsLittleEndian) {
        	if (BitConverter.IsLittleEndian != wireIsLittleEndian) { // need to reverse, because BitConverter uses other endian
            	byte tmp = wireVal[0];
            	wireVal[0] = wireVal[1];
            	wireVal[1] = tmp;
            }
			
        }
		
		private static void Reverse4ForBCIfNeeded(byte[] wireVal, bool wireIsLittleEndian) {
			if (BitConverter.IsLittleEndian != wireIsLittleEndian) { // need to reverse, because BitConverter uses other endian
            	byte tmp = wireVal[0];
            	wireVal[0] = wireVal[3];
            	wireVal[3] = tmp;
            	tmp = wireVal[1];
            	wireVal[1] = wireVal[2];
            	wireVal[2] = tmp;
            }        
        }
		
		private static void Reverse8ForBCIfNeeded(byte[] wireVal, bool wireIsLittleEndian) {
			if (BitConverter.IsLittleEndian != wireIsLittleEndian) { // need to reverse, because BitConverter uses other endian
            	byte tmp = wireVal[0];
            	wireVal[0] = wireVal[7];
            	wireVal[7] = tmp;
            	tmp = wireVal[1];
            	wireVal[1] = wireVal[6];
            	wireVal[6] = tmp;
            	tmp = wireVal[2];
            	wireVal[2] = wireVal[5];
            	wireVal[5] = tmp;
            	tmp = wireVal[3];
            	wireVal[3] = wireVal[4];
            	wireVal[4] = tmp;
            }
        }		
		
		/// <summary>
		/// converts wireVal to a short considering wire and system endian.
		/// </summary>
		/// <remarks>Be careful, this method modifies wireVal</remarks>
		internal static Int16 ToInt16(byte[] wireVal, bool wireIsLittleEndian) {
			Reverse2ForBCIfNeeded(wireVal, wireIsLittleEndian);
			return BitConverter.ToInt16(wireVal, 0);
		}						
		
		/// <summary>
		/// converts wireVal to an int considering wire and system endian.
		/// </summary>
		/// <remarks>Be careful, this method modifies wireVal</remarks>
		internal static Int32 ToInt32(byte[] wireVal, bool wireIsLittleEndian) {
			Reverse4ForBCIfNeeded(wireVal, wireIsLittleEndian);
			return BitConverter.ToInt32(wireVal, 0);
		}								
		
		/// <summary>
		/// converts wireVal to a long considering wire and system endian.
		/// </summary>
		/// <remarks>Be careful, this method modifies wireVal</remarks>
		internal static Int64 ToInt64(byte[] wireVal, bool wireIsLittleEndian) {
			Reverse8ForBCIfNeeded(wireVal, wireIsLittleEndian);
			return BitConverter.ToInt64(wireVal, 0);
		}										

		/// <summary>
		/// converts wireVal to a ushort considering wire and system endian.
		/// </summary>
		/// <remarks>Be careful, this method modifies wireVal</remarks>
		internal static UInt16 ToUInt16(byte[] wireVal, bool wireIsLittleEndian) {
			Reverse2ForBCIfNeeded(wireVal, wireIsLittleEndian);
			return BitConverter.ToUInt16(wireVal, 0);
		}						
		
		/// <summary>
		/// converts wireVal to an uint considering wire and system endian.
		/// </summary>
		/// <remarks>Be careful, this method modifies wireVal</remarks>
		internal static UInt32 ToUInt32(byte[] wireVal, bool wireIsLittleEndian) {
			Reverse4ForBCIfNeeded(wireVal, wireIsLittleEndian);
			return BitConverter.ToUInt32(wireVal, 0);
		}								
		
		/// <summary>
		/// converts wireVal to a ulong considering wire and system endian.
		/// </summary>
		/// <remarks>Be careful, this method modifies wireVal</remarks>
		internal static UInt64 ToUInt64(byte[] wireVal, bool wireIsLittleEndian) {
			Reverse8ForBCIfNeeded(wireVal, wireIsLittleEndian);
			return BitConverter.ToUInt64(wireVal, 0);
		}			
		
		/// <summary>
		/// converts val to a wireval considering wire and system endian.
		/// </summary>
		internal static byte[] GetBytes(Int16 val, bool wireIsLittleEndian) {
			byte[] wireVal = BitConverter.GetBytes(val);
			Reverse2ForBCIfNeeded(wireVal, wireIsLittleEndian);
			return wireVal;
		}
		
		/// <summary>
		/// converts val to a wireval considering wire and system endian.
		/// </summary>
		internal static byte[] GetBytes(Int32 val, bool wireIsLittleEndian) {
			byte[] wireVal = BitConverter.GetBytes(val);
			Reverse4ForBCIfNeeded(wireVal, wireIsLittleEndian);
			return wireVal;
		}		
		
	}
	
}


#if UnitTest

namespace Ch.Elca.Iiop.Tests {
    
    using System;
    using System.Reflection;
    using Ch.Elca.Iiop.Cdr;
    using NUnit.Framework;
    
    /// <summary>
    /// Unit-tests for testing SystemWireBitConverter.
    /// </summary>
    [TestFixture]
    public class SystemWireBitConverterTest {

    	
    	[Test]
    	public void TestInt16WBEWToS() {
    		System.Int16 result = 
    			SystemWireBitConverter.ToInt16(new byte[] { 0, 1 }, false);
    		Assertion.AssertEquals("converted wbe int 16", 1, result);
    		
    		result = 
    			SystemWireBitConverter.ToInt16(new byte[] { 1, 2 }, false);
    		Assertion.AssertEquals("converted wbe int 16 (2)", 258, result);
    		
    		result = 
    			SystemWireBitConverter.ToInt16(new byte[] { 0x7F, 0xFF }, false);
    		Assertion.AssertEquals("converted wbe int 16 (3)", Int16.MaxValue, result);
    		
    		result = 
    			SystemWireBitConverter.ToInt16(new byte[] { 0x80, 0x00 }, false);
    		Assertion.AssertEquals("converted wbe int 16 (4)", Int16.MinValue, result);    		
    	}
    	
    	[Test]
    	public void TestInt16WLEWToS() {
    		System.Int16 result = 
    			SystemWireBitConverter.ToInt16(new byte[] { 1, 0 }, true);
    		Assertion.AssertEquals("converted wbe int 16", 1, result);
    		
			result = 
    			SystemWireBitConverter.ToInt16(new byte[] { 2, 1 }, true);
    		Assertion.AssertEquals("converted wbe int 16 (2)", 258, result);    		
    		
			result = 
    			SystemWireBitConverter.ToInt16(new byte[] { 0xFF, 0x7F }, true);
    		Assertion.AssertEquals("converted wbe int 16 (3)", Int16.MaxValue, result);
    		
			result = 
    			SystemWireBitConverter.ToInt16(new byte[] { 0x00, 0x80 }, true);
    		Assertion.AssertEquals("converted wbe int 16 (4)", Int16.MinValue, result);
    	}
    	
    	[Test]
    	public void TestInt32WBEWToS() {
    		System.Int32 result = 
    			SystemWireBitConverter.ToInt32(new byte[] { 0, 0, 0, 1 }, false);
    		Assertion.AssertEquals("converted wbe int 32", 1, result);
    		
    		result = 
    			SystemWireBitConverter.ToInt32(new byte[] { 0, 0, 1, 2 }, false);
    		Assertion.AssertEquals("converted wbe int 32 (2)", 258, result);
    		
    		result = 
    			SystemWireBitConverter.ToInt32(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, false);
    		Assertion.AssertEquals("converted wbe int 32 (3)", Int32.MaxValue, result);
    		
    		result = 
    			SystemWireBitConverter.ToInt32(new byte[] { 0x80, 0x00, 0x00, 0x00 }, false);
    		Assertion.AssertEquals("converted wbe int 32 (4)", Int32.MinValue, result);    		
    	}
    	
    	[Test]
    	public void TestInt32WLEWToS() {
    		System.Int32 result = 
    			SystemWireBitConverter.ToInt32(new byte[] { 1, 0, 0, 0 }, true);
    		Assertion.AssertEquals("converted wbe int 32", 1, result);
    		
			result = 
    			SystemWireBitConverter.ToInt32(new byte[] { 2, 1, 0, 0 }, true);
    		Assertion.AssertEquals("converted wbe int 32 (2)", 258, result);    		
    		
			result = 
    			SystemWireBitConverter.ToInt32(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, true);
    		Assertion.AssertEquals("converted wbe int 32 (3)", Int32.MaxValue, result);
    		
			result = 
    			SystemWireBitConverter.ToInt32(new byte[] { 0x00, 0x00, 0x00, 0x80 }, true);
    		Assertion.AssertEquals("converted wbe int 32 (4)", Int32.MinValue, result);
    	}
    	
    	[Test]
    	public void TestInt64WBEWToS() {
    		System.Int64 result = 
    			SystemWireBitConverter.ToInt64(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }, false);
    		Assertion.AssertEquals("converted wbe int 64", 1, result);
    		
    		result = 
    			SystemWireBitConverter.ToInt64(new byte[] { 0, 0, 0, 0, 0, 0, 1, 2 }, false);
    		Assertion.AssertEquals("converted wbe int 64 (2)", 258, result);
    		
    		result = 
    			SystemWireBitConverter.ToInt64(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, false);
    		Assertion.AssertEquals("converted wbe int 64 (3)", Int64.MaxValue, result);
    		
    		result = 
    			SystemWireBitConverter.ToInt64(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false);
    		Assertion.AssertEquals("converted wbe int 64 (4)", Int64.MinValue, result);    		
    	}
    	
    	[Test]
    	public void TestInt64WLEWToS() {
    		System.Int64 result = 
    			SystemWireBitConverter.ToInt64(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 }, true);
    		Assertion.AssertEquals("converted wbe int 64", 1, result);
    		
			result = 
    			SystemWireBitConverter.ToInt64(new byte[] { 2, 1, 0, 0, 0, 0, 0, 0 }, true);
    		Assertion.AssertEquals("converted wbe int 64 (2)", 258, result);    		
    		
			result = 
    			SystemWireBitConverter.ToInt64(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, true);
    		Assertion.AssertEquals("converted wbe int 64 (3)", Int64.MaxValue, result);
    		
			result = 
    			SystemWireBitConverter.ToInt64(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, true);
    		Assertion.AssertEquals("converted wbe int 64 (4)", Int64.MinValue, result);
    	}
    	
    	[Test]
    	public void TestUInt16WBEWToS() {
    		System.UInt16 result = 
    			SystemWireBitConverter.ToUInt16(new byte[] { 0, 1 }, false);
    		Assertion.AssertEquals("converted wbe uint 16", 1, result);
    		
    		result = 
    			SystemWireBitConverter.ToUInt16(new byte[] { 1, 2 }, false);
    		Assertion.AssertEquals("converted wbe uint 16 (2)", 258, result);
    		
    		result = 
    			SystemWireBitConverter.ToUInt16(new byte[] { 0xFF, 0xFF }, false);
    		Assertion.AssertEquals("converted wbe uint 16 (3)", UInt16.MaxValue, result);
    		
    		result = 
    			SystemWireBitConverter.ToUInt16(new byte[] { 0x00, 0x00 }, false);
    		Assertion.AssertEquals("converted wbe uint 16 (4)", UInt16.MinValue, result);    		
    	}
    	
    	[Test]
    	public void TestUInt16WLEWToS() {
    		System.UInt16 result = 
    			SystemWireBitConverter.ToUInt16(new byte[] { 1, 0 }, true);
    		Assertion.AssertEquals("converted wbe uint 16", 1, result);
    		
			result = 
    			SystemWireBitConverter.ToUInt16(new byte[] { 2, 1 }, true);
    		Assertion.AssertEquals("converted wbe uint 16 (2)", 258, result);    		
    		
			result = 
    			SystemWireBitConverter.ToUInt16(new byte[] { 0xFF, 0xFF }, true);
    		Assertion.AssertEquals("converted wbe uint 16 (3)", UInt16.MaxValue, result);
    		
			result = 
    			SystemWireBitConverter.ToUInt16(new byte[] { 0x00, 0x00 }, true);
    		Assertion.AssertEquals("converted wbe uint 16 (4)", UInt16.MinValue, result);
    	}
    	
    	[Test]
    	public void TestUInt32WBEWToS() {
    		System.UInt32 result = 
    			SystemWireBitConverter.ToUInt32(new byte[] { 0, 0, 0, 1 }, false);
    		Assertion.AssertEquals("converted wbe uint 32", 1, result);
    		
    		result = 
    			SystemWireBitConverter.ToUInt32(new byte[] { 0, 0, 1, 2 }, false);
    		Assertion.AssertEquals("converted wbe uint 32 (2)", 258, result);
    		
    		result = 
    			SystemWireBitConverter.ToUInt32(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, false);
    		Assertion.AssertEquals("converted wbe uint 32 (3)", UInt32.MaxValue, result);
    		
    		result = 
    			SystemWireBitConverter.ToUInt32(new byte[] { 0x00, 0x00, 0x00, 0x00 }, false);
    		Assertion.AssertEquals("converted wbe uint 32 (4)", UInt32.MinValue, result);    		
    	}
    	
    	[Test]
    	public void TestUInt32WLEWToS() {
    		System.UInt32 result = 
    			SystemWireBitConverter.ToUInt32(new byte[] { 1, 0, 0, 0 }, true);
    		Assertion.AssertEquals("converted wbe uint 32", 1, result);
    		
			result = 
    			SystemWireBitConverter.ToUInt32(new byte[] { 2, 1, 0, 0 }, true);
    		Assertion.AssertEquals("converted wbe uint 32 (2)", 258, result);    		
    		
			result = 
    			SystemWireBitConverter.ToUInt32(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, true);
    		Assertion.AssertEquals("converted wbe uint 32 (3)", UInt32.MaxValue, result);
    		
			result = 
    			SystemWireBitConverter.ToUInt32(new byte[] { 0x00, 0x00, 0x00, 0x00 }, true);
    		Assertion.AssertEquals("converted wbe uint 32 (4)", UInt32.MinValue, result);
    	}
    	
    	[Test]
    	public void TestUInt64WBEWToS() {
    		System.UInt64 result = 
    			SystemWireBitConverter.ToUInt64(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }, false);
    		Assertion.AssertEquals("converted wbe uint 64", 1, result);
    		
    		result = 
    			SystemWireBitConverter.ToUInt64(new byte[] { 0, 0, 0, 0, 0, 0, 1, 2 }, false);
    		Assertion.AssertEquals("converted wbe uint 64 (2)", 258, result);
    		
    		result = 
    			SystemWireBitConverter.ToUInt64(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, false);
    		Assertion.AssertEquals("converted wbe uint 64 (3)", UInt64.MaxValue, result);
    		
    		result = 
    			SystemWireBitConverter.ToUInt64(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false);
    		Assertion.AssertEquals("converted wbe uint 64 (4)", UInt64.MinValue, result);    		
    	}
    	
    	[Test]
    	public void TestUInt64WLEWToS() {
    		System.UInt64 result = 
    			SystemWireBitConverter.ToUInt64(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 }, true);
    		Assertion.AssertEquals("converted wbe uint 64", 1, result);
    		
			result = 
    			SystemWireBitConverter.ToUInt64(new byte[] { 2, 1, 0, 0, 0, 0, 0, 0 }, true);
    		Assertion.AssertEquals("converted wbe uint 64 (2)", 258, result);    		
    		
			result = 
    			SystemWireBitConverter.ToUInt64(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, true);
    		Assertion.AssertEquals("converted wbe uint 64 (3)", UInt64.MaxValue, result);
    		
			result = 
    			SystemWireBitConverter.ToUInt64(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true);
    		Assertion.AssertEquals("converted wbe uint 64 (4)", UInt64.MinValue, result);
    	}
    	    	
    	
    	[Test]
    	public void TestInt16WBESToW() {
    		byte[] result =
    			SystemWireBitConverter.GetBytes((short)1, false);
    		ArrayAssertion.AssertByteArrayEquals("converted wbe int 16", new byte[] { 0, 1 }, result);
    		
    		result =
    			SystemWireBitConverter.GetBytes((short)258, false);
    		ArrayAssertion.AssertByteArrayEquals("converted wbe int 16 (2)", new byte[] { 1, 2 }, result);

    		result =
    			SystemWireBitConverter.GetBytes(Int16.MaxValue, false);
    		ArrayAssertion.AssertByteArrayEquals("converted wbe int 16 (3)", new byte[] { 0x7F, 0xFF }, result);

    		result =
    			SystemWireBitConverter.GetBytes(Int16.MinValue, false);
    		ArrayAssertion.AssertByteArrayEquals("converted wbe int 16 (4)", new byte[] { 0x80, 0x00 }, result);    		
    	}
    	
    	[Test]
    	public void TestInt16WLESToW() {
    		byte[] result =
    			SystemWireBitConverter.GetBytes((short)1, true);
    		ArrayAssertion.AssertByteArrayEquals("converted wbe int 16", new byte[] { 1, 0 }, result);
    		
    		result =
    			SystemWireBitConverter.GetBytes((short)258, true);
    		ArrayAssertion.AssertByteArrayEquals("converted wbe int 16 (2)", new byte[] { 2, 1 }, result);

    		result =
    			SystemWireBitConverter.GetBytes(Int16.MaxValue, true);
    		ArrayAssertion.AssertByteArrayEquals("converted wbe int 16 (3)", new byte[] { 0xFF, 0x7F }, result);

    		result =
    			SystemWireBitConverter.GetBytes(Int16.MinValue, true);
    		ArrayAssertion.AssertByteArrayEquals("converted wbe int 16 (4)", new byte[] { 0x00, 0x80 }, result);    		    		
    	}
    	
    	[Test]
    	public void TestInt32WBESToW() {
    		byte[] result =
    			SystemWireBitConverter.GetBytes((int)1, false);
    		ArrayAssertion.AssertByteArrayEquals("converted wbe int 32", new byte[] { 0, 0, 0, 1 }, result);
    		
    		result =
    			SystemWireBitConverter.GetBytes((int)258, false);
    		ArrayAssertion.AssertByteArrayEquals("converted wbe int 32 (2)", new byte[] { 0, 0, 1, 2 }, result);

    		result =
    			SystemWireBitConverter.GetBytes(Int32.MaxValue, false);
    		ArrayAssertion.AssertByteArrayEquals("converted wbe int 32 (3)", new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, result);

    		result =
    			SystemWireBitConverter.GetBytes(Int32.MinValue, false);
    		ArrayAssertion.AssertByteArrayEquals("converted wbe int 32 (4)", new byte[] { 0x80, 0x00, 0x00, 0x00 }, result);
    	}
    	
    	[Test]
    	public void TestInt32WLESToW() {
    		byte[] result =
    			SystemWireBitConverter.GetBytes((int)1, true);
    		ArrayAssertion.AssertByteArrayEquals("converted wbe int 32", new byte[] { 1, 0, 0, 0 }, result);
    		
    		result =
    			SystemWireBitConverter.GetBytes((int)258, true);
    		ArrayAssertion.AssertByteArrayEquals("converted wbe int 32 (2)", new byte[] { 2, 1, 0, 0 }, result);

    		result =
    			SystemWireBitConverter.GetBytes(Int32.MaxValue, true);
    		ArrayAssertion.AssertByteArrayEquals("converted wbe int 32 (3)", new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, result);

    		result =
    			SystemWireBitConverter.GetBytes(Int32.MinValue, true);
    		ArrayAssertion.AssertByteArrayEquals("converted wbe int 32 (4)", new byte[] { 0x00, 0x00, 0x00, 0x80 }, result);    		    		
    	}

    	
    }

}

#endif
    
