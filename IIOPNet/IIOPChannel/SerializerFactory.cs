/* SerializerFacotry.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 30.12.05  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
 * 
 * Copyright 2005 Dominic Ullmann
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
using System.Diagnostics;
using Ch.Elca.Iiop.Util;
using Ch.Elca.Iiop.Idl;
using omg.org.CORBA;

namespace Ch.Elca.Iiop.Marshalling {
    
    /// <summary>
    /// Creates and caches Serializers for Types.
    /// </summary>
    internal class SerializerFactory : MappingAction {
            
        #region SFields
        
        /// <summary>is responsible for the mapping CLS to IDL</summary>
        private static ClsToIdlMapper s_mapper = ClsToIdlMapper.GetSingleton();        
        
        #endregion SFields
        #region IFields
        
        // base type serialiser
        private Serializer m_wideCharSer = new CharSerializer(true);
        private Serializer m_nonWideCharSer = new CharSerializer(false);
        
        private Serializer m_wideStringSer = new StringSerializer(true);
        private Serializer m_nonWideStringSer = new StringSerializer(false);        
        
        private Serializer m_byteSer = new ByteSerializer();
        private Serializer m_sbyteSer = new SByteSerializer();
        private Serializer m_boolSer = new BooleanSerializer();
        private Serializer m_int16Ser = new Int16Serializer();
        private Serializer m_int32Ser = new Int32Serializer();
        private Serializer m_int64Ser = new Int64Serializer();
        private Serializer m_uint16Ser = new UInt16Serializer();
        private Serializer m_uint32Ser = new UInt32Serializer();
        private Serializer m_uint64Ser = new UInt64Serializer();        
        private Serializer m_singleSer = new SingleSerializer();
        private Serializer m_doubleSer = new DoubleSerializer();   
        
        private Serializer m_boxedWstringValueSer;
        private Serializer m_boxedStringValueSer;
        
        // non-base type generic serializers
        private Serializer m_anySerForObject;
        private Serializer m_anySerForAnyCont;
        private Serializer m_typeSer = new TypeSerializer();
        private Serializer m_typeCodeSer = new TypeCodeSerializer();       
        
        private Serializer m_exceptionSer;
        
        // caches for type specific serializers
        private IDictionary /* Type, Serializer */ m_structSers = new Hashtable();         
        private IDictionary /* Type, Serializer */ m_enumSers = new Hashtable();   
        private IDictionary /* Type, Serializer */ m_flagsSers = new Hashtable();
        private IDictionary /* Type, Serializer */ m_unionSers = new Hashtable();                
        private IDictionary /* Type, Serializer */ m_valTypeSers = new Hashtable();        
        
        private IDictionary /* Type, ValueConcreteInstanceSerializer */ 
            m_concValueInstanceSer = new Hashtable();
        
        #endregion IFields
        #region IConstructors
        
        internal SerializerFactory() {
            m_anySerForObject = new AnySerializer(this, false);
            m_anySerForAnyCont = new AnySerializer(this, true);
            
            m_exceptionSer = new ExceptionSerializer(this);
            
            m_boxedWstringValueSer = new BoxedValueSerializer(ReflectionHelper.WStringValueType, 
                                                              false, this);
                                                              
            m_boxedStringValueSer = new BoxedValueSerializer(ReflectionHelper.StringValueType,
                                                             false, this);
        }
        
        #endregion IConstructors
        #region IMethods
        
        /// <summary>determines the serialiser responsible for a specified formal type and the parameterattributes attributes</summary>
        /// <param name="formal">The formal type. If formal is modified through mapper, result is returned in this parameter</param>
        /// <param name="attributes">the parameter/field attributes</param>
        /// <returns></returns>
        private Serializer DetermineSerializer(Type formal, AttributeExtCollection attributes) {
            CustomMappingDesc customMappingUsed;
            Serializer serialiser = 
                (Serializer)s_mapper.MapClsTypeWithTransform(ref formal, ref attributes, 
                                                             this, out customMappingUsed); // formal can be transformed
            if (serialiser == null) {
                // no serializer present for Type: formal
                Trace.WriteLine("no serialiser for Type: " + formal);
                throw new BAD_PARAM(9001, CompletionStatus.Completed_MayBe);
            }
            Debug.WriteLine("determined to serialize formal type " + formal + " with : " + serialiser);
            if (customMappingUsed != null) {
                // wrap serializer to apply custom mapping
                serialiser = new CustomMappingDecorator(customMappingUsed, serialiser);
            }
            return serialiser;
        }
        
    
        /// <summary>
        /// Creates or retrieve cached Serializer for the given Type and attributes. 
        /// </summary>
        internal Serializer Create(Type forType, AttributeExtCollection attributes) {
            return DetermineSerializer(forType, attributes);                       
        }
        
        /// <summary>
        /// Creates or retrieve cached Serializer for the given concrete value type.
        /// </summary>
        /// <remarks>
        /// This method is only useful for the implmenetation of ValueObjectSerializer.
        /// </remarks>
        internal ValueObjectSerializer.ValueConcreteInstanceSerializer
            CreateConcreteValueTypeSer(Type concreteValueType) {            
            lock(m_concValueInstanceSer.SyncRoot) {
                ValueObjectSerializer.ValueConcreteInstanceSerializer result = 
                    (ValueObjectSerializer.ValueConcreteInstanceSerializer)
                        m_concValueInstanceSer[concreteValueType];
                if (result == null) {
                    result = new 
                        ValueObjectSerializer.ValueConcreteInstanceSerializer(concreteValueType, 
                                                                              this);
                    m_concValueInstanceSer[concreteValueType] = result;
                    result.Initalize(); // determine field mapping
                }
                return result;
            }
        }
        
        #region Implementation of MappingAction
        
        public object MapToIdlStruct(System.Type clsType) {
            lock(m_structSers.SyncRoot) {
                Serializer result = (Serializer)m_structSers[clsType];
                if (result == null) {
                    result = new IdlStructSerializer(clsType, this);                    
                    m_structSers[clsType] = result;
                    ((IdlStructSerializer)result).Initalize(); // to prevent recursive struct issues, must be done
                                                               // after registration of the struct.
                }
                return result;
            }
        }
        public object MapToIdlUnion(System.Type clsType) {
            lock(m_unionSers.SyncRoot) {
                Serializer result = (Serializer)m_unionSers[clsType];
                if (result == null) {
                    result = new IdlUnionSerializer(clsType, this);
                    m_unionSers[clsType] = result;
                }
                return result;
            }            
        }
        public object MapToIdlAbstractInterface(System.Type clsType) {
            // could be cached ...
            return new AbstractInterfaceSerializer(clsType, this);
        }
        public object MapToIdlConcreteInterface(System.Type clsType) {
             // can be cached, but because not expensive to create not (yet?) done
            return new ObjRefSerializer(clsType);
        }
        public object MapToIdlLocalInterface(System.Type clsType) {
            // local interfaces are non-marshable
            throw new MARSHAL(4, CompletionStatus.Completed_MayBe);
        }
        public object MapToIdlConcreateValueType(System.Type clsType) {                        
            lock(m_valTypeSers.SyncRoot) {
                Serializer result = (Serializer)m_valTypeSers[clsType];
                if (result == null) {
                    result = new ValueObjectSerializer(clsType, this);
                    m_valTypeSers[clsType] = result;
                }
                return result;
            }
        }
        public object MapToIdlAbstractValueType(System.Type clsType) {
            return new AbstractValueSerializer(clsType, this);
        }
        public object MapToIdlBoxedValueType(System.Type clsType, Type needsBoxingFrom) {
            if (needsBoxingFrom != null) {
                // need boxing / unboxing of values
                if (needsBoxingFrom.IsArray && (needsBoxingFrom.GetArrayRank() > 1)) {
                    // if mapped from a true .NET multi-dim array, needs a conversion to jagged array before serialse
                    // and after deserialise
                    return new BoxedValueSerializer(clsType, true, this);
                } else {
                    return new BoxedValueSerializer(clsType, false, this);
                }
            } else {
                // do serialize as value type
                // TODO: is this correct? Check for Any mapping ...
                return MapToIdlConcreateValueType(clsType);
            }
        }
        
        public object MapToIdlSequence(System.Type clsType, int bound, AttributeExtCollection allAttributes, AttributeExtCollection elemTypeAttributes) {
            return new IdlSequenceSerializer(clsType, elemTypeAttributes, bound, this);       
        }
        public object MapToIdlArray(System.Type clsType, int[] dimensions, AttributeExtCollection allAttributes, AttributeExtCollection elemTypeAttributes) {            
            return new IdlArraySerializer(clsType, elemTypeAttributes, dimensions, this);
        }
        public object MapToIdlAny(System.Type clsType) {
            if (clsType.Equals(ReflectionHelper.AnyType)) {
                return m_anySerForAnyCont;
            } else {
                return m_anySerForObject;
            }
        }
        public object MapToAbstractBase(System.Type clsType) {
            return MapToIdlAbstractInterface(clsType);
        }
        public object MapToValueBase(System.Type clsType) {
            return MapToIdlAbstractValueType(clsType);
        }
        public object MapException(System.Type clsType) {
            return m_exceptionSer;
        }
                
        public object MapToIdlEnum(System.Type clsType) {
            lock(m_enumSers.SyncRoot) {
                Serializer result = (Serializer)m_enumSers[clsType];
                if (result == null) {
                    if (ClsToIdlMapper.IsIdlEnum(clsType)) {
                        result = new IdlEnumSerializer(clsType);
                    } else {
                        result = new EnumMapClsToIdlRangeSerializer(clsType);
                    }
                    m_enumSers[clsType] = result;
                }
                return result;
            }
        }
        public object MapToIdlFlagsEquivalent(Type clsType) {
            lock(m_flagsSers.SyncRoot) {
                Serializer result = (Serializer)m_flagsSers[clsType];
                if (result == null) {
                    result = new FlagsSerializer(clsType, this);
                    m_flagsSers[clsType] = result;
                }
                return result;
            }
        }
        public object MapToWStringValue(System.Type clsType) {
            // clsType is ReflectionHelper.WStringValueType            
            return m_boxedWstringValueSer;
        }
        public object MapToStringValue(System.Type clsType) {
            // clsType is ReflectionHelper.StringValueType            
            return m_boxedStringValueSer;
        }
        public object MapToTypeDesc(System.Type clsType) {
            return m_typeSer;
        }
        public object MapToTypeCode(System.Type clsType) {
            return m_typeCodeSer;
        }
        public object MapToIdlBoolean(System.Type clsType) {
            return m_boolSer;
        }
        public object MapToIdlFloat(System.Type clsType) {
            return m_singleSer;
        }
        public object MapToIdlDouble(System.Type clsType) {
            return m_doubleSer;
        }
        public object MapToIdlShort(System.Type clsType) {
            return m_int16Ser;
        }
        public object MapToIdlUShort(System.Type clsType) {
            return m_uint16Ser;
        }
        public object MapToIdlLong(System.Type clsType) {
            return m_int32Ser;
        }
        public object MapToIdlULong(System.Type clsType) {
            return m_uint32Ser;
        }
        public object MapToIdlLongLong(System.Type clsType) {
            return m_int64Ser;
        }
        public object MapToIdlULongLong(System.Type clsType) {
            return m_uint64Ser;
        }
        public object MapToIdlOctet(System.Type clsType) {
            return m_byteSer;
        }
        public object MapToIdlSByteEquivalent(Type clsType) {
            return m_sbyteSer;
        }        
        public object MapToIdlVoid(System.Type clsType) {
            // void is not serializable
            throw new INTERNAL(8704, CompletionStatus.Completed_MayBe);
        }
        public object MapToIdlWChar(System.Type clsType) {
            return m_wideCharSer;
        }
        public object MapToIdlWString(System.Type clsType) {
            return m_wideStringSer;
        }
        public object MapToIdlChar(System.Type clsType) {
            return m_nonWideCharSer;
        }
        public object MapToIdlString(System.Type clsType) {
            return m_nonWideStringSer;
        }
    
        #endregion Implementation of MappingAction

        #endregion IMethods                
    
    }

}

