/* ConnectionManager.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 28.04.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.IO;
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using omg.org.CORBA;

using Ch.Elca.Iiop.Services;
using Ch.Elca.Iiop.Util;
using Ch.Elca.Iiop.CorbaObjRef;

namespace Ch.Elca.Iiop {


    /// <summary>this class manages outgoing client side connections</summary>
    internal class GiopClientConnectionManager : IDisposable {
        
        #region IFields
        
        private IClientTransportFactory m_transportFactory;

        /// <summary>contains all connections opened by the client; key is the target of the connection; value is the connection</summary>
        private Hashtable m_allClientConnections = new Hashtable();
    	
    	/// <summary>
    	///  contains the allocated connections. key is the message, which will be sent
    	/// with the connection
    	/// </summary>
    	private Hashtable m_allocatedConnections = new Hashtable();
    	
    	private MessageTimeout m_requestTimeOut;
        
        #endregion IFields
        #region IConstructors                        
        
        internal GiopClientConnectionManager(IClientTransportFactory transportFactory, MessageTimeout requestTimeOut) {
            Initalize(transportFactory, requestTimeOut);
        }
        
        internal GiopClientConnectionManager(IClientTransportFactory transportFactory) :
            this(transportFactory, new MessageTimeout()) {
        }
        
        #endregion IConstructors
        
        ~GiopClientConnectionManager() {
            CleanUp();
        }
        
        #region IMethods
        
        private void Initalize(IClientTransportFactory transportFactory, MessageTimeout requestTimeOut) {
            m_transportFactory = transportFactory;
            m_requestTimeOut = requestTimeOut;
        }
        
        
        public void Dispose() {
            CleanUp();
            GC.SuppressFinalize(this);
        }
        
        private void CleanUp() {
            CloseAllConnections();
        }
        
        /// <summary>checks, if availabe connections contain one, which is usable</summary>
        /// <returns>the connection, if found, otherwise null.</returns>
        private GiopClientConnection GetFromAvailable(string connectionKey) {
            GiopClientConnection result = null;
            GiopClientConnection con = (GiopClientConnection)m_allClientConnections[connectionKey];
            if (con != null) {
                if (con.CheckConnected() && (con.Desc.ReqNumberGen.IsAbleToGenerateNext())) {
                    result = con;
                } else {
                    try {
                        con.CloseConnection();
                    } catch (Exception) {                            
                    } finally {
                        m_allClientConnections.Remove(connectionKey);
                    }
                }                
            }
            return result;
        }
        
        /// <summary>
        /// checks, if this connection manager is able to build up a connection to the given target ior
        /// </summary>
        internal bool CanConnectToIor(Ior target) {            
            return m_transportFactory.CanCreateTranporForIor(target);
        }
        
        private GiopClientConnection CreateAndRegisterNewConnection(Ior target, string targetKey) {
            GiopClientConnection result;
            IClientTransport transport =
                m_transportFactory.CreateTransport(target);
            // already open connection here, because GetConnectionFor 
            // should returns an open connection (if not closed meanwhile)
            transport.OpenConnection();
            result = new GiopClientConnection(targetKey, transport, m_requestTimeOut);
            m_allClientConnections[targetKey] = result;
            return result;
        }        
        
        /// <summary>allocation a connection for the message.</summary>
        internal GiopClientConnectionDesc AllocateConnectionFor(IMessage msg, Ior target) {
            GiopClientConnection result = null;
            
            if (target != null) {
                string targetKey = m_transportFactory.GetEndpointKey(target);
                if (targetKey == null) {
                    throw new BAD_PARAM(1178, CompletionStatus.Completed_MayBe);
                }
                lock(this) {
                    result = GetFromAvailable(targetKey);

                    // if no usable connection, create new one
                    if (result == null) {
                        result = CreateAndRegisterNewConnection(target, targetKey);
                    }
                    m_allocatedConnections[msg] = result;
                }
            } else {
                // should not occur
                throw new BAD_PARAM(995,
                                    omg.org.CORBA.CompletionStatus.Completed_No);
            }
            return result.Desc;
        }
        
        internal void ReleaseConnectionFor(IMessage msg) {
            lock(this) {
                GiopClientConnection connection = 
                    (GiopClientConnection)m_allocatedConnections[msg];

                if (connection == null) {
                    throw new INTERNAL(11111, 
                                       CompletionStatus.Completed_MayBe);
                }
                // remove from allocated connections
                m_allocatedConnections.Remove(msg);
            }
        }
        
        /// <summary>get the reserved connection for the message forMessage</summary>
    	/// <remarks>Prescondition: AllocateConnectionFor is already called for msg</remarks>
    	/// <returns>a client connection; for connection oriented transports, 
    	/// the transport has already been connected by the con-manager.</returns>
    	internal GiopClientConnection GetConnectionFor(IMessage forMessage) {
    		lock(this) {
    			return (GiopClientConnection) m_allocatedConnections[forMessage];
    		}
    	}

        
        /// <summary>generates the request id to use for the given message</summary>
        internal uint GenerateRequestId(IMessage msg, GiopClientConnectionDesc allocatedCon) {
            lock(this) {
                return allocatedCon.ReqNumberGen.GenerateRequestId();
            }
        }
        
        private void CloseAllConnections() {
            lock(this) {
                foreach (GiopClientConnection con in m_allClientConnections.Values) {
                    try {
                        con.CloseConnection();
                    } catch (Exception) {                
                    }
                }
            }
            m_allClientConnections.Clear();
        }
                
        #endregion IMethods
        
    }



}
