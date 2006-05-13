/* Tests.cs
 * 
 * Project: IIOP.NET
 * IDLToCLSCompiler
 * 
 * WHEN      RESPONSIBLE
 * 28.09.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
    /// Unit-tests for testing assembly generation
    /// for IDL
    /// </summary>
    [TestFixture]
    public class CLSForIDLGenerationTest {
            
        #region SFields
        
        private static Encoding s_latin1 = Encoding.GetEncoding("ISO-8859-1");
        
        #endregion
        #region IFields


        #endregion
        #region IMethods

        [SetUp]
        public void SetupEnvironment() {
        
        }

        [TearDown]
        public void TearDownEnvironment() {
        
        }
        
        private AssemblyName GetAssemblyName() {
            AssemblyName result = new AssemblyName();
            result.Name = "testAsm";
            return result;
        }
        
        public Assembly CreateIdl(Stream source) {            
            return CreateIdl(source, false, false);
        }                
        
        public Assembly CreateIdl(Stream source, bool anyToAnyContainerMapping, bool makeInterfaceDisposable) {
            IDLParser parser = new IDLParser(source);
            ASTspecification spec = parser.specification();
            // now parsed representation can be visited with the visitors
            MetaDataGenerator generator = new MetaDataGenerator(GetAssemblyName(), ".", 
                                                                new ArrayList());
            generator.MapAnyToAnyContainer = anyToAnyContainerMapping;
            if(makeInterfaceDisposable) {
                generator.InheritedInterface = typeof(System.IDisposable);
            }
            generator.InitalizeForSource(parser.getSymbolTable());
            spec.jjtAccept(generator, null);
            Assembly result = generator.ResultAssembly;
            return result;            
        }
        
        private void CheckRepId(Type testType, string expected) {
            object[] repAttrs = testType.GetCustomAttributes(typeof(RepositoryIDAttribute), 
                                                             false);
            Assertion.AssertEquals("wrong number of RepIDAttrs", 1, repAttrs.Length);
            RepositoryIDAttribute repId = (RepositoryIDAttribute) repAttrs[0];
            Assertion.AssertEquals("wrong repId", expected,
                                   repId.Id);            
        }                
        
        private void CheckInterfaceAttr(Type testType, IdlTypeInterface expected) {
            object[] ifAttrs = testType.GetCustomAttributes(typeof(InterfaceTypeAttribute), 
                                                            false);
            Assertion.AssertEquals("wrong number of InterfaceTypeAttribute", 1, ifAttrs.Length);
            InterfaceTypeAttribute ifAttr = (InterfaceTypeAttribute) ifAttrs[0];
            Assertion.AssertEquals("wrong ifattr", expected,
                                   ifAttr.IdlType);            
        }
        
        private void CheckImplClassAttr(Type toCheck, string implClassName) {
            object[] attrs = toCheck.GetCustomAttributes(typeof(ImplClassAttribute), 
                                                         false);
            Assertion.AssertEquals("wrong number of ImplClassAttribute", 1, attrs.Length);
            ImplClassAttribute attr = (ImplClassAttribute) attrs[0];
            Assertion.AssertEquals("wrong implclass attr", implClassName,
                                   attr.ImplClass);            
        }        
        
        private void CheckIdlEnumAttributePresent(Type enumType) {
            object[] attrs = enumType.GetCustomAttributes(typeof(IdlEnumAttribute), 
                                                          false);
            Assertion.AssertEquals("wrong number of IdlEnumAttribute", 1, attrs.Length);
        }
        
        private void CheckIdlStructAttributePresent(Type structType) {
            object[] attrs = structType.GetCustomAttributes(typeof(IdlStructAttribute), 
                                                            false);
            Assertion.AssertEquals("wrong number of IdlStructAttribute", 1, attrs.Length);
        }
        
        private void CheckIdlUnionAttributePresent(Type unionType) {
            object[] attrs = unionType.GetCustomAttributes(typeof(IdlUnionAttribute), 
                                                           false);
            Assertion.AssertEquals("wrong number of IdlUnionAttribute", 1, attrs.Length);
        }
        
        private void CheckSerializableAttributePresent(Type toCheck) {
            Assertion.AssertEquals("not serializable", true, toCheck.IsSerializable);
        }

        private void CheckPublicInstanceMethodPresent(Type testType, string methodName, 
                                                      Type returnType, Type[] paramTypes) {
            CheckMethodPresent(testType, methodName, returnType, paramTypes,
                               BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        }
        
        private void CheckMethodPresent(Type testType, string methodName, 
                                        Type returnType, Type[] paramTypes, BindingFlags attrs) {
            MethodInfo testMethod = testType.GetMethod(methodName, 
                                                       attrs,
                                                       null, paramTypes, null);
            Assertion.AssertNotNull(String.Format("method {0} not found", methodName),
                                    testMethod);
            
            Assertion.AssertEquals(String.Format("wrong return type {0} in method {1}", testMethod.ReturnType, methodName),
                                   returnType, testMethod.ReturnType);                                            
        }
        
        private void CheckPropertyPresent(Type testType, string propName, 
                                        Type propType, BindingFlags attrs) {            
            PropertyInfo testProp = testType.GetProperty(propName, attrs,
                                                       null, propType, Type.EmptyTypes,
                                                       null);
            Assertion.AssertNotNull(String.Format("property {0} not found", propName),
                                    testProp);
            
            Assertion.AssertEquals(String.Format("wrong type {0} in property {1}", testProp.PropertyType, propName),
                                   propType, testProp.PropertyType);                                            
        }
        
        private void CheckFieldPresent(Type testType, string fieldName,
                                       Type fieldType, BindingFlags flags) {
            FieldInfo testField = testType.GetField(fieldName, flags);                                           
            Assertion.AssertNotNull(String.Format("field {0} not found in type {1}", fieldName, testType.FullName),
                                    testField);
            Assertion.AssertEquals(String.Format("wrong field type {0} in field {1}", 
                                                 testField.FieldType, testField.Name),
                                   fieldType, testField.FieldType);        
        }
        
        private void CheckNumberOfFields(Type testType, BindingFlags flags, 
                                         System.Int32 expected) {
            FieldInfo[] fields = testType.GetFields(flags);
            Assertion.AssertEquals("wrong number of fields found in type: " + testType.FullName,
                                   expected, fields.Length);        
        }
        
        private void CheckOnlySpecificCustomAttrInCollection(object[] testAttrs, 
                                                             Type attrType) {
            Assertion.AssertEquals("wrong nr of custom attrs found",
                                   1, testAttrs.Length);
            Assertion.AssertEquals("wrong custom attr found",
                                   attrType,
                                   testAttrs[0].GetType());                                                             
        }
        
        private void CheckIIdlEntityInheritance(Type testType) {
            Type idlEntityIf = testType.GetInterface("IIdlEntity");
            Assertion.AssertNotNull(String.Format("type {0} doesn't inherit from IIdlEntity", testType.FullName),
                                    idlEntityIf);
        }
        
        private void CheckEnumField(FieldInfo field, string idlEnumValName) {
            Type enumType = field.DeclaringType;
            Assertion.AssertEquals("wrong enum val field type", 
                                   enumType, field.FieldType);
            Assertion.AssertEquals("wrong enum val field name",
                                   idlEnumValName, field.Name);
        }

        private void WriteIdlTestInterfaceToStream(StreamWriter aWriter, String ifModifier) {
            // idl:
            aWriter.WriteLine("module testmod {");
            aWriter.WriteLine("    " + ifModifier + " interface Test {");
            aWriter.WriteLine("        octet EchoOctet(in octet arg);");
            aWriter.WriteLine("    };");
            aWriter.WriteLine("    #pragma ID Test \"IDL:testmod/Test:1.0\"");
            aWriter.WriteLine("};");
            
            aWriter.Flush();
        }        

        private void CheckInterfaceDefinitions(string ifModifier, 
                                               IdlTypeInterface ifAttrVal) {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            try {
                WriteIdlTestInterfaceToStream(writer, ifModifier);
                testSource.Seek(0, SeekOrigin.Begin);
                Assembly result = CreateIdl(testSource);
                           
                // check if interface is correctly created
                Type ifType = result.GetType("testmod.Test", true);
                CheckPublicInstanceMethodPresent(ifType, "EchoOctet", 
                                                 typeof(System.Byte), new Type[] { typeof(System.Byte) });
                CheckRepId(ifType, "IDL:testmod/Test:1.0");
                CheckInterfaceAttr(ifType, ifAttrVal);
                CheckIIdlEntityInheritance(ifType);            
            } finally {
                writer.Close();
            }
        }

        private void CheckAdditionalBaseInterface(string ifModifier, bool setFlag, bool expectBase) {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            try {
                WriteIdlTestInterfaceToStream(writer, ifModifier);
                testSource.Seek(0, SeekOrigin.Begin);
                Assembly result = CreateIdl(testSource, false, setFlag);
                           
                Type ifType = result.GetType("testmod.Test", true);
                Assertion.AssertEquals("Additional Interface not correctly handled for " + ifModifier + " Interfaces.",
                        expectBase, typeof(IDisposable).IsAssignableFrom(ifType));
            } finally {
                writer.Close();
            }
        }

        [Test]
        public void TestConcreteInterfaces() {
            CheckInterfaceDefinitions("", IdlTypeInterface.ConcreteInterface);
        }
        
        [Test]
        public void TestAbstractInterfaces() {
            CheckInterfaceDefinitions("abstract", IdlTypeInterface.AbstractInterface);
        }
        
        [Test]
        public void TestLocalInterfaces() {
            CheckInterfaceDefinitions("local", IdlTypeInterface.LocalInterface);
        }

        [Test]
        public void TestConcreteInterfaceAdditionalBaseInterface() {
            CheckAdditionalBaseInterface(string.Empty, true, true);
            CheckAdditionalBaseInterface(string.Empty, false, false);
        }


        [Test]
        public void TestAbstractInterfaceAdditionalBaseInterface() {
            CheckAdditionalBaseInterface("abstract", true, true);
            CheckAdditionalBaseInterface("abstract", false, false);
        }

        [Test]
        public void TestLocalInterfaceAdditionalBaseInterface() {
            CheckAdditionalBaseInterface("local", true, false);
            CheckAdditionalBaseInterface("local", false, false);
        }   

        
        [Test]
        public void TestEnum() {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:
            writer.WriteLine("module testmod {");
            writer.WriteLine("    enum Test {");
            writer.WriteLine("        A, B, C");
            writer.WriteLine("    };");
            writer.WriteLine("};");
            
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            Assembly result = CreateIdl(testSource);
                       
            // check if enum is correctly created
            Type enumType = result.GetType("testmod.Test", true);
            CheckIdlEnumAttributePresent(enumType);
            
            // check enum val
            FieldInfo[] fields = enumType.GetFields(BindingFlags.Public | 
                                                    BindingFlags.Static | 
                                                    BindingFlags.DeclaredOnly);
            Assertion.AssertEquals("wrong number of fields in enum", 
                                   3, fields.Length);
            
            CheckEnumField(fields[0], "A");
            CheckEnumField(fields[1], "B");
            CheckEnumField(fields[2], "C");
            
            writer.Close();            
        }
        
        [Test]
        public void TestConcreteValueType() {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:
            writer.WriteLine("module testmod {");
            writer.WriteLine("    valuetype Test {");
            writer.WriteLine("        private octet x;");
            writer.WriteLine("        octet EchoOctet(in octet arg);");
            writer.WriteLine("        attribute octet attr;");
            writer.WriteLine("    };");
            writer.WriteLine("    #pragma ID Test \"IDL:testmod/Test:1.0\"");
            writer.WriteLine("};");
            
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            Assembly result = CreateIdl(testSource);
                       
            // check if val-type is correctly created
            Type valType = result.GetType("testmod.Test", true);
                       
            CheckImplClassAttr(valType, "testmod.TestImpl");
            CheckSerializableAttributePresent(valType);
            CheckRepId(valType, "IDL:testmod/Test:1.0");
            
            CheckIIdlEntityInheritance(valType);
            
            CheckPublicInstanceMethodPresent(valType, "EchoOctet",
                                             typeof(System.Byte), new Type[] { typeof(System.Byte) });
            CheckPropertyPresent(valType, "attr", typeof(System.Byte), 
                                 BindingFlags.Instance | BindingFlags.Public | 
                                 BindingFlags.DeclaredOnly);
            CheckFieldPresent(valType, "m_x", typeof(System.Byte), 
                              BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            CheckNumberOfFields(valType, BindingFlags.Public | BindingFlags.NonPublic |
                                         BindingFlags.Instance | BindingFlags.DeclaredOnly,
                                1);                                    
            writer.Close();
        }
        
        [Test]
        public void TestConcreteValueTypeWithIfInheritance() {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:
            writer.WriteLine("module testmod {");
            writer.WriteLine("    abstract interface TestIf {");
            writer.WriteLine("        void Inc();");
            writer.WriteLine("        readonly attribute boolean IsNegative;");
            writer.WriteLine("    };");
            writer.WriteLine("    #pragma ID TestIf \"IDL:testmod/TestIf:1.0\"");

            writer.WriteLine("    valuetype Test supports TestIf {");
            writer.WriteLine("        private octet x;");
            writer.WriteLine("        octet EchoOctet(in octet arg);");
            writer.WriteLine("        attribute octet attr;");
            writer.WriteLine("    };");
            writer.WriteLine("    #pragma ID Test \"IDL:testmod/Test:1.0\"");
            writer.WriteLine("};");
            
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            Assembly result = CreateIdl(testSource);
                       
            // check if val-type is correctly created
            Type valType = result.GetType("testmod.Test", true);
            // check if if-type is correctly created
            Type ifType = result.GetType("testmod.TestIf", true);
            Assertion.Assert("no inheritance from TestIf", ifType.IsAssignableFrom(valType));
                       
            CheckImplClassAttr(valType, "testmod.TestImpl");
            CheckSerializableAttributePresent(valType);
            CheckRepId(valType, "IDL:testmod/Test:1.0");
            
            CheckIIdlEntityInheritance(valType);
            
            CheckPublicInstanceMethodPresent(valType, "EchoOctet",
                                             typeof(System.Byte), new Type[] { typeof(System.Byte) });
            CheckPublicInstanceMethodPresent(valType, "Inc",
                                             typeof(void), Type.EmptyTypes);
            CheckPropertyPresent(valType, "attr", typeof(System.Byte), 
                                 BindingFlags.Instance | BindingFlags.Public | 
                                 BindingFlags.DeclaredOnly);

            CheckPropertyPresent(valType, "IsNegative", typeof(System.Boolean), 
                                 BindingFlags.Instance | BindingFlags.Public | 
                                 BindingFlags.DeclaredOnly);

            CheckFieldPresent(valType, "m_x", typeof(System.Byte), 
                              BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            CheckNumberOfFields(valType, BindingFlags.Public | BindingFlags.NonPublic |
                                         BindingFlags.Instance | BindingFlags.DeclaredOnly,
                                1);                                    
            writer.Close();
        }
        
        [Test]
        public void TestStruct() {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:
            writer.WriteLine("module testmod {");
            writer.WriteLine("    struct Test {");
            writer.WriteLine("        long a;");
            writer.WriteLine("    };");
            writer.WriteLine("};");
            
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            Assembly result = CreateIdl(testSource);
                       
            // check if struct is correctly created
            Type structType = result.GetType("testmod.Test", true);
            // must be a struct
            Assertion.Assert("is a struct", structType.IsValueType);
            CheckIdlStructAttributePresent(structType);
            CheckSerializableAttributePresent(structType);
            
            CheckFieldPresent(structType, "a", typeof(System.Int32), 
                              BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);            
            CheckNumberOfFields(structType, BindingFlags.Public | BindingFlags.NonPublic |
                                            BindingFlags.Instance | BindingFlags.DeclaredOnly,
                                1);
            writer.Close();
        }
        
        [Test]
        public void TestNestedStruct() {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:
            writer.WriteLine("module testmod {");
            writer.WriteLine("    interface ContainerIf {");
            writer.WriteLine("        struct Test {");
            writer.WriteLine("            long a;");
            writer.WriteLine("        };");
            writer.WriteLine("    };");
            writer.WriteLine("    valuetype ContainerValType {");
            writer.WriteLine("        struct Test {");
            writer.WriteLine("            long a;");
            writer.WriteLine("        };");
            writer.WriteLine("    };");                        
            writer.WriteLine("};");
            
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            Assembly result = CreateIdl(testSource);
                       
            // check if container interface created
            Type containerIfType = result.GetType("testmod.ContainerIf",
                                                true);
            Assertion.AssertNotNull(containerIfType);
            
            // check if struct in if is correctly created
            Type structType1 = result.GetType("testmod.ContainerIf_package.Test", 
                                             true);
            // must be a struct
            Assertion.Assert("is a struct", structType1.IsValueType);
            CheckIdlStructAttributePresent(structType1);
            CheckSerializableAttributePresent(structType1);
            
            CheckFieldPresent(structType1, "a", typeof(System.Int32), 
                              BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);            
            CheckNumberOfFields(structType1, BindingFlags.Public | BindingFlags.NonPublic |
                                             BindingFlags.Instance | BindingFlags.DeclaredOnly,
                                1);
            
            // check if container interface created
            Type containerValType = result.GetType("testmod.ContainerValType",
                                                true);
            Assertion.AssertNotNull(containerValType);
            
            // check if struct in if is correctly created
            Type structType2 = result.GetType("testmod.ContainerValType_package.Test", 
                                             true);
            // must be a struct
            Assertion.Assert("is a struct", structType2.IsValueType);
            CheckIdlStructAttributePresent(structType2);
            CheckSerializableAttributePresent(structType2);
            
            CheckFieldPresent(structType2, "a", typeof(System.Int32), 
                              BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);            
            CheckNumberOfFields(structType2, BindingFlags.Public | BindingFlags.NonPublic |
                                             BindingFlags.Instance | BindingFlags.DeclaredOnly,
                                1);

            writer.Close();
        }
                        
        [Test]
        public void TestUnion() {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:
            writer.WriteLine("module testmod {");
            writer.WriteLine("    union Test switch(long) {");
            writer.WriteLine("        case 0: short val0;");
            writer.WriteLine("        case 1: ");
            writer.WriteLine("        case 2: long val1;");
			writer.WriteLine("        default: boolean val2;");
            writer.WriteLine("    };");
            writer.WriteLine("};");
            
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            Assembly result = CreateIdl(testSource);
                       
            // check if union is correctly created
            Type unionType = result.GetType("testmod.Test", true);
            // must be a struct
            Assertion.Assert("is a struct", unionType.IsValueType);
            CheckIdlUnionAttributePresent(unionType);

            CheckFieldPresent(unionType, "m_discriminator", typeof(System.Int32), 
                              BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            CheckFieldPresent(unionType, "m_val0", typeof(System.Int16), 
                              BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            CheckFieldPresent(unionType, "m_val1", typeof(System.Int32), 
                              BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            CheckFieldPresent(unionType, "m_val2", typeof(System.Boolean), 
                              BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            CheckPublicInstanceMethodPresent(unionType, "Getval0",
                                             typeof(System.Int16), Type.EmptyTypes);
            CheckPublicInstanceMethodPresent(unionType, "Getval1",
                                             typeof(System.Int32), Type.EmptyTypes);
            CheckPublicInstanceMethodPresent(unionType, "Getval2",
                                             typeof(System.Boolean), Type.EmptyTypes);

            CheckPublicInstanceMethodPresent(unionType, "Setval0",
                                             typeof(void), new Type[] { typeof(System.Int16) });
            CheckPublicInstanceMethodPresent(unionType, "Setval1",
                                             typeof(void), new Type[] { typeof(System.Int32), typeof(System.Int32) } );
            CheckPublicInstanceMethodPresent(unionType, "Setval2",
                                             typeof(void), new Type[] { typeof(System.Boolean), typeof(System.Int32) } );            

            CheckMethodPresent(unionType, "GetFieldForDiscriminator", 
                               typeof(FieldInfo), new Type[] { typeof(System.Int32) },
                               BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Static);
            
            writer.Close();
        }          
        
        [Test]
        public void TestIdlSequenceParamters() {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:
            writer.WriteLine("module testmod {");
            writer.WriteLine("    typedef sequence<long> seqLong;");
            writer.WriteLine("    interface Test {");
            writer.WriteLine("        seqLong EchoSeqLong(in seqLong arg);");
            writer.WriteLine("    };");
            writer.WriteLine("};");
            
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            Assembly result = CreateIdl(testSource);
                       
            // check if sequence as method parameters is created correctly
            Type ifContainerType = result.GetType("testmod.Test", true);
            
            MethodInfo seqMethod = ifContainerType.GetMethod("EchoSeqLong",
                                                             BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);                            
            Assertion.AssertNotNull("method not found in seqTest", seqMethod);
            ParameterInfo[] parameters = seqMethod.GetParameters();
            Assertion.AssertEquals("wrong number of paramters; seqTestMethod", 
                                   1, parameters.Length);
            Assertion.AssertEquals("wrong parameter type; seqTestMethod",
                                   typeof(int[]), parameters[0].ParameterType);
            Assertion.AssertEquals("wrong return type; seqTestMethod",
                                   typeof(int[]), seqMethod.ReturnType);
            object[] paramAttrs = parameters[0].GetCustomAttributes(false);
            CheckOnlySpecificCustomAttrInCollection(paramAttrs, typeof(IdlSequenceAttribute));
            object[] returnAttrs = seqMethod.ReturnTypeCustomAttributes.GetCustomAttributes(false);
            CheckOnlySpecificCustomAttrInCollection(returnAttrs, typeof(IdlSequenceAttribute));            
            
            writer.Close();
        }

        public void TestConstants() {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:
            writer.WriteLine("module testmod {");
            writer.WriteLine("    interface Test {");
            writer.WriteLine("        const long MyConstant = 11;");
            writer.WriteLine("    };");
            writer.WriteLine("const long MyOutsideTypeConstant = 13;");
            writer.WriteLine("};");
            writer.WriteLine("const long MyOutsideAllConstant = 19;");
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            Assembly result = CreateIdl(testSource);
                       
            // check if classes for constants were created correctly
            Type const1Type = result.GetType("testmod.Test_package.MyConstant", true);
            Type const2Type = result.GetType("testmod.MyOutsideTypeConstant", true);
            Type const3Type = result.GetType("MyOutsideAllConstant", true);

            CheckFieldPresent(const1Type, "ConstVal", typeof(System.Int32), 
                              BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            CheckNumberOfFields(const1Type, BindingFlags.Public | BindingFlags.NonPublic |
                                            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly,
                                1);                                    
            
            CheckFieldPresent(const2Type, "ConstVal", typeof(System.Int32), 
                              BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            CheckNumberOfFields(const2Type, BindingFlags.Public | BindingFlags.NonPublic |
                                            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly,
                                1);

            CheckFieldPresent(const3Type, "ConstVal", typeof(System.Int32), 
                              BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            CheckNumberOfFields(const3Type, BindingFlags.Public | BindingFlags.NonPublic |
                                            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly,
                                1);            
            
            writer.Close();
        }

        [Test]
        public void TestRecStruct() {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:
            writer.WriteLine("module testmod {");
            writer.WriteLine("    struct RecStruct {");
            writer.WriteLine("        sequence<RecStruct> seq;");
            writer.WriteLine("    };");
            writer.WriteLine("};");
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            Assembly result = CreateIdl(testSource);
                       
            // check if classes for constants were created correctly
            Type recStructType = result.GetType("testmod.RecStruct", true);

            CheckFieldPresent(recStructType, "seq", recStructType.Assembly.GetType("testmod.RecStruct[]", true), 
                              BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            CheckNumberOfFields(recStructType, BindingFlags.Public | BindingFlags.NonPublic |
                                            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly,
                                1);            
            
            writer.Close();
        }

        [Test]
        public void TestBoundedSeq() {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:
            writer.WriteLine("module testmod {");
            writer.WriteLine("    typedef sequence<long, 3> boundedLongSeq;");
            writer.WriteLine("    typedef sequence<long> unboundedLongSeq;");
            writer.WriteLine("    struct TestStructWithSeq {");
            writer.WriteLine("        boundedLongSeq boundedSeqElem;");
            writer.WriteLine("        unboundedLongSeq unboundedSeqElem;");
            writer.WriteLine("    };");
            writer.WriteLine("};");
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            Assembly result = CreateIdl(testSource);
                       
            // check if classes for constants were created correctly
            Type structType = result.GetType("testmod.TestStructWithSeq", true);

            CheckFieldPresent(structType, "boundedSeqElem", typeof(int[]), 
                              BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            CheckFieldPresent(structType, "unboundedSeqElem", typeof(int[]), 
                              BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            CheckNumberOfFields(structType, BindingFlags.Public | BindingFlags.NonPublic |
                                            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly,
                                2);

            FieldInfo boundedElemField = structType.GetField("boundedSeqElem",
                                                              BindingFlags.Public | BindingFlags.Instance |
                                                              BindingFlags.DeclaredOnly);

            object[] bseqAttrs = 
                boundedElemField.GetCustomAttributes(typeof(IdlSequenceAttribute), true);
            Assertion.AssertNotNull(bseqAttrs);
            Assertion.AssertEquals(1, bseqAttrs.Length);
            Assertion.AssertEquals(true, ((IdlSequenceAttribute)bseqAttrs[0]).IsBounded());
            Assertion.AssertEquals(3, ((IdlSequenceAttribute)bseqAttrs[0]).Bound);

            FieldInfo unboundedElemField = structType.GetField("unboundedSeqElem",
                                                               BindingFlags.Public | BindingFlags.Instance |
                                                               BindingFlags.DeclaredOnly);

            object[] ubseqAttrs = 
                unboundedElemField.GetCustomAttributes(typeof(IdlSequenceAttribute), true);
            Assertion.AssertNotNull(ubseqAttrs);
            Assertion.AssertEquals(1, ubseqAttrs.Length);
            Assertion.AssertEquals(false, ((IdlSequenceAttribute)ubseqAttrs[0]).IsBounded());
            Assertion.AssertEquals(0, ((IdlSequenceAttribute)ubseqAttrs[0]).Bound);
                        
            writer.Close();
        }
        
        [Test]
        public void TestInheritedIdentifierResolution() {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:            
            writer.WriteLine("module testmod {");
            writer.WriteLine("    interface A {");
            writer.WriteLine("        exception E { } ;");            
            writer.WriteLine("        void f() raises(E);");            
            writer.WriteLine("    };");
            writer.WriteLine("    interface B : A {");
            writer.WriteLine("        void g() raises(E);");
            writer.WriteLine("    };");
            writer.WriteLine("};");
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            
            try {
                Assembly result = CreateIdl(testSource);
                Type ifB = result.GetType("testmod.B", true);
                CheckPublicInstanceMethodPresent(ifB, "g",
                                                 typeof(void), Type.EmptyTypes);
                MethodInfo testMethod = ifB.GetMethod("g", 
                                                       BindingFlags.Public | BindingFlags.Instance,
                                                       null, Type.EmptyTypes, null);
                Assertion.AssertNotNull(testMethod);
                // not possible to check directly for exceptoin attribute, because Exception type
                // not resolvable because assembly not written to disk!
            } finally {           
                writer.Close();
            }            
        }


        [Test]
        public void TestIdentifiers() {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:
            writer.WriteLine("module WithSpecial� {");
            writer.WriteLine("    enum Test� {");
            writer.WriteLine("        �, B, �");
            writer.WriteLine("    };");
            writer.WriteLine("};");
            
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            Assembly result = CreateIdl(testSource);
                       
            // check if enum is correctly created
            Type enumType = result.GetType("WithSpecial�.Test�", true);
            CheckIdlEnumAttributePresent(enumType);
            
            // check enum val
            FieldInfo[] fields = enumType.GetFields(BindingFlags.Public | 
                                                    BindingFlags.Static | 
                                                    BindingFlags.DeclaredOnly);
            Assertion.AssertEquals("wrong number of fields in enum", 
                                   3, fields.Length);
            
            CheckEnumField(fields[0], "�");
            CheckEnumField(fields[1], "B");
            CheckEnumField(fields[2], "�");
            
            writer.Close();            
        }
        
        /// <summary>
        /// regression test for bug #1042055
        /// </summary>
        [Test]
        public void TestIdDBugNr1042055() {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:
            writer.WriteLine("module testmod {");
            writer.WriteLine("    struct Test {");
            writer.WriteLine("        long d;");
            writer.WriteLine("        long D;");
            writer.WriteLine("    };");
            writer.WriteLine("};");
            
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            Assembly result = CreateIdl(testSource);
                       
            // check if struct is correctly created
            Type structType = result.GetType("testmod.Test", true);
            // must be a struct
            Assertion.Assert("is a struct", structType.IsValueType);
            CheckIdlStructAttributePresent(structType);
            CheckSerializableAttributePresent(structType);
            
            CheckFieldPresent(structType, "d", typeof(System.Int32), 
                              BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);            
            CheckFieldPresent(structType, "D", typeof(System.Int32), 
                              BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            CheckNumberOfFields(structType, BindingFlags.Public | BindingFlags.NonPublic |
                                            BindingFlags.Instance | BindingFlags.DeclaredOnly,
                                2);
            writer.Close();
        }        
        
        [Test]
        [ExpectedException(typeof(InvalidIdlException))]
        public void TestInvalidIdlBoxedValueType() {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:
            // incorrect, because TestStruct referenced before defined
            writer.WriteLine("module testmod {");
            writer.WriteLine("    valuetype BoxedTest TestStruct;");
            writer.WriteLine("");
            writer.WriteLine("    struct TestStruct {");
            writer.WriteLine("        long field;");
            writer.WriteLine("    };");
            writer.WriteLine("};");
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            
            try {
                Assembly result = CreateIdl(testSource);
            } finally {           
                writer.Close();
            }
        }
        
        [Test]
        [ExpectedException(typeof(InvalidIdlException))]
        public void TestInvalidIdlSequenceType() {
            MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:
            // incorrect, because TestStruct referenced before defined
            writer.WriteLine("module testmod {");
            writer.WriteLine("    typedef sequence<TestStruct> invalidSeq;");
            writer.WriteLine("");
            writer.WriteLine("    struct TestStruct {");
            writer.WriteLine("        long field;");
            writer.WriteLine("    };");
            writer.WriteLine("};");
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            
            try {            
                Assembly result = CreateIdl(testSource);
            } finally {
                writer.Close();
            }
        }
        
        [Test]
        public void TestUnionDefinedInStructBugReport() {
        	MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:           
            writer.WriteLine("module C2 {");
            writer.WriteLine("    enum GenericType { GBOOL, GINT, GLONG, GDOUBLE, GSTRING, GMAP, GLIST };");
            writer.WriteLine();
            writer.WriteLine("       struct Generic {");
            writer.WriteLine("        string   name;");
            writer.WriteLine("        union GAny switch (GenericType) {");
            writer.WriteLine("          case GBOOL:");
            writer.WriteLine("            boolean  g_bool;");
            writer.WriteLine("          case GINT:");
            writer.WriteLine("            short    g_int;");
            writer.WriteLine("          case GLONG:");
            writer.WriteLine("            long     g_long;");
            writer.WriteLine("          case GDOUBLE:");
            writer.WriteLine("            double   g_double;");
            writer.WriteLine("          case GSTRING:");
            writer.WriteLine("            string   g_string;");
            writer.WriteLine("          case GMAP:");
            writer.WriteLine("            sequence<Generic> g_map;");
            writer.WriteLine("          case GLIST:");
            writer.WriteLine("            sequence<GAny> g_list;");
            writer.WriteLine("        }");
            writer.WriteLine("        value;");
            writer.WriteLine("    };");
            writer.WriteLine();
            writer.WriteLine("    typedef sequence<Generic> GenericMap;");
   			writer.WriteLine("    typedef sequence<Generic::GAny> GenericList;");
            writer.WriteLine();
            writer.WriteLine("};");
            
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            Assembly result = CreateIdl(testSource);
                       
            // check if struct is correctly created
            Type structType = result.GetType("C2.Generic", true);
            // must be a struct
            Assertion.Assert("is a struct", structType.IsValueType);
            CheckIdlStructAttributePresent(structType);
            CheckSerializableAttributePresent(structType);
            
            CheckFieldPresent(structType, "name", typeof(System.String), 
                              BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);            
            CheckFieldPresent(structType, "value", result.GetType("C2.Generic_package.GAny", true),
                              BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);            
            CheckNumberOfFields(structType, BindingFlags.Public | BindingFlags.NonPublic |
                                            BindingFlags.Instance | BindingFlags.DeclaredOnly,
                                2);
            writer.Close();
        }

        [Test]
        public void TestMultipleNestedTypes() {
        	MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);
            
            // idl:           
            writer.WriteLine("module C2 {");
            writer.WriteLine();
            writer.WriteLine("  struct L1 {");
            writer.WriteLine("      string   name;");
            writer.WriteLine("      struct L2 {");
            writer.WriteLine("          string name;");
            writer.WriteLine("          struct L3 {");
            writer.WriteLine("            string nameL3;");
            writer.WriteLine("              struct L4 {");
            writer.WriteLine("                string    name;");            
            writer.WriteLine("              } valL4;");
            writer.WriteLine("          } valL3;");
            writer.WriteLine("      } valL2;");
            writer.WriteLine("    };");
            writer.WriteLine();
   			writer.WriteLine("    typedef sequence<L1::L2> L2List;");
   			writer.WriteLine("    typedef sequence<L1::L2::L3> L3List;");
   			writer.WriteLine("    typedef sequence<L1::L2::L3::L4> L4List;");
            writer.WriteLine();
            writer.WriteLine("};");
            
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            Assembly result = CreateIdl(testSource);
            writer.Close();
                       
            // check if struct is correctly created
            Type structType = result.GetType("C2.L1", true);
            // must be a struct
            Assertion.Assert("is a struct", structType.IsValueType);
            CheckIdlStructAttributePresent(structType);
            CheckSerializableAttributePresent(structType);
            
            CheckFieldPresent(structType, "name", typeof(System.String), 
                              BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);            
            CheckFieldPresent(structType, "valL2", result.GetType("C2.L1_package.L2", true),
                              BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);            
            CheckNumberOfFields(structType, BindingFlags.Public | BindingFlags.NonPublic |
                                            BindingFlags.Instance | BindingFlags.DeclaredOnly,
                                2);
            Type l3StructType = result.GetType("C2.L1_package.L2_package.L3", true);
            CheckFieldPresent(l3StructType, "nameL3", typeof(System.String), 
                              BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);            
            CheckFieldPresent(l3StructType, "valL4", result.GetType("C2.L1_package.L2_package.L3_package.L4", true),
                              BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);            
            CheckNumberOfFields(l3StructType, BindingFlags.Public | BindingFlags.NonPublic |
                                            BindingFlags.Instance | BindingFlags.DeclaredOnly,
                                2);                        
        }
        
        [Test]
        public void TestAnyToAnyContainerMapping() {
        	MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);

            // idl:            
            writer.WriteLine("module testmod {");
            writer.WriteLine("    interface Test {");
            writer.WriteLine("        any EchoAnyToContainer(in any arg);");
            writer.WriteLine("    };");
            writer.WriteLine("    #pragma ID Test \"IDL:testmod/Test:1.0\"");
            writer.WriteLine("};");
            
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            Assembly result = CreateIdl(testSource, true, false);
            writer.Close();
                       
            // check if interface is correctly created
            Type ifType = result.GetType("testmod.Test", true);
            CheckPublicInstanceMethodPresent(ifType, "EchoAnyToContainer", 
                                             typeof(omg.org.CORBA.Any), new Type[] { typeof(omg.org.CORBA.Any) });
        }

        [Test]
        public void TestAnyToObjectMapping() {
        	MemoryStream testSource = new MemoryStream();
            StreamWriter writer = new StreamWriter(testSource, s_latin1);

            // idl:            
            writer.WriteLine("module testmod {");
            writer.WriteLine("    interface Test {");
            writer.WriteLine("        any EchoAnyToContainer(in any arg);");
            writer.WriteLine("    };");
            writer.WriteLine("    #pragma ID Test \"IDL:testmod/Test:1.0\"");
            writer.WriteLine("};");
            
            writer.Flush();
            testSource.Seek(0, SeekOrigin.Begin);
            Assembly result = CreateIdl(testSource, false, false);
            writer.Close();
                       
            // check if interface is correctly created
            Type ifType = result.GetType("testmod.Test", true);
            CheckPublicInstanceMethodPresent(ifType, "EchoAnyToContainer", 
                                             typeof(object), new Type[] { typeof(object) });
        }

        #endregion
        
    }
        
}


#endif