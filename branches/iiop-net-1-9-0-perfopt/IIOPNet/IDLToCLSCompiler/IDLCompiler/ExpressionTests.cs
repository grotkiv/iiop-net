/* ExpressionTests.cs
 * 
 * Project: IIOP.NET
 * IDLToCLSCompiler
 * 
 * WHEN      RESPONSIBLE
 * 01.08.06  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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

#if UnitTest

namespace Ch.Elca.Iiop.IdlCompiler.Tests {
	
    using System;
    using System.Reflection;
    using System.Collections;
    using System.IO;
    using System.Text;
    using NUnit.Framework;
    using Ch.Elca.Iiop;
    using Ch.Elca.Iiop.Idl;
    using parser;
    using Ch.Elca.Iiop.IdlCompiler.Action;
    using Ch.Elca.Iiop.IdlCompiler.Exceptions;
    using Ch.Elca.Iiop.Util;

    /// <summary>
    /// Unit-tests for testing generation
    /// for IDL constant expressions.
    /// </summary>
    [TestFixture]
    public class ExpressionTest : CompilerTestsBase {
                
        private StreamWriter m_writer;
        
        [SetUp]
        public void SetUp() {
            MemoryStream testSource = new MemoryStream();
            m_writer = CreateSourceWriter(testSource);
        }
        
        [TearDown]
        public void TearDown() {
            m_writer.Close();
        }
        
        private void CheckConstantValue(string constTypeName, Assembly asm,
                                          object expected) {
            Type constType = asm.GetType(constTypeName, false);
            Assertion.AssertNotNull("const type null?", constType);            
            FieldInfo field = constType.GetField("ConstVal", BindingFlags.Public | BindingFlags.Static);
            Assertion.AssertNotNull("const field", field);
            Assertion.AssertEquals("field value", expected, field.GetValue(null));
        }
       
        [Test]
        public void TestAddInteger() {            
            // idl:
            m_writer.WriteLine("module testmod {");
            m_writer.WriteLine("const long TestAddInteger = 1 + 2;");
            m_writer.WriteLine("};");
            m_writer.Flush();
            m_writer.BaseStream.Seek(0, SeekOrigin.Begin);
            Assembly result = 
                CreateIdl(m_writer.BaseStream, GetAssemblyName("ExpressionTest_TestAddInteger"));
                                   
            CheckConstantValue("testmod.TestAddInteger", result, (int)3);
        }
        
        [Test]
        public void TestAddFloat() {            
            // idl:
            m_writer.WriteLine("module testmod {");
            m_writer.WriteLine("const double TestAddFloat = 1.0 + 2.0;");
            m_writer.WriteLine("};");
            m_writer.Flush();
            m_writer.BaseStream.Seek(0, SeekOrigin.Begin);
            Assembly result = 
                CreateIdl(m_writer.BaseStream, GetAssemblyName("ExpressionTest_TestAddFloat"));
                                   
            CheckConstantValue("testmod.TestAddFloat", result, (double)3);
        }        
        
        [Test]
        [ExpectedException(typeof(InvalidOperandInExpressionException))]
        public void TestAddFloatAndInt() {
            // idl:
            m_writer.WriteLine("module testmod {");
            m_writer.WriteLine("const double TestAddFloatAndInt = 1.0 + 2;");
            m_writer.WriteLine("};");
            m_writer.Flush();
            m_writer.BaseStream.Seek(0, SeekOrigin.Begin);
            Assembly result = 
                CreateIdl(m_writer.BaseStream, GetAssemblyName("ExpressionTest_TestAddFloatAndInt"));            
        }                
        
        [Test]
        [ExpectedException(typeof(InvalidOperandInExpressionException))]
        public void TestAddIntAndFloat() {
            // idl:
            m_writer.WriteLine("module testmod {");
            m_writer.WriteLine("const long TestAddIntAndFloat = 1 + 2.0;");
            m_writer.WriteLine("};");
            m_writer.Flush();
            m_writer.BaseStream.Seek(0, SeekOrigin.Begin);
            Assembly result = 
                CreateIdl(m_writer.BaseStream, GetAssemblyName("ExpressionTest_TestAddIntAndFloat"));            
        }

        [Test]
        public void TestSubInteger() {            
            // idl:
            m_writer.WriteLine("module testmod {");
            m_writer.WriteLine("const long TestSubInteger = 2 - 1;");
            m_writer.WriteLine("};");
            m_writer.Flush();
            m_writer.BaseStream.Seek(0, SeekOrigin.Begin);
            Assembly result = 
                CreateIdl(m_writer.BaseStream, GetAssemblyName("ExpressionTest_TestSubInteger"));
                                   
            CheckConstantValue("testmod.TestSubInteger", result, (int)1);
        }
        
        [Test]
        public void TestSubFloat() {            
            // idl:
            m_writer.WriteLine("module testmod {");
            m_writer.WriteLine("const double TestSubFloat = 2.0 - 1.0;");
            m_writer.WriteLine("};");
            m_writer.Flush();
            m_writer.BaseStream.Seek(0, SeekOrigin.Begin);
            Assembly result = 
                CreateIdl(m_writer.BaseStream, GetAssemblyName("ExpressionTest_TestSubFloat"));
                                   
            CheckConstantValue("testmod.TestSubFloat", result, (double)1);
        }        
        
        [Test]
        [ExpectedException(typeof(InvalidOperandInExpressionException))]
        public void TestSubFloatAndInt() {
            // idl:
            m_writer.WriteLine("module testmod {");
            m_writer.WriteLine("const double TestSubFloatAndInt = 2.0 - 1;");
            m_writer.WriteLine("};");
            m_writer.Flush();
            m_writer.BaseStream.Seek(0, SeekOrigin.Begin);
            Assembly result = 
                CreateIdl(m_writer.BaseStream, GetAssemblyName("ExpressionTest_TestSubFloatAndInt"));            
        }                
        
        [Test]
        [ExpectedException(typeof(InvalidOperandInExpressionException))]
        public void TestSubIntAndFloat() {
            // idl:
            m_writer.WriteLine("module testmod {");
            m_writer.WriteLine("const long TestSubIntAndFloat = 2 - 1.0;");
            m_writer.WriteLine("};");
            m_writer.Flush();
            m_writer.BaseStream.Seek(0, SeekOrigin.Begin);
            Assembly result = 
                CreateIdl(m_writer.BaseStream, GetAssemblyName("ExpressionTest_TestSubIntAndFloat"));            
        }
        
        
    
    }
    
    
}

#endif
