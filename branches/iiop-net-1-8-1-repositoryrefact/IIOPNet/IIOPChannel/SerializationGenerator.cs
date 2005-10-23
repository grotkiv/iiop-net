/* SerializationGenerator.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 02.10.05  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.Diagnostics;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Reflection.Emit;
using Ch.Elca.Iiop.Util;
using Ch.Elca.Iiop.Idl;

namespace Ch.Elca.Iiop.Marshalling {
    

    /// <summary>
    /// generates and manages type used for serialisation/deserialisation
    /// </summary>
    internal class SerializationGenerator {
                
        #region Types
                
        private class StaticConstructorContext {
            
            private ILGenerator m_staticConstructorIlGen;
            
            private LocalBuilder m_forTypeLocal;
            private LocalBuilder m_typeArrayLocal;
            private LocalBuilder m_methodInfoLocal;
                       
            internal StaticConstructorContext(ILGenerator gen) {
                m_staticConstructorIlGen = gen;
            }
            
            internal ILGenerator Generator {
                get {
                    return m_staticConstructorIlGen;
                }
            }
            
            /// <summary>
            /// the local containing the type for which the arg ser is.
            /// </summary>
            internal LocalBuilder ForTypeLocal {
                get {
                    if (m_forTypeLocal == null) {
                        // not yet setup
                        throw new omg.org.CORBA.INTERNAL(4765, omg.org.CORBA.CompletionStatus.Completed_MayBe);
                    }
                    return m_forTypeLocal;                    
                }
            }
            
            /// <summary>
            /// a local variable for use to store array of types inside
            /// </summary>
            internal LocalBuilder TypeArrayLocal {
                get {
                    if (m_typeArrayLocal == null) {
                        // not yet setup
                        throw new omg.org.CORBA.INTERNAL(4765, omg.org.CORBA.CompletionStatus.Completed_MayBe);
                    }
                    return m_typeArrayLocal;
                }
            }    

            /// <summary>
            /// a local variable for use to store methodinfos inside
            /// </summary>            
            internal LocalBuilder MethodInfoLocal {
                get {
                    if (m_methodInfoLocal == null) {
                        // not yet setup
                        throw new omg.org.CORBA.INTERNAL(4765, omg.org.CORBA.CompletionStatus.Completed_MayBe);                        
                    }
                    return m_methodInfoLocal;
                }
            }
            
            internal void Setup(Type forType, FieldBuilder typeTypeField, 
                                FieldBuilder requestNameForInfoHtField,
                                FieldBuilder methodInfoForNameHtField,
                                TypeBuilder serType) {
                
                IlEmitHelper.GetSingleton().EmitLoadType(m_staticConstructorIlGen, serType);
                m_staticConstructorIlGen.Emit(OpCodes.Stsfld, typeTypeField);
                m_forTypeLocal = m_staticConstructorIlGen.DeclareLocal(ReflectionHelper.TypeType);
                IlEmitHelper.GetSingleton().EmitLoadType(m_staticConstructorIlGen, forType);
                m_staticConstructorIlGen.Emit(OpCodes.Stloc, m_forTypeLocal);
                m_typeArrayLocal = m_staticConstructorIlGen.DeclareLocal(typeof(Type[]));                
                m_staticConstructorIlGen.Emit(OpCodes.Ldnull);
                m_staticConstructorIlGen.Emit(OpCodes.Stloc, m_typeArrayLocal);
                m_methodInfoLocal = m_staticConstructorIlGen.DeclareLocal(typeof(MethodInfo));
                m_staticConstructorIlGen.Emit(OpCodes.Ldnull);
                m_staticConstructorIlGen.Emit(OpCodes.Stloc, m_methodInfoLocal);
                
                m_staticConstructorIlGen.Emit(OpCodes.Newobj, typeof(Hashtable).GetConstructor(Type.EmptyTypes));
                m_staticConstructorIlGen.Emit(OpCodes.Stsfld, requestNameForInfoHtField);
                m_staticConstructorIlGen.Emit(OpCodes.Newobj, typeof(Hashtable).GetConstructor(Type.EmptyTypes));
                m_staticConstructorIlGen.Emit(OpCodes.Stsfld, methodInfoForNameHtField);                                
            }
            
            internal void End() {
                m_staticConstructorIlGen.Emit(OpCodes.Ret);
            }
            
        }
        
        private class ArgSerializationGenerationContext {           
            
            private Type m_forType;
            private TypeBuilder m_typeBuilder;
            
            private StaticConstructorContext m_staticConstrContext;
            private FieldBuilder m_typetypeField;
            private FieldBuilder m_nameForInfoField;
            private FieldBuilder m_methodInfoForNameField;
        
            internal ArgSerializationGenerationContext(Type forType) {
                m_forType = forType;
            }      
            
            internal Type ForType {
                get {
                    return m_forType;
                }
            }
            
            internal TypeBuilder TypeBuilder {
                get {
                    return m_typeBuilder;
                }
            }            
            
            internal FieldBuilder TypeTypeField {
                get {
                    return m_typetypeField;
                }
            }
            
            /// <summary>
            /// contains a Hashtable with key MethodInfo and value requestname
            /// </summary>
            internal FieldBuilder NameForInfoField {
                get {
                    return m_nameForInfoField;
                }
            }

            /// <summary>
            /// contains a Hashtable with key requestName and value MethodInfo
            /// </summary>            
            internal FieldBuilder MethodInfoForNameField {
                get {
                    return m_methodInfoForNameField;
                }
            }
            
            internal StaticConstructorContext StaticConstrContext {
                get {
                    return m_staticConstrContext;
                }
            }
                        
            internal void InitalizeContext(ConstructorBuilder staticConstructor,
                                           TypeBuilder typeBuilder) {
                m_typeBuilder = typeBuilder;
                m_typetypeField = typeBuilder.DefineField("ClassType", ReflectionHelper.TypeType,
                                                           FieldAttributes.Public | FieldAttributes.InitOnly |
                                                           FieldAttributes.Static);
                
                m_nameForInfoField = typeBuilder.DefineField("s_requestNameForInfo", typeof(Hashtable),
                                                             FieldAttributes.Private | FieldAttributes.InitOnly |
                                                             FieldAttributes.Static);
                
                m_methodInfoForNameField = typeBuilder.DefineField("s_InfoForRequestName", typeof(Hashtable),
                                                             FieldAttributes.Private | FieldAttributes.InitOnly |
                                                             FieldAttributes.Static);
                
                m_staticConstrContext = new StaticConstructorContext(staticConstructor.GetILGenerator());
                m_staticConstrContext.Setup(m_forType, m_typetypeField, m_nameForInfoField, m_methodInfoForNameField,
                                            typeBuilder);
            }
            
            internal void Complete() {
                m_staticConstrContext.End();
            }
            
        }
        
        #endregion Types
        
        #region SFields
        
        private static readonly MethodInfo s_opStringEq = 
            ReflectionHelper.StringType.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static);
        
        private static Marshaller s_marshaller = Marshaller.GetSingleton();
        
        #endregion SFields        
        #region IFields

        private static SerializationGenerator s_serGen = new SerializationGenerator();
        
        private AssemblyBuilder m_asmBuilder;
        private ModuleBuilder m_modBuilder;
        
        private AttributeExtCollection m_oneSeqAttrColl = new AttributeExtCollection(new Attribute[] { new IdlSequenceAttribute(0L),
                                                                                                       new StringValueAttribute(), 
                                                                                                       new WideCharAttribute(false) });
        
        /// <summary>
        /// caches the created arguments-serializers, because Activator.CreateInstance is an expensive operation.
        /// </summary>        
        private Hashtable /* <Type, ArgumentsSerializer> */ m_argumentsSerializers = new Hashtable();

        #endregion IFields
        #region IConstructors
        
        private SerializationGenerator() {
            Initalize();
        }

        #endregion IConstructors
        #region SMethods
        
        public static SerializationGenerator GetSingleton() {
            return s_serGen;
        }

        #endregion SMethods
        #region IMethods

        private void Initalize() {
            AssemblyName asmname = new AssemblyName();
            asmname.Name = "dynSerializationHelpers";        
            m_asmBuilder = System.Threading.Thread.GetDomain().
                DefineDynamicAssembly(asmname, AssemblyBuilderAccess.RunAndSave);
            m_modBuilder = m_asmBuilder.DefineDynamicModule("dynSerializationHelpers.netmodule", 
                                                            "dynSerializationHelpers.dll");                        
        }
        
        private string GetArgumentSerializerTypeName(Type forType) {
            return "Ch.Elca.Iiop.Generators." + forType.Namespace + "." + 
                   "_" + forType.Name + "ArgHelper";            
        }
        
        private string GetInstanceSerializerTypeName(Type forType, Serialiser forTypeSerializer) {            
            string forTypeName = forType.Name;
            if (forType.IsArray) {
                string arrBeginEscape = "_arrSH";
                if (forTypeSerializer is IdlSequenceSerializer) {
                    arrBeginEscape = "_seqSH";
                }
                // need to escape [ and ] and ,
                forTypeName = forTypeName.Replace("[", arrBeginEscape);
                forTypeName = forTypeName.Replace("]", "_");
                forTypeName = forTypeName.Replace(",", "_");
            }            
            
            string result = "Ch.Elca.Iiop.Generators." + forType.Namespace + "." +
                   forTypeName + "Helper";
            return result;
        }
        

        /// <summar>Retrieve or generate a serialiser to use for serialising/deserialising
        /// a request/response for an object with class/interface forType</summary>
        internal ArgumentsSerializer GetArgumentsSerialiser(Type forType) {
            string argSerTypeName = GetArgumentSerializerTypeName(forType);
            lock(this) {
                ArgumentsSerializer ser = (ArgumentsSerializer)m_argumentsSerializers[forType];
                if (ser == null) {
                    Type serType = CreateArgumentsSerialiser(new ArgSerializationGenerationContext(forType),
                                                             argSerTypeName);
                    ser = (ArgumentsSerializer)Activator.CreateInstance(serType);
                    m_argumentsSerializers[forType] = ser;
                }
                if (forType.Name == "_XYZYu") {
                    m_asmBuilder.Save("dynSerializationHelpers.dll");
                }
                return ser;
            }
        }
        
        /// <summar>Retrieve or generate a serialiser to use for serialising/deserialising
        /// an instance of the given type forType. forTypeSerializer is responsible to
        /// serialize/deserialize the type by using reflection. It is also responsible for generating
        /// the real serialization/deserialization code</summary>        
        internal Type GetInstanceSerialiser(Type forType, AttributeExtCollection attributes,
                                            Serialiser forTypeSerializer) {
            string instanceSerTypeName = GetInstanceSerializerTypeName(forType, forTypeSerializer);
            lock(this) {
                Type ser = m_asmBuilder.GetType(instanceSerTypeName);
                if (ser == null) {
                    ser = CreateInstanceSerialiser(forType, attributes, instanceSerTypeName, forTypeSerializer);
                }
                return ser;
            }            
        }        

        /// <summary>
        /// generates the code for marshalling requests arguments for a call to the given method
        /// </summary>        
        /// <remarks>
        /// the generated code is called inside 
        /// the ArgumentSerializer method
        /// public void SerReqArgsFor_$IDLMETHODNAME(object[] actual, CdrOutputStream targetStream)
        /// </remarks>
        private void GenerateSerializeRequestArgsForMethod(ILGenerator gen, MethodInfo method) {
                    
            LocalBuilder streamLocal =
                gen.DeclareLocal(typeof(Ch.Elca.Iiop.Cdr.CdrOutputStream));
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Stloc, streamLocal);
            LocalBuilder actualLocal =
                gen.DeclareLocal(ReflectionHelper.ObjectType);
            gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Stloc, actualLocal);
            LocalBuilder tempForTypeSerDeser =
                gen.DeclareLocal(ReflectionHelper.ObjectType);
            gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Stloc, tempForTypeSerDeser);
            
            ParameterInfo[] parameters = method.GetParameters();
            
            for (int actualParamNr = 0; actualParamNr < parameters.Length; actualParamNr++) {
                ParameterInfo paramInfo = parameters[actualParamNr];
                // iterate through the parameters, nonOut and nonRetval params are serialised for a request
                if (ParameterMarshaller.IsInParam(paramInfo) || ParameterMarshaller.IsRefParam(paramInfo)) {
                    AttributeExtCollection paramAttrs = ReflectionHelper.CollectParameterAttributes(paramInfo, 
                                                                                                    method);
                    gen.Emit(OpCodes.Ldarg_1);
                    gen.Emit(OpCodes.Ldc_I4, actualParamNr);
                    gen.Emit(OpCodes.Ldelem_Ref); 
                    gen.Emit(OpCodes.Stloc, actualLocal); // store actual to serialise in local variable
                    s_marshaller.GenerateMarshallingCodeFor(paramInfo.ParameterType, paramAttrs,
                                                            gen, actualLocal, streamLocal, 
                                                            tempForTypeSerDeser, this);
                }
                // move to next parameter
                // out-args are also part of the actual array -> move to next for those whithout doing something
            }
            
            GenerateSerializeContextElements(gen, method, actualLocal, streamLocal, tempForTypeSerDeser);
            gen.Emit(OpCodes.Ret);
        }

        private void GenerateSerializeContextElements(ILGenerator gen, MethodInfo method,
                                                      LocalBuilder actualLocal, LocalBuilder streamLocal,
                                                      LocalBuilder tempForTypeSerDeser) {        
            AttributeExtCollection methodAttrs =
                ReflectionHelper.GetCustomAttriutesForMethod(method, true,
                                                             ReflectionHelper.ContextElementAttributeType);
            if (methodAttrs.Count > 0) {
                LocalBuilder contextSeqLocal = gen.DeclareLocal(typeof(string[]));
                gen.Emit(OpCodes.Ldc_I4, methodAttrs.Count * 2);
                gen.Emit(OpCodes.Newarr, ReflectionHelper.StringType);
                gen.Emit(OpCodes.Stloc, contextSeqLocal);                
                for (int i = 0; i < methodAttrs.Count; i++) {
                    string contextKey =
                        ((ContextElementAttribute)methodAttrs.GetAttributeAt(i)).ContextElementKey;
                    // store context key at place i*2
                    gen.Emit(OpCodes.Ldloc, contextSeqLocal);
                    gen.Emit(OpCodes.Ldc_I4, i * 2);
                    gen.Emit(OpCodes.Ldstr, contextKey);
                    gen.Emit(OpCodes.Stelem_Ref);
                    // store context Value at place i*2 + 1
                    gen.Emit(OpCodes.Ldloc, contextSeqLocal);
                    gen.Emit(OpCodes.Ldc_I4, i * 2 + 1);
                    gen.Emit(OpCodes.Ldarg_0); // arg0 for GetContextElementFromCallContext
                    gen.Emit(OpCodes.Ldarg_3); // call context
                    gen.Emit(OpCodes.Ldstr, contextKey); // context key
                    gen.Emit(OpCodes.Call,
                             ArgumentsSerializer.ClassType.
                                 GetMethod("GetContextElementFromCallContext", 
                                           BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance));
                    gen.Emit(OpCodes.Stelem_Ref);
                }
                // serialise the context seq                
                gen.Emit(OpCodes.Ldloc, contextSeqLocal);
                gen.Emit(OpCodes.Stloc, actualLocal);
                s_marshaller.GenerateMarshallingCodeFor(typeof(string[]), m_oneSeqAttrColl, gen, actualLocal,
                                                        streamLocal, tempForTypeSerDeser, this);
            }    
        }
        
        
        /// <summary>
        /// generates the code for demarshalling requests arguments for a call to the given method
        /// </summary>        
        /// <remarks>
        /// the generated code is called inside 
        /// the ArgumentSerializer method
        /// public object[] DeserReqArgsFor_$IDLMETHODNAME((CdrInputStream sourceStream,
        ///                                                out IDictionary contextElements)
        /// </remarks>
        private void GenerateDeserializeRequestArgsForMethod(ILGenerator gen, MethodInfo method) {
            LocalBuilder streamLocal =
                gen.DeclareLocal(typeof(Ch.Elca.Iiop.Cdr.CdrInputStream));
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stloc, streamLocal);
            LocalBuilder resultLocal =
                gen.DeclareLocal(ReflectionHelper.ObjectArrayType);
            LocalBuilder tempForTypeSerDeser =
                gen.DeclareLocal(ReflectionHelper.ObjectType);
            gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Stloc, tempForTypeSerDeser);
            
            // DeserializeRequestArgs: init contextElements
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Stind_Ref);            
           
            ParameterInfo[] parameters = method.GetParameters();
            // assign result for this method
            gen.Emit(OpCodes.Ldc_I4, parameters.Length);
            gen.Emit(OpCodes.Newarr, ReflectionHelper.ObjectType);
            gen.Emit(OpCodes.Stloc, resultLocal);
                                    
            for (int actualParamNr = 0; actualParamNr < parameters.Length; actualParamNr++) {
                ParameterInfo paramInfo = parameters[actualParamNr];
                if (ParameterMarshaller.IsInParam(paramInfo) || ParameterMarshaller.IsRefParam(paramInfo)) {
                    AttributeExtCollection paramAttrs = ReflectionHelper.CollectParameterAttributes(paramInfo, 
                                                                                                    method);
                    gen.Emit(OpCodes.Ldloc, resultLocal);
                    gen.Emit(OpCodes.Ldc_I4, actualParamNr);
                    s_marshaller.GenerateUnmarshallingCodeFor(paramInfo.ParameterType, paramAttrs,
                                                              gen, streamLocal, tempForTypeSerDeser,
                                                              this);
                    gen.Emit(OpCodes.Stelem_Ref);
                } // else: null for an out parameter
            }           
            
            // Deserialise context elements
            GenerateDeserializeContextElements(gen, method, streamLocal, tempForTypeSerDeser);
            
            gen.Emit(OpCodes.Ldloc, resultLocal); // push result onto the stack
            gen.Emit(OpCodes.Ret);
        }
        
        private void GenerateDeserializeContextElements(ILGenerator gen, MethodInfo method,
                                                        LocalBuilder streamLocal, LocalBuilder tempForTypeSerDeser) {
            AttributeExtCollection methodAttrs =
                ReflectionHelper.GetCustomAttriutesForMethod(method, true,
                                                             ReflectionHelper.ContextElementAttributeType);
            if (methodAttrs.Count > 0) {
                LocalBuilder contextSeqLocal = gen.DeclareLocal(typeof(string[]));
                
                s_marshaller.GenerateUnmarshallingCodeFor(typeof(string[]), m_oneSeqAttrColl, gen,
                                                          streamLocal, tempForTypeSerDeser, this);
                IlEmitHelper.GetSingleton().GenerateCastObjectToType(gen, typeof(string[]));
                gen.Emit(OpCodes.Stloc, contextSeqLocal);
                
                // store a new HybridDictionary in contextElements out param
                gen.Emit(OpCodes.Ldarg_2);
                gen.Emit(OpCodes.Newobj, typeof(HybridDictionary).GetConstructor(Type.EmptyTypes));
                gen.Emit(OpCodes.Stind_Ref);
                
                // compare, if number of context element is a multiple of 2 (i.e. contextElems.Length % 2 == 0)
                Label lengthCheckOk = gen.DefineLabel();
                gen.Emit(OpCodes.Ldloc, contextSeqLocal);
                gen.Emit(OpCodes.Ldlen);
                gen.Emit(OpCodes.Conv_I4);
                gen.Emit(OpCodes.Ldc_I4_2);
                gen.Emit(OpCodes.Rem);
                gen.Emit(OpCodes.Brfalse, lengthCheckOk); // i.e. branch if reminder is 0
                // if not 0, throw exception:
                gen.Emit(OpCodes.Ldc_I4, 67);
                gen.Emit(OpCodes.Ldc_I4, (int)omg.org.CORBA.CompletionStatus.Completed_No);
                gen.Emit(OpCodes.Newobj, typeof(omg.org.CORBA.MARSHAL).GetConstructor(new Type[] { ReflectionHelper.Int32Type,
                                                                                          typeof(omg.org.CORBA.CompletionStatus) } ));
                gen.Emit(OpCodes.Throw);
                // now check and extract deserialised context elements                
                gen.MarkLabel(lengthCheckOk);
                LocalBuilder loopVar = gen.DeclareLocal(ReflectionHelper.Int32Type);
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Stloc, loopVar);
                Label loopCompare = gen.DefineLabel();                
                gen.Emit(OpCodes.Br, loopCompare);
                Label loopBegin = gen.DefineLabel();
                gen.MarkLabel(loopBegin);
                Label loopIncrement = gen.DefineLabel();
                // loop over received elements, and check if key is specified in signature; otherwise ignore element
                foreach (ContextElementAttribute attr in methodAttrs) {
                    // compare key strings
                    gen.Emit(OpCodes.Ldstr, attr.ContextElementKey);
                    gen.Emit(OpCodes.Ldloc, contextSeqLocal);
                    gen.Emit(OpCodes.Ldloc, loopVar);
                    gen.Emit(OpCodes.Ldelem_Ref);                    
                    gen.Emit(OpCodes.Call, ReflectionHelper.StringType.GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public));
                    Label nextCheck = gen.DefineLabel();
                    gen.Emit(OpCodes.Brfalse, nextCheck);
                    // store in dictionary
                    gen.Emit(OpCodes.Ldarg_2);
                    gen.Emit(OpCodes.Ldind_Ref); // array
                    gen.Emit(OpCodes.Ldstr, attr.ContextElementKey); // key
                    gen.Emit(OpCodes.Ldloc, contextSeqLocal); // val
                    gen.Emit(OpCodes.Ldloc, loopVar);
                    gen.Emit(OpCodes.Ldc_I4_1);
                    gen.Emit(OpCodes.Add);
                    gen.Emit(OpCodes.Ldelem_Ref); // val froms seq
                    gen.Emit(OpCodes.Callvirt, typeof(IDictionary).GetMethod("set_Item", BindingFlags.Public | BindingFlags.Instance));                    
                    gen.Emit(OpCodes.Br, loopIncrement); // ok, branch to loop increment                    
                    gen.MarkLabel(nextCheck);                    
                }
                gen.Emit(OpCodes.Br, loopIncrement); // ok, branch to loop increment
                gen.MarkLabel(loopIncrement);
                // increment loop var
                gen.Emit(OpCodes.Ldloc, loopVar);
                gen.Emit(OpCodes.Ldc_I4_2);
                gen.Emit(OpCodes.Add);
                gen.Emit(OpCodes.Stloc, loopVar);
                // compare loop var
                gen.MarkLabel(loopCompare);
                gen.Emit(OpCodes.Ldloc, loopVar);
                gen.Emit(OpCodes.Ldloc, contextSeqLocal);
                gen.Emit(OpCodes.Ldlen);
                gen.Emit(OpCodes.Conv_I4);
                gen.Emit(OpCodes.Blt, loopBegin);                            
            }            
        }

        /// <summary>
        /// generates the code for marshalling response arguments for a call to the given method
        /// </summary>        
        /// <remarks>
        /// the generated code is called inside 
        /// the ArgumentSerializer method
        /// public void SerRespArgsFor_$IDLMETHODNAME(object retValue, object[] outArgs,
        ///                                           CdrOutputStream targetStream);
        /// </remarks>        
        private void GenerateSerializeResponseArgsForMethod(ILGenerator gen, MethodInfo method) {
            LocalBuilder streamLocal =
                gen.DeclareLocal(typeof(Ch.Elca.Iiop.Cdr.CdrOutputStream));
            gen.Emit(OpCodes.Ldarg_3);
            gen.Emit(OpCodes.Stloc, streamLocal);
            LocalBuilder actualLocal =
                gen.DeclareLocal(ReflectionHelper.ObjectType);
            gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Stloc, actualLocal);
            LocalBuilder tempForTypeSerDeser =
                gen.DeclareLocal(ReflectionHelper.ObjectType);
            gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Stloc, tempForTypeSerDeser);
            
            ParameterInfo[] parameters = method.GetParameters();
            // first serialise the return value, 
            if (!method.ReturnType.Equals(ReflectionHelper.VoidType)) {
                AttributeExtCollection returnAttr = ReflectionHelper.CollectReturnParameterAttributes(method);
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stloc, actualLocal); // store actual to serialise in local
                s_marshaller.GenerateMarshallingCodeFor(method.ReturnType, returnAttr, gen,
                                                        actualLocal, streamLocal, 
                                                        tempForTypeSerDeser, this);
            }
            // ... then the out/ref args
            int outParamNr = 0;
            for (int actualParamNr = 0; actualParamNr < parameters.Length; actualParamNr++) {
                ParameterInfo paramInfo = parameters[actualParamNr];
                // iterate through the parameters, out/ref parameters are serialised
                if (ParameterMarshaller.IsOutParam(paramInfo) || ParameterMarshaller.IsRefParam(paramInfo)) {
                    AttributeExtCollection paramAttrs = ReflectionHelper.CollectParameterAttributes(paramInfo, 
                                                                                                    method);
                    gen.Emit(OpCodes.Ldarg_2);
                    gen.Emit(OpCodes.Ldc_I4, outParamNr);
                    gen.Emit(OpCodes.Ldelem_Ref); 
                    gen.Emit(OpCodes.Stloc, actualLocal); // store actual to serialise in local variable                    
                    s_marshaller.GenerateMarshallingCodeFor(paramInfo.ParameterType, paramAttrs, gen,
                                                            actualLocal, streamLocal, 
                                                            tempForTypeSerDeser, this);
                    outParamNr++;
                }
            }    
            gen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// generates the code for unmarshalling the response arguments for a call to the given method
        /// </summary>        
        /// <remarks>
        /// the generated code is called inside 
        /// the ArgumentSerializer method
        /// public object DeserRespArgsFor_$IDLMETHODNAME(CdrInputStream sourceStream,
        ///                                               out object[] outArgs);
        /// </remarks>
        private void GenerateDeserializeResponseArgsForMethod(ILGenerator gen, MethodInfo method) {
            LocalBuilder streamLocal =
                gen.DeclareLocal(typeof(Ch.Elca.Iiop.Cdr.CdrInputStream));
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stloc, streamLocal);
            LocalBuilder resultLocal =
                gen.DeclareLocal(ReflectionHelper.ObjectType); // the result is an object, initalize with null
            gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Stloc, resultLocal);
            LocalBuilder tempForTypeSerDeser =
                gen.DeclareLocal(ReflectionHelper.ObjectType);
            gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Stloc, tempForTypeSerDeser);            
                                   
            ParameterInfo[] parameters = method.GetParameters();
            // demarshal first the return value            
            if (!method.ReturnType.Equals(ReflectionHelper.VoidType)) {
                AttributeExtCollection returnAttr = ReflectionHelper.CollectReturnParameterAttributes(method);
                
                s_marshaller.GenerateUnmarshallingCodeFor(method.ReturnType, returnAttr,
                                                          gen, streamLocal, 
                                                          tempForTypeSerDeser, this);
                gen.Emit(OpCodes.Stloc, resultLocal);
            }
            
            // ... then the outargs
            // assign the outargs for this method
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Ldc_I4, parameters.Length);
            gen.Emit(OpCodes.Newarr, ReflectionHelper.ObjectType);
            gen.Emit(OpCodes.Stind_Ref); // store in outArgs out-argument
            
            bool outArgFound = false;
            for (int actualParamNr = 0; actualParamNr < parameters.Length; actualParamNr++) {                    
                ParameterInfo paramInfo = parameters[actualParamNr];
                if (ParameterMarshaller.IsOutParam(paramInfo) || ParameterMarshaller.IsRefParam(paramInfo)) {
                    AttributeExtCollection paramAttrs = ReflectionHelper.CollectParameterAttributes(paramInfo, 
                                                                                                    method);                    
                    gen.Emit(OpCodes.Ldarg_2); // load the out args array
                    gen.Emit(OpCodes.Ldind_Ref);
                    gen.Emit(OpCodes.Ldc_I4, actualParamNr); // target index
                    
                    s_marshaller.GenerateUnmarshallingCodeFor(paramInfo.ParameterType, paramAttrs,
                                                              gen, streamLocal, 
                                                              tempForTypeSerDeser, this);
                    
                    gen.Emit(OpCodes.Stelem_Ref); // store the value in the array
                    
                    outArgFound = true;
                } // else: for an in param null must be added to out-args
            }

            // prepare the result
            // need to return empty array, if no out-arg is present, because otherwise async calls fail
            if (!outArgFound) {
                gen.Emit(OpCodes.Ldarg_2);
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Newarr, ReflectionHelper.ObjectType);
                gen.Emit(OpCodes.Stind_Ref); // store in outArgs out-argument
            }
            
            gen.Emit(OpCodes.Ldloc, resultLocal); // push result onto the stack
            gen.Emit(OpCodes.Ret);
        }
                       
        private string DetermineOperationName(MethodInfo method) {
            bool isMethodOverloaded = false;
            try {
                method.DeclaringType.GetMethod(method.Name, BindingFlags.Public | BindingFlags.Instance);
            } catch (AmbiguousMatchException) {
                isMethodOverloaded = true;
            }
            string mappedName = 
                IdlNaming.GetMethodRequestOperationName(method, isMethodOverloaded);
            return mappedName;
        }
        
        private void DefineMethodInfoToNameMapping(MethodInfo method, string mappedName,
                                                    ArgSerializationGenerationContext context) {           
            // init in static constr
            ILGenerator gen = context.StaticConstrContext.Generator;
            gen.Emit(OpCodes.Ldloc, context.StaticConstrContext.ForTypeLocal);
            gen.Emit(OpCodes.Ldstr, method.Name);
            gen.Emit(OpCodes.Ldc_I4, (int)(BindingFlags.Public | BindingFlags.Instance));
            gen.Emit(OpCodes.Ldnull); // binder
            ParameterInfo[] parameters = method.GetParameters();
            gen.Emit(OpCodes.Ldc_I4, parameters.Length);
            gen.Emit(OpCodes.Newarr, ReflectionHelper.TypeType);
            gen.Emit(OpCodes.Stloc, context.StaticConstrContext.TypeArrayLocal);
            for (int i = 0; i < parameters.Length; i++) {
                gen.Emit(OpCodes.Ldloc, context.StaticConstrContext.TypeArrayLocal);
                gen.Emit(OpCodes.Ldc_I4, i);
                IlEmitHelper.GetSingleton().EmitLoadType(gen, parameters[i].ParameterType);
                gen.Emit(OpCodes.Stelem_Ref);                
            }
            gen.Emit(OpCodes.Ldloc, context.StaticConstrContext.TypeArrayLocal);
            gen.Emit(OpCodes.Ldnull);
            MethodInfo getMethodFromType =
                ReflectionHelper.TypeType.GetMethod("GetMethod", BindingFlags.Instance | BindingFlags.Public,
                                                    null, new Type[] { ReflectionHelper.StringType, typeof(BindingFlags),
                                                                      typeof(Binder), typeof(Type[]), typeof(ParameterModifier[]) },
                                                    null);
            gen.Emit(OpCodes.Call, getMethodFromType);
            gen.Emit(OpCodes.Stloc, context.StaticConstrContext.MethodInfoLocal);            
            
            // insert into hashtable the mapping from methodinfo to idl request name
            gen.Emit(OpCodes.Ldsfld, context.NameForInfoField);
            gen.Emit(OpCodes.Ldloc, context.StaticConstrContext.MethodInfoLocal); // key is the methodinfo from above
            gen.Emit(OpCodes.Ldstr, mappedName);
            gen.Emit(OpCodes.Callvirt, 
                     typeof(Hashtable).GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetSetMethod());            
            
            // insert into hashtable the mapping from idl request name to methodinfo
            gen.Emit(OpCodes.Ldsfld, context.MethodInfoForNameField);
            gen.Emit(OpCodes.Ldstr, mappedName); // key is the name
            gen.Emit(OpCodes.Ldloc, context.StaticConstrContext.MethodInfoLocal); // the methodinfo from above
            gen.Emit(OpCodes.Callvirt, 
                     typeof(Hashtable).GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetSetMethod());            
        }
               
        private void GenerateArgumentSerialisationsForMethod(ArgSerializationGenerationContext context,
                                                             MethodInfo method, string mappedName) {
                       
                DefineMethodInfoToNameMapping(method, mappedName, context);
                
                // generate the ser/deser methods for the passed method.                

                // public void SerReqArgsFor_$IDLMETHODNAME(object[] actual, CdrOutputStream targetStream, LogicalCallContex callContext);
                MethodBuilder serReqArgs = 
                    context.TypeBuilder.DefineMethod(ArgumentsSerializer.SER_REQ_ARGS_METHOD_PREFIX + mappedName,
                                                     MethodAttributes.Public | MethodAttributes.HideBySig,
                                                     ReflectionHelper.VoidType,
                                                     new Type[] { ReflectionHelper.ObjectArrayType,
                                                                  typeof(Ch.Elca.Iiop.Cdr.CdrOutputStream),
                                                                  typeof(System.Runtime.Remoting.Messaging.LogicalCallContext)
                                                     });                
                serReqArgs.DefineParameter(1, ParameterAttributes.In, "actual");
                serReqArgs.DefineParameter(2, ParameterAttributes.In, "targetStream");
                serReqArgs.DefineParameter(3, ParameterAttributes.In, "callContext");
                
                GenerateSerializeRequestArgsForMethod(serReqArgs.GetILGenerator(), method);

            
                // public object[] DeserReqArgsFor_$IDLMETHODNAME(CdrInputStream sourceStream,
                //                                                out IDictionary contextElements);            
                MethodBuilder deserReqArgs =
                    context.TypeBuilder.DefineMethod(ArgumentsSerializer.DESER_REQ_ARGS_METHOD_PREFIX + mappedName,
                                                     MethodAttributes.Public | MethodAttributes.HideBySig,
                                                     ReflectionHelper.ObjectArrayType,
                                                     new Type[] { typeof(Ch.Elca.Iiop.Cdr.CdrInputStream),
                                                                  ReflectionHelper.GetByRefTypeFor(typeof(IDictionary))
                                                     });
                deserReqArgs.DefineParameter(1, ParameterAttributes.In, "sourceStream");
                deserReqArgs.DefineParameter(2, ParameterAttributes.Out, "contextElements");
                
                GenerateDeserializeRequestArgsForMethod(deserReqArgs.GetILGenerator(), method);
            
                // public void SerRespArgsFor_$IDLMETHODNAME(object retValue, object[] outArgs,
                //                                           CdrOutputStream targetStream);
                MethodBuilder serRespArgs = 
                    context.TypeBuilder.DefineMethod(ArgumentsSerializer.SER_RESP_ARGS_METHOD_PREFIX + mappedName,
                                                     MethodAttributes.Public | MethodAttributes.HideBySig,
                                                     ReflectionHelper.VoidType,
                                                     new Type[] { ReflectionHelper.ObjectType,
                                                                  ReflectionHelper.ObjectArrayType,
                                                                  typeof(Ch.Elca.Iiop.Cdr.CdrOutputStream)
                                                     });
                serRespArgs.DefineParameter(1, ParameterAttributes.In, "retValue");
                serRespArgs.DefineParameter(2, ParameterAttributes.In, "outArgs");
                serRespArgs.DefineParameter(3, ParameterAttributes.In, "targetStream");
                
                GenerateSerializeResponseArgsForMethod(serRespArgs.GetILGenerator(), method);
            
        
                // public object DeserRespArgsFor_$IDLMETHODNAME(CdrInputStream sourceStream,
                //                                               out object[] outArgs);
                MethodBuilder deserRespArgs = 
                    context.TypeBuilder.DefineMethod(ArgumentsSerializer.DESER_RESP_ARGS_METHOD_PREFIX + mappedName,
                                                     MethodAttributes.Public | MethodAttributes.HideBySig,
                                                     ReflectionHelper.ObjectType,
                                                     new Type[] { typeof(Ch.Elca.Iiop.Cdr.CdrInputStream),
                                                                  ReflectionHelper.GetByRefTypeFor(typeof(object[]))
                                                     });
                deserRespArgs.DefineParameter(1, ParameterAttributes.In, "sourceStream");
                deserRespArgs.DefineParameter(2, ParameterAttributes.Out, "outArgs");                
                
                GenerateDeserializeResponseArgsForMethod(deserRespArgs.GetILGenerator(), method);
        }        
        
        /// <summary>
        /// returns true, if no serialization code should be generated for a method;
        /// otherwise false.
        /// </summary>
        private bool IgnoreMethod(MethodInfo info) {
            if (info.DeclaringType.IsInterface) {
                return false;
            }
            // only ignore methods on classes and not on interfaces
            if ((info.Name == "InitializeLifetimeService") && (info.GetParameters().Length == 0) &&
                (info.ReturnType.Equals(ReflectionHelper.ObjectType))) {
                return true;            
            } // from MarshalByRefObject
            // TODO
            return false;                
        }
        
        private bool IgnoreProperty(PropertyInfo info) {
            // TODO
            return false;
        }
        
        private void DefineArgumentsSerialiserMethods(ArgSerializationGenerationContext context) {                                                                                       
            MethodInfo[] methods =
                context.ForType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < methods.Length; i++) {
                if (methods[i].IsSpecialName) {
                    continue;
                }
                if (IgnoreMethod(methods[i])) {
                    // don't support remote calls for method
                    continue;
                }
                GenerateArgumentSerialisationsForMethod(context,
                                                        methods[i],
                                                        DetermineOperationName(methods[i]));
            }
            
            PropertyInfo[] properties =
                context.ForType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < properties.Length; i++) {
                if (IgnoreProperty(properties[i])) {
                    // don't support remote calls for this property
                    continue;
                }                
                MethodInfo getter = properties[i].GetGetMethod();
                MethodInfo setter = properties[i].GetSetMethod();
                if (getter != null) {
                    GenerateArgumentSerialisationsForMethod(context,
                                                            getter,
                                                            IdlNaming.GetPropertyRequestOperationName(properties[i], false));
                }
                
                if (setter != null) {
                    GenerateArgumentSerialisationsForMethod(context,
                                                            setter,
                                                            IdlNaming.GetPropertyRequestOperationName(properties[i], true));
                }                
            }            
        }
        
        private void DefineGetMethodInfoFor(MethodBuilder getMethodInfoFor, ArgSerializationGenerationContext context) {
            ILGenerator gen = getMethodInfoFor.GetILGenerator();            
            gen.Emit(OpCodes.Ldsfld, context.MethodInfoForNameField);
            gen.Emit(OpCodes.Ldarg_1); // name argument
            PropertyInfo htItem = 
                typeof(Hashtable).GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
            gen.Emit(OpCodes.Callvirt, htItem.GetGetMethod());
            IlEmitHelper.GetSingleton().GenerateCastObjectToType(gen, typeof(MethodInfo));
            gen.Emit(OpCodes.Ret);            
        }
        
        private void DefineGetRequestNameForMethodInfo(MethodBuilder getRequestNameFor, ArgSerializationGenerationContext context) {
            ILGenerator gen = getRequestNameFor.GetILGenerator();            
            gen.Emit(OpCodes.Ldsfld, context.NameForInfoField);
            gen.Emit(OpCodes.Ldarg_1); // methodinfo argument            
            PropertyInfo htItem = 
                typeof(Hashtable).GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
            gen.Emit(OpCodes.Callvirt, htItem.GetGetMethod());
            IlEmitHelper.GetSingleton().GenerateCastObjectToType(gen, ReflectionHelper.StringType);
            gen.Emit(OpCodes.Ret);
        }
        
        private Type CreateArgumentsSerialiser(ArgSerializationGenerationContext context, string serTypeName) {
            if (!(context.ForType.IsInterface || context.ForType.IsMarshalByRef || context.ForType.Equals(ReflectionHelper.ObjectType))) {
                // to allow calling e.g. the equals method; allow object type here too and dont throw exception for object type
                Debug.WriteLine("Can't create an argument serializer for : " + context.ForType.FullName);
                throw new omg.org.CORBA.BAD_PARAM(745, omg.org.CORBA.CompletionStatus.Completed_MayBe);
            }
            
            TypeBuilder resultBuilder = m_modBuilder.DefineType(serTypeName, 
                                                                TypeAttributes.Class | TypeAttributes.Sealed |
                                                                TypeAttributes.Public, 
                                                                typeof(Ch.Elca.Iiop.Marshalling.ArgumentsSerializer),
                                                                Type.EmptyTypes);
            
            ConstructorBuilder staticConstr = resultBuilder.DefineConstructor(MethodAttributes.Static | MethodAttributes.Private |
                                                                              MethodAttributes.HideBySig,
                                                                              CallingConventions.Standard,
                                                                              Type.EmptyTypes);
            
            resultBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            
            context.InitalizeContext(staticConstr,
                                     resultBuilder);
            DefineArgumentsSerialiserMethods(context);
            
            // public abstract MethodInfo GetMethodInfoFor(string method);
            MethodBuilder getMethodInfoFor = resultBuilder.DefineMethod("GetMethodInfoFor",
                                                                        MethodAttributes.Public | MethodAttributes.Virtual |
                                                                        MethodAttributes.Final | MethodAttributes.HideBySig,
                                                                        typeof(MethodInfo),
                                                                        new Type[] { ReflectionHelper.StringType });
            DefineGetMethodInfoFor(getMethodInfoFor, context);
            
            // public abstract MethodInfo GetMethodInfoFor(string method);
            MethodBuilder getRequestNameFor = resultBuilder.DefineMethod("GetRequestNameFor",
                                                                        MethodAttributes.Public | MethodAttributes.Virtual |
                                                                        MethodAttributes.Final | MethodAttributes.HideBySig,
                                                                        ReflectionHelper.StringType,
                                                                        new Type[] { typeof(MethodInfo) });
            
            DefineGetRequestNameForMethodInfo(getRequestNameFor, context);
            
            context.Complete();
    
            Type result = resultBuilder.CreateType();
            return result;
        }        
        
        private void DefineInstanceSerialiserMethods(MethodBuilder serInstance, MethodBuilder deserInstance, 
                                                     Type forType, AttributeExtCollection attributes,
                                                     Serialiser responsibleSerialiser) {
            ILGenerator serInstanceIl = serInstance.GetILGenerator();
            ILGenerator deserInstanceIl = deserInstance.GetILGenerator();
            
            LocalBuilder actualInstance = serInstanceIl.DeclareLocal(ReflectionHelper.ObjectType);
            LocalBuilder targetStream = serInstanceIl.DeclareLocal(typeof(Ch.Elca.Iiop.Cdr.CdrOutputStream));
            LocalBuilder temporaryLocalSer = serInstanceIl.DeclareLocal(ReflectionHelper.ObjectType);            
            serInstanceIl.Emit(OpCodes.Ldarg_1);
            serInstanceIl.Emit(OpCodes.Stloc, actualInstance);
            serInstanceIl.Emit(OpCodes.Ldarg_2);
            serInstanceIl.Emit(OpCodes.Stloc, targetStream);            
            serInstanceIl.Emit(OpCodes.Ldnull);
            serInstanceIl.Emit(OpCodes.Stloc, temporaryLocalSer);
            responsibleSerialiser.GenerateSerialisationCode(forType, attributes,
                                                            serInstanceIl, actualInstance,
                                                            targetStream, temporaryLocalSer, this);
            serInstanceIl.Emit(OpCodes.Ret);
            
                        
            LocalBuilder sourceStream = deserInstanceIl.DeclareLocal(typeof(Ch.Elca.Iiop.Cdr.CdrInputStream));
            LocalBuilder temporaryLocalDeSer = deserInstanceIl.DeclareLocal(ReflectionHelper.ObjectType);
            deserInstanceIl.Emit(OpCodes.Ldarg_1);
            deserInstanceIl.Emit(OpCodes.Stloc, sourceStream);            
            deserInstanceIl.Emit(OpCodes.Ldnull);
            deserInstanceIl.Emit(OpCodes.Stloc, temporaryLocalDeSer);
            responsibleSerialiser.GenerateDeserialisationCode(forType, attributes,
                                                              deserInstanceIl, sourceStream, 
                                                              temporaryLocalDeSer, this);
            deserInstanceIl.Emit(OpCodes.Ret);
        }
        

        /// <summary>generates a serialization helper type for a type, which is complex to 
        /// serialize/deserialize, so that the code is not directly embedded into the serialization/
        /// deserialization requestor</summary>
        private Type CreateInstanceSerialiser(Type forType, AttributeExtCollection attributes,
                                              string serHelperTypeName,
                                              Serialiser responsibleSerializer) {
            TypeBuilder resultBuilder = m_modBuilder.DefineType(serHelperTypeName, 
                                                                TypeAttributes.Class | TypeAttributes.Sealed |
                                                                TypeAttributes.Public, 
                                                                typeof(Ch.Elca.Iiop.Marshalling.TypeSerializationHelper),
                                                                Type.EmptyTypes);
            // methods to override

            // public abstract void SerializeInstance(object actual, CdrOutputStream targetStream);
            MethodBuilder serInstance = 
                resultBuilder.DefineMethod("SerializeInstance",
                                           MethodAttributes.Public | MethodAttributes.Virtual | 
                                           MethodAttributes.Final | MethodAttributes.HideBySig,
                                           ReflectionHelper.VoidType,
                                           new Type[] { ReflectionHelper.ObjectType,                                                        
                                                        typeof(Ch.Elca.Iiop.Cdr.CdrOutputStream)
                                           });
            serInstance.DefineParameter(1, ParameterAttributes.In, "actual");
            serInstance.DefineParameter(2, ParameterAttributes.In, "targetStream");

            
            // public abstract object DeserializeInstance(CdrInputStream sourceStream);            
            MethodBuilder deserInstance =
                resultBuilder.DefineMethod("DeserializeInstance",
                                           MethodAttributes.Public | MethodAttributes.Virtual | 
                                           MethodAttributes.Final | MethodAttributes.HideBySig,
                                           ReflectionHelper.ObjectType,
                                           new Type[] { typeof(Ch.Elca.Iiop.Cdr.CdrInputStream) });            
            deserInstance.DefineParameter(1, ParameterAttributes.In, "sourceStream");            
            

            DefineInstanceSerialiserMethods(serInstance, deserInstance, 
                                            forType, attributes, responsibleSerializer);
    
            Type result = resultBuilder.CreateType();
            return result;
        }                                                        
        
        #endregion IMethods

    }
    
}
