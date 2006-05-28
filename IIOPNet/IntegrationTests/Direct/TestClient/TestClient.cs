/* TestClient.cs
 *
 * Project: IIOP.NET
 * IntegrationTests
 *
 * WHEN      RESPONSIBLE
 * 08.07.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Text;
using NUnit.Framework;
using Ch.Elca.Iiop;
using Ch.Elca.Iiop.Idl;
using Ch.Elca.Iiop.Services;
using omg.org.CosNaming;
using omg.org.CORBA;
using Ch.Elca.Iiop.CorbaObjRef;

namespace Ch.Elca.Iiop.IntegrationTests {

    [TestFixture]
    public class TestClient {

        #region IFields

        private IiopClientChannel m_channel;

        private TestService m_testService;


        #endregion IFields

        [SetUp]
        public void SetupEnvironment() {
            // register the channel
            m_channel = new IiopClientChannel();
            ChannelServices.RegisterChannel(m_channel);

            // get the reference to the test-service
            m_testService = (TestService)RemotingServices.Connect(typeof(TestService), "corbaloc:iiop:1.2@localhost:8087/test");
        }

        [TearDown]
        public void TearDownEnvironment() {
            m_testService = null;
            // unregister the channel            
            ChannelServices.UnregisterChannel(m_channel);
        }

        [Test]
        public void TestDouble() {
            System.Double arg = 1.23;
            System.Double result = m_testService.TestIncDouble(arg);
            Assertion.AssertEquals((System.Double)(arg + 1), result);
        }

        [Test]
        public void TestFloat() {
            System.Single arg = 1.23f;
            System.Single result = m_testService.TestIncFloat(arg);
            Assertion.AssertEquals((System.Single)(arg + 1), result);
        }
        
        [Test]
        public void TestByte() {
            System.Byte arg = 1;
            System.Byte result = m_testService.TestIncByte(arg);
            Assertion.AssertEquals((System.Byte)(arg + 1), result);
        }

        [Test]
        public void TestInt16() {
            System.Int16 arg = 1;
            System.Int16 result = m_testService.TestIncInt16(arg);
            Assertion.AssertEquals((System.Int16)(arg + 1), result);
            arg = -11;
            result = m_testService.TestIncInt16(arg);
            Assertion.AssertEquals((System.Int16)(arg + 1), result);
        }

        [Test]
        public void TestInt32() {
            System.Int32 arg = 1;
            System.Int32 result = m_testService.TestIncInt32(arg);
            Assertion.AssertEquals((System.Int32)(arg + 1), result);
            arg = -11;
            result = m_testService.TestIncInt32(arg);
            Assertion.AssertEquals((System.Int32)(arg + 1), result);
        }

        [Test]
        public void TestInt64() {
            System.Int64 arg = 1;
            System.Int64 result = m_testService.TestIncInt64(arg);
            Assertion.AssertEquals((System.Int64)(arg + 1), result);
            arg = -11;
            result = m_testService.TestIncInt64(arg);
            Assertion.AssertEquals((System.Int64)(arg + 1), result);
        }

        [Test]
        public void TestSByte() {
            System.SByte arg = 1;
            System.SByte result = m_testService.TestIncSByte(arg);
            Assertion.AssertEquals((System.SByte)(arg + 1), result);
            arg = -2;
            result = m_testService.TestIncSByte(arg);
            Assertion.AssertEquals((System.SByte)(arg + 1), result);
        }

        [Test]
        public void TestSByteAsAny() {
            System.SByte arg = 1;
            System.SByte result = (System.SByte)
                ((System.Byte)m_testService.EchoAnything(arg));
            Assertion.AssertEquals((System.SByte)arg, result);
            arg = -2;
            result = (System.SByte)
                ((System.Byte)m_testService.EchoAnything(arg));
            Assertion.AssertEquals((System.SByte)arg, result);

            Any argAny = new Any(arg);
            Any resultAny = m_testService.EchoAnythingContainer(argAny);
            result = (System.SByte)
                ((System.Byte)resultAny.Value);
            Assertion.AssertEquals((System.SByte)arg, result);
        }

        [Test]
        public void TestUInt16() {
            System.UInt16 arg = 1;
            System.UInt16 result = m_testService.TestIncUInt16(arg);
            Assertion.AssertEquals((System.Int16)(arg + 1), result);
            arg = System.UInt16.MaxValue - (System.UInt16)1;
            result = m_testService.TestIncUInt16(arg);
            Assertion.AssertEquals((System.UInt16)(arg + 1), result);
        }

        [Test]
        public void TestUInt32() {
            System.UInt32 arg = 1;
            System.UInt32 result = m_testService.TestIncUInt32(arg);
            Assertion.AssertEquals((System.UInt32)(arg + 1), result);
            arg = System.UInt32.MaxValue - (System.UInt32)1;
            result = m_testService.TestIncUInt32(arg);
            Assertion.AssertEquals((System.UInt32)(arg + 1), result);
        }

        [Test]
        public void TestUInt64() {
            System.UInt64 arg = 1;
            System.UInt64 result = m_testService.TestIncUInt64(arg);
            Assertion.AssertEquals((System.UInt64)(arg + 1), result);
            arg = System.UInt64.MaxValue - 1;
            result = m_testService.TestIncUInt64(arg);
            Assertion.AssertEquals((System.UInt64)(arg + 1), result);
        }

        [Test]
        public void TestUInt64AsAny() {
            System.UInt64 arg = 1;
            System.UInt64 result = (System.UInt64)
                ((System.Int64)m_testService.EchoAnything(arg));
            Assertion.AssertEquals(arg, result);
            arg = System.UInt64.MaxValue - 1;
            result = (System.UInt64)
                ((System.Int64)m_testService.EchoAnything(arg));
            Assertion.AssertEquals(arg, result);
            
            Any argAny = new Any(arg);
            Any resultAny = m_testService.EchoAnythingContainer(argAny);
            result = (System.UInt64)resultAny.Value;
            Assertion.AssertEquals(arg, result);
        }

        [Test]
        public void TestBoolean() {
            System.Boolean arg = true;
            System.Boolean result = m_testService.TestNegateBoolean(arg);
            Assertion.AssertEquals(false, result);
        }

        [Test]
        public void TestVoid() {
            m_testService.TestVoid();
        }
        
        [Test]
        public void TestChar() {
            System.Char arg = 'a';
            System.Char result = m_testService.TestEchoChar(arg);
            Assertion.AssertEquals(arg, result);
            arg = '0';
            result = m_testService.TestEchoChar(arg);
            Assertion.AssertEquals(arg, result);
        }
        
        [Test]
        public void TestString() {
            System.String arg = "test";
            System.String toAppend = "toAppend";
            System.String result = m_testService.TestAppendString(arg, toAppend);
            Assertion.AssertEquals(arg + toAppend, result);
            arg = "test";
            toAppend = null;
            result = m_testService.TestAppendString(arg, toAppend);
            Assertion.AssertEquals(arg, result);
        }       

        [Test]
        public void TestEnumeration() {
            TestEnum arg = TestEnum.A;
            TestEnum result = m_testService.TestEchoEnumVal(arg);
            Assertion.AssertEquals(arg, result);
            arg = TestEnum.D;
            result = m_testService.TestEchoEnumVal(arg);
            Assertion.AssertEquals(arg, result);
        }

        [Test]
        public void TestFlagsArguments() {
            TestFlags arg = TestFlags.A1;
            TestFlags result = m_testService.TestEchoFlagsVal(arg);
            Assertion.AssertEquals(arg, result);
            arg = TestFlags.All;
            result = m_testService.TestEchoFlagsVal(arg);
            Assertion.AssertEquals(arg, result);
        }

        [Test]
        public void TestEnumBI16Val() {
            TestEnumBI16 arg = TestEnumBI16.B1;
            TestEnumBI16 result = m_testService.TestEchoEnumI16Val(arg);
            Assertion.AssertEquals(arg, result);
        }

        [Test]
        public void TestEnumBUI32Val() {
            TestEnumUI32 arg = TestEnumUI32.C2;
            TestEnumUI32 result = m_testService.TestEchoEnumUI32Val(arg);
            Assertion.AssertEquals(arg, result);
            arg = TestEnumUI32.A2;
            result = m_testService.TestEchoEnumUI32Val(arg);
            Assertion.AssertEquals(arg, result);
            arg = TestEnumUI32.B2;
            result = m_testService.TestEchoEnumUI32Val(arg);
            Assertion.AssertEquals(arg, result);
        }

        [Test]
        public void TestEnumBI64Val() {
            TestEnumBI64 arg = TestEnumBI64.AL;
            TestEnumBI64 result = m_testService.TestEchoEnumI64Val(arg);
            Assertion.AssertEquals(arg, result);

            arg = TestEnumBI64.BL;
            result = m_testService.TestEchoEnumI64Val(arg);
            Assertion.AssertEquals(arg, result);
        }

        [Test]
        public void TestEnumAsAny() {
            TestEnum arg = TestEnum.A;
            TestEnum result = (TestEnum)m_testService.EchoAnything(arg);
            Assertion.AssertEquals(arg, result);
            arg = TestEnum.D;
            result = (TestEnum)m_testService.EchoAnything(arg);
            Assertion.AssertEquals(arg, result);
        }

        [Test]
        public void TestFlagsAsAny() {
            TestFlags arg = TestFlags.A1;
            TestFlags result = (TestFlags)m_testService.EchoAnything(arg);
            Assertion.AssertEquals(arg, result);
            arg = TestFlags.All;
            result = (TestFlags)m_testService.EchoAnything(arg);
            Assertion.AssertEquals(arg, result);
        }

        [Test]
        public void TestByteArray() {
            System.Byte[] arg = new System.Byte[1];
            arg[0] = 1;
            System.Byte toAppend = 2;
            System.Byte[] result = m_testService.TestAppendElementToByteArray(arg, toAppend);
            Assertion.AssertEquals(2, result.Length);
            Assertion.AssertEquals((System.Byte) 1, result[0]);
            Assertion.AssertEquals((System.Byte) 2, result[1]);

            arg = null;
            toAppend = 3;
            result = m_testService.TestAppendElementToByteArray(arg, toAppend);
            Assertion.AssertEquals(1, result.Length);
            Assertion.AssertEquals((System.Byte) 3, result[0]);
        }

        [Test]
        public void TestStringArray() {            
            System.String arg1 = "abc";
            System.String arg2 = "def";
            System.String[] result = m_testService.CreateTwoElemStringArray(arg1, arg2);
            Assertion.AssertEquals(arg1, result[0]);
            Assertion.AssertEquals(arg2, result[1]);
            
            System.String[] arg = new System.String[1];
            arg[0] = "abc";
            System.String toAppend = "def";
            result = m_testService.TestAppendElementToStringArray(arg, toAppend);
            Assertion.AssertEquals(2, result.Length);
            Assertion.AssertEquals("abc", result[0]);
            Assertion.AssertEquals("def", result[1]);

            arg = null;
            toAppend = "hik";
            result = m_testService.TestAppendElementToStringArray(arg, toAppend);
            Assertion.AssertEquals(1, result.Length);
            Assertion.AssertEquals("hik", result[0]);
        }
        
        [Test]
        public void TestJaggedArrays() {
            System.Int32[][] arg1 = new System.Int32[2][];
            arg1[0] = new System.Int32[] { 1 };
            arg1[1] = new System.Int32[] { 2, 3 };
            System.Int32[][] result1 = m_testService.EchoJaggedIntArray(arg1);
            Assertion.AssertEquals(2, result1.Length);
            Assertion.AssertNotNull(result1[0]);
            Assertion.AssertNotNull(result1[1]);
            Assertion.AssertEquals(arg1[0][0], result1[0][0]);
            Assertion.AssertEquals(arg1[1][0], result1[1][0]);
            Assertion.AssertEquals(arg1[1][1], result1[1][1]);
            
            System.Byte[][][] arg2 = new System.Byte[3][][];
            arg2[0] = new System.Byte[][] { new System.Byte[] { 1 } };
            arg2[1] = new System.Byte[][] { new System.Byte[0] };
            arg2[2] = new System.Byte[0][];
            System.Byte[][][] result2 = m_testService.EchoJaggedByteArray(arg2);
            Assertion.AssertEquals(3, result2.Length);
            Assertion.AssertNotNull(result2[0]);
            Assertion.AssertNotNull(result2[1]);
            Assertion.AssertNotNull(result2[2]);
            Assertion.AssertEquals(arg2[0][0][0], result2[0][0][0]);
        }

        [Test]
        public void TestJaggedArraysWithNullElems() {
        System.Int32[][] arg1 = null;
            System.Int32[][] result1 = m_testService.EchoJaggedIntArray(arg1);
            Assertion.AssertEquals(arg1, result1);

            System.Int32[][] arg2 = new System.Int32[2][];
            System.Int32[][] result2 = m_testService.EchoJaggedIntArray(arg2);
            Assertion.AssertNotNull(result2);

            System.String[][] arg3 = null;
            System.String[][] result3 = m_testService.EchoJaggedStringArray(arg3);
            Assertion.AssertEquals(arg3, result3);

            System.String[][] arg4 = new System.String[][] { null, new System.String[] { "abc", "def" } };
            System.String[][] result4 = m_testService.EchoJaggedStringArray(arg4);
            Assertion.AssertNotNull(result4);
            Assertion.AssertNull(result4[0]);
            Assertion.AssertNotNull(result4[1]);
            Assertion.AssertEquals(result4[1][0], arg4[1][0]);
            Assertion.AssertEquals(result4[1][1], arg4[1][1]);
        }

        
        [Test]
        public void TestJaggedStringArrays() {
            System.String[][] arg1 = new System.String[2][];
            arg1[0] = new System.String[] { "test" };
            arg1[1] = new System.String[] { "test2", "test3" };
            System.String[][] result1 = m_testService.EchoJaggedStringArray(arg1);
            Assertion.AssertEquals(2, result1.Length);
            Assertion.AssertNotNull(result1[0]);
            Assertion.AssertNotNull(result1[1]);
            Assertion.AssertEquals(arg1[0][0], result1[0][0]);
            Assertion.AssertEquals(arg1[1][0], result1[1][0]);
            Assertion.AssertEquals(arg1[1][1], result1[1][1]);                        
        }
        
        [Test]
        public void TestMultidimArrays() {
            System.Int32[,] arg1 = new System.Int32[2,2];
            arg1[0,0] = 1;
            arg1[0,1] = 2;
            arg1[1,0] = 3;
            arg1[1,1] = 4;

            System.Int32[,] result1 = m_testService.EchoMultiDimIntArray(arg1);
            Assertion.AssertEquals(arg1[0,0], result1[0,0]);
            Assertion.AssertEquals(arg1[1,0], result1[1,0]);
            Assertion.AssertEquals(arg1[0,1], result1[0,1]);
            Assertion.AssertEquals(arg1[1,1], result1[1,1]);            
        }
        
        [Test]
        public void TestMutlidimStringArrays() {
            System.String[,] arg1 = new System.String[2,2];
            arg1[0,0] = "test0";
            arg1[0,1] = "test1";
            arg1[1,0] = "test2";
            arg1[1,1] = "test3";
            System.String[,] result1 = m_testService.EchoMultiDimStringArray(arg1);
            Assertion.AssertEquals(arg1[0,0], result1[0,0]);
            Assertion.AssertEquals(arg1[0,1], result1[0,1]);
            Assertion.AssertEquals(arg1[1,0], result1[1,0]);
            Assertion.AssertEquals(arg1[1,1], result1[1,1]);
        }

        [Test]
        public void TestRemoteObjects() {
            Adder adder = m_testService.RetrieveAdder();
            System.Int32 arg1 = 1;
            System.Int32 arg2 = 2;
            System.Int32 result = adder.Add(1, 2);
            Assertion.AssertEquals((System.Int32) arg1 + arg2, result);            
        }

        [Test]
        public void TestSendRefOfAProxy() {
            Adder adder = m_testService.RetrieveAdder();
            System.Int32 arg1 = 1;
            System.Int32 arg2 = 2;
            System.Int32 result = m_testService.AddWithAdder(adder, arg1, arg2);
            Assertion.AssertEquals((System.Int32) arg1 + arg2, result);
        }

        [Test]
        public void TestStruct() {
            TestStructA arg = new TestStructA();
            arg.X = 11;
            arg.Y = -15;
            TestStructA result = m_testService.TestEchoStruct(arg);
            Assertion.AssertEquals(arg.X, result.X);
            Assertion.AssertEquals(arg.Y, result.Y);
        }

        /// <summary>
        /// Checks, if the repository id of the value-type itself is used and not the rep-id 
        /// for the implementation class
        /// </summary>
        [Test]
        public void TestTypeOfValueTypePassed() {
            TestSerializableClassB2 arg = new TestSerializableClassB2();
            arg.Msg = "msg";            
            TestSerializableClassB2 result = m_testService.TestChangeSerializableB2(arg, arg.DetailedMsg);
            Assertion.AssertEquals(result.Msg, arg.Msg);
        }
        
        /// <summary>
        /// Checks, if the fields of a super-type are serilised too
        /// </summary>
        [Test]
        public void TestValueTypeInheritance() {
            TestSerializableClassB2 arg = new TestSerializableClassB2();
            arg.Msg = "msg";
            System.String newDetail = "new detail";
            TestSerializableClassB2 result = m_testService.TestChangeSerializableB2(arg, newDetail);
            Assertion.AssertEquals(newDetail, result.DetailedMsg);
            Assertion.AssertEquals(arg.Msg, result.Msg);
        }

        /// <summary>
        /// Checks, if a formal parameter type, which is not Serilizable works correctly,
        /// if an instance of a Serializable subclass is passed.
        /// </summary>
        [Test]
        public void TestNonSerilizableFormalParam() {
            TestNonSerializableBaseClass arg = new TestSerializableClassC();
            TestNonSerializableBaseClass result = m_testService.TestAbstractValueTypeEcho(arg);
            Assertion.AssertEquals(typeof(TestSerializableClassC), result.GetType());
        }

        [Test]
        public void TestBaseTypeNonSerializableParam() {
            TestSerializableClassC arg = new TestSerializableClassC();
            arg.Msg = "test";
            TestSerializableClassC result = m_testService.TestEchoSerializableC(arg);
            Assertion.AssertEquals(arg.Msg, result.Msg);
        }

        /// <summary>
        /// Checks, if fields with reference semantics retain their semantic during serialisation / deserialisation
        /// </summary>
        [Test]
        public void TestReferenceSematicForValueTypeField() {
            TestSerializableClassD arg = new TestSerializableClassD();
            arg.val1 = new TestSerializableClassB1();
            arg.val1.Msg = "test";
            arg.val2 = arg.val1;
            System.String newMsg = "test-new";
            TestSerializableClassD result = m_testService.TestChangeSerilizableD(arg, newMsg);
            Assertion.AssertEquals(newMsg, result.val1.Msg);
            Assertion.AssertEquals(result.val1, result.val2);
            Assertion.AssertEquals(result.val1.Msg, result.val2.Msg);
        }
        
        [Test]
        public void TestRecursiveValueType() {            
            TestSerializableClassE arg = new TestSerializableClassE();
            arg.RecArrEntry = new TestSerializableClassE[1];
            arg.RecArrEntry[0] = arg;
            TestSerializableClassE result = m_testService.TestEchoSerializableE(arg);
            Assertion.AssertNotNull(result);
            Assertion.AssertNotNull(result.RecArrEntry);
            Assertion.AssertEquals(arg.RecArrEntry.Length, result.RecArrEntry.Length);
            Assertion.Assert("invalid entry in recArrEntry", (result == result.RecArrEntry[0]));
            
        }        

        /// <summary>
        /// Checks if a ByRef actual value for a formal parameter interface is passed correctly
        /// </summary>
        [Test]
        public void TestInterfacePassingByRef() {
            TestEchoInterface result = m_testService.RetrieveEchoInterfaceImplementor();
            // result is a proxy
            Assertion.AssertEquals(true, RemotingServices.IsTransparentProxy(result));
            System.Int32 arg = 23;
            System.Int32 echo = result.EchoInt(arg);
            Assertion.AssertEquals(arg, echo);
        }

        /// <summary>
        /// Checks if a ByVal actual value for a formal parameter interface is passed correctly
        /// </summary>        
        [Test]
        public void TestInterfacePassingByVal() {
            System.String initialMsg = "initial";
            TestInterfaceA result = m_testService.RetrieveTestInterfaceAImplementor(initialMsg);
            Assertion.AssertEquals(initialMsg, result.Msg);

            System.String passedBack = m_testService.ExtractMsgFromInterfaceAImplmentor(result);
            Assertion.AssertEquals(initialMsg, passedBack);
        }

        [Test]
        public void TestInheritanceFromInterfaceForValueType() {
            System.String initialMsg = "initial";
            TestAbstrInterfaceImplByMarshalByVal impl = m_testService.RetriveTestInterfaceAImplemtorTheImpl(initialMsg);            
            Assertion.Assert("cast to Interface TestInterfaceA failed", (impl as TestInterfaceA) != null);
            Assertion.AssertEquals(initialMsg, impl.Msg);
        }

        [Test]
        public void TestWritableProperty() {
            System.Double arg = 1.2;
            m_testService.TestProperty = arg;
            System.Double result = m_testService.TestProperty;
            Assertion.AssertEquals(arg, result);
        }

        [Test]
        public void TestReadOnlyProperty() {
            System.Double result = m_testService.TestReadOnlyPropertyReturningZero;
            Assertion.AssertEquals((System.Double) 0, result);
            PropertyInfo prop = typeof(TestService).GetProperty("TestReadOnlyPropertyReturningZero");
            Assertion.AssertNotNull(prop);
            Assertion.AssertEquals(false, prop.CanWrite);
            Assertion.AssertEquals(true, prop.CanRead);
        }

        [Test]
        public void TestPassingNullForFormalParamObjectAndAny() {
            object arg1 = null;
            object result1 = m_testService.EchoAnything(arg1);
            Assertion.AssertEquals(arg1, result1);
            
            Any any = new Any(null);
            Any result = m_testService.EchoAnythingContainer(any);
            Assertion.AssertEquals(any.Value, result.Value);
        }

        
        /// <summary>
        /// Test passing instances, if formal parameter is System.Object
        /// </summary>
        [Test]
        public void TestPassingForFormalParamObjectSimpleTypes() {
            System.Double arg1 = 1.23;
            System.Double result1 = (System.Double) m_testService.EchoAnything(arg1);
            Assertion.AssertEquals(arg1, result1);

            System.Char arg2 = 'a';
            System.Char result2 = (System.Char) m_testService.EchoAnything(arg2);
            Assertion.AssertEquals(arg2, result2);

            System.Boolean arg3 = true;
            System.Boolean result3 = (System.Boolean) m_testService.EchoAnything(arg3);
            Assertion.AssertEquals(arg3, result3);

            System.Int32 arg4 = 89;
            System.Int32 result4 = (System.Int32) m_testService.EchoAnything(arg4);
            Assertion.AssertEquals(arg4, result4);
        }
        
        [Test]
        public void TestCustomAnyTypeCode() {
            System.String testString = "abcd";
            OrbServices orb = OrbServices.GetSingleton();
            omg.org.CORBA.TypeCode wstringTc = orb.create_wstring_tc(0);
            Any any = new Any(testString, wstringTc);
            System.String echo = (System.String)m_testService.EchoAnything(any);
            Assertion.AssertEquals(testString, echo);
        }

        [Test]
        public void TestPassingForFormalParamObjectComplexTypes() {
            System.String arg1 = "test";
            System.String result1 = (System.String) m_testService.EchoAnything(arg1);
            Assertion.AssertEquals(arg1, result1);
            
            TestSerializableClassB1 arg2 = new TestSerializableClassB1();
            arg2.Msg = "msg";
            TestSerializableClassB1 result2 = (TestSerializableClassB1) m_testService.EchoAnything(arg2);
            Assertion.AssertEquals(arg2.Msg, result2.Msg);
        }

        /// <summary>
        /// Checks if arrays can be passed for formal parameter object.
        /// </summary>
        /// <remarks>
        /// Difficulty here is, that at the server, boxed type may not exist yet for array type and must be created on deserialising
        /// any!
        /// </remarks>
        [Test]
        public void TestPassingForFormalParamObjectArrays() {
            System.Byte[] arg3 = new System.Byte[1];
            arg3[0] = 1;
            System.Byte[] result3 = (System.Byte[]) m_testService.EchoAnything(arg3);
            Assertion.AssertEquals(arg3[0], result3[0]);

            System.Int32[] arg4 = new System.Int32[1];
            arg4[0] = 1;
            System.Int32[] result4 = (System.Int32[]) m_testService.EchoAnything(arg4);
            Assertion.AssertEquals(arg4[0], result4[0]);
        }

        [Test]
        public void TestAnyContainer() {
            System.String testString = "abcd";
            OrbServices orb = OrbServices.GetSingleton();
            omg.org.CORBA.TypeCode wstringTc = orb.create_wstring_tc(0);
            Any any = new Any(testString, wstringTc);
            Any result = m_testService.EchoAnythingContainer(any);
            Assertion.AssertEquals(any.Value, result.Value);
        }

        // this test is a replacement for the next one, until behaviour is decided
        [Test]
        public void TestCallEqualityServerAndProxy() {
            m_testService.CheckEqualityWithServiceV2((TestService)m_testService);
            m_testService.CheckEqualityWithService((MarshalByRefObject)m_testService);
        }
                        
        [Ignore("Not yet decided, what behaviour should be supported by IIOP.NET")]
        [Test]
        public void TestEqualityServerAndProxy() {
            bool result = m_testService.CheckEqualityWithServiceV2((TestService)m_testService);
            Assertion.AssertEquals(true, result);
            result = m_testService.CheckEqualityWithService((MarshalByRefObject)m_testService);
            Assertion.AssertEquals(true, result);
        }

        delegate System.Boolean TestNegateBooleanDelegate(System.Boolean arg);

        [Test]
        public void TestAsyncCall() {
            System.Boolean arg = true;
            TestNegateBooleanDelegate nbd = new TestNegateBooleanDelegate(m_testService.TestNegateBoolean);
            // async call
            IAsyncResult ar = nbd.BeginInvoke(arg, null, null);
            // wait for response
            System.Boolean result = nbd.EndInvoke(ar);
            Assertion.AssertEquals(false, result);
        }

        [Test]
        public void TestRefArgs() {
            System.Int32 argInit = 1;
            System.Int32 arg = argInit;
            System.Int32 result = m_testService.TestRef(ref arg);
            Assertion.AssertEquals(arg, result);
            Assertion.AssertEquals(argInit + 1, arg);
        }

        delegate System.Int32 TestOutArgsDelegate(System.Int32 arg, out System.Int32 argOut);

        [Test]
        public void TestOutArgsMixed() {
            System.Int32 argOut;
            System.Int32 arg = 1;
            System.Int32 result = m_testService.TestOut(arg, out argOut);
            Assertion.AssertEquals(arg, argOut);
            Assertion.AssertEquals(arg, result);

            System.Int32 argOut2;
            TestOutArgsDelegate oad = new TestOutArgsDelegate(m_testService.TestOut);
            // async call
            IAsyncResult ar = oad.BeginInvoke(arg, out argOut2, null, null);
            // wait for response
            System.Int32 result2 = oad.EndInvoke(out argOut2, ar);
            Assertion.AssertEquals(arg, argOut2);
            Assertion.AssertEquals(arg, result2);
        }
        
        [Test]
        public void TestOutArgAlone() {
            System.Int32 result;
            m_testService.Assign5ToOut(out result);
            Assertion.AssertEquals(5, result);
        }
        
        [Test]
        public void TestOverloadedMethods() {
            System.Int32 arg1int = 1;
            System.Int32 arg2int = 2;
            System.Int32 arg3int = 2;

            System.Double arg1double = 1.0;
            System.Double arg2double = 2.0;

            System.Int32 result1 = m_testService.AddOverloaded(arg1int, arg2int);
            Assertion.AssertEquals((System.Int32)(arg1int + arg2int), result1);
            System.Int32 result2 = m_testService.AddOverloaded(arg1int, arg2int, arg3int);
            Assertion.AssertEquals((System.Int32)(arg1int + arg2int + arg3int), result2);
            System.Double result3 = m_testService.AddOverloaded(arg1double, arg2double);
            Assertion.AssertEquals((System.Double)(arg1double + arg2double), result3);
        }

        [Test]
        public void TestNameClashes() {
            System.Int32 arg = 89;
            System.Int32 result = m_testService.custom(arg);
            Assertion.AssertEquals(arg, result);
           
            m_testService.context = arg;
            Assertion.AssertEquals(arg, m_testService.context);
        }

        [Test]
        public void TestNamesStartingWithUnderScore() {
            System.Int32 arg = 99;
            System.Int32 result = m_testService._echoInt(arg);
            Assertion.AssertEquals(arg, result);
        }

        [Test]
        public void TestCheckParamAttrs() {
            System.String arg = "testArg";
            System.String result = m_testService.CheckParamAttrs(arg);
            Assertion.AssertEquals(arg, result);
        }

        [Test]
        public void TestSimpleUnionNoExceptions() {
            TestUnion arg = new TestUnion();
            short case0Val = 11;
            arg.Setval0(case0Val);
            TestUnion result = m_testService.EchoUnion(arg);
            Assertion.AssertEquals(case0Val, result.Getval0());
            Assertion.AssertEquals(0, result.Discriminator);

            TestUnion arg2 = new TestUnion();
            int case1Val = 12;
            arg2.Setval1(case1Val, 2);
            TestUnion result2 = m_testService.EchoUnion(arg2);
            Assertion.AssertEquals(case1Val, result2.Getval1());
            Assertion.AssertEquals(2, result2.Discriminator);

            TestUnion arg3 = new TestUnion();
            bool case2Val = true;
            arg3.Setval2(case2Val, 7);
            TestUnion result3 = m_testService.EchoUnion(arg3);
            Assertion.AssertEquals(case2Val, result3.Getval2());
            Assertion.AssertEquals(7, result3.Discriminator);            

            TestUnionULong arg4 = new TestUnionULong();
            int case1Val2 = 13;
            arg4.Setval1(case1Val2);
            TestUnionULong result4 = m_testService.EchoUnionULong(arg4);
            Assertion.AssertEquals(case1Val2, result4.Getval1());
            uint case1DiscrVal = 0x80000000;
            Assertion.AssertEquals((int)case1DiscrVal, result4.Discriminator);            

        }

        [Test]
        public void TestEnumBasedUnionNoExceptions() {
            TestUnionE arg = new TestUnionE();
            short case0Val = 11;
            arg.SetvalE0(case0Val);
            TestUnionE result = m_testService.EchoUnionE(arg);
            Assertion.AssertEquals(case0Val, result.GetvalE0());
            Assertion.AssertEquals(TestEnumForU.A, result.Discriminator);

        TestUnionE arg2 = new TestUnionE();
            TestEnumForU case1Val = TestEnumForU.A;
            arg2.SetvalE1(case1Val, TestEnumForU.B);
            TestUnionE result2 = m_testService.EchoUnionE(arg2);
            Assertion.AssertEquals(case1Val, result2.GetvalE1());
            Assertion.AssertEquals(TestEnumForU.B, result2.Discriminator);
        }

        [Test]
        public void TestUnionExceptions() {
            try {
                TestUnion arg = new TestUnion();
                arg.Getval0();
                Assertion.Fail("exception not thrown for getting value from non-initalized union");
            } catch (omg.org.CORBA.BAD_OPERATION) {
            }
            try {
                TestUnion arg = new TestUnion();
                arg.Setval0(11);
                arg.Getval1();
                Assertion.Fail("exception not thrown for getting value from non-initalized union");
            } catch (omg.org.CORBA.BAD_OPERATION) {
            }
            try {
                TestUnion arg1 = new TestUnion();
                arg1.Setval1(11, 7);
                Assertion.Fail("exception not thrown on wrong discriminator value.");
            } catch (omg.org.CORBA.BAD_PARAM) {
            }
            try {
                TestUnion arg2 = new TestUnion();
                arg2.Setval2(false, 0);
                Assertion.Fail("exception not thrown on wrong discriminator value.");
            } catch (omg.org.CORBA.BAD_PARAM) {
            }
        }

        [Test]
        public void TestConstantRegression() {
            Int32 constVal = MyConstant.ConstVal;
            Assertion.AssertEquals("wrong constant value", 11, constVal);
            
            Int64 maxIntVal = Max_int.ConstVal;
            Assertion.AssertEquals("wrong constant value", 
                                   Int64.MaxValue, maxIntVal);            
            
            // regression test for BUG #909562
            Int64 minIntVal = Min_int.ConstVal;
            Assertion.AssertEquals("wrong constant value", 
                                   Int64.MinValue, minIntVal);
                                              
            Int64 zeroIntVal = Zero_val.ConstVal;
            Assertion.AssertEquals("wrong constant value", 0, zeroIntVal);

            Int64 zeroFromHex = Zero_from_hex.ConstVal;             
            Assertion.AssertEquals("wrong constant value", 0, zeroFromHex);
            
            Int64 oneFromHex = One_from_hex.ConstVal;           
            Assertion.AssertEquals("wrong constant value", 1, oneFromHex);
            
            Int64 minusOneFromHex = Minus_one_from_hex.ConstVal;            
            Assertion.AssertEquals("wrong constant value", -1, minusOneFromHex);
    
            Single zeroValFloat = Zero_val_float.ConstVal;
            Assertion.AssertEquals("wrong constant value", 0.0, zeroValFloat);
    
            Single minusOneFloat = Minus_one_float.ConstVal;
            Assertion.AssertEquals("wrong constant value", -1.0, minusOneFloat);
            
            Single plusOneFloat = Plus_one_float.ConstVal;
            Assertion.AssertEquals("wrong constant value", 1.0, plusOneFloat);
            
            Single plus_inf = Plus_Inf.ConstVal;
            Assertion.AssertEquals("wrong constant value", Single.PositiveInfinity, plus_inf);
            Single minus_inf = Minus_Inf.ConstVal;
            Assertion.AssertEquals("wrong constant value", Single.NegativeInfinity, minus_inf);

            UInt16 expectedValUShort = 0x8000;
            Assertion.AssertEquals("wrong constant value", (Int16)expectedValUShort, UShort_BiggerThanShort.ConstVal);
            UInt32 expectedValULong = 0x80000000;
            Assertion.AssertEquals("wrong constant value", (Int32)expectedValULong, ULong_BiggerThanLong.ConstVal);
            UInt64 expectedValULongLong = 0x8000000000000000;
            Assertion.AssertEquals("wrong constant value", (Int64)expectedValULongLong, ULongLong_BiggerThanLongLong.ConstVal);
            
        }

        [Test]
        public void TestConstantAllKinds() {
            Assertion.AssertEquals("wrong const val short", -29, A_SHORT_CONST.ConstVal);
            Assertion.AssertEquals("wrong const val short other const", 
                                   A_SHORT_CONST.ConstVal, VAL_OF_A_SHORT_CONST.ConstVal);
            Assertion.AssertEquals("wrong const val long", 30, A_LONG_CONST.ConstVal);
            Assertion.AssertEquals("wrong const val long long", -31, A_LONG_LONG_CONST.ConstVal);

            Assertion.AssertEquals("wrong const val ushort", 81, A_UNSIGNED_SHORT_CONST.ConstVal);
            Assertion.AssertEquals("wrong const val ulong", 101, A_UNSIGNED_LONG_CONST.ConstVal);
            Assertion.AssertEquals("wrong const val ulong long", 102, A_UNSIGNED_LONG_LONG_CONST.ConstVal);

            Assertion.AssertEquals("wrong const val char", 'C', A_CHAR_CONST.ConstVal);
            Assertion.AssertEquals("wrong const val char other const", 
                                   A_CHAR_CONST.ConstVal, VAL_OF_A_CHAR_CONST.ConstVal);

            Assertion.AssertEquals("wrong const val wchar", 'D', A_WCHAR_CONST.ConstVal);

            Assertion.AssertEquals("wrong const val boolean true", true, A_BOOLEAN_CONST_TRUE.ConstVal);
            Assertion.AssertEquals("wrong const val boolean false", false, A_BOOLEAN_CONST_FALSE.ConstVal);

            Assertion.AssertEquals("wrong const val float", (Single)1.1, A_FLOAT_CONST.ConstVal);
            Assertion.AssertEquals("wrong const val double", (Double)6.7E8, A_DOUBLE_CONST.ConstVal);

            Assertion.AssertEquals("wrong const val string", "test", A_STRING_CONST.ConstVal);
            Assertion.AssertEquals("wrong const val string bounded", 
                                   "test-b", A_STRING_CONST_BOUNDED.ConstVal);

            Assertion.AssertEquals("wrong const val wstring", "w-test", A_WSTRING_CONST.ConstVal);
            Assertion.AssertEquals("wrong const val wstring bounded", 
                                   "w-test-b", A_WSTRING_CONST_BOUNDED.ConstVal);

            Assertion.AssertEquals("wrong const val typedef long", 10, 
                                   SCOPED_NAME_CONST_LONGTD.ConstVal);

            Assertion.AssertEquals("wrong const val enum", A_ENUM_FOR_CONST.CV1, 
                                   SCOPED_NAME_CONST_ENUM.ConstVal);

            Assertion.AssertEquals("wrong const val octet", 8, A_OCTET_CONST.ConstVal);
        }

        [Test]
        public void TestConstValueAndSwitch() {
            // check, if switch is possbile with constant values
            int testValue = A_LONG_CONST.ConstVal;
            switch(testValue) {
                case A_LONG_CONST.ConstVal:
                    // ok
                    break;
                default:
                    Assertion.Fail("wrong value: " + testValue + "; should be: " + A_LONG_CONST.ConstVal);
                    break;
            }            
        }

        [Test]
        public void TestPassingUnionsAsAny() {
            TestUnion arg = new TestUnion();
            short case0Val = 11;
            arg.Setval0(case0Val);
            TestUnion result = (TestUnion)m_testService.EchoAnything(arg);
            Assertion.AssertEquals(case0Val, result.Getval0());
            Assertion.AssertEquals(0, result.Discriminator);

        TestUnionE arg2 = new TestUnionE();
            TestEnumForU case1Val = TestEnumForU.A;
            arg2.SetvalE1(case1Val, TestEnumForU.B);
            TestUnionE result2 = (TestUnionE)m_testService.EchoAnything(arg2);
            Assertion.AssertEquals(case1Val, result2.GetvalE1());
            Assertion.AssertEquals(TestEnumForU.B, result2.Discriminator);
        }

        [Test]
        public void TestReceivingUnknownUnionsAsAny() {
        object result = m_testService.RetrieveUnknownUnionAsAny();
            Assertion.AssertNotNull("union not retrieved", result);
            Assertion.AssertEquals("type name", "Ch.Elca.Iiop.IntegrationTests.TestUnionE2", result.GetType().FullName);
        }


        [Test]
        public void TestReferenceOtherConstant() {
            Int32 constValA = AVAL.ConstVal;
            Assertion.AssertEquals("wrong constant value", 1, constValA);
            Int32 constValB = BVAL.ConstVal;
            Assertion.AssertEquals("wrong constant value", 1, constValB);
        }


        [Test]
        public void TestCharacterConstant() {
            Char constValNonEscapeCharConst = NonEscapeCharConst.ConstVal;
            Assertion.AssertEquals("wrong constant value", 'a', constValNonEscapeCharConst);

            Char constValUnicodeEscapeCharConst1 = UnicodeEscapeCharConst1.ConstVal;
            Assertion.AssertEquals("wrong constant value", '\u0062', constValUnicodeEscapeCharConst1);

            Char constValUnicodeEscapeCharConst2 = UnicodeEscapeCharConst2.ConstVal;
            Assertion.AssertEquals("wrong constant value", '\uFFFF', constValUnicodeEscapeCharConst2);

            Char constValHexEscapeCharConst = HexEscapeCharConst.ConstVal;
            Assertion.AssertEquals("wrong constant value", '\u0062', constValHexEscapeCharConst);

            Char constValDecEscapeCharConst1 = DecEscapeCharConst1.ConstVal;
            Assertion.AssertEquals("wrong constant value", 'a', constValDecEscapeCharConst1);

            Char constValDecEscapeCharConst2 = DecEscapeCharConst2.ConstVal;
            Assertion.AssertEquals("wrong constant value", '\u0000', constValDecEscapeCharConst2);
        }

        [Test]
        public void TestCharacterConstantBugReport841774() {
            Char constValStandAlone = STAND_ALONE_TEST.ConstVal;
            Assertion.AssertEquals("wrong constant value", '1', constValStandAlone);
            Char constValNetWork = NETWORK_TEST.ConstVal;
            Assertion.AssertEquals("wrong constant value", '2', constValNetWork);
            Char constValProduction = PRODUCTION.ConstVal;
            Assertion.AssertEquals("wrong constant value", '3', constValProduction);
        }

        [Test]
        public void TestWstringLiteralBugReport906401() {
            String val_a = COMP_NAME_A.ConstVal;
            Assertion.AssertEquals("wrong constant value", "test", val_a);
            String val_b = COMP_NAME_B.ConstVal;
            Assertion.AssertEquals("wrong constant value", "java:comp/env/ejb/Fibo", val_b);
        }

        /// <summary>checks, if channel uses is_a to check interface compatiblity on IOR deser,
        /// if other checks don't work</summary>
        [Test]
        public void TestInterfaceCompMbrDeser() {
        TestSimpleInterface1 proxy1 = (TestSimpleInterface1)m_testService.GetSimpleService1();
            Assertion.AssertNotNull("testSimpleService1 ref not received", proxy1);
            Assertion.AssertEquals(true, proxy1.ReturnTrue());

        TestSimpleInterface2 proxy2 = (TestSimpleInterface2)m_testService.GetSimpleService2();
            Assertion.AssertNotNull("testSimpleService2 ref not received", proxy2);
            Assertion.AssertEquals(false, proxy2.ReturnFalse());

            TestSimpleInterface1 proxy3 = (TestSimpleInterface1)m_testService.GetWhenSuppIfMissing();
            Assertion.AssertNotNull("testSimpleService1 ref not received", proxy3);
            Assertion.AssertEquals(true, proxy3.ReturnTrue());
                        
        }

        [Test]
        public void TestIsACall() {
            omg.org.CORBA.IObject proxy1 = (omg.org.CORBA.IObject)m_testService.GetSimpleService1();
            Assertion.AssertNotNull("testSimpleService1 ref not received", proxy1);            
            Assertion.AssertEquals(true, proxy1._is_a("IDL:Ch/Elca/Iiop/IntegrationTests/TestSimpleInterface1:1.0"));
            
            omg.org.CORBA.IObject proxy2 = (omg.org.CORBA.IObject)m_testService.GetSimpleService2();
            Assertion.AssertNotNull("testSimpleService2 ref not received", proxy2);
            Assertion.AssertEquals(true, proxy2._is_a("IDL:Ch/Elca/Iiop/IntegrationTests/TestSimpleInterface2:1.0"));
            
            
            // test using ORBServices
            omg.org.CORBA.OrbServices orb = omg.org.CORBA.OrbServices.GetSingleton();
            Assertion.AssertEquals(true, orb.is_a(proxy1, typeof(TestSimpleInterface1)));
            Assertion.AssertEquals(true, orb.is_a(proxy2, typeof(TestSimpleInterface2)));
            // target object implements both interfaces
            Assertion.AssertEquals(true, orb.is_a(proxy1, typeof(TestSimpleInterface2)));
            Assertion.AssertEquals(true, orb.is_a(proxy2, typeof(TestSimpleInterface1)));
            
            Assertion.AssertEquals(false, orb.is_a(m_testService, typeof(TestSimpleInterface1)));
            Assertion.AssertEquals(false, orb.is_a(m_testService, typeof(TestSimpleInterface2)));
        }
        
        [Test]
        public void TestNonExistentCall() {         
            // test using ORBServices
            omg.org.CORBA.OrbServices orb = omg.org.CORBA.OrbServices.GetSingleton();

            Assertion.AssertEquals(false, orb.non_existent(m_testService));
            object nonExObject = orb.string_to_object("iiop://localhost:8087/someNonExistingObject");
            Assertion.AssertEquals(true, orb.non_existent(nonExObject));
        }
        
        [Test]
        public void TestUserIdForMbr() {
            string id = "myAdderId";
            Adder adder = m_testService.CreateNewWithUserID(id);
            string marshalUrl = RemotingServices.GetObjectUri(adder);
            Ior adderIor = new Ior(marshalUrl);
            IInternetIiopProfile prof = adderIor.FindInternetIiopProfile();
            byte[] objectKey = prof.ObjectKey;
            ASCIIEncoding enc = new ASCIIEncoding();
            string marshalUri = new String(enc.GetChars(objectKey));
            Assertion.AssertEquals("wrong user id", id, marshalUri);
            
            // check if callable
            int arg1 = 1;
            int arg2 = 2;
            Assertion.AssertEquals("wrong adder result", arg1 + arg2, adder.Add(arg1, arg2));
        }
        
        [Test]
        public void TestSystemIdForMbr() {
            Adder adder = m_testService.CreateNewWithSystemID();
            string marshalUrl = RemotingServices.GetObjectUri(adder);
            Ior adderIor = new Ior(marshalUrl);
            IInternetIiopProfile prof = adderIor.FindInternetIiopProfile();
            byte[] objectKey = prof.ObjectKey;
            ASCIIEncoding enc = new ASCIIEncoding();
            string marshalUri = new String(enc.GetChars(objectKey));            
            if (marshalUri.StartsWith("/")) {
                marshalUri = marshalUri.Substring(1);
            }
            Assertion.Assert("no appdomain-guid", marshalUri.IndexOf("/") > 0);
            string guid_string = marshalUri.Substring(0, marshalUri.IndexOf("/"));
            guid_string = guid_string.Replace("_", "-");
            try {
                Guid guid = new Guid(guid_string);
            } catch (Exception ex) {
                Assertion.Fail("guid not in uri: " + ex);
            }            
            
            // check if callable
            int arg1 = 1;
            int arg2 = 2;
            Assertion.AssertEquals("wrong adder result", arg1 + arg2, adder.Add(arg1, arg2));
        }

        [Test]
        public void TestObjectToString() {
            OrbServices orbServices = OrbServices.GetSingleton();
            string id = "myAdderId2";
            Adder adder = m_testService.CreateNewWithUserID(id);
            string iorString = orbServices.object_to_string(adder);
            Ior adderIor = new Ior(iorString);
            IInternetIiopProfile prof = adderIor.FindInternetIiopProfile();
            Assertion.AssertEquals(8087, prof.Port);            
            Assertion.AssertEquals(1, prof.Version.Major);
            Assertion.AssertEquals(2, prof.Version.Minor);
            
            byte[] oid = { 0x6d, 0x79, 0x41, 0x64, 0x64, 0x65, 0x72, 0x49, 0x64, 0x32 };
            CheckIorKey(oid, prof.ObjectKey);                        

            string testServiceIorString = m_testService.GetIorStringForThisObject();
            Ior testServiceIor = new Ior(testServiceIorString);
            IInternetIiopProfile profSvcIor = testServiceIor.FindInternetIiopProfile();
            Assertion.AssertEquals(8087, profSvcIor.Port);
            Assertion.AssertEquals(1, profSvcIor.Version.Major);
            Assertion.AssertEquals(2, profSvcIor.Version.Minor);            
            
            byte[] oidTestService = { 0x74, 0x65, 0x73, 0x74 };
            CheckIorKey(oidTestService, profSvcIor.ObjectKey);                        


        }

        private void CheckIorKey(byte[] expected, byte[] actual) {
            Assertion.AssertEquals("wrong id length", expected.Length, actual.Length);
            for (int i = 0; i <expected.Length; i++) {
                Assertion.AssertEquals("wrong element nr " + i, expected[i], actual[i]);
            }
        }
        
        [Test]
        public void TestIdsIncludingNonAscii() {
            string id = "myAdderId" + '\u0765' + "1" + @"\uA";
            string expectedMarshalledId = @"myAdderId\u07651\\uA";
            Adder adder = m_testService.CreateNewWithUserID(id);
            string marshalUrl = RemotingServices.GetObjectUri(adder);
            Ior adderIor = new Ior(marshalUrl);
            IInternetIiopProfile prof = adderIor.FindInternetIiopProfile();
            byte[] objectKey = prof.ObjectKey;
            ASCIIEncoding enc = new ASCIIEncoding();
            string marshalUri = new String(enc.GetChars(objectKey));
            Assertion.AssertEquals("wrong user id", expectedMarshalledId, marshalUri);
            
            // check if callable
            int arg1 = 1;
            int arg2 = 2;
            Assertion.AssertEquals("wrong adder result", arg1 + arg2, adder.Add(arg1, arg2));
        }

        [Test]
        public void TestErrorReportBadOperation() {
            m_testService.GetAllUsagerType();
        }

        [Test]
        public void TestSystemType() {
            Type arg1 = typeof(System.Int32);
            Type result1 = m_testService.EchoType(arg1);
            Assertion.AssertEquals("wrong type for int32 echo", arg1, result1);

            Type arg2 = typeof(System.Boolean);
            Type result2 = m_testService.EchoType(arg2);
            Assertion.AssertEquals("wrong type for Boolean echo", arg2, result2);

            Type arg3 = typeof(TestService);
            Type result3 = m_testService.EchoType(arg3);
            Assertion.AssertEquals("wrong type for testService type echo", arg3, result3);            

            Type arg4 = null;
            Type result4 = m_testService.EchoType(arg4);
            Assertion.AssertEquals("wrong type for null type echo", arg4, result4);            
        }

        [Test]
        public void TestUnionNetSerializableOptimized() {
            // check, that the generated union is serializable also with other formatters optimized (not all fields, but only needed ones)
            TestUnion arg = new TestUnion();
            int case1Val = 12;
            arg.Setval1(case1Val, 2);

            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter =
                new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            System.IO.MemoryStream serialised = new System.IO.MemoryStream();
            try {
                formatter.Serialize(serialised, arg);

                serialised.Seek(0, System.IO.SeekOrigin.Begin);
                formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                TestUnion deser = (TestUnion)formatter.Deserialize(serialised);

                Assertion.AssertEquals(2, deser.Discriminator);
                Assertion.AssertEquals(case1Val, deser.Getval1());
            } finally {
                serialised.Close();
            }
        }        

    }

}
