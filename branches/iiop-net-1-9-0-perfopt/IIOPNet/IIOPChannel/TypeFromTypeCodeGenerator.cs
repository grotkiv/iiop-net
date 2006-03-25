/* TypeFromTypeCodeGenerator.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 11.07.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.Reflection;
using System.Reflection.Emit;
using omg.org.CORBA;


namespace Ch.Elca.Iiop.Idl {

    /// <summary>
    /// generates type from a type-code, if the type represented by the type-code is not known!
    /// </summary>
    internal class TypeFromTypeCodeRuntimeGenerator {
        
        #region SFields

        private static TypeFromTypeCodeRuntimeGenerator s_runtimeGen = new TypeFromTypeCodeRuntimeGenerator();

        #endregion SFields
        #region IFields
            
        private AssemblyBuilder m_asmBuilder;
        private ModuleBuilder m_modBuilder;

        #endregion IFields
        #region IConstructors
        
        private TypeFromTypeCodeRuntimeGenerator() {
            Initalize();
        }

        #endregion IConstructors
        #region SMethods
        
        public static TypeFromTypeCodeRuntimeGenerator GetSingleton() {
            return s_runtimeGen;
        }

        #endregion SMethods
        #region IMethods

        private void Initalize() {
            AssemblyName asmname = new AssemblyName();
            asmname.Name = "dynTypeCode.dll";        
            m_asmBuilder = System.Threading.Thread.GetDomain().
                DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            m_modBuilder = m_asmBuilder.DefineDynamicModule("typecodeTypes");            
        }

        /// <summar>
        /// check if the type with the name fullname is defined among the generated types. 
        /// If so, return the type
        /// </summary>
        internal Type RetrieveType(string fullname) {
            return m_modBuilder.GetType(fullname);
        }

        /// <summary>
        /// create the type with name fullname, described by forTypeCode!
        /// </summary>
        /// <param name="fullname"></param>
        /// <param name="forTypeCode"></param>
        /// <returns></returns>
        internal Type CreateOrGetType(string fullname, TypeCodeImpl forTypeCode) {
            lock(this) {
                Type result = RetrieveType(fullname);
                if (result == null) {
                    result = forTypeCode.CreateType(m_modBuilder, fullname);
                    Repository.RegisterDynamicallyCreatedType(result);
                }
                return result;
            }
        }

        #endregion IMethods

    }

}


#if UnitTest

namespace Ch.Elca.Iiop.Tests {
    
    using System;
    using System.Reflection;
    using NUnit.Framework;
    using omg.org.CORBA;
    using Ch.Elca.Iiop.Idl;
    
    /// <summary>
    /// Unit-tests for testing type from typecode code generation for value types.
    /// </summary>
    [TestFixture]
    public class TypeFromTypeCodeGeneratorValueTypeTest {
    	
    	private TypeFromTypeCodeRuntimeGenerator m_gen;
    	
    	[SetUp]
    	public void SetUp() {
    		m_gen = TypeFromTypeCodeRuntimeGenerator.GetSingleton();
    	}
    	
    	[Test]
    	public void TestGenerateSimple() {
    		string name = "TestGenForTypeCodeType";
    		string typeName = "Ch.Elca.Iiop.Tests." + name;
    		string repId = "IDL:Ch/Elca/Iiop/Tests/TestGenForTypeCodeType:1.0";
    		ValueTypeTC vt = new ValueTypeTC(repId,
    		                                 name, new ValueTypeMember[0],
    		                                 new NullTC(), 0);
    		
    		Type res = m_gen.CreateOrGetType(typeName, vt);
    		Assertion.AssertNotNull(res);
            Assertion.AssertEquals("type name", typeName, res.FullName);
    		Assertion.AssertEquals("rep id", repId, Repository.GetRepositoryID(res));
    	}
    	
    	[Test]
    	public void TestGenerateSpecialVersionRepId() {
    		string name = "TestGenForTypeCodeType2";
    		string typeName = "Ch.Elca.Iiop.Tests." + name;
    		string repId = "IDL:Ch/Elca/Iiop/Tests/TestGenForTypeCodeType2:2.0";
    		ValueTypeTC vt = new ValueTypeTC(repId,
    		                                 name, new ValueTypeMember[0],
    		                                 new NullTC(), 0);
    		
    		Type res = m_gen.CreateOrGetType(typeName, vt);
    		Assertion.AssertNotNull(res);
            Assertion.AssertEquals("type name", typeName, res.FullName);
    		Assertion.AssertEquals("rep id", repId, Repository.GetRepositoryID(res));
    	}
    	
    	[Test]
    	public void TestGenerateSpecialNameRepId() {
    	    string name = "TestGenForTypeCodeType3";
    		string typeName = "Ch.Elca.Iiop.Tests." + name;
    		string repId = "IDL:Ch/Elca/Iiop/Tests/Special_TestGenForTypeCodeType3:1.0";
    		ValueTypeTC vt = new ValueTypeTC(repId,
    		                                 name, new ValueTypeMember[0],
    		                                 new NullTC(), 0);
    		
    		Type res = m_gen.CreateOrGetType(typeName, vt);
    		Assertion.AssertNotNull(res);
    		Assertion.AssertEquals("type name", typeName, res.FullName);
    		Assertion.AssertEquals("rep id", repId, Repository.GetRepositoryID(res));
    	}

    	
    }
    
    /// <summary>
    /// Unit-tests for testing type from typecode code generation for boxed value types.
    /// </summary>
    [TestFixture]
    public class TypeFromTypeCodeGeneratorBoxedValueTypeTest {

    	private TypeFromTypeCodeRuntimeGenerator m_gen;
    	
    	[SetUp]
    	public void SetUp() {
    		m_gen = TypeFromTypeCodeRuntimeGenerator.GetSingleton();
    	}
    	
    	[Test]
    	public void TestGenerateSimple() {
    		string name = "TestBoxedGenForTypeCodeType";
    		string typeName = "Ch.Elca.Iiop.Tests." + name;
    		string repId = "IDL:Ch/Elca/Iiop/Tests/TestBoxedGenForTypeCodeType:1.0";
    		LongTC boxedTC = new LongTC();
    		ValueBoxTC vt = new ValueBoxTC(repId,
    		                               name,
    		                               boxedTC);
    		
    		Type res = m_gen.CreateOrGetType(typeName, vt);
    		Assertion.AssertNotNull(res);
            Assertion.AssertEquals("type name", typeName, res.FullName);
    		Assertion.AssertEquals("rep id", repId, Repository.GetRepositoryID(res));
    	}    	
    	
    	[Test]
    	public void TestGenerateSpecialVersionRepId() {
    		string name = "TestBoxedGenForTypeCodeType2";
    		string typeName = "Ch.Elca.Iiop.Tests." + name;
    		string repId = "IDL:Ch/Elca/Iiop/Tests/TestBoxedGenForTypeCodeType:2.0";
    		LongTC boxedTC = new LongTC();
    		ValueBoxTC vt = new ValueBoxTC(repId,
    		                               name,
    		                               boxedTC);
    		
    		Type res = m_gen.CreateOrGetType(typeName, vt);
    		Assertion.AssertNotNull(res);
            Assertion.AssertEquals("type name", typeName, res.FullName);
    		Assertion.AssertEquals("rep id", repId, Repository.GetRepositoryID(res));
    	}
    	
    	[Test]
    	public void TestGenerateSpecialNameRepId() {
    		string name = "TestBoxedGenForTypeCodeType3";
    		string typeName = "Ch.Elca.Iiop.Tests." + name;
    		string repId = "IDL:Ch/Elca/Iiop/Tests/Special_TestBoxedGenForTypeCodeType:1.0";
    		LongTC boxedTC = new LongTC();
    		ValueBoxTC vt = new ValueBoxTC(repId,
    		                               name,
    		                               boxedTC);
    		
    		Type res = m_gen.CreateOrGetType(typeName, vt);
    		Assertion.AssertNotNull(res);
            Assertion.AssertEquals("type name", typeName, res.FullName);
    		Assertion.AssertEquals("rep id", repId, Repository.GetRepositoryID(res));
    	}        
        
    }
}

#endif
