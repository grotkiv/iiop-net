/* PortableInterceptor.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 13.02.05  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
 * 
 * Copyright 2005 Dominic Ullmann
 *
 * Copyright 2005 ELCA Informatique SA
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
using Ch.Elca.Iiop.Idl;


namespace omg.org.PortableInterceptor {

    /// <summary>the reply status in request-info</summary>
    public enum ReplyStatus : short {
        SUCCESSFUL = 0, SYSTEM_EXCEPTION = 1, USER_EXCEPTION = 2, LOCATION_FORWARD = 3,
        TRANSPORT_RETRY = 4
    }
    
    
    /// <summary>
    /// Base interface for all portable interceptors.
    /// </summary>
    [RepositoryID("IDL:omg.org/PortableInterceptor/Interceptor:1.0")]
    [InterfaceType(IdlTypeInterface.LocalInterface)]
    public interface Interceptor {
    
    
        /// <summary>
        /// Each interceptor may have a name that may be used administratively to order the lists of Interceptors.
        /// Only one interceptor of a given name can be registered for each interceptor type except for 
        /// anonymous interceptors (i.e. name empty string): they may be registered more than once.
        /// </summary>
        [StringValue()]
        [WideChar(false)]
        string Name {
            get;
        }
        
        // no ORB.destroy -> the following method is not useful
        // void destroy();
        
    }
    
    
    /// <summary>
    /// Interface to be implemented by a client side request interceptor. 
    /// The client side interceptors intercepts the request/reply sequence at specific points
    /// on the client side.        
    /// </summary>
    /// <remarks>
    /// The interceptor list is traversed in order on sending interception points and
    /// in reverse order on the receiving interception points.
    /// </remarks>
    [RepositoryID("IDL:omg.org/PortableInterceptor/ClientRequestInterceptor:1.0")]
    [InterfaceType(IdlTypeInterface.LocalInterface)]
    public interface ClientRequestInterceptor : Interceptor {
        
        
        /// <summary>
        /// this interception point allows an interceptor to quest request information
        /// and modify the service context before the request is sent to the server.
        /// This point may raise a system exception. If it does, no other interceptors send_request operations 
        /// are called.
        /// The interceptors already on the flow stack are popped, and their receive_exception point are called.
        /// </summary>
        /// <param name="ri"></param>        
        /// <remarks>Interceptors shall follow completion_status semantics if they raise a system exception from
        /// this point: The status shall be COMPLETED_NO</remarks>
        // TODO: forwardrequest description        
        void send_request(ClientRequestInfo ri);
        
        // not supported, because no locate request message may be sent
        // void send_poll(ClientRequestInfo ri);
        
        /// <summary>
        /// This interception point allows an interceptor to query the information on a reply after
        /// it is returned from the server and before control is returned to the client.
        /// This point may raise a system exception. If it does, no other interceptors receive_reply operations 
        /// are called.
        /// The remaining interceptors in the flow stack shall have their receive exception interception 
        /// point called.
        /// </summary>
        /// <param name="ri"></param>
        /// <remarks>Interceptors shall follow completion_status semantics if they raise a system exception from
        /// this point: The status shall be COMPLETED_YES</remarks>
        void receive_reply(ClientRequestInfo ri);
        
        /// <summary>
        /// When an exception occurs, this interception point is called. It allows an interceptor
        /// to query the exception's information before it is raised to the client.
        /// </summary>
        /// <param name="ri"></param>
        void receive_exception(ClientRequestInfo ri);
        
        /// <summary>
        /// This interception point allows an Interceptor to query the information available, when
        /// a request results in something other than a normal reply or exception. For example, a request
        /// could result in a retry (e.g. on LOCATION_FORWARD status); or for an asynchronous call
        /// the reply does not follow immediately the request, but control shall return to client and
        /// an ending interception point shall be called.
        /// Asynchronous requests are simply two separate requests: The first received no reply.
        /// The second receives a normal reply. So the normal (no exception) flow is:
        /// send_request followed by receive_other. second: send_request followed by receive_reply.
        /// 
        /// This interception point is also called for oneway requests.
        /// </summary>
        /// <param name="ri"></param>
        void receive_other(ClientRequestInfo ri);
    }    
    
    
    /// <summary>
    /// Interface to be implemented by a server side request interceptor. 
    /// The server side interceptors intercepts the request/reply sequence at specific points
    /// on the server side.        
    /// </summary>
    [RepositoryID("IDL:omg.org/PortableInterceptor/ServerRequestInterceptor:1.0")]
    [InterfaceType(IdlTypeInterface.LocalInterface)]
    public interface ServerRequestInterceptor : Interceptor {
        
        /// <summary>
        /// At this interception point, interceptors must get their service context information
        /// from the incoming request and transfer it to PortableInterceptor::Current's slots.
        /// Hint: Operation parameters are not yet available at this point.
        /// </summary>
        /// <param name="ri"></param>
        /// <remarks>On throwing system exception: completion status is Completed_NO</remarks>
        void receive_request_service_contexts(ServerRequestInfo ri);
        
        /// <summary>
        /// This interception point allos an interceptor to query information after all the information,
        /// including operation parameters are available. 
        /// </summary>
        /// <param name="ri"></param>
        /// <remarks>On throwing system exception: completion status is Completed_NO</remarks>
        void receive_request(ServerRequestInfo ri);
        
        /// <summary>
        /// this interception point allows an interceptor to query reply information and modfy 
        /// the reply service context after the target operation has been invoked and before the 
        /// reply is returned to the client.
        /// </summary>
        /// <param name="ri"></param>
        /// <remarks>On throwing system exception: completion status is Completed_YES</remarks>
        void send_reply(ServerRequestInfo ri);
        
        /// <summary>
        /// When an exception occurs, this interception point is called. It allows an interceptor
        /// to query the exception information and modify the reply service context before
        /// the exception is raised to the client.
        /// </summary>
        /// <param name="ri"></param>
        void send_exception(ServerRequestInfo ri);
        
        /// <summary>
        /// This interception point allows an interceptor to query the information available
        /// when a request results in something other than a normal reply or exception.
        /// </summary>
        /// <param name="ri"></param>
        /// <remarks>On throwing system exception: completion status is Completed_NO</remarks>
        void send_other(ServerRequestInfo ri);
                
    }
    
    

    /// <summary>
    /// base interface for ServerRequestInfo and ClientRequestInfo.
    /// </summary>
    [RepositoryID("IDL:omg.org/PortableInterceptor/RequestInfo:1.0")]
    [InterfaceType(IdlTypeInterface.LocalInterface)]
    public interface RequestInfo {

        /// <summary>
        /// request id, which identifies the request/reply sequence.
        /// </summary>
        long request_id {
            get;
        }
        
        /// <summary>
        /// The name of the operation being invoked.
        /// </summary>
        [StringValue()]
        [WideChar(false)]        
        string operation {
            get;
        }
        
        
        /// <summary>
        /// contains the result of the invocation.
        /// </summary>
        object result {
            get;
        }
        
        /// <summary>
        /// indicates, wheter a reponse is expected.
        /// </summary>
        bool response_expected {
            get;
        }
        
        /// <summary>
        /// indicates the state of the result of the invocation.
        /// </summary>
        ReplyStatus reply_status {
            get;
        }
        
        /// <summary>
        /// if reply status is location_forward, this property will contain
        /// the forward target.
        /// </summary>
        MarshalByRefObject forward_reference {
            get;
        }
        
        // TODO: missing properties
    }
    
    
    /// <summary>
    /// used in client side request interceptors to pass information to interception points
    /// </summary>
    [RepositoryID("IDL:omg.org/PortableInterceptor/ClientRequestInfo:1.0")]
    [InterfaceType(IdlTypeInterface.LocalInterface)]
    public interface ClientRequestInfo : RequestInfo {
        
        /// <summary>the object, the client called to perform the operation</summary>
        MarshalByRefObject target {
            get;
        }
        
        /// <summary>the actual object, the operation will be invoked on.</summary>
        MarshalByRefObject effective_target {
            get;
        }
        
        // TODO: missing properties
        
        
    }
    
    
    /// <summary>
    /// used in server side request interceptors to pass information to interception points
    /// </summary>
    [RepositoryID("IDL:omg.org/PortableInterceptor/ServerRequestInfo:1.0")]
    [InterfaceType(IdlTypeInterface.LocalInterface)]
    public interface ServerRequestInfo : RequestInfo {

        /// <summary>the opaque id, describing the target of the operation invocation.</summary>
        byte[] object_id {
            get;
        }
        
    }
    
    
    /// <summary>
    /// A portable service implementation may add information to ior's (tagged components)
    /// in order that client side service works correctly.
    /// </summary>
    [RepositoryID("IDL:omg.org/PortableInterceptor/IORInterceptor:1.0")]
    [InterfaceType(IdlTypeInterface.LocalInterface)]
    public interface IORInterceptor : Interceptor {
        
        /// <summary>
        /// establishes tagged components in the profiles within an IOR.
        /// </summary>
        /// <param name="info"></param>
        void establish_components (IORInfo info);
        
    }
    
    
    /// <summary>
    /// The IORInfo allows IORInterceptor (on the server side) to components
    /// to an ior profile.
    /// </summary>
    [RepositoryID("IDL:omg.org/PortableInterceptor/IORInfo:1.0")]
    [InterfaceType(IdlTypeInterface.LocalInterface)]
    public interface IORInfo {
        
        // TODO
        
    }
    
      
}
