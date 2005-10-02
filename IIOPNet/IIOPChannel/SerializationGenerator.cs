
using System;
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
            asmname.Name = "dynSerializationHelpers.dll";        
            m_asmBuilder = System.Threading.Thread.GetDomain().
                DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            m_modBuilder = m_asmBuilder.DefineDynamicModule("dynSerializationHelpers");            
        }
        
        private string GetArgumentSerializerTypeName(Type forType) {
            return "Ch.Elca.Iiop.Generators." + forType.Namespace +
                   "_" + forType.Name + "ArgHelper";            
        }
        
/*        private string GetInstanceSerializerTypeName(Type forType) {
            return "Ch.Elca.Iiop.Generators." + forType.NameSpace +
                   forType.Name + "Helper";
        } */
        

        /// <summar>Retrieve or generate a serialiser to use for serialising/deserialising
        /// a request/response for an object with class/interface forType</summary>
        internal ArgumentsSerializer GetArgumentsSerialiser(Type forType) {
            string argSerTypeName = GetArgumentSerializerTypeName(forType);
            lock(this) {
                Type ser = m_asmBuilder.GetType(argSerTypeName);
                if (ser == null) {
                    ser = CreateArgumentsSerialiser(forType, argSerTypeName);
                }
                return (ArgumentsSerializer)Activator.CreateInstance(ser);
            }
        }

