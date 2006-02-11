/* Serializer.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 14.01.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.ComponentModel;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using Ch.Elca.Iiop.Cdr;
using Ch.Elca.Iiop.Util;
using Ch.Elca.Iiop.Idl;
using Ch.Elca.Iiop.CorbaObjRef;
using Corba;
using omg.org.CORBA;

namespace Ch.Elca.Iiop.Marshalling {

    /// <summary>
    /// base class for all Serializer.
    /// </summary>
    internal abstract class Serializer {

        #region IConstructors

        internal Serializer() {
        }

        #endregion IConstructors
        #region IMethods

        /// <summary>
        /// serializes the actual value into the given stream
        /// </summary>
        internal abstract void Serialize(object actual, 
                                         CdrOutputStream targetStream);

        /// <summary>
        /// deserialize the value from the given stream
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <returns></returns>
        internal abstract object Deserialize(CdrInputStream sourceStream);
        
        /// <summary>
        /// Creates a serializer for serialising/deserialing a field
        /// </summary>
        protected static Serializer CreateSerializerForField(FieldInfo fieldToSer, SerializerFactory serFactory) {
            Type fieldType = fieldToSer.FieldType;
            AttributeExtCollection fieldAttrs = 
                ReflectionHelper.GetCustomAttriutesForField(fieldToSer, true);
            return serFactory.Create(fieldType, fieldAttrs);
        }                

        /// <summary>
        /// serialises a field of a value-type
        /// </summary>
        /// <param name="fieldToSer"></param>
        protected static void SerializeField(FieldInfo fieldToSer, object actual, Serializer ser,
                                      CdrOutputStream targetStream) {            
            ser.Serialize(fieldToSer.GetValue(actual), targetStream);
        }

        /// <summary>
        /// deserialises a field of a value-type and sets the value
        /// </summary>
        /// <returns>the deserialised value</returns>
        protected static object DeserializeField(FieldInfo fieldToDeser, object actual, Serializer ser,
                                          CdrInputStream sourceStream) {            
            object fieldVal = ser.Deserialize(sourceStream);
            fieldToDeser.SetValue(actual, fieldVal);
            return fieldVal;
        }

        protected void CheckActualNotNull(object actual) {            
            if (actual == null) {
                // not allowed
                throw new BAD_PARAM(3433, CompletionStatus.Completed_MayBe);
            }
            // ok
        }

        #endregion IMethods

    }

    // **************************************************************************************************
    #region serializer for primitive types

    /// <summary>serializes instances of System.Byte</summary>
    internal class ByteSerializer : Serializer {

        #region IMethods

        internal override void Serialize(object actual, 
                                       CdrOutputStream targetStream) {
            targetStream.WriteOctet((byte)actual);
        }

        internal override object Deserialize(CdrInputStream sourceStream) {
            return sourceStream.ReadOctet();
        }

        #endregion IMethods

    }

    /// <summary>serializes instances of System.Boolean</summary> 
    internal class BooleanSerializer : Serializer {

        #region IMethods

        internal override void Serialize(object actual,
                                       CdrOutputStream targetStream) {
            targetStream.WriteBool((bool)actual);
        }

        internal override object Deserialize(CdrInputStream sourceStream) {
            return sourceStream.ReadBool();
        }

        #endregion IMethods

    }

    /// <summary>serializes instances of System.Int16</summary>
    internal class Int16Serializer : Serializer {

        #region IMethods
        
        internal override void Serialize(object actual,
                                       CdrOutputStream targetStream) {
            targetStream.WriteShort((short)actual);
        }

        internal override object Deserialize(CdrInputStream sourceStream) {
            return sourceStream.ReadShort();
        }

        #endregion IMethods

    }
    
    /// <summary>serializes instances of System.Int32</summary>
    internal class Int32Serializer : Serializer {

        #region IMethods
        
        internal override void Serialize(object actual, 
                                       CdrOutputStream targetStream) {
            targetStream.WriteLong((int)actual);
        }

        internal override object Deserialize(CdrInputStream sourceStream) {
            return sourceStream.ReadLong();
        }

        #endregion IMethods

    }

    /// <summary>serializes instances of System.Int64</summary>
    internal class Int64Serializer : Serializer {

        #region IMethods

        internal override void Serialize(object actual,
                                       CdrOutputStream targetStream) {
            targetStream.WriteLongLong((long)actual);
        }

        internal override object Deserialize(CdrInputStream sourceStream) {
            return sourceStream.ReadLongLong();
        }

        #endregion IMethods

    }

    /// <summary>serializes instances of System.Single</summary>
    internal class SingleSerializer : Serializer {

        #region IMethods
    
        internal override void Serialize(object actual,
                                       CdrOutputStream targetStream) {
            targetStream.WriteFloat((float)actual);
        }

        internal override object Deserialize(CdrInputStream sourceStream) {
            return sourceStream.ReadFloat();
        }

        #endregion IMethods

    }

    /// <summary>serializes instances of System.Double</summary>
    internal class DoubleSerializer : Serializer {

        #region IMethods

        internal override void Serialize(object actual,
                                       CdrOutputStream targetStream) {
            targetStream.WriteDouble((double)actual);
        }

        internal override object Deserialize(CdrInputStream sourceStream) {
            return sourceStream.ReadDouble();
        }

        #endregion IMethods

    }

    /// <summary>serializes instances of System.Char</summary>
    internal class CharSerializer : Serializer {

        #region IFields
        
        private bool m_useWide;
        
        #endregion IFields
        #region IConstructors
        
        public CharSerializer(bool useWide) {
            m_useWide = useWide;
        }
        
        #endregion IConstructors
        #region IMethods

        internal override void Serialize(object actual,
                                       CdrOutputStream targetStream) {            
            if (m_useWide) {
                targetStream.WriteWChar((char)actual);
            } else {
                // the high 8 bits of the character is cut off
                targetStream.WriteChar((char)actual);
            }
        }

        internal override object Deserialize(CdrInputStream sourceStream) {            
            char result;
            if (m_useWide) {
                result = sourceStream.ReadWChar();
            } else {
                result = sourceStream.ReadChar();
            }
            return result;
        }

        #endregion IMethods

    }

    /// <summary>serializes instances of System.String which are serialized as string values</summary>
    internal class StringSerializer : Serializer {

        #region IFields
        
        private bool m_useWide;
        
        #endregion IFields
        #region IConstructors
        
        public StringSerializer(bool useWide) {
            m_useWide = useWide;
        }
        
        #endregion IConstructors
        #region IMethods

        internal override void Serialize(object actual,
                                       CdrOutputStream targetStream) {            
            // string may not be null, if StringValueAttriubte is set
            CheckActualNotNull(actual);
            if (m_useWide) {
                targetStream.WriteWString((string)actual);
            } else {
                // encode with selected encoder, this can throw an exception, if an illegal character is encountered
                targetStream.WriteString((string)actual);
            }
        }

        internal override object Deserialize(CdrInputStream sourceStream) {
            object result = "";
            if (m_useWide) {
                result = sourceStream.ReadWString();
            } else {
                result = sourceStream.ReadString();
            }
            return result;
        }

        #endregion IMethods

    }

    #endregion
    // **************************************************************************************************
    
    // **************************************************************************************************    
    #region serializer for marshalbyref types
    
    /// <summary>serializes object references</summary>
    internal class ObjRefSerializer : Serializer {

        #region IFields
        
        private Type m_forType;
        
        #endregion IFields
        #region IConstructors
        
        public ObjRefSerializer(Type forType) {
            m_forType = forType;
        }
        
        #endregion IConstructors
        #region IMethods
        
        internal override void Serialize(object actual, CdrOutputStream targetStream) {
            if (actual == null) { 
                WriteNullReference(targetStream); // null must be handled specially
                return;
            }
            MarshalByRefObject target = (MarshalByRefObject) actual; // this could be a proxy or the server object
            
            // create the IOR for this URI, possibilities:
            // is a server object -> create ior from key and channel-data
            // is a proxy --> create IOR from url
            //                url possibilities: IOR:--hex-- ; iiop://addr/key ; corbaloc::addr:key ; ...
            Ior ior = null;
            if (RemotingServices.IsTransparentProxy(target)) {
                // proxy
                string url = RemotingServices.GetObjectUri(target);
                Debug.WriteLine("marshal object reference (from a proxy) with url " + url);
                Type actualType = actual.GetType();
                if (actualType.Equals(ReflectionHelper.MarshalByRefObjectType) &&
                    m_forType.IsInterface && m_forType.IsInstanceOfType(actual)) {
                    // when marshalling a proxy, without having adequate type information from an IOR
                    // and formal is an interface, use interface type instead of MarshalByRef to
                    // prevent problems on server
                    actualType = m_forType;
                }
                // get the repository id for the type of this MarshalByRef object
                string repositoryID = Repository.GetRepositoryID(actualType);
                if (actualType.Equals(ReflectionHelper.MarshalByRefObjectType)) { 
                    repositoryID = ""; 
                } // CORBA::Object has "" repository id
                ior = IiopUrlUtil.CreateIorForUrl(url, repositoryID);
            } else {
                // server object
                ior = IiopUrlUtil.CreateIorForObjectFromThisDomain(target);
            }

            Debug.WriteLine("connection information for objRef, nr of profiles: " + ior.Profiles.Length);

            // now write the IOR to the stream
            ior.WriteToStream(targetStream);
        }

        private void WriteNullReference(CdrOutputStream targetStream) {
            Ior ior = new Ior("", new IorProfile[0]);
            ior.WriteToStream(targetStream); // write the null reference to the stream
        }

        internal override object Deserialize(CdrInputStream sourceStream) {
            // reads the encoded IOR from this stream
            Ior ior = new Ior(sourceStream);
            if (ior.IsNullReference()) { 
                return null; 
            } // received a null reference, return null
            // create a url from this ior:
            string url = ior.ToString(); // use stringified form of IOR as url --> do not lose information
            Type interfaceType;
            if (!Repository.IsInterfaceCompatible(m_forType, ior.TypID, out interfaceType)) {
                // will be checked on first call remotely with is_a; don't do a remote check here, 
                // because not an appropriate place for a remote call; also safes call if ior not used.
                Trace.WriteLine(String.Format("ObjRef deser, not locally verifiable, that ior type-id " +
                                              "{0} is compatible to required formal type {1}. " + 
                                              "Remote check will be done on first call to this ior.",
                                              ior.TypID, m_forType.FullName));                
            }                        
            // create a proxy
            object proxy = RemotingServices.Connect(interfaceType, url);
            return proxy;
        }
        
        #endregion IMethods

    }

    #endregion

    // **************************************************************************************************
    // ********************************* Serializer for value types *************************************
    // **************************************************************************************************

    /// <summary>standard serializer for pass by value object</summary>
    /// <remarks>if a CLS struct should be serialized as IDL struct and not as ValueType, use the IDLStruct Serializer</remarks>
    internal class ValueObjectSerializer : Serializer {

        #region Types
        
        /// <summary>
        /// Serialises/deserialises a concrete instance of a value type.
        /// This is a helper to improve performance, and is only used by the ValueObjectSerializer.
        /// It's not directly selected by the SerializerFactory as Serializer.        
        /// </summary>
        /// <remarks>
        /// This class can't inherit from the Serializer base class, because additional
        /// context information are needed.
        /// This class additionally doesn't inherit from Serializer base class, because 
        /// it should not be used like other Serializers.
        /// </remarks>
        internal class ValueConcreteInstanceSerializer {
            
            private Type m_forConcreteType;            
            private Type m_forConcreteInstanceType;  
            private bool m_isCustomMarshalled;
            private FieldInfo[] m_fieldInfos;
            private Serializer[] m_fieldSerializers;
            private SerializerFactory m_serFactory;
            private bool m_initalized;
            
            internal ValueConcreteInstanceSerializer(Type concreteType, SerializerFactory serFactory) {
                m_forConcreteType = concreteType;
                m_serFactory = serFactory;
                // determine instance to instantiate for concreteType: 
                // check for a value type implementation class
                m_forConcreteInstanceType = DetermineInstanceToCreateType(m_forConcreteType);
                m_isCustomMarshalled = CheckForCustomMarshalled(m_forConcreteType);                
            }
            
            private Type DetermineInstanceToCreateType(Type concreteType) {
                Type result = concreteType;
                object[] implAttr =
                    result.GetCustomAttributes(ReflectionHelper.ImplClassAttributeType, false);
                if ((implAttr != null) && (implAttr.Length > 0)) {
                    if (implAttr.Length > 1) {
                        // invalid type: actualType, only one ImplClassAttribute allowed
                        throw new INTERNAL(923, CompletionStatus.Completed_MayBe);
                    }
                    ImplClassAttribute implCl = (ImplClassAttribute)implAttr[0];
                    // get the type
                    result = Repository.GetValueTypeImplClass(implCl.ImplClass);
                    if (result == null) {
                        Trace.WriteLine("implementation class : " + implCl.ImplClass +
                                        " of value-type: " + concreteType + " couldn't be found");
                        throw new NO_IMPLEMENT(1, CompletionStatus.Completed_MayBe, implCl.ImplClass);
                    }
                }
                // type must not be abstract for beeing instantiable
                if (result.IsAbstract) {
                    // value-type couln't be instantiated: actualType
                    throw new NO_IMPLEMENT(931, CompletionStatus.Completed_MayBe);
                }
                return result;
            }
            
            /// <summary>checks, if custom marshalling must be used</summary>
            private bool CheckForCustomMarshalled(Type forType) {
                // subclasses of a custom marshalled type are automatically also custom marshalled: CORBA-spec-99-10-07: page 3-27
                return ReflectionHelper.ICustomMarshalledType.IsAssignableFrom(forType);
            }
            
            /// <summary>checks, if the type is an implementation of a value-type</summary>
            /// <remarks>fields of implementation classes are not serialized/deserialized</remarks>
            private bool IsImplClass(Type forType) {
                Type baseType = forType.BaseType;
                if (baseType != null) {
                    AttributeExtCollection attrs = AttributeExtCollection.ConvertToAttributeCollection(
                                                        baseType.GetCustomAttributes(false));
                    if (attrs.IsInCollection(ReflectionHelper.ImplClassAttributeType)) {
                        ImplClassAttribute implAttr = (ImplClassAttribute)
                                                      attrs.GetAttributeForType(ReflectionHelper.ImplClassAttributeType);
                        Type implClass = Repository.GetValueTypeImplClass(implAttr.ImplClass);
                        if (implClass == null) {
                            Trace.WriteLine("implementation class : " + implAttr.ImplClass +
                                        " of value-type: " + baseType + " couldn't be found");
                            throw new NO_IMPLEMENT(1, CompletionStatus.Completed_MayBe, implAttr.ImplClass);
                        }
                        if (implClass.Equals(forType)) {
                            return true;
                        }
                    }
                }
                return false;
            }            
            
            /// <summary>
            /// initalize the serializer for usage. Before, the serializer is non-usable
            /// </summary>
            internal void Initalize() {
                if (m_initalized) {
                    throw new BAD_INV_ORDER(1678, CompletionStatus.Completed_MayBe);
                }
                if (!m_isCustomMarshalled) {
                    // could map fields also in consturctor, because no recursive
                    // occurence of ValueConcreteInstanceSerializer:
                    // ValueObjectSerializers are always created in between breaking the
                    // possible recursive chain
                    // but to be consistent with IdlStruct, do it this way.
                    DetermineFieldSerializers(m_serFactory);
                }                
                m_initalized = true;
            }   
        
            private void CheckInitalized() {
                if (!m_initalized) {
                    throw new BAD_INV_ORDER(1678, CompletionStatus.Completed_MayBe);
                }
            }            
            
            private void DetermineFieldSerializers(SerializerFactory serFactory) {
                ArrayList allFields = new ArrayList();
                ArrayList allSerializers = new ArrayList();
                Stack typeHierarchy = CreateTypeHirarchyStack(m_forConcreteType);
                while (typeHierarchy.Count > 0) {
                    Type demarshalType = (Type)typeHierarchy.Pop();
                    // reads all fields declared in the Type: no inherited fields
                    FieldInfo[] fields = ReflectionHelper.GetAllDeclaredInstanceFields(demarshalType);
                    allFields.AddRange(fields);
                    for (int i = 0; i < fields.Length; i++) {
                        allSerializers.Add(CreateSerializerForField(fields[i], serFactory));
                    }
                }
                m_fieldInfos = (FieldInfo[])allFields.ToArray(typeof(FieldInfo));
                m_fieldSerializers =(Serializer[]) allSerializers.ToArray(typeof(Serializer));
            }

            /// <summary>
            /// creates a Stack with the inheritance information for the Type forType.
            /// </summary>
            private Stack CreateTypeHirarchyStack(Type forType) {
                Stack typeHierarchy = new Stack();
                Type currentType = forType;
                while (currentType != null) {
                    if (!IsImplClass(currentType)) { // ignore impl-classes in serialization code
                        typeHierarchy.Push(currentType);
                    }
    
                    currentType = currentType.BaseType;
                    if (currentType == ReflectionHelper.ObjectType || currentType == ReflectionHelper.ValueTypeType ||
                       (ClsToIdlMapper.IsMappedToAbstractValueType(currentType,
                                                                   AttributeExtCollection.EmptyCollection))) { // abstract value types are not serialized
                        break;
                    }
                }
                return typeHierarchy;
            }
            
            /// <summary>writes all the fields of the instance</summary>
            private void WriteFields(object instance, 
                                     CdrOutputStream targetStream) {
                for (int i = 0; i < m_fieldInfos.Length; i++) {
                    if (!m_fieldInfos[i].IsNotSerialized) { // do not serialize transient fields
                        SerializeField(m_fieldInfos[i], instance, m_fieldSerializers[i],
                                       targetStream);
                    }
                }
            }
                        
            /// <summary>reads and sets the all the fields of the instance</summary>
            private void ReadFields(object instance,
                                    CdrInputStream sourceStream) {
                for (int i = 0; i < m_fieldInfos.Length; i++) {
                    if (!m_fieldInfos[i].IsNotSerialized) { // do not serialize transient fields
                        DeserializeField(m_fieldInfos[i], instance, m_fieldSerializers[i],
                                         sourceStream);
                    }
                }                                
            }                                    
            
            internal void Serialize(object actual, CdrOutputStream targetStream) {
                CheckInitalized();
                uint valueTag = CdrStreamHelper.MIN_VALUE_TAG; // value-tag with no option set
                // attentition here: if formal type represents an IDL abstract interface, writing no type information is not ok.
                // do not use no typing information option, because java orb can't handle it
                valueTag = valueTag | 0x00000002;
                StreamPosition indirPos = targetStream.WriteIndirectableInstanceTag(valueTag);
                string repId = "";
                if (!IsImplClass(actual.GetType())) {
                    repId = Repository.GetRepositoryID(actual.GetType());
                } else { // an impl-class is not serialized, because it's not known at the receiving ORB
                    repId = Repository.GetRepositoryID(actual.GetType().BaseType);
                }
                targetStream.WriteIndirectableString(repId, IndirectionType.IndirRepId,
                                                     IndirectionUsage.ValueType);

                // add instance to indirection table
                targetStream.StoreIndirection(actual,
                                              new IndirectionInfo(indirPos.GlobalPosition,
                                                                  IndirectionType.IndirValue,
                                                                  IndirectionUsage.ValueType));

                // value content
                if (!m_isCustomMarshalled) {
                    WriteFields(actual, targetStream);
                } else {
                    // custom marshalled
                    if (!(actual is ICustomMarshalled)) {
                        // can't deserialise custom value type, because ICustomMarshalled not implented
                        throw new INTERNAL(909, CompletionStatus.Completed_MayBe);
                    }
                    ((ICustomMarshalled)actual).Serialize(
                        new DataOutputStreamImpl(targetStream, m_serFactory));
                }                
            }
            
            internal object Deserialize(CdrInputStream sourceStream,
                                        StreamPosition instanceStartPos, uint valueTag) {
                CheckInitalized();
                object result = Activator.CreateInstance(m_forConcreteInstanceType);
                // store indirection info for this instance, if another instance contains a reference to this one
                sourceStream.StoreIndirection(new IndirectionInfo(instanceStartPos.GlobalPosition,
                                                                  IndirectionType.IndirValue,
                                                                  IndirectionUsage.ValueType), 
                                              result);

                // now the value fields follow
                sourceStream.BeginReadValueBody(valueTag);                
                
                // value content
                if (!m_isCustomMarshalled) {
                    ReadFields(result, 
                               sourceStream);
                } else {
                    // custom marshalled
                    if (!(result is ICustomMarshalled)) {
                        // can't deserialise custom value type, because ICustomMarshalled not implented
                        throw new INTERNAL(909, CompletionStatus.Completed_MayBe);
                    }
                    ((ICustomMarshalled)result).Deserialise(
                        new DataInputStreamImpl(sourceStream, m_serFactory));
                }
                
                sourceStream.EndReadValue(valueTag);
                return result;             
            }
            
            
        }
        
        #endregion Types
        
        #region IFields
        
        private Type m_forType;        
        private SerializerFactory m_serFactory;
        
        #endregion IFields
        #region IConstructors
        
        internal ValueObjectSerializer(Type forType, 
                                       SerializerFactory serFactory) {
            m_forType = forType;
            m_serFactory = serFactory;
        }
        
        #endregion IConstructors
        #region IMethods


        internal override void Serialize(object actual,
                                         CdrOutputStream targetStream) {
            if (actual == null) {
                targetStream.WriteULong(0); // write a null-value
                return;
            }

            // if value is already in indirection table, write indirection
            if (targetStream.IsPreviouslyMarshalled(actual,
                                                    IndirectionType.IndirValue,
                                                    IndirectionUsage.ValueType)) {
                // write indirection
                targetStream.WriteIndirection(actual);
                return; // write completed
            } else {
                // serialize a concrete instance
                ValueConcreteInstanceSerializer valConSer =
                    m_serFactory.CreateConcreteValueTypeSer(actual.GetType());
                valConSer.Serialize(actual, targetStream);
            }
        }

        internal override object Deserialize(CdrInputStream sourceStream) {
            sourceStream.BeginReadNewValue();
            StreamPosition instanceStartPos;
            bool isIndirection;
            uint valueTag = sourceStream.ReadInstanceOrIndirectionTag(out instanceStartPos, 
                                                                      out isIndirection);
            if (isIndirection) {
                // return indirected value
                // resolve indirection:
                StreamPosition indirectionPosition = sourceStream.ReadIndirectionOffset();
                return sourceStream.GetObjectForIndir(new IndirectionInfo(indirectionPosition.GlobalPosition,
                                                                          IndirectionType.IndirValue,
                                                                          IndirectionUsage.ValueType),
                                                      true);
            } else {             
                if (IsNullValue(valueTag)) {
                    return null;
                }
                
                // non-null value
                if (HasCodeBaseUrl(valueTag)) {
                    HandleCodeBaseUrl(sourceStream);
                }

                Type actualType = GetActualType(m_forType, sourceStream, valueTag);
                if (!m_forType.IsAssignableFrom(actualType)) {
                    // invalid implementation class of value type: 
                    // instance.GetType() is incompatible with: formal
                    throw new BAD_PARAM(903, CompletionStatus.Completed_MayBe);
                }
                ValueConcreteInstanceSerializer valConSer =
                    m_serFactory.CreateConcreteValueTypeSer(actualType);
                return valConSer.Deserialize(sourceStream, instanceStartPos, valueTag);
            }
        }

        private bool IsNullValue(uint valueTag) {
            return valueTag == 0;
        }

        private bool HasCodeBaseUrl(uint valueTag) {
            return ((valueTag & 0x00000001) > 0);
        }

        private void HandleCodeBaseUrl(CdrInputStream sourceStream) {
            sourceStream.ReadIndirectableString(IndirectionType.CodeBaseUrl,
                                                IndirectionUsage.ValueType,
                                                false);
        }

        /// <summary>
        /// gets the type of which the actual parameter is / should be ...
        /// </summary>
        private Type GetActualType(Type formal, CdrInputStream sourceStream, uint valueTag) {            
            Type actualType = null;
            switch (valueTag & 0x00000006) {
                case 0: 
                    // actual = formal-type
                    actualType = formal;
                    break;
                case 2:
                    // single repository-id follows
                    string repId = sourceStream.ReadIndirectableString(IndirectionType.IndirRepId,
                                                                       IndirectionUsage.ValueType,
                                                                       false);
                    actualType = Repository.GetTypeForId(repId);
                    if (actualType == null) { 
                        // repository id used is unknown: repId
                        throw new NO_IMPLEMENT(941, CompletionStatus.Completed_MayBe, repId);
                    }
                    break;
                case 6:
                    // TODO: handle indirections here
                    // a list of repository-id's
                    int nrOfIds = sourceStream.ReadLong();
                    if (nrOfIds == 0) { 
                        // a list of repository-id's for type-information must contain at least one element
                        throw new MARSHAL(935, CompletionStatus.Completed_MayBe);
                    }
                    string mostDerived = sourceStream.ReadString(); // use only the most derived type, no truncation allowed
                    for (int i = 1; i < nrOfIds; i++) { 
                        sourceStream.ReadString(); 
                    }
                    actualType = Repository.GetTypeForId(mostDerived);
                    break;
                default:
                    // invalid value-tag found: " + valueTag
                    throw new MARSHAL(937, CompletionStatus.Completed_MayBe);
            }
            if (ClsToIdlMapper.IsInterface(actualType)) { 
                // can't instantiate value-type of type: actualType
                throw new NO_IMPLEMENT(945, CompletionStatus.Completed_MayBe);
            }
            return actualType;
        }

        #endregion IMethods

    }

    /// <summary>serializes an non boxed value as an IDL boxed value and deserialize an IDL boxed value as an unboxed value</summary>
    /// <remarks>do not use this serializer with instances of BoxedValues which should not be boxed or unboxed</remarks>
    internal class BoxedValueSerializer : Serializer {

        #region IFields

        private ValueObjectSerializer m_valueSer;
        private bool m_convertMultiDimArray = false;
        private Type m_forType;

        #endregion IFields
        #region IConstructors
        
        public BoxedValueSerializer(Type forType, bool convertMultiDimArray,
                                    SerializerFactory serFactory) {
            CheckFormalIsBoxedValueType(forType);
            m_forType = forType;
            m_convertMultiDimArray = convertMultiDimArray;
            m_valueSer = new ValueObjectSerializer(forType, serFactory);            
        }

        #endregion IConstructors
        #region IMethods
        
        private void CheckFormalIsBoxedValueType(Type formal) {
            if (!formal.IsSubclassOf(ReflectionHelper.BoxedValueBaseType)) { 
                // BoxedValueSerializer can only serialize formal types, 
                // which are subclasses of BoxedValueBase
                throw new INTERNAL(10041, CompletionStatus.Completed_MayBe);
            }
        }
        
        internal override void Serialize(object actual,
                                         CdrOutputStream targetStream) {
            Debug.WriteLine("Begin serialization of boxed value type");
            // perform a boxing
            object boxed = null;
            if (actual != null) {
                if (m_convertMultiDimArray) {
                    // actual is a multi dimensional array, which must be first converted to a jagged array
                    actual = 
                        BoxedArrayHelper.ConvertMoreDimToNestedOneDimChecked(actual);
                }
                boxed = Activator.CreateInstance(m_forType, new object[] { actual } );
            }
            m_valueSer.Serialize(boxed, targetStream);
        }

        internal override object Deserialize(CdrInputStream sourceStream) {  
            Debug.WriteLine("Begin deserialization of boxed value type");
            BoxedValueBase boxedResult = (BoxedValueBase) 
                m_valueSer.Deserialize(sourceStream);
            object result = null;
            if (boxedResult != null) {
                // perform an unboxing
                result = boxedResult.Unbox();
            if (m_convertMultiDimArray) {
                // result is a jagged arary, which must be converted to a true multidimensional array
                    result = BoxedArrayHelper.ConvertNestedOneDimToMoreDimChecked(result);
                }
            }

            Debug.WriteLine("unboxed result of boxedvalue-ser: " + result);
            return result;
        }

        #endregion IMethods

    }

    /// <summary>
    /// this class serializes .NET structs, which were mapped from an IDL-struct
    /// </summary>
    internal class IdlStructSerializer : Serializer {

        #region IFields

        private Serializer[] m_fieldSerializers;
		private FieldInfo[] m_fields;
        private Type m_forType;
        private bool m_initalized = false;
        private SerializerFactory m_serFactory;

        #endregion IFields
        #region IConstructors

        /// <summary>
        /// creates the idl struct serializer. To prevent issues with
        /// recurive elements, the serializer must be initalized in
        /// an additional step by calling Initalize.
        /// The serializer must be cached to return it for recursive requests.
        /// </summary>
        internal IdlStructSerializer(Type forType, SerializerFactory serFactory) : base() {
            m_forType = forType;
            m_serFactory = serFactory;			
        }

        #endregion IConstructors
        #region IMethods
        
        /// <summary>
        /// initalize the serializer for usage. Before, the serializer is non-usable
        /// </summary>        
        internal void Initalize() {
            if (m_initalized) {
                throw new BAD_INV_ORDER(1678, CompletionStatus.Completed_MayBe);
            }
            DetermineFieldMapping();
            m_initalized = true;
        }   
        
        private void CheckInitalized() {
            if (!m_initalized) {
                throw new BAD_INV_ORDER(1678, CompletionStatus.Completed_MayBe);
            }
        }

        private void DetermineFieldMapping() {
            m_fields = ReflectionHelper.GetAllDeclaredInstanceFields(m_forType);
            m_fieldSerializers = new Serializer[m_fields.Length];
            for (int i = 0; i < m_fields.Length; i++) {
                m_fieldSerializers[i] = CreateSerializerForField(m_fields[i], m_serFactory);           
            }
        }
    
        internal override object Deserialize(CdrInputStream sourceStream) {
            CheckInitalized();
            object instance = Activator.CreateInstance(m_forType);
            for (int i = 0; i < m_fieldSerializers.Length; i++) {
                DeserializeField(m_fields[i], instance, m_fieldSerializers[i], sourceStream);                
            }
            return instance;
        }

        internal override void Serialize(object actual, CdrOutputStream targetStream) {
            CheckInitalized();
            for (int i = 0; i < m_fieldSerializers.Length; i++) {
                SerializeField(m_fields[i], actual, m_fieldSerializers[i], targetStream);                
            }
        }
    
        #endregion IMethods

    }

    internal class IdlUnionSerializer : Serializer {

        #region Constants

        private const string GET_FIELD_FOR_DISCR_METHOD_NAME = UnionGenerationHelper.GET_FIELD_FOR_DISCR_METHOD;

        private const string DISCR_FIELD_NAME = UnionGenerationHelper.DISCR_FIELD_NAME;

        private const string INITALIZED_FIELD_NAME = UnionGenerationHelper.INIT_FIELD_NAME;

        #endregion Constants
        #region IFields
        
        private Type m_forType;        
        private FieldInfo m_discrField;
        private FieldInfo m_initField;
        private Serializer m_discrSerializer;
        private SerializerFactory m_serFactory;
        
        #endregion IFields
        #region IConstructors
        
        internal IdlUnionSerializer(Type forType, SerializerFactory serFactory) {
            m_forType = forType;
            // disciminator can't be the same type then the union -> therefore no recursive problem here.
            m_discrField = GetDiscriminatorField(m_forType);
            m_discrSerializer = CreateSerializerForField(m_discrField, serFactory);
            m_initField = GetInitalizedField(m_forType);
            m_serFactory = serFactory;
        }        
        
        #endregion IConstructors
        #region IMethods

        private FieldInfo GetDiscriminatorField(Type formal) {
            FieldInfo discrValField = formal.GetField(DISCR_FIELD_NAME, 
                                                      BindingFlags.Instance | BindingFlags.NonPublic);
            if (discrValField == null) {
                throw new INTERNAL(898, CompletionStatus.Completed_MayBe);
            }            
            return discrValField;
        }
        
        private FieldInfo GetValFieldForDiscriminator(Type formal, object discrValue) {
            MethodInfo getCurrentField = formal.GetMethod(GET_FIELD_FOR_DISCR_METHOD_NAME, 
                                                          BindingFlags.Static | BindingFlags.NonPublic);
            if (getCurrentField == null) {
                throw new INTERNAL(898, CompletionStatus.Completed_MayBe);
            }
            return (FieldInfo)getCurrentField.Invoke(null, new object[] { discrValue });
        }

        private FieldInfo GetInitalizedField(Type formal) {
            FieldInfo initalizedField = formal.GetField(INITALIZED_FIELD_NAME, 
                                                        BindingFlags.Instance | BindingFlags.NonPublic);
            if (initalizedField == null) {
                throw new INTERNAL(898, CompletionStatus.Completed_MayBe);
            }            
            return initalizedField;
        }

        internal override object Deserialize(CdrInputStream sourceStream) {            
            // instantiate the resulting union
            object result = Activator.CreateInstance(m_forType);
            // deserialise discriminator value            
            object discrVal = DeserializeField(m_discrField, result, m_discrSerializer, sourceStream);
            
            // determine value to deser
            FieldInfo curField = GetValFieldForDiscriminator(m_forType, discrVal);
            if (curField != null) {
                // deserialise value
                Serializer curFieldSer = CreateSerializerForField(curField, m_serFactory);
                DeserializeField(curField, result, curFieldSer, sourceStream);
            }            
            m_initField.SetValue(result, true);
            return result;
        }

        internal override void Serialize(object actual, CdrOutputStream targetStream) {            
            bool isInit = (bool)m_initField.GetValue(actual);
            if (isInit == false) {
                throw new BAD_PARAM(34, CompletionStatus.Completed_MayBe);
            }
            // determine value of the discriminator            
            object discrVal = m_discrField.GetValue(actual);
            // get the field matching the current discriminator
            FieldInfo curField = GetValFieldForDiscriminator(m_forType, discrVal);
            
            m_discrSerializer.Serialize(discrVal, targetStream);
            if (curField != null) {
                // seraialise value
                Serializer curFieldSer = CreateSerializerForField(curField, m_serFactory);
                SerializeField(curField, actual, curFieldSer, targetStream);
            } 
            // else:  case outside covered discr range, do not serialise value, only discriminator
        }

        #endregion IMethods
    }

    /// <summary>serailizes an instances as IDL abstract-value</summary>
    internal class AbstractValueSerializer : Serializer {

        #region IFields
        
        private ValueObjectSerializer m_valObjectSer;

        #endregion IFields
        #region IConstructors
        
        internal AbstractValueSerializer(Type forType, SerializerFactory serFactory) {
            m_valObjectSer = new ValueObjectSerializer(forType, serFactory);
        }
        
        #endregion IConstructors
        #region IMethods

        internal override void Serialize(object actual,
                                         CdrOutputStream targetStream) {
            if (actual != null) {
                // check if actual parameter is an IDL-struct: 
                // this is an illegal parameter for IDL-abstract value parameters
                if (ClsToIdlMapper.IsMarshalledAsStruct(actual.GetType())) {
                    // IDL-struct illegal parameter for formal type abstract value (actual type: actual.GetType() )
                    throw new MARSHAL(20011, CompletionStatus.Completed_MayBe);
                }
                // check if it's a concrete value-type:
                if (!ClsToIdlMapper.IsMappedToConcreteValueType(actual.GetType())) {
                    // only a value type is possible as acutal value for a formal type abstract value / value base, actual type: actual.GetType() )
                    throw new MARSHAL(20012, CompletionStatus.Completed_MayBe);
                }
            }
            // if actual parameter is ok, serialize as idl-value object
            m_valObjectSer.Serialize(actual, targetStream);
        }

        internal override object Deserialize(CdrInputStream sourceStream) {
            // deserialise as IDL-value-type
            return m_valObjectSer.Deserialize(sourceStream);
        }

        #endregion IMethods

    }

    /// <summary>serializes an instance of the class System.Type</summary>
    internal class TypeSerializer : Serializer {

        #region IFields
        
        private TypeCodeSerializer m_typeCodeSer = new TypeCodeSerializer();

        #endregion IFields
        #region IMethods

        internal override void Serialize(object actual, CdrOutputStream targetStream) {
            omg.org.CORBA.TypeCode tc;
            tc = Repository.CreateTypeCodeForType((Type)actual, AttributeExtCollection.EmptyCollection);
            m_typeCodeSer.Serialize(tc, targetStream);
        }

        internal override object Deserialize(CdrInputStream sourceStream) {            
            omg.org.CORBA.TypeCode tc = 
                (omg.org.CORBA.TypeCode)m_typeCodeSer.Deserialize(sourceStream);
            Type result = null;
            if (!(tc is NullTC)) {
                result = Repository.GetTypeForTypeCode(tc);
            }
            return result;
        }

        #endregion IMethods

    }
    
    
    /// <summary>serializes enums</summary>
    internal class EnumSerializer : Serializer {
        
        #region IFields
        
        private Serializer m_netEnumValSerializer;   
        private Type m_forType;
        
        #endregion IFields
        #region IConstructors
        
        internal EnumSerializer(Type forType, SerializerFactory serFactory) {
            m_forType = forType;
            // check for IDL-enum mapped to a .NET enum
            AttributeExtCollection attrs = ReflectionHelper.GetCustomAttributesForType(m_forType, true);
            bool isIdlEnum = (attrs.IsInCollection(ReflectionHelper.IdlEnumAttributeType));
            if (!isIdlEnum) {
                Type underlyingType = Enum.GetUnderlyingType(m_forType);
                // undelying type is not the same than the enum -> no problem with recursive serializers
                m_netEnumValSerializer =
                    serFactory.Create(underlyingType, AttributeExtCollection.EmptyCollection);
            }
        }
        
        #endregion IConstructors
        #region IMethods

        internal override void Serialize(object actual,
                                         CdrOutputStream targetStream) {
            // check for IDL-enum mapped to a .NET enum
            if (m_netEnumValSerializer == null) {
                // idl enum's are mapped to .NET enums with long base-type, therefore all possible 2^32 idl-values can be represented
                int enumVal = (int) actual;
                targetStream.WriteULong((uint)enumVal);
            } else {
                // map to the base-type of the enum, write the value of the enum
                m_netEnumValSerializer.Serialize(actual, targetStream);
            }
        
        }

        internal override object Deserialize(CdrInputStream sourceStream) {
            // check for IDL-enum mapped to a .NET enum
            if (m_netEnumValSerializer == null) {
                uint enumVal = sourceStream.ReadULong();
                return Enum.ToObject(m_forType, enumVal);
            } else {
                // .NET enum handled with .NET to IDL mapping                                
                object val = m_netEnumValSerializer.Deserialize(sourceStream);
                if (!Enum.IsDefined(m_forType, val)) { 
                    // illegal enum value for enum: formal, val: val
                    throw new BAD_PARAM(10041, CompletionStatus.Completed_MayBe);
                }
                return Enum.ToObject(m_forType, val);
            }
        }
    
        #endregion IMethods

    }
    
    /// <summary>
    /// Serializer for cls flags mapped to idl equivalents
    /// </summary>
    internal class FlagsSerializer : Serializer {
        
        #region IFields
        
        private Serializer m_netFlagsValSerializer;   
        private Type m_forType;
        
        #endregion IFields
        #region IConstructors
        
        internal FlagsSerializer(Type forType, SerializerFactory serFactory) {
            m_forType = forType;
            Type underlyingType = Enum.GetUnderlyingType(m_forType);
            // undelying type is not the same than the flags enum -> no problem with recursive serializers
            // flags are mapped to the corresponding underlying type in idl, because no
            // flags concept in idl
            m_netFlagsValSerializer =
                serFactory.Create(underlyingType, AttributeExtCollection.EmptyCollection);
        }
        
        #endregion IConstructors
        #region IMethods
        
        internal override void Serialize(object actual,
                                         CdrOutputStream targetStream) {
            // map to the base-type of the enum, write the value of the enum
            m_netFlagsValSerializer.Serialize(actual, targetStream);
        }

        
        internal override object Deserialize(CdrInputStream sourceStream) {            
            // .NET flags handled with .NET to IDL mapping
            object val = m_netFlagsValSerializer.Deserialize(sourceStream);
            // every value is allowed for flags -> therefore no checks
            return Enum.ToObject(m_forType, val);
        }
        
        #endregion IMethods
        
    }

    /// <summary>serializes idl sequences</summary>
    internal class IdlSequenceSerializer : Serializer {
        
        #region IFields
        
        private int m_bound;
        private Type m_forTypeElemType;
        private Serializer m_elementSerializer;
        
        #endregion IFields
        #region IConstructors
        
        public IdlSequenceSerializer(Type forType, AttributeExtCollection elemAttrs,
                                     int bound, SerializerFactory serFactory) {
            m_forTypeElemType = forType.GetElementType();
            m_bound = bound;    
            // element is not the same than the sequence -> therefore problems with recursion
            DetermineElementSerializer(m_forTypeElemType, elemAttrs, serFactory);
        }
        
        #endregion IConstructors
        #region IMethods

        private void DetermineElementSerializer(Type elemType,
												AttributeExtCollection elemAttrs,
                                                SerializerFactory serFactory) {
			m_elementSerializer =
                serFactory.Create(elemType, elemAttrs);
        }

        /// <summary>
        /// checks, if parameter to serialise does not contain more elements than allowed
        /// </summary>
        private void CheckBound(uint sequenceLength) {
            if (IdlSequenceAttribute.IsBounded(m_bound) && (sequenceLength > m_bound)) {
                throw new BAD_PARAM(3434, CompletionStatus.Completed_MayBe);
            }
        }
        
        internal override void Serialize(object actual, CdrOutputStream targetStream) {
            Array array = (Array) actual;
            // not allowed for a sequence:
            CheckActualNotNull(array);
            CheckBound((uint)array.Length);
            targetStream.WriteULong((uint)array.Length);
            // serialize sequence elements            
            for (int i = 0; i < array.Length; i++) {
                // it's more efficient to not determine serialise for each element; instead use cached ser
                m_elementSerializer.Serialize(array.GetValue(i), targetStream);
            }
        }

        internal override object Deserialize(CdrInputStream sourceStream) {
            // mapped from an IDL-sequence
            uint nrOfElements = sourceStream.ReadULong();
            CheckBound(nrOfElements);
            
            Array result = Array.CreateInstance(m_forTypeElemType, (int)nrOfElements);
            // serialize sequence elements                        
            for (int i = 0; i < nrOfElements; i++) {
                // it's more efficient to not determine serialise for each element; instead use cached ser
                object entry = m_elementSerializer.Deserialize(sourceStream);
                result.SetValue(entry, i);
            }
            return result;
        }

        #endregion IMethods

    }


    /// <summary>serialises IDL-arrays</summary>
    internal class IdlArraySerializer : Serializer {

        #region IFields
        
        private int[] m_dimensions;
        private Type m_forTypeElemType;
        private Serializer m_elementSer;
        
        #endregion IFields
        #region IConstructors
        
        public IdlArraySerializer(Type forType, AttributeExtCollection elemAttributes, 
                                  int[] dimensions, SerializerFactory serFactory) {
            m_dimensions = dimensions;    
            m_forTypeElemType = forType.GetElementType();
            // element is not the same than the sequence -> therefore problems with recursion
            m_elementSer = serFactory.Create(m_forTypeElemType, elemAttributes);
        }
        
        #endregion IConstructors
        #region IMethods

        private void CheckInstanceDimensions(Array array) {
            if (m_dimensions.Length != array.Rank) {
                throw new BAD_PARAM(3436, CompletionStatus.Completed_MayBe);
            }
            for (int i = 0; i < m_dimensions.Length; i++) {
                if (m_dimensions[i] != array.GetLength(i)) {
                    throw new BAD_PARAM(3437, CompletionStatus.Completed_MayBe);
                }
            }
        } 


        private void SerialiseDimension(Array array, Serializer elementSer, CdrOutputStream targetStream,
                                        int[] indices, int currentDimension) {
            if (currentDimension == m_dimensions.Length) {
                object value = array.GetValue(indices);
                elementSer.Serialize(value, targetStream);
            } else {
                // the first dimension index in the array is increased slower than the second and so on ...
                for (int j = 0; j < m_dimensions[currentDimension]; j++) {
                    indices[currentDimension] = j;                    
                    SerialiseDimension(array, elementSer, targetStream, indices, currentDimension + 1);
                }
            }
        }
        
        internal override void Serialize(object actual, CdrOutputStream targetStream) {
            Array array = (Array) actual;
                // not allowed for an idl array:
            CheckActualNotNull(array);
            CheckInstanceDimensions(array);
            // get marshaller for elemtype                        
            SerialiseDimension(array, m_elementSer, targetStream, new int[m_dimensions.Length], 0);
        }

        private void DeserialiseDimension(Array array, Serializer elementSer, CdrInputStream sourceStream,
                                          int[] indices, int currentDimension) {
            if (currentDimension == array.Rank) {
                object entry = elementSer.Deserialize(sourceStream);
                array.SetValue(entry, indices);
            } else {
                // the first dimension index in the array is increased slower than the second and so on ...
                for (int j = 0; j < m_dimensions[currentDimension]; j++) {
                    indices[currentDimension] = j;                    
                    DeserialiseDimension(array, elementSer, sourceStream, indices, currentDimension + 1);
                }
            }            
        }

        internal override object Deserialize(CdrInputStream sourceStream) {           
            Array result = Array.CreateInstance(m_forTypeElemType, m_dimensions);
            // get marshaller for array element type                        
            DeserialiseDimension(result, m_elementSer, sourceStream, new int[m_dimensions.Length], 0);
            return result;
        }

        #endregion IMethods        

    }

    /// <summary>serializes an instance as IDL-any</summary>
    internal class AnySerializer : Serializer {

        #region SFields

        private static Type s_supInterfaceAttrType = typeof(SupportedInterfaceAttribute);

        #endregion SFields
        #region IFields
        
        private TypeCodeSerializer m_typeCodeSer = new TypeCodeSerializer();
        private SerializerFactory m_serFactory;
        private bool m_formalIsAnyContainer;

        #endregion IFields
        #region IConstructors
        
        internal AnySerializer(SerializerFactory serFactory, bool formalIsAnyContainer) : base() {
            m_serFactory = serFactory;
            m_formalIsAnyContainer = formalIsAnyContainer;
        }
        
        #endregion IConstructors
        #region IMethods

        /// <summary>
        /// get the type to use for serialisation.        
        /// </summary>
        /// <remarks>
        /// If a supported-interface attr is present on a MarshalByRefObject, then, the serialisation
        /// must be done for the interface type and not for the implementation type of the MarshalByRefObject,
        /// because otherwise the deserialisation would result into a problem, because only the sup-if type
        /// is know at deser.
        /// </remarks>
        /// <param name="actual"></param>
        /// <returns></returns>
        private Type DetermineTypeToUse(object actual) {
            if (actual == null) {
                return null;
            }
            Type result = actual.GetType();
            object[] attr = actual.GetType().GetCustomAttributes(s_supInterfaceAttrType, true);
            if (attr != null && attr.Length > 0) {
                SupportedInterfaceAttribute ifType = (SupportedInterfaceAttribute) attr[0];
                result = ifType.FromType;
            }
            return result;
        }
        
        
        internal override void Serialize(object actual,
                                       CdrOutputStream targetStream) {
            TypeCodeImpl typeCode = new NullTC();
            object actualToSerialise = actual;
            Type actualType = null;
            if (actual != null) {
                if (actual.GetType().Equals(ReflectionHelper.AnyType)) {
                    // use user defined type code
                    typeCode = ((Any)actual).Type as TypeCodeImpl;
                    if (typeCode == null) {
                        throw new INTERNAL(457, CompletionStatus.Completed_MayBe);
                    }
                    // type, which should be used to serialise value is determined by typecode!                    
                    if ((!(typeCode is NullTC)) && (!(typeCode is VoidTC))) {
                        actualType = Repository.GetTypeForTypeCode(typeCode); // no .NET type for null-tc, void-tc
                    }
                    actualToSerialise = ((Any)actual).Value;
                } else {
                    // automatic type code creation
                    actualType = DetermineTypeToUse(actual);
                    typeCode = Repository.CreateTypeCodeForType(actualType, 
                                                                AttributeExtCollection.EmptyCollection);
                }
            }
            m_typeCodeSer.Serialize(typeCode, targetStream);
            if (actualType != null) {
                AttributeExtCollection typeAttributes = Repository.GetAttrsForTypeCode(typeCode);                
                Serializer actualSer = 
                    m_serFactory.Create(actualType, typeAttributes);
                actualSer.Serialize(actualToSerialise, targetStream);              
            }
        }

        internal override object Deserialize(CdrInputStream sourceStream) {
            omg.org.CORBA.TypeCode typeCode = 
                (omg.org.CORBA.TypeCode)m_typeCodeSer.Deserialize(sourceStream);
            object result;
            // when returning 0 in a mico-server for any, the typecode used is VoidTC
            if ((!(typeCode is NullTC)) && (!(typeCode is VoidTC))) {
                Type dotNetType = Repository.GetTypeForTypeCode(typeCode);
                AttributeExtCollection typeAttributes = Repository.GetAttrsForTypeCode(typeCode);
                Serializer actualSer = 
                    m_serFactory.Create(dotNetType, typeAttributes);                
                result = actualSer.Deserialize(sourceStream);                
                if (result is BoxedValueBase) {
                    result = ((BoxedValueBase)result).Unbox(); // unboxing the boxed-value, because BoxedValueTypes are internal types, which should not be used by users
                }
            } else {
                result = null;
            }
            if (!m_formalIsAnyContainer) {
                return result;
            } else {
                return new Any(result, typeCode);
            }
        }
        
        #endregion IMethods

    }

    /// <summary>serializes a typecode</summary>
    internal class TypeCodeSerializer : Serializer {
        
        #region IMethods

        internal override object Deserialize(CdrInputStream sourceStream) {

            bool isIndirection;
            StreamPosition indirPos;
            uint kindVal = (uint)sourceStream.ReadInstanceOrIndirectionTag(out indirPos, 
                                                                           out isIndirection);
            if (!isIndirection) {
            
                omg.org.CORBA.TCKind kind = (omg.org.CORBA.TCKind)Enum.ToObject(typeof(omg.org.CORBA.TCKind),
                                                                                (int)kindVal);
                omg.org.CORBA.TypeCodeImpl result;
                switch(kind) {
                    case omg.org.CORBA.TCKind.tk_abstract_interface :
                        result = new omg.org.CORBA.AbstractIfTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_alias:
                        result = new omg.org.CORBA.AliasTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_any:
                        result = new omg.org.CORBA.AnyTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_array:
                        result = new omg.org.CORBA.ArrayTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_boolean:
                        result = new omg.org.CORBA.BooleanTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_char:
                        result = new omg.org.CORBA.CharTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_double:
                        result = new omg.org.CORBA.DoubleTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_enum:
                        result = new omg.org.CORBA.EnumTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_except:
                        result = new omg.org.CORBA.ExceptTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_fixed:
                        throw new NotImplementedException("fixed not implemented");
                    case omg.org.CORBA.TCKind.tk_float:
                        result = new omg.org.CORBA.FloatTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_local_interface :
                        result = new omg.org.CORBA.LocalIfTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_long:
                        result = new omg.org.CORBA.LongTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_longdouble:
                        throw new NotImplementedException("long double not implemented");
                    case omg.org.CORBA.TCKind.tk_longlong:
                        result = new omg.org.CORBA.LongLongTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_native:
                        throw new NotSupportedException("native not supported");
                    case omg.org.CORBA.TCKind.tk_null:
                        result = new omg.org.CORBA.NullTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_objref:
                        result = new omg.org.CORBA.ObjRefTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_octet:
                        result = new omg.org.CORBA.OctetTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_Principal:
                        throw new NotImplementedException("Principal not implemented");
                    case omg.org.CORBA.TCKind.tk_sequence:
                        result = new omg.org.CORBA.SequenceTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_short:
                        result = new omg.org.CORBA.ShortTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_string:
                        result = new omg.org.CORBA.StringTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_struct:
                        result = new omg.org.CORBA.StructTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_TypeCode:
                        result = new omg.org.CORBA.TypeCodeTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_ulong:
                        result = new omg.org.CORBA.ULongTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_ulonglong:
                        result = new omg.org.CORBA.ULongLongTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_union:
                        result = new omg.org.CORBA.UnionTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_ushort:
                        result = new omg.org.CORBA.UShortTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_value:
                        result = new omg.org.CORBA.ValueTypeTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_value_box:
                        result = new omg.org.CORBA.ValueBoxTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_void:
                        result = new omg.org.CORBA.VoidTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_wchar:
                        result = new omg.org.CORBA.WCharTC();
                        break;
                    case omg.org.CORBA.TCKind.tk_wstring:
                        result = new omg.org.CORBA.WStringTC();                                    
                        break;
                    default:
                        // unknown typecode: kind
                        throw new omg.org.CORBA.BAD_PARAM(1504, 
                                                          omg.org.CORBA.CompletionStatus.Completed_MayBe);
                }
                // store indirection
                IndirectionInfo indirInfo = new IndirectionInfo(indirPos.GlobalPosition, 
                                                                IndirectionType.TypeCode,
                                                                IndirectionUsage.TypeCode);
                sourceStream.StoreIndirection(indirInfo, result);
                // read additional parts of typecode, if present
                result.ReadFromStream(sourceStream);                                
                return result;
            } else {
                // resolve indirection:
                StreamPosition indirectionPosition = sourceStream.ReadIndirectionOffset();                                
                return sourceStream.GetObjectForIndir(new IndirectionInfo(indirectionPosition.GlobalPosition,
                                                                          IndirectionType.TypeCode,        
                                                                          IndirectionUsage.TypeCode), 
                                                      true);
            }
        }

        internal override void Serialize(object actual, CdrOutputStream targetStream) {
            if (!(actual is omg.org.CORBA.TypeCodeImpl)) { 
                // typecode not serializable
                throw new omg.org.CORBA.INTERNAL(1654, omg.org.CORBA.CompletionStatus.Completed_MayBe);
            }
            omg.org.CORBA.TypeCodeImpl tcImpl = actual as omg.org.CORBA.TypeCodeImpl;
            if (!targetStream.IsPreviouslyMarshalled(tcImpl, IndirectionType.TypeCode, IndirectionUsage.TypeCode)) {
                tcImpl.WriteToStream(targetStream);
            } else {
                targetStream.WriteIndirection(tcImpl);
            }
        }

        #endregion IMethods

    }
    
    /// <summary>serializes an instance as IDL abstract-interface</summary>
    internal class AbstractInterfaceSerializer : Serializer {

        #region IFields
        
        private Type m_forType;
        private Serializer m_objRefSer;
        private Serializer m_valueSer;

        #endregion IFields
        #region IConstructors
        
        internal AbstractInterfaceSerializer(Type forType, SerializerFactory serFactory) {
            m_forType = forType;
            m_objRefSer = new ObjRefSerializer(forType);
            m_valueSer = new ValueObjectSerializer(forType, serFactory);
        }
        
        #endregion IConstructors
        #region IMethods

        internal override void Serialize(object actual, CdrOutputStream targetStream) {
            // if actual is null it shall be encoded as a valuetype: 15.3.7
            if ((actual != null) && (ClsToIdlMapper.IsMappedToConcreteInterface(actual.GetType()))) {
                targetStream.WriteBool(true); // an obj-ref is serialized
                m_objRefSer.Serialize(actual, targetStream);
            } else if ((actual == null) || (ClsToIdlMapper.IsMappedToConcreteValueType(actual.GetType()))) {
                targetStream.WriteBool(false); // a value-type is serialised
                m_valueSer.Serialize(actual, targetStream);
            } else {
                // actual value ( actual ) with type: 
                // actual.GetType() is not serializable for the formal type
                // formal
                throw new BAD_PARAM(6, CompletionStatus.Completed_MayBe);
            }
        }

        internal override object Deserialize(CdrInputStream sourceStream) {
            bool isObjRef = sourceStream.ReadBool();
            if (isObjRef) {
                Type formal = m_forType;
                if (formal.Equals(ReflectionHelper.ObjectType)) {
                    // if in interface only abstract interface base type is used, set formal now
                    // to base type of all objref for deserialization
                    formal = ReflectionHelper.MarshalByRefObjectType;
                }
                object result = m_objRefSer.Deserialize(sourceStream);    
                return result;
            } else {
                object result = m_valueSer.Deserialize(sourceStream);
                return result;
            }
        }

        #endregion IMethods
    
    }

    
    /// <summary>serializes .NET exceptions as IDL-Exceptions</summary>
    internal class ExceptionSerializer : Serializer {

        #region IFields
        
        private SerializerFactory m_serFactory;
        
        #endregion IFields
        #region IConstructors
        
        public ExceptionSerializer(SerializerFactory serFactory) {
            m_serFactory = serFactory;
        }
        
        #endregion IConstructors
        #region IMethods

        internal override object Deserialize(CdrInputStream sourceStream) {
            string repId = sourceStream.ReadString();
            Type exceptionType = Repository.GetTypeForId(repId);
            if (exceptionType == null) {
                throw new UnknownUserException("user exception not found for id: " + repId);
            } else if (exceptionType.IsSubclassOf(typeof(AbstractCORBASystemException))) {
                // system exceptions are deserialized specially, because no inheritance is possible for exceptions, see implementation of the system exceptions
                uint minor = sourceStream.ReadULong();
                CompletionStatus completion = (CompletionStatus)((int) sourceStream.ReadULong());
                return (Exception)Activator.CreateInstance(exceptionType, new object[] { (int)minor, completion } );
            } else {
                Exception exception = (Exception)Activator.CreateInstance(exceptionType);
                // deserialise fields
                FieldInfo[] fields = ReflectionHelper.GetAllDeclaredInstanceFields(exceptionType);                
                foreach (FieldInfo field in fields) {
                    Serializer ser = m_serFactory.Create(field.FieldType, 
                                                         ReflectionHelper.GetCustomAttriutesForField(field, true));
                    object fieldVal = ser.Deserialize(sourceStream);
                    field.SetValue(exception, fieldVal);
                }                
                return exception;
            }
        }

        internal override void Serialize(object actual, CdrOutputStream targetStream) {
            string repId = Repository.GetRepositoryID(actual.GetType());
            targetStream.WriteString(repId);

            if (actual.GetType().IsSubclassOf(typeof(AbstractCORBASystemException))) {
                // system exceptions are serialized specially, because no inheritance is possible for exceptions, see implementation of the system exceptions
                AbstractCORBASystemException sysEx = (AbstractCORBASystemException) actual;
                targetStream.WriteULong((uint)sysEx.Minor);
                targetStream.WriteULong((uint)sysEx.Status);
            } else {
                FieldInfo[] fields = ReflectionHelper.GetAllDeclaredInstanceFields(actual.GetType());                
                foreach (FieldInfo field in fields) {
                    object fieldVal = field.GetValue(actual);
                    Serializer ser = m_serFactory.Create(field.FieldType,                     
                                                         ReflectionHelper.GetCustomAttriutesForField(field, true));
                    ser.Serialize(fieldVal, targetStream);
                }
            }
        }

        #endregion IMethods

    }
    
    /// <summary>
    /// Serializer decorator for handling custom mapping.
    /// </summary>
    internal class CustomMappingDecorator : Serializer {
        
        #region IFields
        
        private CustomMappingDesc m_customMappingUsed;
        private Serializer m_decorated;
        
        #endregion IFields
        #region IConstructors
        
        internal CustomMappingDecorator(CustomMappingDesc customMappingUsed, Serializer decorated) {
            m_decorated = decorated;
            m_customMappingUsed = customMappingUsed;
        }
        
        #endregion IConstructors
        #region IMethods
        
        internal override void Serialize(object actual, CdrOutputStream targetStream) {
            if (actual != null) {
                CustomMapperRegistry cReg = CustomMapperRegistry.GetSingleton();
                // custom mapping maps the actual object to an instance of 
                // the idl formal type.
                actual = cReg.CreateIdlForClsInstance(actual, m_customMappingUsed.IdlType);
            }            
            m_decorated.Serialize(actual, targetStream);
        }
        
        internal override object Deserialize(CdrInputStream sourceStream) {
            object result =
                m_decorated.Deserialize(sourceStream);

            // check for plugged special mappings, e.g. CLS ArrayList -> java.util.ArrayList
            // --> if present, need to convert instance after deserialising
            if (result != null) {
                CustomMapperRegistry cReg = CustomMapperRegistry.GetSingleton();
                result = cReg.CreateClsForIdlInstance(result, m_customMappingUsed.ClsType);
            }                      
            return result;
        }
        
        #endregion IMethods
    }

}

#if UnitTest

namespace Ch.Elca.Iiop.Tests {
    
    using System.IO;
    using System.Runtime.Remoting.Channels;
    using NUnit.Framework;
    using Ch.Elca.Iiop.Marshalling;
    using Ch.Elca.Iiop.Cdr;
    using Ch.Elca.Iiop.Util;
    using omg.org.CORBA;
    
    /// <summary>
    /// Unit-tests for the serialisers
    /// </summary>
    [TestFixture]    
    public class SerialiserTest {
        
        public SerialiserTest() {
        }

        private void GenericSerTest(Serializer ser, object actual, byte[] expected) {
            MemoryStream outStream = new MemoryStream();
            try {
                CdrOutputStream cdrOut = new CdrOutputStreamImpl(outStream, 0);                
                ser.Serialize(actual, cdrOut);
                outStream.Seek(0, SeekOrigin.Begin);
                byte[] result = new byte[expected.Length];
                outStream.Read(result, 0, result.Length);
                ArrayAssertion.AssertByteArrayEquals("value " + actual + " incorrectly serialized.",
                                                     expected, result);
            } finally {
                outStream.Close();
            }
        }
        
        private void GenericDeserTest(Serializer ser, byte[] actual, object expected) {
            MemoryStream inStream = new MemoryStream();
            inStream.Write(actual, 0, actual.Length);
            inStream.Seek(0, SeekOrigin.Begin);
            CdrInputStreamImpl cdrIn = new CdrInputStreamImpl(inStream);
            cdrIn.ConfigStream(0, new GiopVersion(1, 2));            
            Assertion.AssertEquals("value " + expected + " not deserialized.", 
                                   expected, ser.Deserialize(cdrIn));
            inStream.Close();
        }        
        
        [Test]
        public void TestByteSerialise() {
            Serializer ser = new ByteSerializer();
            GenericSerTest(ser, (byte)0, new byte[] { 0 });
            GenericSerTest(ser, (byte)11, new byte[] { 11 });
            GenericSerTest(ser, (byte)12, new byte[] { 12 });
            GenericSerTest(ser, (byte)225, new byte[] { 225 });            
        }
        
        [Test]
        public void TestByteDeserialise() {
            Serializer ser = new ByteSerializer();
            GenericDeserTest(ser, new byte[] { 0 }, (byte)0);
            GenericDeserTest(ser, new byte[] { 11 }, (byte)11);
            GenericDeserTest(ser, new byte[] { 12 }, (byte)12);
            GenericDeserTest(ser, new byte[] { 225 }, (byte)225);
        }

        [Test]        
        public void TestBooleanSerialise() {
            Serializer ser = new BooleanSerializer();
            GenericSerTest(ser, false, new byte[] { 0 });
            GenericSerTest(ser, true, new byte[] { 1 });
        }
        
        [Test]
        public void TestBooleanDeserialise() {
            Serializer ser = new BooleanSerializer();
            GenericDeserTest(ser, new byte[] { 0 }, false);
            GenericDeserTest(ser, new byte[] { 1 }, true);
        }
        
        [Test]
        [ExpectedException(typeof(BAD_PARAM))]
        public void TestBooleanDeserialiseInvalidValue() {
            Serializer ser = new BooleanSerializer();
            GenericDeserTest(ser, new byte[] { 2 }, null);            
        }
        
        private void EnumGenericSerTest(Type enumType, object actual, byte[] expected) {
            GenericSerTest(new EnumSerializer(enumType, new SerializerFactory()),
                           actual, expected);
        }
        
        private void EnumGenericDeserTest(Type enumType, byte[] actual, object expected) {
            GenericDeserTest(new EnumSerializer(enumType, new SerializerFactory()),
                             actual, expected);            
        }
        
        private void FlagsGenericSerTest(Type flagsType, object actual, byte[] expected) {
            GenericSerTest(new FlagsSerializer(flagsType, new SerializerFactory()),
                           actual, expected);
        }
        
        private void FlagsGenericDeserTest(Type flagsType, byte[] actual, object expected) {
            GenericDeserTest(new FlagsSerializer(flagsType, new SerializerFactory()),
                             actual, expected);
        }        
        
        [Test]        
        public void TestIdlEnumSerialise() {
            EnumGenericSerTest(typeof(TestIdlEnumBI32), TestIdlEnumBI32.IDL_A, new byte[] { 0, 0, 0, 0});
            EnumGenericSerTest(typeof(TestIdlEnumBI32), TestIdlEnumBI32.IDL_B, new byte[] { 0, 0, 0, 1});
            EnumGenericSerTest(typeof(TestIdlEnumBI32), TestIdlEnumBI32.IDL_C, new byte[] { 0, 0, 0, 2});
        }
        
        [Test]
        public void TestIdlEnumDeserialise() {
            EnumGenericDeserTest(typeof(TestIdlEnumBI32), new byte[] { 0, 0, 0, 0}, TestIdlEnumBI32.IDL_A);
            EnumGenericDeserTest(typeof(TestIdlEnumBI32), new byte[] { 0, 0, 0, 1}, TestIdlEnumBI32.IDL_B);
            EnumGenericDeserTest(typeof(TestIdlEnumBI32), new byte[] { 0, 0, 0, 2}, TestIdlEnumBI32.IDL_C);
        }        
        
        [Test]        
        public void TestEnumBI16Serialise() {
            EnumGenericSerTest(typeof(TestEnumBI16), TestEnumBI16.a2, new byte[] { 0, 0, 0, 0});
            EnumGenericSerTest(typeof(TestEnumBI16), TestEnumBI16.b2, new byte[] { 0, 0, 0, 1});
            EnumGenericSerTest(typeof(TestEnumBI16), TestEnumBI16.c2, new byte[] { 0, 0, 0, 2});
        }
        
        [Test]
        public void TestEnumBI16Deserialise() {
            EnumGenericDeserTest(typeof(TestEnumBI16), new byte[] { 0, 0, 0, 0}, TestEnumBI16.a2);
            EnumGenericDeserTest(typeof(TestEnumBI16), new byte[] { 0, 0, 0, 1}, TestEnumBI16.b2);
            EnumGenericDeserTest(typeof(TestEnumBI16), new byte[] { 0, 0, 0, 2}, TestEnumBI16.c2);
        }        
        
        [Test]        
        public void TestEnumBI32Serialise() {
            EnumGenericSerTest(typeof(TestEnumBI32), TestEnumBI32.a1, new byte[] { 0, 0, 0, 0});
            EnumGenericSerTest(typeof(TestEnumBI32), TestEnumBI32.b1, new byte[] { 0, 0, 0, 1});
            EnumGenericSerTest(typeof(TestEnumBI32), TestEnumBI32.c1, new byte[] { 0, 0, 0, 2});
        }
        
        [Test]
        public void TestEnumBI32Deserialise() {
            EnumGenericDeserTest(typeof(TestEnumBI32), new byte[] { 0, 0, 0, 0}, TestEnumBI32.a1);
            EnumGenericDeserTest(typeof(TestEnumBI32), new byte[] { 0, 0, 0, 1}, TestEnumBI32.b1);
            EnumGenericDeserTest(typeof(TestEnumBI32), new byte[] { 0, 0, 0, 2}, TestEnumBI32.c1);
        }        
        
        [Test]        
        public void TestEnumBBSerialise() {
            EnumGenericSerTest(typeof(TestEnumBB), TestEnumBB.a4, new byte[] { 0, 0, 0, 0});
            EnumGenericSerTest(typeof(TestEnumBB), TestEnumBB.b4, new byte[] { 0, 0, 0, 1});
            EnumGenericSerTest(typeof(TestEnumBB), TestEnumBB.c4, new byte[] { 0, 0, 0, 2});
        }
        
        [Test]
        public void TestEnumBBDeserialise() {
            EnumGenericDeserTest(typeof(TestEnumBB), new byte[] { 0, 0, 0, 0}, TestEnumBB.a4);
            EnumGenericDeserTest(typeof(TestEnumBB), new byte[] { 0, 0, 0, 1}, TestEnumBB.b4);
            EnumGenericDeserTest(typeof(TestEnumBB), new byte[] { 0, 0, 0, 2}, TestEnumBB.c4);
        }        
        
        [Test]        
        public void TestEnumI64Serialise() {
            EnumGenericSerTest(typeof(TestEnumBI64), TestEnumBI64.a3, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0});
            EnumGenericSerTest(typeof(TestEnumBI64), TestEnumBI64.b3, new byte[] { 0, 0, 0, 0, 0, 0, 0, 1});
            EnumGenericSerTest(typeof(TestEnumBI64), TestEnumBI64.c3, new byte[] { 0, 0, 0, 0, 0, 0, 0, 2});
        }
        
        [Test]
        public void TestEnumI64Deserialise() {
            EnumGenericDeserTest(typeof(TestEnumBI64), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0}, TestEnumBI64.a3);
            EnumGenericDeserTest(typeof(TestEnumBI64), new byte[] { 0, 0, 0, 0, 0, 0, 0, 1}, TestEnumBI64.b3);
            EnumGenericDeserTest(typeof(TestEnumBI64), new byte[] { 0, 0, 0, 0, 0, 0, 0, 2}, TestEnumBI64.c3);
        }        
        
        [Test]        
        public void TestEnumBUI16Serialise() {
            EnumGenericSerTest(typeof(TestEnumBUI16), TestEnumBUI16.a6, new byte[] { 0, 0, 0, 0});
            EnumGenericSerTest(typeof(TestEnumBUI16), TestEnumBUI16.b6, new byte[] { 0, 0, 0, 1});
            EnumGenericSerTest(typeof(TestEnumBUI16), TestEnumBUI16.c6, new byte[] { 0, 0, 0, 2});
        }
        
        [Test]
        public void TestEnumBUI16Deserialise() {
            EnumGenericDeserTest(typeof(TestEnumBUI16), new byte[] { 0, 0, 0, 0}, TestEnumBUI16.a6);
            EnumGenericDeserTest(typeof(TestEnumBUI16), new byte[] { 0, 0, 0, 1}, TestEnumBUI16.b6);
            EnumGenericDeserTest(typeof(TestEnumBUI16), new byte[] { 0, 0, 0, 2}, TestEnumBUI16.c6);
        }        
        
        [Test]        
        public void TestEnumBUI32Serialise() {
            EnumGenericSerTest(typeof(TestEnumBUI32), TestEnumBUI32.a7, new byte[] { 0, 0, 0, 0});
            EnumGenericSerTest(typeof(TestEnumBUI32), TestEnumBUI32.b7, new byte[] { 0, 0, 0, 1});
            EnumGenericSerTest(typeof(TestEnumBUI32), TestEnumBUI32.c7, new byte[] { 0, 0, 0, 2});
        }
        
        [Test]
        public void TestEnumBUI32Deserialise() {
            EnumGenericDeserTest(typeof(TestEnumBUI32), new byte[] { 0, 0, 0, 0}, TestEnumBUI32.a7);
            EnumGenericDeserTest(typeof(TestEnumBUI32), new byte[] { 0, 0, 0, 1}, TestEnumBUI32.b7);
            EnumGenericDeserTest(typeof(TestEnumBUI32), new byte[] { 0, 0, 0, 2}, TestEnumBUI32.c7);
        }        
        
        [Test]        
        public void TestEnumBSBSerialise() {
            EnumGenericSerTest(typeof(TestEnumBSB), TestEnumBSB.a5, new byte[] { 0, 0, 0, 0});
            EnumGenericSerTest(typeof(TestEnumBSB), TestEnumBSB.b5, new byte[] { 0, 0, 0, 1});
            EnumGenericSerTest(typeof(TestEnumBSB), TestEnumBSB.c5, new byte[] { 0, 0, 0, 2});
        }
        
        [Test]
        public void TestEnumBSBDeserialise() {
            EnumGenericDeserTest(typeof(TestEnumBSB), new byte[] { 0, 0, 0, 0}, TestEnumBSB.a5);
            EnumGenericDeserTest(typeof(TestEnumBSB), new byte[] { 0, 0, 0, 1}, TestEnumBSB.b5);
            EnumGenericDeserTest(typeof(TestEnumBSB), new byte[] { 0, 0, 0, 2}, TestEnumBSB.c5);
        }        
        
        [Test]        
        public void TestEnumUI64Serialise() {
            EnumGenericSerTest(typeof(TestEnumBUI64), TestEnumBUI64.a8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0});
            EnumGenericSerTest(typeof(TestEnumBUI64), TestEnumBUI64.b8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 1});
            EnumGenericSerTest(typeof(TestEnumBUI64), TestEnumBUI64.c8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 2});
        }
        
        [Test]
        public void TestEnumUI64Deserialise() {
            EnumGenericDeserTest(typeof(TestEnumBUI64), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0}, TestEnumBUI64.a8);
            EnumGenericDeserTest(typeof(TestEnumBUI64), new byte[] { 0, 0, 0, 0, 0, 0, 0, 1}, TestEnumBUI64.b8);
            EnumGenericDeserTest(typeof(TestEnumBUI64), new byte[] { 0, 0, 0, 0, 0, 0, 0, 2}, TestEnumBUI64.c8);
        }        
                
        [Test]        
        public void TestFlagsBI16Serialise() {
            FlagsGenericSerTest(typeof(TestFlagsBI16), TestFlagsBI16.a2, new byte[] { 0, 0});
            FlagsGenericSerTest(typeof(TestFlagsBI16), TestFlagsBI16.b2, new byte[] { 0, 1});
            FlagsGenericSerTest(typeof(TestFlagsBI16), TestFlagsBI16.c2, new byte[] { 0, 2});
        }
        
        [Test]
        public void TestFlagsBI16Deserialise() {
            EnumGenericDeserTest(typeof(TestFlagsBI16), new byte[] { 0, 0}, TestFlagsBI16.a2);
            EnumGenericDeserTest(typeof(TestFlagsBI16), new byte[] { 0, 1}, TestFlagsBI16.b2);
            EnumGenericDeserTest(typeof(TestFlagsBI16), new byte[] { 0, 2}, TestFlagsBI16.c2);
        }        
        
        [Test]        
        public void TestFlagsBI32Serialise() {
            FlagsGenericSerTest(typeof(TestFlagsBI32), TestFlagsBI32.a1, new byte[] { 0, 0, 0, 0});
            FlagsGenericSerTest(typeof(TestFlagsBI32), TestFlagsBI32.b1, new byte[] { 0, 0, 0, 1});
            FlagsGenericSerTest(typeof(TestFlagsBI32), TestFlagsBI32.c1, new byte[] { 0, 0, 0, 2});
        }
        
        [Test]
        public void TestFlagsBI32Deserialise() {
            FlagsGenericDeserTest(typeof(TestFlagsBI32), new byte[] { 0, 0, 0, 0}, TestFlagsBI32.a1);
            FlagsGenericDeserTest(typeof(TestFlagsBI32), new byte[] { 0, 0, 0, 1}, TestFlagsBI32.b1);
            FlagsGenericDeserTest(typeof(TestFlagsBI32), new byte[] { 0, 0, 0, 2}, TestFlagsBI32.c1);
        }        
        
        [Test]        
        public void TestFlagsBBSerialise() {
            FlagsGenericSerTest(typeof(TestFlagsBB), TestFlagsBB.a4, new byte[] { 0});
            FlagsGenericSerTest(typeof(TestFlagsBB), TestFlagsBB.b4, new byte[] { 1});
            FlagsGenericSerTest(typeof(TestFlagsBB), TestFlagsBB.c4, new byte[] { 2});
        }
        
        [Test]
        public void TestFlagsBBDeserialise() {
            FlagsGenericDeserTest(typeof(TestFlagsBB), new byte[] { 0}, TestFlagsBB.a4);
            FlagsGenericDeserTest(typeof(TestFlagsBB), new byte[] { 1}, TestFlagsBB.b4);
            FlagsGenericDeserTest(typeof(TestFlagsBB), new byte[] { 2}, TestFlagsBB.c4);
        }        
        
        [Test]        
        public void TestFlagsI64Serialise() {
            FlagsGenericSerTest(typeof(TestFlagsBI64), TestFlagsBI64.a3, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0});
            FlagsGenericSerTest(typeof(TestFlagsBI64), TestFlagsBI64.b3, new byte[] { 0, 0, 0, 0, 0, 0, 0, 1});
            FlagsGenericSerTest(typeof(TestFlagsBI64), TestFlagsBI64.c3, new byte[] { 0, 0, 0, 0, 0, 0, 0, 2});
        }
        
        [Test]
        public void TestFlagsI64Deserialise() {
            FlagsGenericDeserTest(typeof(TestFlagsBI64), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0}, TestFlagsBI64.a3);
            FlagsGenericDeserTest(typeof(TestFlagsBI64), new byte[] { 0, 0, 0, 0, 0, 0, 0, 1}, TestFlagsBI64.b3);
            FlagsGenericDeserTest(typeof(TestFlagsBI64), new byte[] { 0, 0, 0, 0, 0, 0, 0, 2}, TestFlagsBI64.c3);
        }        
        
        [Test]        
        public void TestFlagsBUI16Serialise() {
            FlagsGenericSerTest(typeof(TestFlagsBUI16), TestFlagsBUI16.a6, new byte[] { 0, 0});
            FlagsGenericSerTest(typeof(TestFlagsBUI16), TestFlagsBUI16.b6, new byte[] { 0, 1});
            FlagsGenericSerTest(typeof(TestFlagsBUI16), TestFlagsBUI16.c6, new byte[] { 0, 2});
        }
        
        [Test]
        public void TestFlagsBUI16Deserialise() {
            FlagsGenericDeserTest(typeof(TestFlagsBUI16), new byte[] { 0, 0}, TestFlagsBUI16.a6);
            FlagsGenericDeserTest(typeof(TestFlagsBUI16), new byte[] { 0, 1}, TestFlagsBUI16.b6);
            FlagsGenericDeserTest(typeof(TestFlagsBUI16), new byte[] { 0, 2}, TestFlagsBUI16.c6);
        }        
        
        [Test]        
        public void TestFlagsBUI32Serialise() {
            FlagsGenericSerTest(typeof(TestFlagsBUI32), TestFlagsBUI32.a7, new byte[] { 0, 0, 0, 0});
            FlagsGenericSerTest(typeof(TestFlagsBUI32), TestFlagsBUI32.b7, new byte[] { 0, 0, 0, 1});
            FlagsGenericSerTest(typeof(TestFlagsBUI32), TestFlagsBUI32.c7, new byte[] { 0, 0, 0, 2});
        }
        
        [Test]
        public void TestFlagsBUI32Deserialise() {
            FlagsGenericDeserTest(typeof(TestFlagsBUI32), new byte[] { 0, 0, 0, 0}, TestFlagsBUI32.a7);
            FlagsGenericDeserTest(typeof(TestFlagsBUI32), new byte[] { 0, 0, 0, 1}, TestFlagsBUI32.b7);
            FlagsGenericDeserTest(typeof(TestFlagsBUI32), new byte[] { 0, 0, 0, 2}, TestFlagsBUI32.c7);
        }        
        
        [Test]        
        public void TestFlagsBSBSerialise() {
            FlagsGenericSerTest(typeof(TestFlagsBSB), TestFlagsBSB.a5, new byte[] { 0});
            FlagsGenericSerTest(typeof(TestFlagsBSB), TestFlagsBSB.b5, new byte[] { 1});
            FlagsGenericSerTest(typeof(TestFlagsBSB), TestFlagsBSB.c5, new byte[] { 2});
        }
        
        [Test]
        public void TestFlagsBSBDeserialise() {
            FlagsGenericDeserTest(typeof(TestFlagsBSB), new byte[] { 0 }, TestFlagsBSB.a5);
            FlagsGenericDeserTest(typeof(TestFlagsBSB), new byte[] { 1 }, TestFlagsBSB.b5);
            FlagsGenericDeserTest(typeof(TestFlagsBSB), new byte[] { 2 }, TestFlagsBSB.c5);
        }        
        
        [Test]        
        public void TestFlagsUI64Serialise() {
            FlagsGenericSerTest(typeof(TestFlagsBUI64), TestFlagsBUI64.a8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0});
            FlagsGenericSerTest(typeof(TestFlagsBUI64), TestFlagsBUI64.b8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 1});
            FlagsGenericSerTest(typeof(TestFlagsBUI64), TestFlagsBUI64.c8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 2});
        }
        
        [Test]
        public void TestFlagsUI64Deserialise() {
            FlagsGenericDeserTest(typeof(TestFlagsBUI64), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0}, TestFlagsBUI64.a8);
            FlagsGenericDeserTest(typeof(TestFlagsBUI64), new byte[] { 0, 0, 0, 0, 0, 0, 0, 1}, TestFlagsBUI64.b8);
            FlagsGenericDeserTest(typeof(TestFlagsBUI64), new byte[] { 0, 0, 0, 0, 0, 0, 0, 2}, TestFlagsBUI64.c8);
        }        
        
        [Test]  
        public void TestIorDeserialisation() {
            IiopClientChannel testChannel = new IiopClientChannel();
            ChannelServices.RegisterChannel(testChannel);
        
            MemoryStream inStream = new MemoryStream();
            byte[] testIor = new byte[] {                 
                0x00, 0x00, 0x00, 0x28, 0x49, 0x44, 0x4C, 0x3A,
                0x6F, 0x6D, 0x67, 0x2E, 0x6F, 0x72, 0x67, 0x2F,
                0x43, 0x6F, 0x73, 0x4E, 0x61, 0x6D, 0x69, 0x6E,
                0x67, 0x2F, 0x4E, 0x61, 0x6D, 0x69, 0x6E, 0x67,
                0x43, 0x6F, 0x6E, 0x74, 0x65, 0x78, 0x74, 0x3A,
                0x31, 0x2E, 0x30, 0x00, 0x00, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x74,
                0x00, 0x01, 0x02, 0x00, 0x00, 0x00, 0x00, 0x0A,
                0x31, 0x32, 0x37, 0x2E, 0x30, 0x2E, 0x30, 0x2E,
                0x31, 0x00, 0x04, 0x19, 0x00, 0x00, 0x00, 0x30,
                0xAF, 0xAB, 0xCB, 0x00, 0x00, 0x00, 0x00, 0x22,
                0x00, 0x00, 0x03, 0xE8, 0x00, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x0C, 0x4E, 0x61, 0x6D, 0x65,
                0x53, 0x65, 0x72, 0x76, 0x69, 0x63, 0x65, 0x00,
                0x00, 0x00, 0x00, 0x03, 0x4E, 0x43, 0x30, 0x0A,
                0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02,
                0x05, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x20,
                0x00, 0x01, 0x01, 0x09, 0x00, 0x00, 0x00, 0x01,
                0x00, 0x01, 0x01, 0x00
            };
            inStream.Write(testIor, 0, testIor.Length);
            inStream.Seek(0, SeekOrigin.Begin);
            CdrInputStreamImpl cdrIn = new CdrInputStreamImpl(inStream);
            cdrIn.ConfigStream(0, new GiopVersion(1, 2));            
            
            Serializer ser = new ObjRefSerializer(typeof(omg.org.CosNaming.NamingContext));
            object result = ser.Deserialize(cdrIn);
            Assertion.AssertNotNull("not correctly deserialised proxy for ior", result);
            Assertion.Assert(RemotingServices.IsTransparentProxy(result));
            Assertion.AssertEquals("IOR:000000000000002849444C3A6F6D672E6F72672F436F734E616D696E672F4E616D696E67436F6E746578743A312E3000000000010000000000000074000102000000000A3132372E302E302E3100041900000030AFABCB0000000022000003E80000000100000000000000010000000C4E616D655365727669636500000000034E43300A0000000100000001000000200000000000010001000000020501000100010020000101090000000100010100",
                                   RemotingServices.GetObjectUri((MarshalByRefObject)result));
            ChannelServices.UnregisterChannel(testChannel);
            
        }

        [Test]
        [ExpectedException(typeof(BAD_PARAM))]
        public void TestNotAllowEmptyStringForBasicStrings() {
            StringSerializer stringSer = new StringSerializer(false);
            MemoryStream outStream = new MemoryStream();
            try {            
                CdrOutputStream cdrOut = new CdrOutputStreamImpl(outStream, 0);            
                stringSer.Serialize(null, cdrOut);
            } finally {
                outStream.Close();
            }
            
        }



    }

}
    
#endif
