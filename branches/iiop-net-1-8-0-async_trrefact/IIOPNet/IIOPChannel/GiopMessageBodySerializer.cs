/* IIOPMessageBodySerializer.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 17.01.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using Ch.Elca.Iiop.Cdr;
using Ch.Elca.Iiop.Marshalling;
using Ch.Elca.Iiop.Services;
using Ch.Elca.Iiop.Idl;
using Ch.Elca.Iiop.Util;
using Ch.Elca.Iiop.CorbaObjRef;
using omg.org.CORBA;

namespace Ch.Elca.Iiop.MessageHandling {

    
    /// <summary>
    /// used to specify the LocateStatus for a locate reply
    /// </summary>
    public enum LocateStatus {
        UNKNOWN_OBJECT,
        OBJECT_HERE,
        OBJECT_FORWARD
    }
    
    
    /// <summary>
    /// simple implementation of the IMessage interface
    /// </summary>
    public class SimpleGiopMsg : IMessage {
        
        #region Constants

        /// <summary>the key to access the requestId in the message properties</summary>
        public const string REQUEST_ID_KEY = "_request_ID";
        /// <summary>the key to access the giop-version in the message properties</summary>
        public const string GIOP_VERSION_KEY = "_giop_version";
        /// <summary>the key to access the response-flags in the message properties</summary>
        public const string RESPONSE_FLAGS_KEY = "_response_flags";
        /// <summary>the key to access the idl method name in the message properties</summary>
        public const string IDL_METHOD_NAME_KEY = "_idl_method_name";        
        /// <summary>the key to access the flag, specifying if one of the corba standard ops, like is_a is called or a regular object operation, in the message properties</summary>
        public const string IS_STANDARD_CORBA_OP_KEY = "_is_standard_corba_op";           
        /// <summary>the key to access the client side requested uri in the message properties</summary>
        public const string REQUESTED_URI_KEY = "_requested_uri_op";                   
        /// <summary>the key to access the called method info</summary>
        public const string CALLED_METHOD_KEY = "_called_method";        
        /// <summary>the key used to access the uri-property in messages</summary>         
        public const string URI_KEY = "__Uri";
        /// <summary>the key used to access the typename-property in messages</summary>
        public const string TYPENAME_KEY = "__TypeName";
        /// <summary>the key used to access the methodname-property in messages</summary>
        public const string METHODNAME_KEY = "__MethodName";
        /// <summary>the key used to access the argument-property in messages</summary>
        public const string ARGS_KEY = "__Args";
        /// <summary>the key used to access the method-signature property in messages</summary>
        public const string METHOD_SIG_KEY = "__MethodSignature";

        #endregion Constants
        #region IFields
        
        private Hashtable m_properties = new Hashtable();

        #endregion IFields
        #region IProperties
    
        #region Implementation of IMessage
        
        public IDictionary Properties {
            get {
                return m_properties;
            }
        }
        
        #endregion

        #endregion IProperties

    }
    
    /// <summary>contains the information for a location forward reply</summary>
    internal class LocationForwardMessage : IMessage {
        
        #region Constants
        
        internal const string FWD_PROXY_KEY = "__FwdToProxy";
        
        #endregion Constants
        #region IFields        
        
        private IDictionary m_properties = new Hashtable();
        
        #endregion IFields
        #region IConstructors
        
        internal LocationForwardMessage(MarshalByRefObject toProxy) {
            m_properties[FWD_PROXY_KEY] = toProxy;            
        }
        
        #endregion IConstructors
        #region IProperties
        
        public IDictionary Properties {
            get {
                return m_properties;
            }
        }
        
        internal MarshalByRefObject FwdToProxy {
            get {
                return (MarshalByRefObject)m_properties[FWD_PROXY_KEY];
            }
        }
        
        #endregion IProperties
        
    }


    /// <summary>
    /// this exception is thrown, when something does not work during request deserialization.
    /// This exception stores
    /// all the information needed to construct an exception reply.
    /// </summary>
    [Serializable]
    internal class RequestDeserializationException : Exception {
        
        #region IFields
        
        private Exception m_reason;
        private IMessage m_requestMessage;

        #endregion IFields
        #region IConstructors
        
        /// <param name="reason">the reason for deserialization error</param>
        /// <param name="requestMessage">the message decoded so far</param>
        public RequestDeserializationException(Exception reason, IMessage requestMessage) {
            m_reason = reason;
            m_requestMessage = requestMessage;
        }

        #endregion IConstructors
        #region IProperties

        public Exception Reason {
            get { 
                return m_reason; 
            }
        }

        public IMessage RequestMessage {
            get { 
                return m_requestMessage; 
            }
        }

        #endregion IProperties

    }

    
    /// <summary>
    /// This class is reponsible for serialising/deserialising message bodys of Giop Messages for
    /// the different message types
    /// </summary>
    internal class GiopMessageBodySerialiser {

        #region SFields

        private static GiopMessageBodySerialiser s_singleton = new GiopMessageBodySerialiser();               
        
        #endregion SFields
        #region SMethods

        public static GiopMessageBodySerialiser GetSingleton() {
            return s_singleton;
        }

        #endregion SMethods
        #region IFields

        private MarshallerForType m_contextSeqMarshaller;

        #endregion IFields
        #region IConstructors

        private GiopMessageBodySerialiser() {            
            m_contextSeqMarshaller = new MarshallerForType(typeof(string[]), 
                                        new AttributeExtCollection(new Attribute[] { new IdlSequenceAttribute(0L),
                                                                                     new StringValueAttribute(),
                                                                                     new WideCharAttribute(false) }));
        }

        #endregion IConstructors
        #region IMethods
        
        #region Common

        protected void SerialiseContext(CdrOutputStream targetStream, ServiceContextCollection cntxColl) {
            IEnumerator enumerator = cntxColl.GetEnumerator();
            targetStream.WriteULong((uint)cntxColl.Count); // nr of service contexts
            while (enumerator.MoveNext()) {
                ServiceContext cntx = (ServiceContext) enumerator.Current;
                cntx.Serialize(targetStream);    
            }
        }

        protected ServiceContextCollection DeserialiseContext(CdrInputStream sourceStream) {
            ServiceContextCollection cntxColl = new ServiceContextCollection();
            CosServices services = CosServices.GetSingleton();
            uint nrOfContexts = sourceStream.ReadULong();
            for (uint i = 0; i < nrOfContexts; i++) {
                uint serviceId = sourceStream.ReadULong();
                CorbaService service = services.GetForServiceId((int)serviceId);
                CdrEncapsulationInputStream serviceData = sourceStream.ReadEncapsulation();
                ServiceContext cntx = service.DeserialiseContext(serviceData);
                // add the service context if not already present. 
                // Important: Don't throw an exception if already present,
                // because WAS4.0.4 includes more than one with same id.
                if (!cntxColl.ContainsContextForService(cntx.ServiceID)) {
                    cntxColl.AddServiceContext(cntx);
                }
            }
            return cntxColl;
        }

        protected void AlignBodyIfNeeded(CdrInputStream cdrStream, GiopVersion version) {
            if ((version.Major == 1) && (version.Minor >= 2)) {
                cdrStream.ForceReadAlign(Aligns.Align8);
            } // force an align on 8 for GIOP-version >= 1.2
        }

        protected void AlignBodyIfNeeded(CdrOutputStream cdrStream, GiopVersion version) {
            if ((version.Major == 1) && (version.Minor >= 2)) { 
                cdrStream.ForceWriteAlign(Aligns.Align8); 
            } // force an align on 8 for GIOP-version >= 1.2
        }

        /// <summary>
        /// set the codesets for the stream after codeset service descision
        /// </summary>
        /// <param name="cdrStream"></param>
        private void SetCodeSet(CdrInputStream cdrStream, GiopConnectionDesc conDesc) {
            cdrStream.CharSet = conDesc.CharSet;
            cdrStream.WCharSet = conDesc.WCharSet;
        }

        /// <summary>
        /// set the codesets for the stream after codeset service descision
        /// </summary>
        /// <param name="cdrStream"></param>
        private void SetCodeSet(CdrOutputStream cdrStream, GiopConnectionDesc conDesc) {            
            cdrStream.CharSet = conDesc.CharSet;
            cdrStream.WCharSet = conDesc.WCharSet;
        }

        /// <summary>read the target for the request</summary>
        /// <returns>the objectURI extracted from this msg</returns>
        private string ReadTarget(CdrInputStream cdrStream, GiopVersion version) {
            if (version.IsBeforeGiop1_2()) {
                // for GIOP 1.0 / 1.1 only object key is possible
                return ReadTargetKey(cdrStream);
            }
            
            // for GIOP >= 1.2, a union is used for target information
            ushort targetAdrType = cdrStream.ReadUShort();
            switch (targetAdrType) {
                case 0:
                    return ReadTargetKey(cdrStream);
                default:
                    throw new NotSupportedException("target address type not supported: " + targetAdrType);
            }
        }

        private string ReadTargetKey(CdrInputStream cdrStream) {
            uint length = cdrStream.ReadULong();
            Debug.WriteLine("object key follows:");
            byte[] objectKey = cdrStream.ReadOpaque((int)length);
                    
            // get the object-URI of the responsible object
            return IiopUrlUtil.GetObjectUriForObjectKey(objectKey);
        }

        #endregion Common
        #region Requests

        private void WriteTarget(CdrOutputStream cdrStream, 
                                 byte[] objectKey, GiopVersion version) {
            if (!((version.Major == 1) && (version.Minor <= 1))) {
                // for GIOP >= 1.2
                ushort targetAdrType = 0;
                cdrStream.WriteUShort(targetAdrType); // object key adressing
            }
            WriteTargetKey(cdrStream, objectKey);
        }

        private void WriteTargetKey(CdrOutputStream cdrStream, byte[] objectKey) {
            Debug.WriteLine("writing object key with length: " + objectKey.Length);
            cdrStream.WriteULong((uint)objectKey.Length); // object-key length
            cdrStream.WriteOpaque(objectKey);
        }               

        /// <summary>
        /// serialises the message body for a GIOP request
        /// </summary>
        /// <param name="clientRequest">the giop request Msg</param>
        /// <param name="targetStream"></param>
        /// <param name="version">the Giop version to use</param>      
        /// <param name="conDesc">the connection used for this request</param>  
        internal void SerialiseRequest(GiopClientRequest clientRequest,
                                       CdrOutputStream targetStream, 
                                       Ior targetIor, GiopConnectionDesc conDesc) {
            Trace.WriteLine(String.Format("serializing request for method {0}; uri {1}; id {2}", 
                                          clientRequest.MethodToCall, clientRequest.Uri, 
                                          clientRequest.RequestId));
            GiopVersion version = targetIor.Version;

            ServiceContextCollection cntxColl = CosServices.GetSingleton().
                                                    InformInterceptorsRequestToSend(clientRequest.Request, targetIor, 
                                                                                    conDesc);

            // set code-set for the stream
            SetCodeSet(targetStream, conDesc);
                        
            if (version.IsBeforeGiop1_2()) { // for GIOP 1.0 / 1.1
                SerialiseContext(targetStream, cntxColl); // service context                
            }

            targetStream.WriteULong(clientRequest.RequestId);
            byte responseFlags = 0;
            if (version.IsBeforeGiop1_2()) { // GIOP 1.0 / 1.1
                responseFlags = 1;
            } else {
                // reply-expected, no DII-call --> must be 0x03, no reply --> must be 0x00
                responseFlags = 3;
            }
            if (clientRequest.IsOneWayCall) { 
                responseFlags = 0; 
            } // check if one-way
            // write response-flags
            targetStream.WriteOctet(responseFlags); 
                        
            targetStream.WritePadding(3); // reserved bytes
            WriteTarget(targetStream, targetIor.ObjectKey, version); // write the target-info            
            targetStream.WriteString(clientRequest.RequestMethodName); // write the method name
            
            if (version.IsBeforeGiop1_2()) { // GIOP 1.0 / 1.1
                targetStream.WriteULong(0); // no principal
            } else { // GIOP 1.2
                SerialiseContext(targetStream, cntxColl); // service context
            }
            SerialiseRequestBody(targetStream, clientRequest, version);
        }

        private void SerialiseContextElements(CdrOutputStream targetStream, MethodInfo methodToCall,
                                              LogicalCallContext callContext) {
            AttributeExtCollection methodAttrs =
                ReflectionHelper.GetCustomAttriutesForMethod(methodToCall, true,
                                                             ReflectionHelper.ContextElementAttributeType);
            if (methodAttrs.Count > 0) {
                string[] contextSeq = new string[methodAttrs.Count * 2];
                for (int i = 0; i < methodAttrs.Count; i++) {
                    string contextKey =
                        ((ContextElementAttribute)methodAttrs.GetAttributeAt(i)).ContextElementKey;
                    contextSeq[i * 2] = contextKey;
                    if (callContext.GetData(contextKey) != null) {
                        contextSeq[i * 2 + 1] = callContext.GetData(contextKey).ToString();
                    } else {
                        contextSeq[i * 2 + 1] = "";
                    }
                }
                m_contextSeqMarshaller.Marshal(contextSeq, targetStream);
            }
        }

        /// <summary>serializes the request body</summary>
        /// <param name="targetStream"></param>
        /// <param name="clientRequest">the request to serialise</param>
        /// <param name="version">the GIOP-version</param>
        private void SerialiseRequestBody(CdrOutputStream targetStream, GiopClientRequest clientRequest,
                                          GiopVersion version) {
            // body of request msg: serialize arguments
            // clarification from CORBA 2.6, chapter 15.4.1: no padding, when no arguments are serialised  -->
            // for backward compatibility, do it nevertheless
            AlignBodyIfNeeded(targetStream, version);
            ParameterMarshaller marshaller = ParameterMarshaller.GetSingleton();
            marshaller.SerialiseRequestArgs(clientRequest.MethodToCall, clientRequest.RequestArguments, 
                                            targetStream);
            // check for context elements
            SerialiseContextElements(targetStream, clientRequest.MethodToCall,
                                     clientRequest.RequestCallContext);
        }

        /// <summary>
        /// Deserialises the Giop Message body for a request
        /// </summary>
        /// <param name="cdrStream"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        internal IMessage DeserialiseRequest(CdrInputStream cdrStream, GiopVersion version,
                                           GiopConnectionDesc conDesc) {            
            GiopServerRequest serverRequest = new GiopServerRequest();
            serverRequest.Version = version;
            try {
                if (version.IsBeforeGiop1_2()) { // GIOP 1.0 / 1.1
                    ServiceContextCollection coll = DeserialiseContext(cdrStream); // Service context deser
                    CosServices.GetSingleton().InformInterceptorsReceivedRequest(coll, conDesc);
                }
                
                // read the request-ID and set it as a message property
                uint requestId = cdrStream.ReadULong(); 
                serverRequest.RequestId = requestId;                
                Trace.WriteLine("received a message with reqId: " + requestId);
                // read response-flags:
                byte respFlags = cdrStream.ReadOctet(); Debug.WriteLine("response-flags: " + respFlags);
                cdrStream.ReadPadding(3); // read reserved bytes
                serverRequest.ResponseFlags = respFlags;
                
                // decode the target of this request
                serverRequest.RequestUri = ReadTarget(cdrStream, version);
                serverRequest.RequestMethodName = cdrStream.ReadString();
                Trace.WriteLine("call for .NET object: " + serverRequest.RequestUri + 
                                ", methodName: " + serverRequest.RequestMethodName);

                if (version.IsBeforeGiop1_2()) { // GIOP 1.0 / 1.1
                    uint principalLength = cdrStream.ReadULong();
                    cdrStream.ReadOpaque((int)principalLength);
                } else {
                    ServiceContextCollection coll = DeserialiseContext(cdrStream); // Service context deser
                    CosServices.GetSingleton().InformInterceptorsReceivedRequest(coll, conDesc);
                }
                // set codeset for stream
                SetCodeSet(cdrStream, conDesc);
                // request header deserialised

                IDictionary contextElements;
                serverRequest.ResolveCall(); // determine the .net target method
                DeserialiseRequestBody(cdrStream, version, serverRequest, out contextElements);
                MethodCall methodCallInfo = new MethodCall(serverRequest.Request);
                if (contextElements != null) {
                    AddContextElementsToCallContext(methodCallInfo.LogicalCallContext, contextElements);
                }
                return methodCallInfo;
            } catch (Exception e) {
                // an Exception encountered during deserialisation
                try {
                    cdrStream.SkipRest(); // skip rest of the message, to not corrupt the stream
                } catch (Exception) {
                    // ignore exception here, already an other exception leading to problems
                }
                throw new RequestDeserializationException(e, serverRequest.Request);
            }
        }

        private void AddContextElementsToCallContext(LogicalCallContext callContext, IDictionary elements) {
            foreach (DictionaryEntry entry in elements) {
                callContext.SetData((string)entry.Key, new CorbaContextElement((string)entry.Value));
            }
        }

        private IDictionary DeserialseContextElements(CdrInputStream cdrStream, AttributeExtCollection contextElemAttrs) {
            IDictionary result = new HybridDictionary();
            string[] contextElems = (string[])m_contextSeqMarshaller.Unmarshal(cdrStream);
            if (contextElems.Length % 2 != 0) {
                throw new MARSHAL(67, CompletionStatus.Completed_No);
            }
            for (int i = 0; i < contextElems.Length; i += 2) {
                string contextElemKey = contextElems[i];
                // insert into call context, if part of signature
                foreach (ContextElementAttribute attr in contextElemAttrs) {
                    if (attr.ContextElementKey == contextElemKey) {
                        result[contextElemKey] = contextElems[i + 1];
                        break;
                    }
                }
            }
            return result;
        }
        
        private object[] AdaptArgsForStandardOp(object[] args, string objectUri) {
            object[] result = new object[args.Length+1];
            result[0] = objectUri; // this argument is passed to all standard operations
            Array.Copy((Array)args, 0, result, 1, args.Length);
            return result;
        }                                                

        /// <summary>deserialise the request body</summary>
        /// <param name="contextElements">the deserialised context elements, if any or null</param>        
        private void DeserialiseRequestBody(CdrInputStream cdrStream, GiopVersion version,
                                            GiopServerRequest request,
                                            out IDictionary contextElements) {
            // unmarshall parameters
            ParameterMarshaller paramMarshaller = ParameterMarshaller.GetSingleton();
            object[] args;
            // clarification from CORBA 2.6, chapter 15.4.1: no padding, when no arguments/no context elements
            // are serialised, i.e. body empty
            bool hasRequestArgs = paramMarshaller.HasRequestArgs(request.CalledMethod);
            AttributeExtCollection methodAttrs =
                ReflectionHelper.GetCustomAttriutesForMethod(request.CalledMethod, true,
                                                             ReflectionHelper.ContextElementAttributeType);
            contextElements = null;
            if ((hasRequestArgs) || (methodAttrs.Count > 0)) {
                AlignBodyIfNeeded(cdrStream, version); // aling request body
            } else {
                cdrStream.SkipRest(); // ignore paddings, if included    
            }
            if (hasRequestArgs) {
                args = paramMarshaller.DeserialiseRequestArgs(request.CalledMethod, cdrStream);
            } else {
                args = new object[request.CalledMethod.GetParameters().Length];
            }
            if (methodAttrs.Count > 0) {
                contextElements = DeserialseContextElements(cdrStream, methodAttrs);
            }            
            // for standard corba ops, adapt args:
            if (request.IsStandardCorbaOperation) {
                args = AdaptArgsForStandardOp(args, request.RequestUri);
            }
            request.RequestArgs = args;            
        }

        #endregion Requests
        #region Replys

        /// <summary>serialize the GIOP message body of a repsonse message</summary>
        /// <param name="requestId">the requestId of the request, this response belongs to</param>
        internal void SerialiseReply(GiopServerRequest request, CdrOutputStream targetStream, 
                                   GiopVersion version,
                                   GiopConnectionDesc conDesc) {
            Trace.WriteLine("serializing response for method: " + request.CalledMethodName);
            
            ServiceContextCollection cntxColl = CosServices.GetSingleton().
                                                    InformInterceptorsReplyToSend(conDesc);
            // set codeset for stream
            SetCodeSet(targetStream, conDesc);

            if (version.IsBeforeGiop1_2()) { // for GIOP 1.0 / 1.1
                SerialiseContext(targetStream, cntxColl); // serialize the context
            }
            
            targetStream.WriteULong(request.RequestId);

            if (!request.IsExceptionReply) { 
                Trace.WriteLine("sending normal response to client");
                targetStream.WriteULong(0); // reply status ok
                
                if (!((version.Major == 1) && (version.Minor <= 1))) { // for GIOP 1.2 and later, service context is here
                    SerialiseContext(targetStream, cntxColl); // serialize the context                
                }
                // serialize a response to a successful request
                SerialiseResponseOk(targetStream, request, version);
                Trace.WriteLine("reply body serialised");
            } else {
                Trace.WriteLine("exception to pass to client: " + request.Exception.GetType());
                Exception exceptionToSend = DetermineExceptionToThrow(request.Exception, request.CalledMethod);
                Trace.WriteLine("excpetion to send to client: " + exceptionToSend.GetType());
                
                if (SerialiseAsSystemException(exceptionToSend)) {
                    targetStream.WriteULong(2); // system exception
                } else if (SerialiseAsUserException(exceptionToSend)) {
                    targetStream.WriteULong(1); // user exception
                } else {
                    // should not occur
                    targetStream.WriteULong(2);
                    exceptionToSend = new INTERNAL(204, CompletionStatus.Completed_Yes);
                }

                if (!((version.Major == 1) && (version.Minor <= 1))) { // for GIOP 1.2 and later, service context is here
                    SerialiseContext(targetStream, cntxColl); // serialize the context                
                }
                AlignBodyIfNeeded(targetStream, version);
                if (SerialiseAsSystemException(exceptionToSend)) {
                    SerialiseSystemException(targetStream, exceptionToSend);
                } else {
                    SerialiseUserException(targetStream, (AbstractUserException)exceptionToSend);
                }
                Trace.WriteLine("exception reply serialised");
            }
        }

        private void SerialiseResponseOk(CdrOutputStream targetStream, GiopServerRequest request,
                                         GiopVersion version) {
            // reply body
            // clarification form CORBA 2.6, chapter 15.4.2: no padding, when no arguments are serialised  -->
            // for backward compatibility, do it nevertheless
            AlignBodyIfNeeded(targetStream, version);
            // marshal the parameters
            ParameterMarshaller marshaller = ParameterMarshaller.GetSingleton();
            marshaller.SerialiseResponseArgs(request.CalledMethod, 
                                             request.ReturnValue, request.OutArgs, targetStream);            
        }                

        /// <summary>serialize the exception as a CORBA System exception</summary>
        private bool SerialiseAsSystemException(Exception e) {
            return (e is omg.org.CORBA.AbstractCORBASystemException);
        }
        
        /// <summary>serialize the exception as a CORBA user exception</summary>
        private bool SerialiseAsUserException(Exception e) {
            return (e is AbstractUserException);
        }
        
        /// <summary>
        /// determines, which exception to return to the client based on
        /// the called method/attribute and on the Exception thrown.
        /// Make sure to return only exceptions, which are allowed for the thrower; e.g.
        /// only those specified in the interface for methods and for attributes only system exceptions.
        /// </summary>
        private Exception DetermineExceptionToThrow(Exception thrown, MethodBase thrower) {
            if (SerialiseAsSystemException(thrown)) {
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
                Debug.WriteLine("exception encountered before remote method call target determined: " + 
                                thrown);
                exceptionToThrow = new UNKNOWN(201, CompletionStatus.Completed_No);
            }
            return exceptionToThrow;
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
        
        private void SerialiseSystemException(CdrOutputStream targetStream, Exception corbaEx) {
            // serialize a system exception
            if (!(corbaEx is AbstractCORBASystemException)) {
                corbaEx = new UNKNOWN(202, omg.org.CORBA.CompletionStatus.Completed_MayBe);
            }
            
            Marshaller marshaller = Marshaller.GetSingleton();
            marshaller.Marshal(corbaEx.GetType(), Util.AttributeExtCollection.EmptyCollection,
                               corbaEx, targetStream);
        }

        private void SerialiseUserException(CdrOutputStream targetStream, AbstractUserException userEx) {            
            Type exceptionType = userEx.GetType();            
            // marshal the exception
            Marshaller marshaller = Marshaller.GetSingleton();
            marshaller.Marshal(exceptionType, Util.AttributeExtCollection.EmptyCollection,
                               userEx, targetStream);
        }


        internal IMessage DeserialiseReply(CdrInputStream cdrStream, 
                                         GiopVersion version, GiopClientRequest request,
                                         GiopConnectionDesc conDesc) {

            if (version.IsBeforeGiop1_2()) { // for GIOP 1.0 / 1.1, the service context is placed here
                ServiceContextCollection coll = DeserialiseContext(cdrStream); // deserialize the service contexts
                CosServices.GetSingleton().InformInterceptorsReceivedReply(coll, conDesc);
            }
            
            uint forRequestId = cdrStream.ReadULong();
            uint responseStatus = cdrStream.ReadULong();
            if (!version.IsBeforeGiop1_2()) { // for GIOP 1.2 and later, service context is here
                ServiceContextCollection coll = DeserialiseContext(cdrStream); // deserialize the service contexts
                CosServices.GetSingleton().InformInterceptorsReceivedReply(coll, conDesc);
            }
            
            // set codeset for stream
            SetCodeSet(cdrStream, conDesc);
            
            IMessage response = null;
            try {
                switch (responseStatus) {
                    case 0 : 
                        Trace.WriteLine("deserializing normal reply for methodCall: " + request.MethodToCall);
                        response = DeserialiseNormalReply(cdrStream, version, request); 
                        break;
                    case 1 :                         
                        Exception userEx = DeserialiseUserException(cdrStream, version); // the error .NET message for this exception is created in the formatter
                        response = new ReturnMessage(userEx, request.Request);
                        break;
                    case 2 : 
                        Exception systemEx = DeserialiseSystemError(cdrStream, version); // the error .NET message for this exception is created in the formatter
                        response = new ReturnMessage(systemEx, request.Request);
                        break;
                    case 3 :
                        // LOCATION_FORWARD:
                        // --> deserialise it and return location fwd message
                        response = DeserialiseLocationFwdReply(cdrStream, version, request); 
                        break;
                    default : 
                        // deseralization of reply error, unknown reply status: responseStatus
                        // the error .NET message for this exception is created in the formatter
                        throw new MARSHAL(2401, CompletionStatus.Completed_MayBe);
                }
                request.Reply = response;
            } catch (Exception e) {
                Debug.WriteLine("exception while deserialising reply: " + e);
                // do not corrupt stream --> skip
                try {
                    cdrStream.SkipRest();
                } catch (Exception) {
                    // ignore this one, already problems.
                }
                throw;
            }

            return response;
        }

        /// <summary>deserialize response with ok-status.</summary>
        private IMessage DeserialiseNormalReply(CdrInputStream cdrStream, GiopVersion version, 
                                                GiopClientRequest request) {
            MethodInfo targetMethod = request.MethodToCall;
            ParameterMarshaller paramMarshaller = ParameterMarshaller.GetSingleton();
            object[] outArgs;
            object retVal = null;
            // body
            // clarification from CORBA 2.6, chapter 15.4.2: no padding, when no arguments are serialised
            if (paramMarshaller.HasResponseArgs(targetMethod)) {
                AlignBodyIfNeeded(cdrStream, version);
                // read the parameters                            
                retVal = paramMarshaller.DeserialiseResponseArgs(targetMethod, cdrStream, out outArgs);
            } else {
                outArgs = new object[0];
                cdrStream.SkipRest(); // skip padding, if present
            }
            ReturnMessage response = new ReturnMessage(retVal, outArgs, outArgs.Length, null, request.Request);
            LogicalCallContext dnCntx = response.LogicalCallContext;
            // TODO: fill in .NET context ...
            return response;
        }

        /// <summary>deserialises a CORBA system exception </summary>
        private Exception DeserialiseSystemError(CdrInputStream cdrStream, GiopVersion version) {
            // exception body
            AlignBodyIfNeeded(cdrStream, version);

            Marshaller marshaller = Marshaller.GetSingleton();
            Exception result = (Exception) marshaller.Unmarshal(typeof(omg.org.CORBA.AbstractCORBASystemException),
                                                                Util.AttributeExtCollection.EmptyCollection,
                                                                cdrStream);
            
            if (result == null) { 
                return new Exception("received system error from peer orb, but error was not deserializable");
            } else {
                return result;
            }
        }

        private Exception DeserialiseUserException(CdrInputStream cdrStream, GiopVersion version) {
            // exception body
            AlignBodyIfNeeded(cdrStream, version);

            Marshaller marshaller = Marshaller.GetSingleton();
            Exception result = (Exception) marshaller.Unmarshal(typeof(AbstractUserException),
                                                                Util.AttributeExtCollection.EmptyCollection,
                                                                cdrStream);
            if (result == null) {
                return new Exception("user exception received from peer orb, but was not deserializable");
            } else {
                return result;
            }
        }
        
        /// <summary>
        /// deserialise the location fwd
        /// </summary>
        private LocationForwardMessage DeserialiseLocationFwdReply(CdrInputStream cdrStream, 
                                                                   GiopVersion version,
                                                                   GiopClientRequest request) {
            AlignBodyIfNeeded(cdrStream, version);
            // read the Location fwd IOR
            Marshaller marshaller = Marshaller.GetSingleton();
            MarshalByRefObject newProxy = marshaller.Unmarshal(request.MethodToCall.DeclaringType, 
                                                               AttributeExtCollection.EmptyCollection, cdrStream)
                                              as MarshalByRefObject;
            if (newProxy == null) {
                throw new OBJECT_NOT_EXIST(2402, CompletionStatus.Completed_No);
            }
            return new LocationForwardMessage(newProxy);            
        }
        
        /// <summary>
        /// creates a return message for a return value and possible out/ref args among the sent arguments
        /// </summary>
        internal ReturnMessage CreateReturnMsgForValues(object retVal, object[] reqArgs,
                                                        IMethodCallMessage request) {
            // find out args
            MethodInfo targetMethod = (MethodInfo)request.MethodBase;
            ParameterInfo[] parameters = targetMethod.GetParameters();

            bool outArgFound = false;
            ArrayList outArgsList = new ArrayList();
            for (int i = 0; i < parameters.Length; i++) {
                if (ParameterMarshaller.IsOutParam(parameters[i]) || 
                    ParameterMarshaller.IsRefParam(parameters[i])) {
                    outArgsList.Add(reqArgs[i]); // i-th argument is an out/ref param
                    outArgFound = true;
                } else {
                    outArgsList.Add(null); // for an in param null must be added to out-args
                }
            }
            
            object[] outArgs = outArgsList.ToArray();
            if ((!outArgFound) || (outArgs == null)) { 
                outArgs = new object[0]; 
            }
            // create the return message
            return new ReturnMessage(retVal, outArgs, outArgs.Length, null, request); 
        }        
            
        #endregion Replys
        #region Locate

        /// <summary>
        /// deserialise a locate request msg.
        /// </summary>
        /// <param name="cdrStream"></param>
        /// <param name="version"></param>
        /// <param name="forRequestId">returns the request id as out param</param>
        /// <returns>the uri of the object requested to find</returns>
        public string DeserialiseLocateRequest(CdrInputStream cdrStream, GiopVersion version, out uint forRequestId) {
            forRequestId = cdrStream.ReadULong(); 
            return ReadTarget(cdrStream, version);
        }

        /// <summary>
        /// serialises a locate reply message.
        /// </summary>
        /// <param name="forwardAddr">
        /// specifies the IOR of the object to forward the call to. This parameter must be != null,
        /// if LocateStatus is OBJECT_FORWARD.
        ///  </param>
        public void SerialiseLocateReply(CdrOutputStream targetStream, GiopVersion version, uint forRequestId, 
                                         LocateStatus status, Ior forward) {
            targetStream.WriteULong(forRequestId);
            switch (status) {
                case LocateStatus.OBJECT_HERE:
                    targetStream.WriteULong(1);
                    break;
                default:
                    Debug.WriteLine("Locate reply status not supported");
                    throw new NotSupportedException("not supported");
            }            
        }

        #endregion Locate
        
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
    using Ch.Elca.Iiop.MessageHandling;
    using Ch.Elca.Iiop.Cdr;
    using omg.org.CORBA;
    

    /// <summary>
    /// Unit-tests for testing request/reply serialisation/deserialisation
    /// </summary>
    public class MessageBodySerialiserTest : TestCase {
        
        public void TestSameServiceIdMultiple() {
            // checks if service contexts with the same id, doesn't throw an exception
            // checks, that the first service context is considered, others are thrown away
            GiopMessageBodySerialiser ser = GiopMessageBodySerialiser.GetSingleton();    
            MemoryStream stream = new MemoryStream();
            CdrOutputStreamImpl cdrOut = new CdrOutputStreamImpl(stream, 0, new GiopVersion(1,2));
            cdrOut.WriteULong(2); // nr of contexts
            cdrOut.WriteULong(1234567); // id of context 1
            CdrEncapsulationOutputStream encap = new CdrEncapsulationOutputStream(0);
            cdrOut.WriteEncapsulation(encap);
            cdrOut.WriteULong(1234567); // id of context 2
            encap = new CdrEncapsulationOutputStream(0);
            cdrOut.WriteEncapsulation(encap);
            // reset stream
            stream.Seek(0, SeekOrigin.Begin);
            CdrInputStreamImpl cdrIn = new CdrInputStreamImpl(stream);
            cdrIn.ConfigStream(0, new GiopVersion(1,2));
            // call deser method via reflection, because of protection level
            Type msgBodySerType = ser.GetType();
            MethodInfo method = msgBodySerType.GetMethod("DeserialiseContext", BindingFlags.NonPublic | BindingFlags.Instance);
            Assertion.Assert(method != null);
            ServiceContextCollection result = (ServiceContextCollection) method.Invoke(ser, new object[] { cdrIn });
            // check if context is present
            Assertion.Assert("expected context not in collection", result.ContainsContextForService(1234567) == true);
        }        
                
    }
    
}

#endif