/*        /// <summar>Retrieve or generate a serialiser to use for serialising/deserialising
        /// an instance of the given type forType</summary>        
        internal Type GetInstanceSerialiser(Type forType) {
            string instanceSerTypeName = GetInstanceSerializerTypeName(forType);
            lock(this) {
                
            }
            
        }
*/        

        /// <summary>
        /// generates the code for marshalling requests arguments for a call to the given method
        /// </summary>        
        /// <remarks>
        /// the generated code is called inside 
        /// the ArgumentSerializer method
        /// public abstract void SerializeRequestArgs(string targetMethod, object[] actual, CdrOutputStream targetStream)        
        /// </remarks>
        private void GenerateSerializeRequestArgsForMethod(ILGenerator gen, MethodInfo method) {
            
            LocalBuilder actualInstance = gen.DeclareLocal(ReflectionHelper.ObjectType);
            LocalBuilder targetStream = gen.DeclareLocal(typeof(Ch.Elca.Iiop.Cdr.CdrOutputStream));
            gen.Emit(OpCodes.Ldarg_3);
            gen.Emit(OpCodes.Stloc, targetStream);
            
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
                    gen.Emit(OpCodes.Stloc, actualInstance); // store actual to serialise in local variable
                    s_marshaller.GenerateMarshallingCodeFor(paramInfo.ParameterType, paramAttrs,
                                                            gen, actualInstance, targetStream);
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
        private void GenerateDeserializeRequestArgsForMethod(ILGenerator gen, MethodInfo method) {
            LocalBuilder resultLoc = 
                gen.DeclareLocal(ReflectionHelper.ObjectArrayType);
            gen.Emit(OpCodes.Ldc_I4, 0);
            gen.Emit(OpCodes.Newarr, ReflectionHelper.ObjectType);
            gen.Emit(OpCodes.Stloc, resultLoc);                

            LocalBuilder sourceStream = gen.DeclareLocal(typeof(Ch.Elca.Iiop.Cdr.CdrInputStream));
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Stloc, sourceStream);            
            
            ParameterInfo[] parameters = method.GetParameters();
            // assign result for this method
            gen.Emit(OpCodes.Ldc_I4, parameters.Length);
            gen.Emit(OpCodes.Newarr, ReflectionHelper.ObjectType);
            gen.Emit(OpCodes.Stloc, resultLoc);
                                    
            for (int actualParamNr = 0; actualParamNr < parameters.Length; actualParamNr++) {
                ParameterInfo paramInfo = parameters[actualParamNr];
                if (ParameterMarshaller.IsInParam(paramInfo) || ParameterMarshaller.IsRefParam(paramInfo)) {
                    AttributeExtCollection paramAttrs = ReflectionHelper.CollectParameterAttributes(paramInfo, 
                                                                                                    method);
                    gen.Emit(OpCodes.Ldc_I4, actualParamNr);
                    s_marshaller.GenerateUnmarshallingCodeFor(paramInfo.ParameterType, paramAttrs,
                                                              gen, sourceStream);
                    if (paramInfo.ParameterType.IsValueType) {
                        gen.Emit(OpCodes.Box, paramInfo.ParameterType);                        
                    }                    
                    gen.Emit(OpCodes.Stelem_Ref);
                } // else: null for an out parameter
            }           
            
            gen.Emit(OpCodes.Ldloc, resultLoc); // push result onto the stack
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
        private void GenerateSerializeResponseArgsForMethod(ILGenerator gen, MethodInfo method) {
            LocalBuilder actualInstance = gen.DeclareLocal(ReflectionHelper.ObjectType);
            LocalBuilder targetStream = gen.DeclareLocal(typeof(Ch.Elca.Iiop.Cdr.CdrOutputStream));
            gen.Emit(OpCodes.Ldarg, 4);
            gen.Emit(OpCodes.Stloc, targetStream);           
            
            ParameterInfo[] parameters = method.GetParameters();
            // first serialise the return value, 
            if (!method.ReturnType.Equals(ReflectionHelper.VoidType)) {
                AttributeExtCollection returnAttr = ReflectionHelper.CollectReturnParameterAttributes(method);
                gen.Emit(OpCodes.Ldarg_2);
                gen.Emit(OpCodes.Stloc, actualInstance); // store actual to serialise in local
                s_marshaller.GenerateMarshallingCodeFor(method.ReturnType, returnAttr, gen,
                                                        actualInstance, targetStream);
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
                    gen.Emit(OpCodes.Stloc, actualInstance); // store actual to serialise in local variable                    
                    s_marshaller.GenerateMarshallingCodeFor(paramInfo.ParameterType, paramAttrs, gen,
                                                            actualInstance, targetStream);                                        
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
        private void GenerateDeserializeResponseArgsForMethod(ILGenerator gen, MethodInfo method) {
            LocalBuilder resultLoc = 
                gen.DeclareLocal(ReflectionHelper.ObjectArrayType);
            gen.Emit(OpCodes.Ldc_I4, 0);
            gen.Emit(OpCodes.Newarr, ReflectionHelper.ObjectType);
            gen.Emit(OpCodes.Stloc, resultLoc);                

            LocalBuilder sourceStream = gen.DeclareLocal(typeof(Ch.Elca.Iiop.Cdr.CdrInputStream));
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Stloc, sourceStream);            
            
            ParameterInfo[] parameters = method.GetParameters();
            // demarshal first the return value            
            if (!method.ReturnType.Equals(ReflectionHelper.VoidType)) {
                AttributeExtCollection returnAttr = ReflectionHelper.CollectReturnParameterAttributes(method);
                
                s_marshaller.GenerateUnmarshallingCodeFor(method.ReturnType, returnAttr,
                                                          gen, sourceStream);
                if (method.ReturnType.IsValueType) {
                    gen.Emit(OpCodes.Box, method.ReturnType);
                }
                gen.Emit(OpCodes.Stloc, resultLoc);
            }
            
            // ... then the outargs
            // assign the outargs for this method
            gen.Emit(OpCodes.Ldarg_3);
            gen.Emit(OpCodes.Ldc_I4, parameters.Length);
            gen.Emit(OpCodes.Newarr, ReflectionHelper.ObjectType);
            gen.Emit(OpCodes.Stind_Ref);
            
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
                                                              gen, sourceStream);
                    
                    if (paramInfo.ParameterType.IsValueType) {
                        gen.Emit(OpCodes.Box, paramInfo.ParameterType);
                    }
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
                gen.Emit(OpCodes.Stind_Ref);
            }
            
            gen.Emit(OpCodes.Ldloc, resultLoc); // push result onto the stack
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
               
        private void GenerateArgumentSerialisationsForMethod(ILGenerator serReqArgsIl, 
                                                       Label endLabelSerReqArgs,
                                                       ILGenerator deserReqArgsIl,
                                                       Label endLabelDeserReqArgs,
                                                       ILGenerator serRespArgsIl,
                                                       Label endLabelSerRespArgs,
                                                       ILGenerator deserRespArgsIl,
                                                       Label endLabelDeserRespArgs,
                                                       MethodInfo method) {
    
                // the label after the code for the given method
                Label nextBranchLabelSerReqArgs = serReqArgsIl.DefineLabel();
                Label nextBranchLabelDeserReqArgs = deserReqArgsIl.DefineLabel();
                Label nextBranchLabelSerRespArgs = serRespArgsIl.DefineLabel();
                Label nextBranchLabelDeserRespArgs = deserRespArgsIl.DefineLabel();                                    

    
                string mappedName = DetermineMethodName(method);
                GenerateMethodNameCompare(serReqArgsIl, mappedName, nextBranchLabelSerReqArgs);
                GenerateMethodNameCompare(deserReqArgsIl, mappedName, nextBranchLabelDeserReqArgs);
                GenerateMethodNameCompare(serRespArgsIl, mappedName, nextBranchLabelSerRespArgs);
                GenerateMethodNameCompare(deserRespArgsIl, mappedName, nextBranchLabelDeserRespArgs);
                
                // the real serialisation code, if the method name is equal to the checked name
                GenerateSerializeRequestArgsForMethod(serReqArgsIl, method);                                
                GenerateDeserializeRequestArgsForMethod(deserReqArgsIl, method);
                GenerateSerializeResponseArgsForMethod(serRespArgsIl, method);
                GenerateDeserializeResponseArgsForMethod(deserRespArgsIl, method);
                
                // jump to end after handling for the given method name
                serReqArgsIl.Emit(OpCodes.Br, endLabelSerReqArgs);
                deserReqArgsIl.Emit(OpCodes.Br, endLabelDeserReqArgs);
                serRespArgsIl.Emit(OpCodes.Br, endLabelSerRespArgs);
                deserRespArgsIl.Emit(OpCodes.Br, endLabelDeserRespArgs);
                
                serReqArgsIl.MarkLabel(nextBranchLabelSerReqArgs);
                deserReqArgsIl.MarkLabel(nextBranchLabelDeserReqArgs);
                serRespArgsIl.MarkLabel(nextBranchLabelSerRespArgs);
                deserRespArgsIl.MarkLabel(nextBranchLabelDeserRespArgs);    
        }
        
        private void DefineArgumentsSerialiserMethods(MethodBuilder serReqArgs, 
                                                      MethodBuilder deserReqArgs,
                                                      MethodBuilder serRespArgs,
                                                      MethodBuilder deserRespArgs,
                                                      Type forType) {    
            
            ILGenerator serReqArgsIl = serReqArgs.GetILGenerator();
            ILGenerator deserReqArgsIl = deserReqArgs.GetILGenerator();
            // init contextElements
            deserReqArgsIl.Emit(OpCodes.Ldarg_3);
            deserReqArgsIl.Emit(OpCodes.Ldnull);
            deserReqArgsIl.Emit(OpCodes.Stind_Ref);
            
            ILGenerator serRespArgsIl = serRespArgs.GetILGenerator();
            ILGenerator deserRespArgsIl = deserRespArgs.GetILGenerator();
            // init outArgs
            deserRespArgsIl.Emit(OpCodes.Ldarg_3);
            deserRespArgsIl.Emit(OpCodes.Ldnull);
            deserRespArgsIl.Emit(OpCodes.Stind_Ref);
                            
            Label endLabelSerReqArgs = serReqArgsIl.DefineLabel();
            Label endLabelDeserReqArgs = deserReqArgsIl.DefineLabel();
            Label endLabelSerRespArgs = serRespArgsIl.DefineLabel();
            Label endLabelDeserRespArgs = deserRespArgsIl.DefineLabel();
            
            MethodInfo[] methods =
                forType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < methods.Length; i++) {
                if (methods[i].IsSpecialName) {
                    continue;
                }
                GenerateArgumentSerialisationsForMethod(serReqArgsIl, endLabelSerReqArgs,
                                                        deserReqArgsIl, endLabelDeserReqArgs,
                                                        serRespArgsIl, endLabelSerRespArgs,
                                                        deserRespArgsIl, endLabelDeserRespArgs,
                                                        methods[i]);
            }
            
            PropertyInfo[] properties =
                forType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < properties.Length; i++) {
                MethodInfo getter = properties[i].GetGetMethod();
                MethodInfo setter = properties[i].GetGetMethod();
                if (getter != null) {
                    GenerateArgumentSerialisationsForMethod(serReqArgsIl, endLabelSerReqArgs,
                                                            deserReqArgsIl, endLabelDeserReqArgs,
                                                            serRespArgsIl, endLabelSerRespArgs,
                                                            deserRespArgsIl, endLabelDeserRespArgs,
                                                            getter);
                }
                
                if (setter != null) {
                    GenerateArgumentSerialisationsForMethod(serReqArgsIl, endLabelSerReqArgs,
                                                            deserReqArgsIl, endLabelDeserReqArgs,
                                                            serRespArgsIl, endLabelSerRespArgs,
                                                            deserRespArgsIl, endLabelDeserRespArgs,
                                                            setter);
                }
                
            }
            
            // default cases: generate BAD_OPERATION
            ConstructorInfo badOpCtr =
                typeof(omg.org.CORBA.BAD_OPERATION).GetConstructor(new Type[] { ReflectionHelper.Int32Type, 
                                                                                typeof(omg.org.CORBA.CompletionStatus) });
            serReqArgsIl.Emit(OpCodes.Ldc_I4, 11);
            serReqArgsIl.Emit(OpCodes.Ldc_I4, (int)omg.org.CORBA.CompletionStatus.Completed_No); // TODO
            serReqArgsIl.Emit(OpCodes.Newobj, badOpCtr);
            serReqArgsIl.Emit(OpCodes.Throw);
            
            deserReqArgsIl.Emit(OpCodes.Ldc_I4, 11);
            deserReqArgsIl.Emit(OpCodes.Ldc_I4, (int)omg.org.CORBA.CompletionStatus.Completed_No); // TODO
            deserReqArgsIl.Emit(OpCodes.Newobj, badOpCtr);
            deserReqArgsIl.Emit(OpCodes.Throw);

            serRespArgsIl.Emit(OpCodes.Ldc_I4, 11);
            serRespArgsIl.Emit(OpCodes.Ldc_I4, (int)omg.org.CORBA.CompletionStatus.Completed_MayBe); // TODO
            serRespArgsIl.Emit(OpCodes.Newobj, badOpCtr);
            serRespArgsIl.Emit(OpCodes.Throw);

            deserRespArgsIl.Emit(OpCodes.Ldc_I4, 11);
            deserRespArgsIl.Emit(OpCodes.Ldc_I4, (int)omg.org.CORBA.CompletionStatus.Completed_MayBe); // TODO
            deserRespArgsIl.Emit(OpCodes.Newobj, badOpCtr);                                   
            deserRespArgsIl.Emit(OpCodes.Throw);
            
            // the labels after all tests
            serReqArgsIl.MarkLabel(endLabelSerReqArgs);
            deserReqArgsIl.MarkLabel(endLabelDeserReqArgs);
            serRespArgsIl.MarkLabel(endLabelSerRespArgs);
            deserRespArgsIl.MarkLabel(endLabelDeserRespArgs);            
            
            serReqArgsIl.Emit(OpCodes.Ret);
            deserReqArgsIl.Emit(OpCodes.Ret);
            serRespArgsIl.Emit(OpCodes.Ret);
            deserRespArgsIl.Emit(OpCodes.Ret);            
        }
        
        private Type CreateArgumentsSerialiser(Type forType, string serTypeName) {
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

            DefineArgumentsSerialiserMethods(serReqArgs, deserReqArgs, 
                                             serRespArgs, deserRespArgs, forType);                                                                
    
            Type result = resultBuilder.CreateType();
            return result;
        }        
        
/*        internal Type CreateInstanceSerialiser(Type forType, string serTypeName) {
            
        }
*/
                                                
        
        #endregion IMethods

    }
    
}
