/* ParameterMarshaller.cs
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
using System.Reflection;
using System.Collections;
using Ch.Elca.Iiop.Cdr;
using Ch.Elca.Iiop.Util;

namespace Ch.Elca.Iiop.Marshalling {
    
    
    /// <summary>
    /// Marshalles and Unmarshalles method parameters
    /// </summary>
    [CLSCompliant(false)]
    public class ParameterMarshaller {

        #region SFields

        private static ParameterMarshaller s_singletonMarshaller = new ParameterMarshaller();

        #endregion SFields
        #region IConstructors
        
        private ParameterMarshaller() {
        }

        #endregion IConstructors
        #region SMethods

        public static ParameterMarshaller GetSingleton() {
            return s_singletonMarshaller;
        }

        #endregion SMethods
        #region IMethods        

        /// <summary>
        /// marshals an intem
        /// </summary>
        /// <param name="type"></param>
        /// <param name="attributes"></param>
        /// <param name="actual">the, which should be marshalled</param>
        /// <param name="targetStream"></param>
        private void Marshal(Type type, AttributeExtCollection attributes, object actual,
                             CdrOutputStream targetStream) {
            Marshaller marshal = Marshaller.GetSingleton();
            marshal.Marshal(type, attributes, actual, targetStream);
        }

        /// <summary>
        /// unmarshals an item
        /// </summary>
        /// <param name="type"></param>
        /// <param name="attributes"></param>
        /// <param name="sourceStream"></param>
        /// <returns></returns>
        private object Unmarshal(Type type, AttributeExtCollection attributes, 
                                 CdrInputStream sourceStream) {
            Marshaller marshal = Marshaller.GetSingleton();   
            object unmarshalled = marshal.Unmarshal(type, attributes, sourceStream);
            return unmarshalled;
        }


        public static bool IsOutParam(ParameterInfo paramInfo) {
            return paramInfo.IsOut;
        }

        public static bool IsRefParam(ParameterInfo paramInfo) {
            if (!paramInfo.ParameterType.IsByRef) { return false; }
            return (!paramInfo.IsOut) || (paramInfo.IsOut && paramInfo.IsIn);
        }

        public static bool IsInParam(ParameterInfo paramInfo) {
            if (paramInfo.ParameterType.IsByRef) { return false; } // only out/ref params have a byRef type
            return ((paramInfo.IsIn) || 
                    ((!(paramInfo.IsOut)) && (!(paramInfo.IsRetval))));
        }

        /// <summary>
        /// serialises the parameters while sending a request.
        /// </summary>
        /// <param name="method">the information about the method the request is targeted for</param>
        /// <param name="actual">the values, which should be marshalled for the parameters</param>
        public void SerialiseRequestArgs(MethodInfo method, object[] actual, CdrOutputStream targetStream) {
            ParameterInfo[] parameters = method.GetParameters();
            
            for (int actualParamNr = 0; actualParamNr < parameters.Length; actualParamNr++) {
                ParameterInfo paramInfo = parameters[actualParamNr];
                // iterate through the parameters, nonOut and nonRetval params are serialised for a request
                if (IsInParam(paramInfo) || IsRefParam(paramInfo)) {
                    AttributeExtCollection paramAttrs = ReflectionHelper.CollectParameterAttributes(paramInfo, 
                                                                                                    method);                    
                    Marshal(paramInfo.ParameterType, paramAttrs, 
                            actual[actualParamNr], targetStream);                    
                }
                // move to next parameter
                // out-args are also part of the actual array -> move to next for those whithout doing something
            }
        }

        /// <summary>
        /// deserialises the parameters while receiving a request.
        /// </summary>
        /// <param name="method">the information about the method the request is targeted for</param>
        /// <returns>
        /// an array of all deserialised arguments
        /// </returns>
        public object[] DeserialiseRequestArgs(MethodInfo method, CdrInputStream sourceStream) {
            ParameterInfo[] parameters = method.GetParameters();


            object[] result = new object[parameters.Length];
            for (int actualParamNr = 0; actualParamNr < parameters.Length; actualParamNr++) {
                ParameterInfo paramInfo = parameters[actualParamNr];
                if (IsInParam(paramInfo) || IsRefParam(paramInfo)) {
                    AttributeExtCollection paramAttrs = ReflectionHelper.CollectParameterAttributes(paramInfo, 
                                                                                                    method);
                    result[actualParamNr] = Unmarshal(paramInfo.ParameterType, paramAttrs,
                                                         sourceStream);
                } // else: null for an out parameter
            }
            return result;
        }

        /// <summary>
        /// checks, if the method has request args, i.e. in or ref arguments
        /// </summary>
        /// <param name="method">the method to check</param>
        /// <returns>returns true, if such arguments are found, otherwise false</returns>
        public bool HasRequestArgs(MethodInfo method) {
            ParameterInfo[] parameters = method.GetParameters();
            foreach (ParameterInfo paramInfo in parameters) {    
                if (IsInParam(paramInfo) || IsRefParam(paramInfo)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// serialises the paramters while sending a response.
        /// </summary>
        /// <param name="method">the information about the the method this response is from</param>
        /// <param name="retValue">the return value of the method call</param>
        public void SerialiseResponseArgs(MethodInfo method, object retValue, object[] outArgs,
                                          CdrOutputStream targetStream) {
            ParameterInfo[] parameters = method.GetParameters();
            // first serialise the return value, 
            if (!method.ReturnType.Equals(ReflectionHelper.VoidType)) {
                AttributeExtCollection returnAttr = ReflectionHelper.CollectReturnParameterAttributes(method);
                Marshal(method.ReturnType, returnAttr, retValue, targetStream);
            }
            // ... then the out/ref args
            int outParamNr = 0;
            for (int actualParamNr = 0; actualParamNr < parameters.Length; actualParamNr++) {
                ParameterInfo paramInfo = parameters[actualParamNr];
                // iterate through the parameters, out/ref parameters are serialised
                if (IsOutParam(paramInfo) || IsRefParam(paramInfo)) {
                    AttributeExtCollection paramAttrs = ReflectionHelper.CollectParameterAttributes(paramInfo, 
                                                                                                    method);
                    Marshal(paramInfo.ParameterType, paramAttrs, outArgs[outParamNr], targetStream);
                    outParamNr++;
                }
            }
        }        
        
        /// <summary>
        /// Checks, if the method has response args
        /// </summary>
        /// <param name="method">the method to check</param>
        /// <returns>true, if response args are present, otherwise returns false</returns>
        public bool HasResponseArgs(MethodInfo method) {
            if (!method.ReturnType.Equals(ReflectionHelper.VoidType)) {
                return true;
            }
            
            ParameterInfo[] parameters = method.GetParameters();
            foreach (ParameterInfo paramInfo in parameters) {
                if (IsOutParam(paramInfo) || IsRefParam(paramInfo)) {
                    return true; // an out or ref parameter found
                }
            }
            
            return false;            
        }
        
        
        /// <summary>
        /// deserialises the parameters while receiving a response.
        /// </summary>
        /// <param name="method">the information about the the method this response is from</param>
        /// <param name="outArgs">the out-arguments deserialiesed here</param>
        /// <returns>the return value of the method-call</returns>
        public object DeserialiseResponseArgs(MethodInfo method, CdrInputStream sourceStream,
                                              out object[] outArgs) {
            ParameterInfo[] parameters = method.GetParameters();
            // demarshal first the return value, 
            object retValue = null;
            if (!method.ReturnType.Equals(ReflectionHelper.VoidType)) {
                AttributeExtCollection returnAttr = ReflectionHelper.CollectReturnParameterAttributes(method);
                retValue = Unmarshal(method.ReturnType, returnAttr, sourceStream);
            }
            
            // ... then the outargs
            outArgs = new object[parameters.Length];            
            bool outArgFound = false;
            for (int actualParamNr = 0; actualParamNr < parameters.Length; actualParamNr++) {                    
                ParameterInfo paramInfo = parameters[actualParamNr];
                if (IsOutParam(paramInfo) || IsRefParam(paramInfo)) {
                    AttributeExtCollection paramAttrs = ReflectionHelper.CollectParameterAttributes(paramInfo, 
                                                                                                    method);
                    outArgs[actualParamNr] = Unmarshal(paramInfo.ParameterType, paramAttrs, 
                                                       sourceStream);                    
                    outArgFound = true;
                } // else: for an in param null must be added to out-args
            }

            // prepare the result
            // need to return empty array, if no out-arg is present, because otherwise async calls fail
            if (!outArgFound) {
                outArgs = new object[0]; 
            }
            return retValue;
        }

        #endregion IMethods

    }
}


#if UnitTest

namespace Ch.Elca.Iiop.Tests {
    
    using System.IO;
    using NUnit.Framework;
    using Ch.Elca.Iiop;
    using Ch.Elca.Iiop.Idl;
    using Ch.Elca.Iiop.Util;
    using Ch.Elca.Iiop.Marshalling;
    using Ch.Elca.Iiop.Cdr;
    using omg.org.CORBA;
    

    public class ParameterMarshallerTestRemote : MarshalByRefObject {
        
        public int TestSomeInts(int a1, int a2, int a3, int a4, int a5) {
            return a1 + a2 + a3 + a4 + a5; // unimportant for test
        }
        
    }

    /// <summary>
    /// Unit-tests for testing request/reply serialisation/deserialisation
    /// </summary>
    public class ParameterMarshallerTest : TestCase {
        
        private MethodInfo GetTestSomeIntMethod() {
            Type parameterMarshallerTestRemoteType = typeof(ParameterMarshallerTestRemote);
            return parameterMarshallerTestRemoteType.GetMethod("TestSomeInts", BindingFlags.Instance | BindingFlags.Public);
        }
        
        private void CheckArrayEqual(object[] a1, object[] a2) {
            Assertion.AssertEquals(a1.Length, a2.Length);
            for (int i = 0; i < a1.Length; i++) {
                Assertion.AssertEquals(a1[i], a2[i]);
            }            
        }
        
        
        public void TestRequestArguments() {
            MethodInfo testMethod = GetTestSomeIntMethod();
            
            object[] actual = new object[] { 1 , 2, 3, 4, 5 };
            for (int j = 0; j < 10; j++) { // test more than one call
                object[] deser = MarshalAndUnmarshalRequestArgsOnce(testMethod, actual);
                CheckArrayEqual(actual, deser);
            }
        }
        
        private object[] MarshalAndUnmarshalRequestArgsOnce(MethodInfo testMethod, object[] actual) {
            ParameterMarshaller marshaller = ParameterMarshaller.GetSingleton();
            
            MemoryStream data = new MemoryStream();
            GiopVersion version = new GiopVersion(1, 2);
            byte endian = 0;
            CdrOutputStream targetStream = new CdrOutputStreamImpl(data, endian, version);            
            marshaller.SerialiseRequestArgs(testMethod, actual, targetStream);
            
            data.Seek(0, SeekOrigin.Begin);
            CdrInputStreamImpl sourceStream = new CdrInputStreamImpl(data);
            sourceStream.ConfigStream(endian, version);
            object[] deser = marshaller.DeserialiseRequestArgs(testMethod, sourceStream);
            return deser;
        }
        
        public void TestReplyArguments() {                       
            MethodInfo testMethod = GetTestSomeIntMethod();
            
            object returnValue = 9876;
            object[] outArgs = new object[0];
            for (int j = 0; j < 10; j++) { // check more than one call
                object[] deserOut;
                object deser = MarshalAndUnmarshalResponeArgsOnce(testMethod, returnValue, outArgs, 
                                                                  out deserOut);
                Assertion.AssertEquals(returnValue, deser);
                CheckArrayEqual(outArgs, deserOut);
            }
            
        }
        
        private object MarshalAndUnmarshalResponeArgsOnce(MethodInfo testMethod, object returnValue,
                                                          object[] outArgs, out object[] deserOutArgs) {
            ParameterMarshaller marshaller = ParameterMarshaller.GetSingleton();
            
            MemoryStream data = new MemoryStream();
            GiopVersion version = new GiopVersion(1, 2);
            byte endian = 0;
            CdrOutputStream targetStream = new CdrOutputStreamImpl(data, endian, version);
            marshaller.SerialiseResponseArgs(testMethod, returnValue, outArgs, targetStream);
            
            data.Seek(0, SeekOrigin.Begin);            
            CdrInputStreamImpl sourceStream = new CdrInputStreamImpl(data);
            sourceStream.ConfigStream(endian, version);            
            object returnValueDeser = marshaller.DeserialiseResponseArgs(testMethod, sourceStream, 
                                                                         out deserOutArgs);
            return returnValueDeser;
        }
        
        
    }
    
}

#endif