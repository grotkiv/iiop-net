/* TestServiceCommon.cs
 *
 * Project: IIOP.NET
 * IntegrationTests
 *
 * WHEN      RESPONSIBLE
 * 25.07.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using Ch.Elca.Iiop.Idl;

namespace Ch.Elca.Iiop.IntegrationTests {

    public enum TestEnum {
        A, B, C, D
    }

    [Serializable]
    public struct TestStructA {
        public System.Int32 X;
        public System.Int32 Y;
    }

    [Serializable]
    public class TestSerializableClassB1 {
        public System.String Msg;
    }
    
    [Serializable]
    public class TestSerializableClassB2 : TestSerializableClassB1 {
        public System.String DetailedMsg;
    }

    public abstract class TestNonSerializableBaseClass {
        public abstract System.String Format();
    }

    [Serializable]
    public class TestSerializableClassC : TestNonSerializableBaseClass {
        
        public String Msg;
        
        public override System.String Format() {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class TestSerializableClassD {
        public TestSerializableClassB1 val1;
        public TestSerializableClassB1 val2;
    }


    public class Adder : MarshalByRefObject {
        public System.Int32 Add(System.Int32 sum1, System.Int32 sum2) {
            return sum1 + sum2;
        }
    }

    public interface TestEchoInterface {
        System.Int32 EchoInt(System.Int32 arg);
    }

    public interface TestInterfaceA {
        System.String Msg {
            get;
        }
    }

    [Serializable]
    public class TestAbstrInterfaceImplByMarshalByVal : TestInterfaceA {
        
        private System.String m_msg = "standard";

        public TestAbstrInterfaceImplByMarshalByVal() {
        }

        public TestAbstrInterfaceImplByMarshalByVal(System.String msg) {
            m_msg = msg;
        }

        public System.String Msg {
            get {
                return m_msg;
            }
        }

    }

    public interface TestService {
       
        System.Double TestProperty {
            get;
            set;
        }
        
        System.Double TestReadOnlyPropertyReturningZero {
            get;
        }
        
        System.Double TestIncDouble(System.Double arg);

        System.Single TestIncFloat(System.Single arg);

        System.Byte TestIncByte(System.Byte arg);

        System.Int16 TestIncInt16(System.Int16 arg);

        System.Int32 TestIncInt32(System.Int32 arg);

        System.Int64 TestIncInt64(System.Int64 arg);

        System.Boolean TestNegateBoolean(System.Boolean arg);

        void TestVoid();
        
        System.Char TestEchoChar(System.Char arg);

        System.String TestAppendString(System.String basic, System.String toAppend);

        TestEnum TestEchoEnumVal(TestEnum arg);

        System.Byte[] TestAppendElementToByteArray(System.Byte[] arg, System.Byte toAppend);

        System.String[] TestAppendElementToStringArray(System.String[] arg, System.String toAppend);

        System.String[] CreateTwoElemStringArray(System.String arg1, System.String arg2);
        
        System.Int32[][] EchoJaggedIntArray(System.Int32[][] arg);
        
        System.String[][] EchoJaggedStringArray(System.String[][] arg);
        
        System.Byte[][][] EchoJaggedByteArray(System.Byte[][][] arg);
        
        System.Int32[,] EchoMultiDimIntArray(System.Int32[,] arg);
        
        System.Byte[,,] EchoMultiDimByteArray(System.Byte[,,] arg);
        
        System.String[,] EchoMultiDimStringArray(System.String[,] arg);

        Adder RetrieveAdder();

        System.Int32 AddWithAdder(Adder adder, System.Int32 sum1, System.Int32 sum2);

        TestStructA TestEchoStruct(TestStructA arg);

        TestSerializableClassB2 TestChangeSerializableB2(TestSerializableClassB2 arg, System.String detail);

        TestSerializableClassC TestEchoSerializableC(TestSerializableClassC arg);
        
        TestNonSerializableBaseClass TestAbstractValueTypeEcho(TestNonSerializableBaseClass arg);

        TestSerializableClassD TestChangeSerilizableD(TestSerializableClassD arg, System.String newMessage);

        TestEchoInterface RetrieveEchoInterfaceImplementor();

        TestInterfaceA RetrieveTestInterfaceAImplementor(System.String initialMsg);

        System.String ExtractMsgFromInterfaceAImplmentor(TestInterfaceA arg);

        TestAbstrInterfaceImplByMarshalByVal RetriveTestInterfaceAImplemtorTheImpl(System.String initialMsg);

        object EchoAnything(object arg);
        
        /// <summary>
        /// used to check, if a reference passed is equal to this object itself.
        /// </summary>
        bool CheckEqualityWithService(MarshalByRefObject toCheck);
        
        bool CheckEqualityWithServiceV2(TestService toCheck);
        
    }

}

