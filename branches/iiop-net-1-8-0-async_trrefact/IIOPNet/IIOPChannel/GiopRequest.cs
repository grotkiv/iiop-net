/* GiopRequest.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 29.03.05  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using Ch.Elca.Iiop.Idl;
using Ch.Elca.Iiop.Util;
using omg.org.CORBA;

namespace Ch.Elca.Iiop.MessageHandling {
          
     
    /// <summary>
    /// gives access to corba relevant parts of a .NET message for the client side request processing
    /// </summary>
    internal class GiopClientRequest {
        
        #region IFields
        
        private IMethodCallMessage m_requestMessage;
        private IMessage m_replyMessage;
        
        #endregion IFields
        #region IConstructors
         
        internal GiopClientRequest(IMethodCallMessage requestMsg) {
            m_requestMessage = requestMsg;
        }
        
        #endregion IConstructors
        #region IProperties
        
        /// <summary>
        /// is this request a one way call.
        /// </summary>
        internal bool IsOneWayCall {
            get{
                return RemotingServices.IsOneWay(m_requestMessage.MethodBase);
            }
        }        

        /// <summary>
        /// the request id
        /// </summary>
        internal uint RequestId {
            get {
                return (uint)m_requestMessage.Properties[SimpleGiopMsg.REQUEST_ID_KEY];
            }
            set {
                m_requestMessage.Properties[SimpleGiopMsg.REQUEST_ID_KEY] = value;
            }
        }    
        
        /// <summary>
        /// the name of the target method for this request
        /// </summary>
        internal string RequestMethodName {
            get {
                return IdlNaming.GetRequestMethodName((MethodInfo)m_requestMessage.MethodBase,
                                                      RemotingServices.IsMethodOverloaded(m_requestMessage));
            }
        }
        
        /// <summary>
        /// the request arguments
        /// </summary>
        internal object[] RequestArguments {
            get {
                return m_requestMessage.Args;
            }
        }
        
        /// <summary>
        /// the target uri for this request
        /// </summary>
        internal string Uri {
            get {
                return m_requestMessage.Uri;
            }
        }
        
        /// <summary>
        /// the MethodInfo of the request target method
        /// </summary>
        internal MethodInfo MethodToCall {
            get {
                return (MethodInfo)m_requestMessage.MethodBase;
            }
        }
        
        /// <summary>
        /// the request call context
        /// </summary>        
        internal LogicalCallContext RequestCallContext {
            get {
                return m_requestMessage.LogicalCallContext;
            }
        }
        
        /// <summary>the .NET remoting request</summary>
        internal IMethodCallMessage Request {
            get {
                return m_requestMessage;
            }
        }
        
        /// <summary>
        /// the reply for this request; set this after deserialisation.
        /// </summary>
        internal IMessage Reply {
            get {
                return m_replyMessage;
            }
            set {
                m_replyMessage = value;
            }
        }                        
                
        #endregion IProperties
         
    }
    
    
    /// <summary>
    /// gives access to corba relevant parts of a .NET message for the sever side request processing
    /// </summary>    
    internal class GiopServerRequest {
        
        #region IFields
        
        private IMessage m_requestMessage;
        /// <summary>
        /// the request as IMethodCallMessage; Is null on the request path
        /// </summary>
        private IMethodCallMessage m_requestCallMessage;
        private ReturnMessage m_replyMessage;
        
        #endregion IFields
        #region IConstructors
    
        internal GiopServerRequest() {
            m_requestMessage = new SimpleGiopMsg();
            m_requestCallMessage = null; // not yet created; will be created from requestMessage later.
            m_replyMessage = null; // not yet available
        }
        
        /// <summary>
        /// constructor for the out-direction
        /// </summary>
        /// <param name="request">the request message, may be null</param>
        /// <param name="reply">the reply message</param>
        internal GiopServerRequest(IMethodCallMessage request, ReturnMessage reply) {
            if (request != null) {
                m_requestMessage = request;
                m_requestCallMessage = request;
            } else {
                m_requestMessage = new SimpleGiopMsg();
            }
            m_replyMessage = reply;
        }
        
        #endregion IConstructors
        #region IProperties
        
        /// <summary>
        /// the request id
        /// </summary>
        internal uint RequestId {
            get {
                if (m_requestMessage.Properties[SimpleGiopMsg.REQUEST_ID_KEY] != null) {
                    return (uint)m_requestMessage.Properties[SimpleGiopMsg.REQUEST_ID_KEY];
                } else {
                    throw new BAD_INV_ORDER(200, CompletionStatus.Completed_MayBe);
                }
            }
            set {
                if (m_requestCallMessage == null) {
                    m_requestMessage.Properties[SimpleGiopMsg.REQUEST_ID_KEY] = value;
                } else {
                    throw new BAD_OPERATION(200, CompletionStatus.Completed_MayBe);
                }
            }
        }        
                
        /// <summary>
        /// the giop version this message is encoded with
        /// </summary>
        internal GiopVersion Version {
            get {
                if (m_requestMessage.Properties[SimpleGiopMsg.GIOP_VERSION_KEY] != null) {
                    return (GiopVersion)m_requestMessage.Properties[SimpleGiopMsg.GIOP_VERSION_KEY];
                } else {
                    throw new BAD_INV_ORDER(201, CompletionStatus.Completed_MayBe);
                }
            }
            set {
                if (m_requestCallMessage == null) {
                    m_requestMessage.Properties[SimpleGiopMsg.GIOP_VERSION_KEY] = value;
                } else {
                    throw new BAD_OPERATION(201, CompletionStatus.Completed_MayBe);
                }
            }
        }
        
        /// <summary>
        /// the response flags for this request
        /// </summary>
        internal byte ResponseFlags {
            get {
                if (m_requestMessage.Properties[SimpleGiopMsg.RESPONSE_FLAGS_KEY] != null) {
                    return (byte)m_requestMessage.Properties[SimpleGiopMsg.RESPONSE_FLAGS_KEY];
                } else {
                    throw new BAD_INV_ORDER(202, CompletionStatus.Completed_MayBe);
                }                
            }
            set {
                if (m_requestCallMessage == null) {
                    m_requestMessage.Properties[SimpleGiopMsg.RESPONSE_FLAGS_KEY] = value;
                } else {
                    throw new BAD_OPERATION(202, CompletionStatus.Completed_MayBe);
                }
            }
        }
        
        /// <summary>
        /// the idl name of the reqeusted method
        /// </summary>
        internal string RequestMethodName {
            get {
                if (m_requestMessage.Properties[SimpleGiopMsg.IDL_METHOD_NAME_KEY] != null) {
                    return (string)m_requestMessage.Properties[SimpleGiopMsg.IDL_METHOD_NAME_KEY];
                } else {
                    throw new BAD_INV_ORDER(203, CompletionStatus.Completed_MayBe);
                }                                                
            }
            set {
                if (m_requestCallMessage == null) {
                    m_requestMessage.Properties[SimpleGiopMsg.IDL_METHOD_NAME_KEY] = value;
                } else {
                    throw new BAD_OPERATION(203, CompletionStatus.Completed_MayBe);
                }
            }
        }
        
        internal string RequestUri {
            get {
                if (m_requestMessage.Properties[SimpleGiopMsg.REQUESTED_URI_KEY] != null) {
                    return (string)m_requestMessage.Properties[SimpleGiopMsg.REQUESTED_URI_KEY];
                } else {
                    throw new BAD_INV_ORDER(204, CompletionStatus.Completed_MayBe);
                }                
            }
            set {
                if (m_requestCallMessage == null) {
                    m_requestMessage.Properties[SimpleGiopMsg.REQUESTED_URI_KEY] = value;                
                } else {
                    throw new BAD_OPERATION(204, CompletionStatus.Completed_MayBe);
                }
            }
        }
        
        /// <summary>
        /// the object uri of the published .net object, which should handle this request.
        /// </summary>
        internal string CalledUri {
            get {
                if (m_requestCallMessage == null) {
                    if (m_requestMessage.Properties[SimpleGiopMsg.URI_KEY] != null) {
                        return (string)m_requestMessage.Properties[SimpleGiopMsg.URI_KEY];
                    } else {
                        throw new BAD_INV_ORDER(205, CompletionStatus.Completed_MayBe);
                    }                                
                } else {
                    return m_requestCallMessage.Uri;
                }
            }
            set {                
                if (m_requestCallMessage == null) {
                    m_requestMessage.Properties[SimpleGiopMsg.URI_KEY] = value;
                } else {
                    throw new BAD_OPERATION(205, CompletionStatus.Completed_MayBe);
                }
            }
        }
        
        /// <summary>
        /// the signature of the called .net method
        /// </summary>
        private Type[] CalledMethodSignature {
            get {
                if (m_requestCallMessage == null) {
                    if (m_requestMessage.Properties[SimpleGiopMsg.METHOD_SIG_KEY] != null) {
                        return (Type[])m_requestMessage.Properties[SimpleGiopMsg.METHOD_SIG_KEY];
                    } else {
                        throw new BAD_INV_ORDER(206, CompletionStatus.Completed_MayBe);
                    }                                                                                
                } else {
                    return (Type[])m_requestCallMessage.MethodSignature;
                }
            }
            set {
                if (m_requestCallMessage == null) {
                    m_requestMessage.Properties[SimpleGiopMsg.METHOD_SIG_KEY] = value;
                } else {
                    throw new BAD_OPERATION(206, CompletionStatus.Completed_MayBe);
                }
            }
        }
        
        /// <summary>the name of the called .net method; can be different from RequestMethodName.</summary>
        internal string CalledMethodName {
            get {
                if (m_requestCallMessage == null) {
                    if (m_requestMessage.Properties[SimpleGiopMsg.METHODNAME_KEY] != null) {
                        return (string)m_requestMessage.Properties[SimpleGiopMsg.METHODNAME_KEY];
                    } else {
                        throw new BAD_INV_ORDER(207, CompletionStatus.Completed_MayBe);
                    }                
                } else {
                    return m_requestCallMessage.MethodName;
                }
            }
            set {
                if (m_requestCallMessage == null) {
                    m_requestMessage.Properties[SimpleGiopMsg.METHODNAME_KEY] = value;                
                } else {
                    throw new BAD_OPERATION(207, CompletionStatus.Completed_MayBe);
                }
            }
        }
        
        /// <summary>the info of the called .net method.</summary>
        internal MethodInfo CalledMethod {
            get {                
                if (m_requestMessage.Properties[SimpleGiopMsg.CALLED_METHOD_KEY] != null) {
                    return (MethodInfo)m_requestMessage.Properties[SimpleGiopMsg.CALLED_METHOD_KEY];
                } else {
                    throw new BAD_INV_ORDER(208, CompletionStatus.Completed_MayBe);
                }                
            }
            set {
                if (m_requestCallMessage == null) {
                    m_requestMessage.Properties[SimpleGiopMsg.CALLED_METHOD_KEY] = value;
                } else {
                    throw new BAD_OPERATION(208, CompletionStatus.Completed_MayBe);
                }
            }
        }        
        
        /// <summary>
        /// is one of the standard corba operaton like is_a called, or a regular operation.
        /// </summary>
        internal bool IsStandardCorbaOperation {
            get {
                if (m_requestMessage.Properties[SimpleGiopMsg.IS_STANDARD_CORBA_OP_KEY] != null) {
                    return (bool)m_requestMessage.Properties[SimpleGiopMsg.IS_STANDARD_CORBA_OP_KEY];
                } else {
                    throw new BAD_INV_ORDER(209, CompletionStatus.Completed_MayBe);
                }                                
            }
            set {
                if (m_requestCallMessage == null) {
                    m_requestMessage.Properties[SimpleGiopMsg.IS_STANDARD_CORBA_OP_KEY] = value;
                } else {
                    throw new BAD_OPERATION(209, CompletionStatus.Completed_MayBe);
                }
            }            
        }
        
        /// <summary>
        /// the full name of the type of .NET object, servicing this request.
        /// </summary>
        internal string ServerType {
            get {
                if (m_requestCallMessage == null) {
                    if (m_requestMessage.Properties[SimpleGiopMsg.TYPENAME_KEY] != null) {
                        return (string)m_requestMessage.Properties[SimpleGiopMsg.TYPENAME_KEY];
                    } else {
                        throw new BAD_INV_ORDER(210, CompletionStatus.Completed_MayBe);
                    }
                } else {
                    return m_requestCallMessage.TypeName;
                }
            }
            set {
                if (m_requestCallMessage == null) {
                    m_requestMessage.Properties[SimpleGiopMsg.TYPENAME_KEY] = value;
                } else {
                    throw new BAD_OPERATION(210, CompletionStatus.Completed_MayBe);
                }
            }
        }
        
        /// <summary>
        /// the request arguments
        /// </summary>
        internal object[] RequestArgs {
            get {
                if (m_requestCallMessage == null) {
                    if (m_requestMessage.Properties[SimpleGiopMsg.ARGS_KEY] != null) {
                        return (object[])m_requestMessage.Properties[SimpleGiopMsg.ARGS_KEY];
                    } else {
                        throw new BAD_INV_ORDER(211, CompletionStatus.Completed_MayBe);
                    }                                
                } else {
                    return m_requestCallMessage.Args;
                }
            }
            set {
                if (m_requestCallMessage == null) {
                    m_requestMessage.Properties[SimpleGiopMsg.ARGS_KEY] = value;
                } else {
                    throw new BAD_OPERATION(211, CompletionStatus.Completed_MayBe);
                }
            }
        }
        
        /// <summary>
        /// the reply out arguments
        /// </summary>
        internal object[] OutArgs {
            get {
                if (m_replyMessage != null) {
                    return m_replyMessage.OutArgs;
                } else {
                    throw new BAD_INV_ORDER(212, CompletionStatus.Completed_MayBe);
                }
            }
        }
        
        /// <summary>
        /// the reply value
        /// </summary>
        internal object ReturnValue {
            get {
                if (m_replyMessage != null) {
                    return m_replyMessage.ReturnValue;
                } else {
                    throw new BAD_INV_ORDER(213, CompletionStatus.Completed_MayBe);
                }                
            }
        }
        
        /// <summary>
        /// is the reply an exception or not
        /// </summary>
        internal bool IsExceptionReply {
            get {
                if (m_replyMessage != null) {
                    return m_replyMessage.Exception != null;
                } else {
                    throw new BAD_INV_ORDER(214, CompletionStatus.Completed_MayBe);
                }
            }
        }
        
        /// <summary>
        /// the exception thrown by the invocation or null, if no exception encountered.
        /// </summary>
        internal Exception Exception {
            get {
                if (m_replyMessage != null) {
                    return m_replyMessage.Exception;
                } else {
                    throw new BAD_INV_ORDER(215, CompletionStatus.Completed_MayBe);
                }                
            }
        }
        
        /// <summary>
        /// the exception to pass to the client or null, if no exception encountered.
        /// </summary>
        internal Exception IdlException {
            get {
                Exception ex = Exception;
                if (ex != null) {
                    return DetermineExceptionToThrow(ex, CalledMethod);
                } else {
                    return null;
                }
            }
        }
        
        /// <summary>the .NET remoting request</summary>
        internal IMessage Request {
            get {
                return m_requestMessage;
            }
        }    
        
        /// <summary>the .NET remoting reply</summary>
        internal ReturnMessage Reply {
            get {
                return m_replyMessage;
            }
        }
        
        #endregion IProperties
        #region IMethods
        
        /// <summary>
        /// aquire the information needed to call a standard corba operation, which is possible for every object
        /// </summary>
        /// <param name="methodName">the name of the method called</param>
        /// <returns>the method-info of the method which describes signature for deserialisation</returns>
        private MethodInfo DecodeStandardOperation(string methodName) {
            Type serverType = StandardCorbaOps.s_type; // generic handler
            MethodInfo calledMethodInfo = serverType.GetMethod(methodName); // for parameter unmarshalling, use info of the signature method
            if (calledMethodInfo == null) { 
                // unexpected exception: can't load method of type StandardCorbaOps
                throw new INTERNAL(2801, CompletionStatus.Completed_MayBe);
            }
            return calledMethodInfo;
        }

        /// <summary>aquire the information for a specific object method call</summary>
        /// <param name="serverType">the type of the object called</param>
        /// <param name="calledMethodInfo">the MethodInfo of the method, which is called</param>
        /// <returns>returns the mapped methodName of the operation to call of this object specific method</returns>
        private MethodInfo DecodeObjectOperation(string methodName, Type serverType) {
            // method name mapping
            string resultMethodName;
            if (ReflectionHelper.IIdlEntityType.IsAssignableFrom(serverType)) {
                resultMethodName = methodName;
                // an interface mapped to from Idl is implemented by server ->
                // compensate 3.2.3.1: removal of _ for names, which clashes with CLS id's
                if (IdlNaming.NameClashesWithClsKeyWord(methodName)) {
                    resultMethodName = "_" + methodName;
                } else if (methodName.StartsWith("_get_")) {
                    // handle properties correctly
                    PropertyInfo prop = serverType.GetProperty(methodName.Substring(5), BindingFlags.Instance | BindingFlags.Public);
                    if (prop != null) {
                        resultMethodName = prop.GetGetMethod().Name;
                    }
                } else if (methodName.StartsWith("_set_")) {
                    // handle properties correctly
                    PropertyInfo prop = serverType.GetProperty(methodName.Substring(5), BindingFlags.Instance | BindingFlags.Public);
                    if (prop != null) {
                        resultMethodName = prop.GetSetMethod().Name;
                    }
                }
            } else {                
                resultMethodName = IdlNaming.ReverseClsToIdlNameMapping(methodName);
                if (resultMethodName.StartsWith("get_") || resultMethodName.StartsWith("set_")) {
                    // special handling for properties, because properties with a name, which is transformed on mapping,
                    // need to be specially identified, because porperty name is included in method name.
                    string propName = IdlNaming.ReverseClsToIdlNameMapping(resultMethodName.Substring(4));
                    PropertyInfo prop = serverType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null) {
                        if (resultMethodName.StartsWith("get_")) {
                            resultMethodName = prop.GetGetMethod().Name;
                        } else {
                            resultMethodName = prop.GetSetMethod().Name;
                        }
                    }
                }

            }
            
            MethodInfo calledMethodInfo = serverType.GetMethod(resultMethodName);
            if (calledMethodInfo == null) { 
                // possibly an overloaded method!
                calledMethodInfo = IdlNaming.FindClsMethodForOverloadedMethodIdlName(methodName, serverType);
                if (calledMethodInfo == null) { // not found -> BAD_OPERATION
                    throw new BAD_OPERATION(0, CompletionStatus.Completed_No); 
                }
            }
            return calledMethodInfo;
        }                
                
        /// <summary>generate the signature info for the method</summary>
        private Type[] GenerateSigForMethod(MethodInfo method) {
            ParameterInfo[] parameters = method.GetParameters();
            Type[] result = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) {                             
                result[i] = parameters[i].ParameterType;
            }
            return result;
        }        
        
        /// <summary>
        /// resolve the call to a .net method according to the properties already set.
        /// As a result, update the request message.
        /// </summary>
        internal void ResolveCall() {
            if (m_requestCallMessage != null) { // is fixed
                throw new BAD_OPERATION(300, CompletionStatus.Completed_MayBe);
            }
            
            MethodInfo callForMethod;
            Type serverType;
            bool regularOp;            
            string calledUri = RequestUri;
                                                
            string internalMethodName; // the implementation method name
            if (!StandardCorbaOps.CheckIfStandardOp(RequestMethodName)) {
                regularOp = true; // non-pseude op
                serverType = RemotingServices.GetServerTypeForUri(calledUri);
                if (serverType == null) {
                    throw new OBJECT_NOT_EXIST(0, CompletionStatus.Completed_No); 
                }
                // handle object specific-ops
                callForMethod = DecodeObjectOperation(RequestMethodName, serverType);
                internalMethodName = callForMethod.Name;
                // to handle overloads correctly, add signature info:
                CalledMethodSignature = GenerateSigForMethod(callForMethod);                
            } else {
                regularOp = false; // pseude-object op
                // handle standard corba-ops like _is_a
                callForMethod = DecodeStandardOperation(RequestMethodName);
                MethodInfo internalCall = 
                    StandardCorbaOps.GetMethodToCallForStandardMethod(callForMethod.Name);
                if (internalCall == null) {
                    throw new INTERNAL(2802, CompletionStatus.Completed_MayBe);    
                }
                internalMethodName = internalCall.Name;
                calledUri = StandardCorbaOps.WELLKNOWN_URI; // change object-uri
                serverType = StandardCorbaOps.s_type;
            }            
            ServerType = serverType.FullName;
            CalledUri = calledUri;
            CalledMethodName = internalMethodName;            
            IsStandardCorbaOperation = !regularOp;
            CalledMethod = callForMethod;
        }
                
        /// <summary>
        /// for methods mapped from idl, check if exception is allowed to throw
        /// according to throws clause and if not creae a unknown exception instead.
        /// </summary>
        private Exception DetermineIdlExceptionToThrow(Exception thrown, MethodInfo thrower) {
            // for idl interfaces, check if thrown exception is in the raises clause;
            // if not, throw an unknown system exception
            if (ReflectionHelper.IsExceptionInRaiseAttributes(thrown, thrower) && (thrown is AbstractUserException)) {
                return thrown;
            } else {
                return new UNKNOWN(189, CompletionStatus.Completed_Yes); // if not in raises clause
            }
        }
        
        /// <summary>
        /// determines, which exception to return to the client based on
        /// the called method/attribute and on the Exception thrown.
        /// Make sure to return only exceptions, which are allowed for the thrower; e.g.
        /// only those specified in the interface for methods and for attributes only system exceptions.
        /// </summary>
        private Exception DetermineExceptionToThrow(Exception thrown, MethodBase thrower) {
            if (thrown is omg.org.CORBA.AbstractCORBASystemException) {
                return thrown; // system exceptions are not wrapped or transformed
            }
            Exception exceptionToThrow;
            if ((thrower is MethodInfo) && (!((MethodInfo)thrower).IsSpecialName)) { // is a normal method (i.e. no property accessor, ...)
                if (ReflectionHelper.IIdlEntityType.IsAssignableFrom(thrower.DeclaringType)) { 
                    exceptionToThrow = DetermineIdlExceptionToThrow(thrown,
                                                                    (MethodInfo)thrower);
                } else {
                    if (ReflectionHelper.IsExceptionInRaiseAttributes(thrown, (MethodInfo)thrower) &&
                        (thrown is AbstractUserException)) {
                        exceptionToThrow = thrown; // a .NET method could also use ThrowsIdlException attribute to return non-wrapped exceptions
                    } else {
                        // wrap into generic user exception, because CLS to IDL gen adds this exception to
                        // all methods
                        exceptionToThrow = new GenericUserException(thrown);
                    }
                }
            } else if ((thrower is MethodInfo) && (((MethodInfo)thrower).IsSpecialName)) { // is a special method (i.e. a property accessor, ...) 
                exceptionToThrow = new UNKNOWN(190, CompletionStatus.Completed_Yes);
            } else {
                // thrower == null means here, that the target method was not determined,
                // i.e. the request deserialisation was not ok
                Debug.WriteLine("target method unknown, can't determine what exception client accepts; thrown was: " + 
                                thrown);
                exceptionToThrow = new UNKNOWN(201, CompletionStatus.Completed_No);
            }
            return exceptionToThrow;
        }        
        
        #endregion IMethods
        
    }
     
}
