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
        
        #region Types
        
        /// <summary>Encapsulates a connections, used for connection management</summary>
        private class ConnectionDescription {
            
            #region IFields

            private GiopClientConnection m_connection;
            private DateTime m_lastUsed;
            private bool m_isAllowedToBeClosed;
            private bool m_isInUse;            

            #endregion IFields
            #region IConstructors

            public ConnectionDescription(GiopClientConnection connection, bool isAllowedToBeClosed) {
                m_lastUsed = DateTime.Now;
                m_isAllowedToBeClosed = isAllowedToBeClosed;
                m_connection = connection;                
            }

            #endregion IConstructors
            #region IProperties

            /// <summary>
            /// the encapsulated connection
            /// </summary>
            public GiopClientConnection Connection {
                get { 
                    return m_connection; 
                }
                set { 
                    m_connection = value; 
                }
            }
            
            /// <summary>
            /// can this connection be closed
            /// </summary>
            public bool IsAllowedToBeClosed {
                get {
                    return m_isAllowedToBeClosed;
                }
            }
            
            /// <summary>
            /// is this connection currenty used for a sending/receiving a giop message.
            /// </summary>
            public bool IsInUse {
                get {
                    return m_isInUse;
                }
                set {
                    m_isInUse = value;
                }
            }

            #endregion IProperties
            #region IMethods
            
            /// <summary>
            /// returns ture, if the connection is not use for at least the specified time; otherwise false.
            /// </summary>
            public bool IsNotUsedForAtLeast(TimeSpan idleTime) {
                return (m_lastUsed + idleTime < DateTime.Now);
            }
            
            public bool CanBeClosedAsIdle(TimeSpan idleTime) {
                return (IsNotUsedForAtLeast(idleTime) && IsAllowedToBeClosed &&
                        !IsInUse);
            }
            
            /// <summary>
            /// updates the time, this connection has been used last.
            /// </summary>
            public void UpdateLastUsedTime() {
                m_lastUsed = DateTime.Now;
            }
            
            #endregion IMethods

        }
        
        #endregion Types
        #region IFields
        
        private IClientTransportFactory m_transportFactory;

        /// <summary>contains all connections opened by the client; key is the target of the connection; 
        /// value is the ConnectionDesc instance</summary>
        private Hashtable m_allClientConnections /* target, ConnectionDescription */ = new Hashtable();
    	
    	/// <summary>
    	///  contains the allocated connections. key is the message, which will be sent
    	/// with the connection, value is a ConnectionDesc instance
    	/// </summary>
    	private Hashtable /* IMessage, ConnectionDescription */ m_allocatedConnections = new Hashtable();
    	
    	private MessageTimeout m_requestTimeOut;
    	
    	private Timer m_destroyTimer;
    	private TimeSpan m_connectionLifeTime;
        
        #endregion IFields
        #region IConstructors                        
        
        internal GiopClientConnectionManager(IClientTransportFactory transportFactory, MessageTimeout requestTimeOut,
                                             int unusedKeepAliveTime) {
            Initalize(transportFactory, requestTimeOut, unusedKeepAliveTime);
        }
        
        internal GiopClientConnectionManager(IClientTransportFactory transportFactory, int unusedKeepAliveTime) :
            this(transportFactory, MessageTimeout.Infinite, unusedKeepAliveTime) {
        }
        
        #endregion IConstructors
        
        ~GiopClientConnectionManager() {
            CleanUp();
        }
        
        #region IMethods
        
        private void Initalize(IClientTransportFactory transportFactory, MessageTimeout requestTimeOut,
                               int unusedKeepAliveTime) {
            m_transportFactory = transportFactory;
            m_requestTimeOut = requestTimeOut;
            if (unusedKeepAliveTime != Timeout.Infinite) {
                m_connectionLifeTime = TimeSpan.FromMilliseconds(unusedKeepAliveTime);
                TimerCallback timerDelegate = new TimerCallback(DestroyUnusedConnections);
                // Create a timer which invokes the session destroyer every unusedKeepAliveTime
                m_destroyTimer = new Timer(timerDelegate, null, 2 * unusedKeepAliveTime, unusedKeepAliveTime);
            }
        }
        
        
        public void Dispose() {
            CleanUp();
            GC.SuppressFinalize(this);
        }
        
        private void CleanUp() {
            if (m_destroyTimer != null) {
                m_destroyTimer.Dispose();
                m_destroyTimer = null;
            }
            CloseAllConnections();
        }
        
        /// <summary>checks, if availabe connections contain one, which is usable</summary>
        /// <returns>the connection, if found, otherwise null.</returns>
        private ConnectionDescription GetFromAvailable(string connectionKey) {
            ConnectionDescription result = null;
            ConnectionDescription con = (ConnectionDescription)m_allClientConnections[connectionKey];
            if (con != null) {
                if (con.Connection.CheckConnected() && (con.Connection.Desc.ReqNumberGen.IsAbleToGenerateNext())) {
                    result = con;
                } else {
                    try {
                        con.Connection.CloseConnection();
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
        
        private ConnectionDescription CreateAndRegisterNewConnection(Ior target, string targetKey) {
            ConnectionDescription result;
            IClientTransport transport =
                m_transportFactory.CreateTransport(target);
            // already open connection here, because GetConnectionFor 
            // should returns an open connection (if not closed meanwhile)
            transport.OpenConnection();
            result = new ConnectionDescription(
                         new GiopClientConnection(targetKey, transport, m_requestTimeOut),
                         true);
            m_allClientConnections[targetKey] = result;
            return result;
        }        
        
        /// <summary>allocation a connection for the message.</summary>
        internal GiopClientConnectionDesc AllocateConnectionFor(IMessage msg, Ior target) {
            ConnectionDescription result = null;
            
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
                    result.IsInUse = true;
                    m_allocatedConnections[msg] = result;
                }
            } else {
                // should not occur
                throw new BAD_PARAM(995,
                                    omg.org.CORBA.CompletionStatus.Completed_No);
            }
            return result.Connection.Desc;
        }
        
        internal void ReleaseConnectionFor(IMessage msg) {
            lock(this) {
                ConnectionDescription connection = 
                    (ConnectionDescription)m_allocatedConnections[msg];

                if (connection == null) {
                    throw new INTERNAL(11111, 
                                       CompletionStatus.Completed_MayBe);
                }
                connection.UpdateLastUsedTime();
                connection.IsInUse = false;
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
    	        return ((ConnectionDescription) m_allocatedConnections[forMessage]).Connection;
    		}
    	}

        
        /// <summary>generates the request id to use for the given message</summary>
        internal uint GenerateRequestId(IMessage msg, GiopClientConnectionDesc allocatedCon) {
            lock(this) {
                return allocatedCon.ReqNumberGen.GenerateRequestId();
            }
        }
                
        private void DestroyUnusedConnections(Object state) {
            lock(this) {
                ArrayList toClose = new ArrayList();
                foreach (DictionaryEntry de in m_allClientConnections) {
                    if (((ConnectionDescription)de.Value).CanBeClosedAsIdle(m_connectionLifeTime)) {
                        toClose.Add(de.Key);
                    }
                }
                foreach (object key in toClose) {
                    ConnectionDescription conDesc = (ConnectionDescription)m_allClientConnections[key];
                    m_allClientConnections.Remove(key);
                    try {
                        conDesc.Connection.CloseConnection();
                    } catch (Exception) {
                        // ignore
                    }
                }                
            }            
        }
        
        private void CloseAllConnections() {
            lock(this) {
                foreach (ConnectionDescription conDesc in m_allClientConnections.Values) {
                    try {
                        conDesc.Connection.CloseConnection();
                    } catch (Exception) {                
                    }
                }
            }
            m_allClientConnections.Clear();
        }
                
        #endregion IMethods
        
    }



}
