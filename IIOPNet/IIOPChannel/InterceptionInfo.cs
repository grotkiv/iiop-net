/* InterceptionInfo.cs
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
using omg.org.CORBA;
using omg.org.PortableInterceptor;
using Ch.Elca.Iiop.MessageHandling;
using Ch.Elca.Iiop.CorbaObjRef;

namespace Ch.Elca.Iiop.Interception {
    
    
    /// <summary>
    /// implementation of RequestInfo interface
    /// </summary>
    internal abstract class RequestInfoImpl : RequestInfo {
        
        #region IConstructors
        
        internal RequestInfoImpl() {            
        }
        
        #endregion IConstructors
        #region IProperties
        
        public int request_id {
            get {
                throw new NotImplementedException();
            }
        }
        
        public string operation {
            get {
                throw new NotImplementedException();
            }
        }
        
        /// <summary>
        /// <see cref="omg.org.PortableInterceptor.RequestInfo.result"
        /// </summary>
        public object result {
            get {
                // not mandatory, for the beginning, don't implement.
                // TODO
                throw new NO_RESOURCES(1, CompletionStatus.Completed_MayBe);
            }
        }
        
        public bool response_expected {
            get {
                throw new NotImplementedException();
            }
        }
        
        public MarshalByRefObject forward_reference {
            get {
                throw new NotImplementedException();
            }
        }
        
        public ReplyStatus reply_status {
            get {
                throw new NotImplementedException();
            }
        }
        
        #endregion IProperties        
        
    }
    
    /// <summary>
    /// implementation of ClientRequestInfo interface used for client side interception
    /// </summary>    
    internal class ClientRequestInfoImpl : RequestInfoImpl, ClientRequestInfo {
        
        #region IFields
        
        private GiopClientRequest m_clientRequest;
        private Exception m_receivedException = null;
        
        #endregion IFields
        #region IConstructors
        
        /// <summary>
        /// construct a client request info based on the ClientRequest data.        
        /// </summary>
        /// <remarks>delegates client requests normally to the serverRequest instance.</remarks>
        internal ClientRequestInfoImpl(GiopClientRequest clientRequest) {
            m_clientRequest = clientRequest;
        }
        
        #endregion IConstructors
        #region IProperties
        

        public MarshalByRefObject target {
            get {
                throw new NotImplementedException();
            }
        }
                
        public MarshalByRefObject effective_target {
            get {
                throw new NotImplementedException();
            }
        }

        
        #endregion IProperties
        #region IMethods
        
        /// <summary>sets the received exception to the given one.</summary>
        internal void SetReceivedException(Exception ex) {
            m_receivedException = ex;    
        }
        
        #endregion IMethods
        
    }
    
    /// <summary>
    /// implementation of ServerRequestInfo interface used for client side interception
    /// </summary>    
    internal class ServerRequestInfoImpl : RequestInfoImpl, ServerRequestInfo {
    
        #region IFields
        
        private GiopServerRequest m_serverRequest;
        private Exception m_sentException = null;
        
        #endregion IFields
        #region IConstructors
        
        /// <summary>
        /// construct a server request info based on the ServerRequest data.        
        /// </summary>
        /// <remarks>delegates client requests normally to the serverRequest instance.</remarks>
        internal ServerRequestInfoImpl(GiopServerRequest serverRequest) {
            m_serverRequest = serverRequest;
        }
        
        #endregion IConstructors
        #region IProperties
        
        /// <summary>the opaque id, describing the target of the operation invocation.</summary>
        public byte[] object_id {
            get {
                throw new NotImplementedException();
            }
        }                
        
        #endregion IProperties
        #region IMethods
        
        /// <summary>sets the sent exception to the given one.</summary>
        internal void SetSentException(Exception ex) {
            m_sentException = ex;
        }
        
        #endregion IMethods
        
    }
    
    
    /// <summary>
    /// implementation of IORInfo interface used for ior interception.
    /// </summary>
    internal class IORInfoImpl : IORInfo {
    
        #region IFields
        
        private InternetIiopProfile[] m_profiles;
        
        #endregion IFields
        #region IConstructors
        
        public IORInfoImpl(InternetIiopProfile[] profiles) {
            m_profiles = profiles;
        }
        
        public IORInfoImpl(InternetIiopProfile profile) : this(new InternetIiopProfile[] { profile }) {            
        }
        
        #endregion IConstructors
        #region IMethods
                
        
        #endregion IMethods
        
    }
    
}
