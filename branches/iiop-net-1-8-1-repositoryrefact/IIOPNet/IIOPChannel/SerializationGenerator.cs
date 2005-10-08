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
        
        private class ArgSerializerMethodContext {
                                    
            private static ConstructorInfo s_badOpCtr =                            
                typeof(omg.org.CORBA.BAD_OPERATION).GetConstructor(new Type[] { ReflectionHelper.Int32Type, 
                                                                                typeof(omg.org.CORBA.CompletionStatus) });
            
            private ILGenerator m_gen;
            private LocalBuilder m_streamLocal;
            private LocalBuilder m_workingLocal;
            private object m_endLabel;
        
            internal ArgSerializerMethodContext(ILGenerator gen) {
                m_gen = gen;
            }
            
            internal ILGenerator Generator {
                get {
                    if (m_gen == null) {
                        // already completed
                        throw new omg.org.CORBA.INTERNAL(4764, omg.org.CORBA.CompletionStatus.Completed_MayBe);
                    }
                    return m_gen;
                }
            }
            
            internal LocalBuilder StreamLocal {
                get {
                    if (m_streamLocal == null) {
                        // not yet setup
                        throw new omg.org.CORBA.INTERNAL(4765, omg.org.CORBA.CompletionStatus.Completed_MayBe);
                    }
                    return m_streamLocal;
                }
            }
            
            internal LocalBuilder WorkingLocal {
                get {
                    if (m_workingLocal == null) {
                        // not yet setup
                        throw new omg.org.CORBA.INTERNAL(4765, omg.org.CORBA.CompletionStatus.Completed_MayBe);
                    }
                    return m_workingLocal;
                }
            }
            
            /// <summary>
            /// the label before method return.
            /// </summary>
            internal Label EndLabel {
                get {
                    if (m_endLabel == null) {
                        throw new omg.org.CORBA.INTERNAL(4765, omg.org.CORBA.CompletionStatus.Completed_MayBe);
                    }
                    return (Label)m_endLabel;
                }
            }
            
            /// <summary>
            /// peforms the inital step, i.e.
            /// - store the stream in a local variable
            /// - define an end label
            /// - define a local to work with
            /// </summary>
            /// <param name="streamType"></param>
            /// <param name="streanFromArgument"></param>
            internal void Setup(Type streamType, int streanFromArgument,
                                Type workingLocalType) {
                m_streamLocal = m_gen.DeclareLocal(streamType);
                m_gen.Emit(OpCodes.Ldarg, streanFromArgument);
                m_gen.Emit(OpCodes.Stloc, m_streamLocal);                
                m_endLabel = m_gen.DefineLabel();
                m_workingLocal = m_gen.DeclareLocal(workingLocalType);
            }
            
            /// <summary>
            /// emit throwing a CORBA System error BAD_OPERATION
            /// </summary>
            internal void EmitThrowBadOperation(int minor,
                                                omg.org.CORBA.CompletionStatus status) {
                Generator.Emit(OpCodes.Ldc_I4, minor);
                Generator.Emit(OpCodes.Ldc_I4, (int)status);
                Generator.Emit(OpCodes.Newobj, s_badOpCtr);
                Generator.Emit(OpCodes.Throw);                
            }
            
            /// <summary>complete the method definition, i.e.
            /// define the end label and emit the ret statement
            /// </summary>
            internal void End() {           
                // the labels after all tests
                Generator.MarkLabel(EndLabel);
                // return
                Generator.Emit(OpCodes.Ret);                
                
                m_endLabel = null;
                m_streamLocal = null;
                m_gen = null;                
            }
            
        }
        
        private class ArgSerializationGenerationContext {           
            
            private Type m_forType;
            
            private ArgSerializerMethodContext m_serializeRequestArgsContext;
            private ArgSerializerMethodContext m_deserializeRequestArgsContext;
            private ArgSerializerMethodContext m_serializeResponseArgsContext;
            private ArgSerializerMethodContext m_deserializeResponseArgsContext;
        
            internal ArgSerializationGenerationContext(Type forType) {
                m_forType = forType;
            }      
            
            internal Type ForType {
                get {
                    return m_forType;
                }
            }
            
            
            internal ArgSerializerMethodContext SerializeRequestArgsContext {
                get {
                    return m_serializeRequestArgsContext;
                }
            }
            
            internal ArgSerializerMethodContext DeserializeRequestArgsContext {
                get {
                    return m_deserializeRequestArgsContext;
                }
            }
            
            internal ArgSerializerMethodContext SerializeResponseArgsContext {
                get {
                    return m_serializeResponseArgsContext;
                }
            }    
            
            internal ArgSerializerMethodContext DeserializeResponseArgsContext {
                get {
                    return m_deserializeResponseArgsContext;
                }
            }                                                    
            
            internal void InitalizeArgSerializerMethodContext(MethodBuilder serializeRequestArgs,
                                                              MethodBuilder deserializeRequestArgs,
                                                              MethodBuilder serializeResponseArgs,
                                                              MethodBuilder deserializeResponseArgs) {
                m_serializeRequestArgsContext = 
                    new ArgSerializerMethodContext(serializeRequestArgs.GetILGenerator());
                m_deserializeRequestArgsContext =
                    new ArgSerializerMethodContext(deserializeRequestArgs.GetILGenerator());
                m_serializeResponseArgsContext =
                    new ArgSerializerMethodContext(serializeResponseArgs.GetILGenerator());
                m_deserializeResponseArgsContext =
                    new ArgSerializerMethodContext(deserializeResponseArgs.GetILGenerator());
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
        
        private string GetInstanceSerializerTypeName(Type forType) {            
            string result = "Ch.Elca.Iiop.Generators." + forType.Namespace + "." +
                   forType.Name + "Helper";
            return result;
        }
        

        /// <summar>Retrieve or generate a serialiser to use for serialising/deserialising
        /// a request/response for an object with class/interface forType</summary>
        internal ArgumentsSerializer GetArgumentsSerialiser(Type forType) {
            string argSerTypeName = GetArgumentSerializerTypeName(forType);
            lock(this) {
                Type ser = m_asmBuilder.GetType(argSerTypeName);
                if (ser == null) {
                    ser = CreateArgumentsSerialiser(new ArgSerializationGenerationContext(forType),
                                                    argSerTypeName);
                }
                if (forType.Name == "_XYZYu") {
                    m_asmBuilder.Save("dynSerializationHelpers.dll");
                }
                return (ArgumentsSerializer)Activator.CreateInstance(ser);
            }
        }
        
        /// <summar>Retrieve or generate a serialiser to use for serialising/deserialising
        /// an instance of the given type forType. forTypeSerializer is responsible to
        /// serialize/deserialize the type by using reflection. It is also responsible for generating
        /// the real serialization/deserialization code</summary>        
        internal Type GetInstanceSerialiser(Type forType, AttributeExtCollection attributes,
                                            Serialiser forTypeSerializer) {
            string instanceSerTypeName = GetInstanceSerializerTypeName(forType);
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
        /// public abstract void SerializeRequestArgs(string targetMethod, object[] actual, CdrOutputStream targetStream)        
        /// </remarks>
        private void GenerateSerializeRequestArgsForMethod(ArgSerializerMethodContext context, MethodInfo method) {
            
            ILGenerator gen = context.Generator;           
            ParameterInfo[] parameters = method.GetParameters();
            
            for (int actualParamNr = 0; actualParamNr < parameters.Length; actualParamNr++) {
                ParameterInfo paramInfo = parameters[actualParamNr];
                // iterate through the parameters, nonOut and nonRetval params are serialised for a request
                if (ParameterMarshaller.IsInParam(paramInfo) || ParameterMarshaller.IsRefParam(paramInfo)) {
                    AttributeExtCollection paramAttrs = ReflectionHelper.CollectParameterAttributes(paramInfo, 
                                                                                                    method);
                    gen.Emit(OpCodes.Ldarg_2);
                    gen.Emit(OpCodes.Ldc_I4, actualParamNr);
                    gen.Emit(OpCodes.Ldelem_Ref); 
                    gen.Emit(OpCodes.Stloc, context.WorkingLocal); // store actual to serialise in local variable
                    s_marshaller.GenerateMarshallingCodeFor(paramInfo.ParameterType, paramAttrs,
                                                            gen, context.WorkingLocal, context.StreamLocal, this);
                }
                // move to next parameter
                // out-args are also part of the actual array -> move to next for those whithout doing something
            }
        }
        
        /// <summary>
        /// generates the code for demarshalling requests arguments for a call to the given method
        /// </summary>        
        /// <remarks>
        /// the generated code is called inside 
        /// the ArgumentSerializer method
        /// public abstract object[] DeserializeRequestArgs(string targetMethod, CdrInputStream sourceStream,
        ///                                                 out IDictionary contextElements)
        /// </remarks>
        private void GenerateDeserializeRequestArgsForMethod(ArgSerializerMethodContext context, MethodInfo method) {
            ILGenerator gen = context.Generator;
            
            ParameterInfo[] parameters = method.GetParameters();
            // assign result for this method
            gen.Emit(OpCodes.Ldc_I4, parameters.Length);
            gen.Emit(OpCodes.Newarr, ReflectionHelper.ObjectType);
            gen.Emit(OpCodes.Stloc, context.WorkingLocal);
                                    
            for (int actualParamNr = 0; actualParamNr < parameters.Length; actualParamNr++) {
                ParameterInfo paramInfo = parameters[actualParamNr];
                if (ParameterMarshaller.IsInParam(paramInfo) || ParameterMarshaller.IsRefParam(paramInfo)) {
                    AttributeExtCollection paramAttrs = ReflectionHelper.CollectParameterAttributes(paramInfo, 
                                                                                                    method);
                    gen.Emit(OpCodes.Ldloc, context.WorkingLocal);
                    gen.Emit(OpCodes.Ldc_I4, actualParamNr);
                    s_marshaller.GenerateUnmarshallingCodeFor(paramInfo.ParameterType, paramAttrs,
                                                              gen, context.StreamLocal, this);
                    gen.Emit(OpCodes.Stelem_Ref);
                } // else: null for an out parameter
            }           
            
            gen.Emit(OpCodes.Ldloc, context.WorkingLocal); // push result onto the stack
        }

        /// <summary>
        /// generates the code for marshalling response arguments for a call to the given method
        /// </summary>        
        /// <remarks>
        /// the generated code is called inside 
        /// the ArgumentSerializer method
        /// public abstract void SerializeResponseArgs(string targetMethod, object retValue, object[] outArgs,
        ///                                            CdrOutputStream targetStream);
        /// </remarks>        
        private void GenerateSerializeResponseArgsForMethod(ArgSerializerMethodContext context, MethodInfo method) {
            ILGenerator gen = context.Generator;
            
            ParameterInfo[] parameters = method.GetParameters();
            // first serialise the return value, 
            if (!method.ReturnType.Equals(ReflectionHelper.VoidType)) {
                AttributeExtCollection returnAttr = ReflectionHelper.CollectReturnParameterAttributes(method);
                gen.Emit(OpCodes.Ldarg_2);
                gen.Emit(OpCodes.Stloc, context.WorkingLocal); // store actual to serialise in local
                s_marshaller.GenerateMarshallingCodeFor(method.ReturnType, returnAttr, gen,
                                                        context.WorkingLocal, context.StreamLocal, this);
            }
            // ... then the out/ref args
            int outParamNr = 0;
            for (int actualParamNr = 0; actualParamNr < parameters.Length; actualParamNr++) {
                ParameterInfo paramInfo = parameters[actualParamNr];
                // iterate through the parameters, out/ref parameters are serialised
                if (ParameterMarshaller.IsOutParam(paramInfo) || ParameterMarshaller.IsRefParam(paramInfo)) {
                    AttributeExtCollection paramAttrs = ReflectionHelper.CollectParameterAttributes(paramInfo, 
                                                                                                    method);
                    gen.Emit(OpCodes.Ldarg_3);
                    gen.Emit(OpCodes.Ldc_I4, actualParamNr);
                    gen.Emit(OpCodes.Ldelem_Ref); 
                    gen.Emit(OpCodes.Stloc, context.WorkingLocal); // store actual to serialise in local variable                    
                    s_marshaller.GenerateMarshallingCodeFor(paramInfo.ParameterType, paramAttrs, gen,
                                                            context.WorkingLocal, context.StreamLocal, this);                                        
                    outParamNr++;
                }
            }            
        }

        /// <summary>
        /// generates the code for unmarshalling the response arguments for a call to the given method
        /// </summary>        
        /// <remarks>
        /// the generated code is called inside 
        /// the ArgumentSerializer method
        /// public abstract object DeserializeResponseArgs(string targetMethod, CdrInputStream sourceStream,
        ///                                                out object[] outArgs);
        /// </remarks>
        private void GenerateDeserializeResponseArgsForMethod(ArgSerializerMethodContext context, MethodInfo method) {
            ILGenerator gen = context.Generator;
            gen.Emit(OpCodes.Ldnull);            
            gen.Emit(OpCodes.Stloc, context.WorkingLocal); // the result is an object, initalize with null
           
            ParameterInfo[] parameters = method.GetParameters();
            // demarshal first the return value            
            if (!method.ReturnType.Equals(ReflectionHelper.VoidType)) {
                AttributeExtCollection returnAttr = ReflectionHelper.CollectReturnParameterAttributes(method);
                
                s_marshaller.GenerateUnmarshallingCodeFor(method.ReturnType, returnAttr,
                                                          gen, context.StreamLocal, this);
                gen.Emit(OpCodes.Stloc, context.WorkingLocal);
            }
            
            // ... then the outargs
            // assign the outargs for this method
            gen.Emit(OpCodes.Ldarg_3);
            gen.Emit(OpCodes.Ldc_I4, parameters.Length);
            gen.Emit(OpCodes.Newarr, ReflectionHelper.ObjectType);
            gen.Emit(OpCodes.Stind_Ref); // store in outArgs out-argument
            
            bool outArgFound = false;
            for (int actualParamNr = 0; actualParamNr < parameters.Length; actualParamNr++) {                    
                ParameterInfo paramInfo = parameters[actualParamNr];
                if (ParameterMarshaller.IsOutParam(paramInfo) || ParameterMarshaller.IsRefParam(paramInfo)) {
                    AttributeExtCollection paramAttrs = ReflectionHelper.CollectParameterAttributes(paramInfo, 
                                                                                                    method);                    
                    gen.Emit(OpCodes.Ldarg_3); // load the out args array
                    gen.Emit(OpCodes.Ldind_Ref);
                    gen.Emit(OpCodes.Ldc_I4, actualParamNr); // target index
                    
                    s_marshaller.GenerateUnmarshallingCodeFor(paramInfo.ParameterType, paramAttrs,
                                                              gen, context.StreamLocal, this);
                    
                    gen.Emit(OpCodes.Stelem_Ref); // store the value in the array
                    
                    outArgFound = true;
                } // else: for an in param null must be added to out-args
            }

            // prepare the result
            // need to return empty array, if no out-arg is present, because otherwise async calls fail
            if (!outArgFound) {
                gen.Emit(OpCodes.Ldarg_3);
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Newarr, ReflectionHelper.ObjectType);
                gen.Emit(OpCodes.Stind_Ref); // store in outArgs out-argument
            }
            
            gen.Emit(OpCodes.Ldloc, context.WorkingLocal); // push result onto the stack
        }
               
        private void GenerateMethodNameCompare(ILGenerator gen, string methodName, Label branchOnFalse) {
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldstr, methodName);
            gen.Emit(OpCodes.Call, s_opStringEq);
            gen.Emit(OpCodes.Brfalse,  branchOnFalse);
        }
        
        private string DetermineMethodName(MethodInfo method) {
            bool isMethodOverloaded = false;
            try {
                method.DeclaringType.GetMethod(method.Name, BindingFlags.Public | BindingFlags.Instance);
            } catch (AmbiguousMatchException) {
                isMethodOverloaded = true;
            }
            string mappedName = 
                IdlNaming.GetRequestMethodName(method, isMethodOverloaded);    
            return mappedName;
        }
               
        private void GenerateArgumentSerialisationsForMethod(ArgSerializationGenerationContext context,
                                                             MethodInfo method) {
    
                // the label after the code for the given method
                Label nextBranchLabelSerReqArgs = context.SerializeRequestArgsContext.Generator.DefineLabel();
                Label nextBranchLabelDeserReqArgs = context.DeserializeRequestArgsContext.Generator.DefineLabel();
                Label nextBranchLabelSerRespArgs = context.SerializeResponseArgsContext.Generator.DefineLabel();
                Label nextBranchLabelDeserRespArgs = context.DeserializeResponseArgsContext.Generator.DefineLabel();

                string mappedName = DetermineMethodName(method);
                GenerateMethodNameCompare(context.SerializeRequestArgsContext.Generator, 
                                          mappedName, nextBranchLabelSerReqArgs);
                GenerateMethodNameCompare(context.DeserializeRequestArgsContext.Generator, 
                                          mappedName, nextBranchLabelDeserReqArgs);
                GenerateMethodNameCompare(context.SerializeResponseArgsContext.Generator, 
                                          mappedName, nextBranchLabelSerRespArgs);
                GenerateMethodNameCompare(context.DeserializeResponseArgsContext.Generator, 
                                          mappedName, nextBranchLabelDeserRespArgs);
                
                // the real serialisation code, if the method name is equal to the checked name
                GenerateSerializeRequestArgsForMethod(context.SerializeRequestArgsContext, method);                                
                GenerateDeserializeRequestArgsForMethod(context.DeserializeRequestArgsContext, method);
                GenerateSerializeResponseArgsForMethod(context.SerializeResponseArgsContext, method);
                GenerateDeserializeResponseArgsForMethod(context.DeserializeResponseArgsContext, method);
                
                // jump to end after handling for the given method name
                context.SerializeRequestArgsContext.Generator.Emit(OpCodes.Br, 
                                                                   context.SerializeRequestArgsContext.EndLabel);
                context.DeserializeRequestArgsContext.Generator.Emit(OpCodes.Br, 
                                                                     context.DeserializeRequestArgsContext.EndLabel);
                context.SerializeResponseArgsContext.Generator.Emit(OpCodes.Br, 
                                                                    context.SerializeResponseArgsContext.EndLabel);
                context.DeserializeResponseArgsContext.Generator.Emit(OpCodes.Br, 
                                                            context.DeserializeResponseArgsContext.EndLabel);
                
                context.SerializeRequestArgsContext.Generator.MarkLabel(nextBranchLabelSerReqArgs);
                context.DeserializeRequestArgsContext.Generator.MarkLabel(nextBranchLabelDeserReqArgs);
                context.SerializeResponseArgsContext.Generator.MarkLabel(nextBranchLabelSerRespArgs);
                context.DeserializeResponseArgsContext.Generator.MarkLabel(nextBranchLabelDeserRespArgs);    
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
            
            context.SerializeRequestArgsContext.
                Setup(typeof(Ch.Elca.Iiop.Cdr.CdrOutputStream), 3,
                      ReflectionHelper.ObjectType);
            context.DeserializeRequestArgsContext.
                Setup(typeof(Ch.Elca.Iiop.Cdr.CdrInputStream), 2,
                      ReflectionHelper.ObjectArrayType);
            context.SerializeResponseArgsContext.
                Setup(typeof(Ch.Elca.Iiop.Cdr.CdrOutputStream), 4,
                      ReflectionHelper.ObjectType);
            context.DeserializeResponseArgsContext.
                Setup(typeof(Ch.Elca.Iiop.Cdr.CdrInputStream), 2,
                      ReflectionHelper.ObjectType);
                                    
            // DeserializeRequestArgs: init contextElements
            context.DeserializeRequestArgsContext.Generator.Emit(OpCodes.Ldarg_3);
            context.DeserializeRequestArgsContext.Generator.Emit(OpCodes.Ldnull);
            context.DeserializeRequestArgsContext.Generator.Emit(OpCodes.Stind_Ref);
            
            // DeserializeResponseArgsContext: init outArgs
            context.DeserializeResponseArgsContext.Generator.Emit(OpCodes.Ldarg_3);
            context.DeserializeResponseArgsContext.Generator.Emit(OpCodes.Ldnull);
            context.DeserializeResponseArgsContext.Generator.Emit(OpCodes.Stind_Ref);
                                        
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
                                                        methods[i]);
            }
            
            PropertyInfo[] properties =
                context.ForType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < properties.Length; i++) {
                if (IgnoreProperty(properties[i])) {
                    // don't support remote calls for this property
                    continue;
                }                
                MethodInfo getter = properties[i].GetGetMethod();
                MethodInfo setter = properties[i].GetGetMethod();
                if (getter != null) {
                    GenerateArgumentSerialisationsForMethod(context,
                                                            getter);
                }
                
                if (setter != null) {
                    GenerateArgumentSerialisationsForMethod(context,
                                                            setter);
                }
                
            }
            
            // default cases: generate BAD_OPERATION: TODO
            context.SerializeRequestArgsContext.EmitThrowBadOperation(10, omg.org.CORBA.CompletionStatus.Completed_No);
            context.DeserializeRequestArgsContext.EmitThrowBadOperation(11, omg.org.CORBA.CompletionStatus.Completed_No);
            context.SerializeResponseArgsContext.EmitThrowBadOperation(12, omg.org.CORBA.CompletionStatus.Completed_MayBe);
            context.DeserializeResponseArgsContext.EmitThrowBadOperation(13, omg.org.CORBA.CompletionStatus.Completed_MayBe);
            
            // the labels after all tests and return
            context.SerializeRequestArgsContext.End();
            context.DeserializeRequestArgsContext.End();
            context.SerializeResponseArgsContext.End();
            context.DeserializeResponseArgsContext.End();            
        }
        
        private Type CreateArgumentsSerialiser(ArgSerializationGenerationContext context, string serTypeName) {
            if (!(context.ForType.IsInterface || context.ForType.IsMarshalByRef)) {
                Debug.WriteLine("Can't create an argument serializer for : " + context.ForType.FullName);
                throw new omg.org.CORBA.BAD_PARAM(745, omg.org.CORBA.CompletionStatus.Completed_MayBe);
            }
            
            TypeBuilder resultBuilder = m_modBuilder.DefineType(serTypeName, 
                                                                TypeAttributes.Class | TypeAttributes.Sealed |
                                                                TypeAttributes.Public, 
                                                                typeof(Ch.Elca.Iiop.Marshalling.ArgumentsSerializer),
                                                                Type.EmptyTypes);
            // methods to override

            // public abstract void SerializeRequestArgs(string targetMethod, object[] actual, CdrOutputStream targetStream);                                    
            MethodBuilder serReqArgs = 
                resultBuilder.DefineMethod("SerializeRequestArgs",
                                           MethodAttributes.Public | MethodAttributes.Virtual | 
                                           MethodAttributes.Final | MethodAttributes.HideBySig,
                                           ReflectionHelper.VoidType,
                                           new Type[] { ReflectionHelper.StringType, 
                                                        ReflectionHelper.ObjectArrayType,
                                                        typeof(Ch.Elca.Iiop.Cdr.CdrOutputStream)
                                           });
            serReqArgs.DefineParameter(1, ParameterAttributes.In, "targetMethod");
            serReqArgs.DefineParameter(2, ParameterAttributes.In, "actual");
            serReqArgs.DefineParameter(3, ParameterAttributes.In, "targetStream");

            
            // public abstract object[] DeserializeRequestArgs(string targetMethod, CdrInputStream sourceStream,
            //                                                 out IDictionary contextElements);            
            MethodBuilder deserReqArgs =
                resultBuilder.DefineMethod("DeserializeRequestArgs",
                                           MethodAttributes.Public | MethodAttributes.Virtual | 
                                           MethodAttributes.Final | MethodAttributes.HideBySig,
                                           ReflectionHelper.ObjectArrayType,
                                           new Type[] { ReflectionHelper.StringType, 
                                                        typeof(Ch.Elca.Iiop.Cdr.CdrInputStream),
                                                        ReflectionHelper.GetByRefTypeFor(typeof(IDictionary))
                                           });
            deserReqArgs.DefineParameter(1, ParameterAttributes.In, "targetMethod");
            deserReqArgs.DefineParameter(2, ParameterAttributes.In, "sourceStream");
            deserReqArgs.DefineParameter(3, ParameterAttributes.Out, "contextElements");
            
            // public abstract void SerializeResponseArgs(string targetMethod, object retValue, object[] outArgs,
            //                                       CdrOutputStream targetStream);
            MethodBuilder serRespArgs = 
                resultBuilder.DefineMethod("SerializeResponseArgs",
                                           MethodAttributes.Public | MethodAttributes.Virtual | 
                                           MethodAttributes.Final | MethodAttributes.HideBySig,
                                           ReflectionHelper.VoidType,
                                           new Type[] { ReflectionHelper.StringType, 
                                                        ReflectionHelper.ObjectType,
                                                        ReflectionHelper.ObjectArrayType,
                                                        typeof(Ch.Elca.Iiop.Cdr.CdrOutputStream)
                                           });
            serRespArgs.DefineParameter(1, ParameterAttributes.In, "targetMethod");
            serRespArgs.DefineParameter(2, ParameterAttributes.In, "retValue");
            serRespArgs.DefineParameter(3, ParameterAttributes.In, "outArgs");
            serRespArgs.DefineParameter(4, ParameterAttributes.In, "targetStream");
            
        
            // public abstract object DeserializeResponseArgs(string targetMethod, CdrInputStream sourceStream,
            //                                           out object[] outArgs);
            MethodBuilder deserRespArgs = 
                resultBuilder.DefineMethod("DeserializeResponseArgs",
                                           MethodAttributes.Public | MethodAttributes.Virtual | 
                                           MethodAttributes.Final | MethodAttributes.HideBySig,
                                           ReflectionHelper.ObjectType,
                                           new Type[] { ReflectionHelper.StringType, 
                                                        typeof(Ch.Elca.Iiop.Cdr.CdrInputStream),
                                                        ReflectionHelper.GetByRefTypeFor(typeof(object[]))
                                           });
            deserRespArgs.DefineParameter(1, ParameterAttributes.In, "targetMethod");
            deserRespArgs.DefineParameter(2, ParameterAttributes.In, "sourceStream");
            deserRespArgs.DefineParameter(3, ParameterAttributes.Out, "outArgs");

            context.InitalizeArgSerializerMethodContext(serReqArgs, deserReqArgs,
                                                        serRespArgs, deserRespArgs);
            DefineArgumentsSerialiserMethods(context);
    
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
            serInstanceIl.Emit(OpCodes.Ldarg_1);
            serInstanceIl.Emit(OpCodes.Stloc, actualInstance);
            serInstanceIl.Emit(OpCodes.Ldarg_2);
            serInstanceIl.Emit(OpCodes.Stloc, targetStream);            
            responsibleSerialiser.GenerateSerialisationCode(forType, attributes,
                                                            serInstanceIl, actualInstance,
                                                            targetStream);            
            serInstanceIl.Emit(OpCodes.Ret);
            
                        
            LocalBuilder sourceStream = deserInstanceIl.DeclareLocal(typeof(Ch.Elca.Iiop.Cdr.CdrInputStream));
            deserInstanceIl.Emit(OpCodes.Ldarg_1);
            deserInstanceIl.Emit(OpCodes.Stloc, sourceStream);            
            responsibleSerialiser.GenerateDeserialisationCode(forType, attributes,
                                                              deserInstanceIl, sourceStream);                        
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