#if UnitTest

namespace Ch.Elca.Iiop.Tests {
    
    using NUnit.Framework;
    using Ch.Elca.Iiop.Marshalling;
    using Ch.Elca.Iiop.Idl;    
    using Ch.Elca.Iiop.Util;    
   
    /// <summary>
    /// Unit-tests for the SerializerFactory
    /// </summary>
    [TestFixture]    
    public class SerialiserFactoryTest {
                       
        public SerialiserFactoryTest() {
        }
        
        private void GenericFactoryTest(Type createFor, Type expectedSerType) {
            SerializerFactory factory = new SerializerFactory();
            Serializer ser = factory.Create(createFor, 
                                            AttributeExtCollection.EmptyCollection);
            Assertion.AssertEquals("wrong serializer type", expectedSerType, ser.GetType());            
        }
        
        [Test]
        public void TestIdlEnumMapping() {
            GenericFactoryTest(typeof(TestIdlEnumBI32), typeof(IdlEnumSerializer));
        }
        
        [Test]
        public void TestIndexMappedEnumMapping() {
            GenericFactoryTest(typeof(TestEnumWithValueNotIndexBI32), 
                               typeof(EnumMapClsToIdlRangeSerializer));
        }
        
        [Test]
        public void TestInt64EnumMapping() {
            GenericFactoryTest(typeof(TestEnumBI64), 
                               typeof(EnumMapClsToIdlRangeSerializer));
        }
                
    }

}

#endif
