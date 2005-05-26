/* Connection.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 30.04.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.Net.Sockets;
using System.IO;
using System.Collections;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using Ch.Elca.Iiop.Services;

namespace Ch.Elca.Iiop {


    /// <summary>
    /// Stores information associated with a GIOP connection,
    /// e.g. the Codesets chosen
    /// </summary>
    public class GiopConnectionDesc {

        #region Constants
        
        internal const string SERVER_TR_HEADER_KEY = "_server_giop_con_desc_";
        internal const string CLIENT_TR_HEADER_KEY = "_client_giop_con_desc_";
        
        #endregion Constants
        #region IFields

        private int m_charSetChosen = CodeSetService.DEFAULT_CHAR_SET;
        private int m_wcharSetChosen = CodeSetService.DEFAULT_WCHAR_SET;
               
        private bool m_codeSetNegotiated = false;
        
        private GiopClientConnectionManager m_conManager;
        private GiopTransportMessageHandler m_transportHandler;

        #endregion IFields
        #region IConstructors

        internal GiopConnectionDesc(GiopClientConnectionManager conManager,
                                   GiopTransportMessageHandler transportHandler) {
            m_conManager = conManager;
            m_transportHandler = transportHandler;
        }

        #endregion IConstructors
        #region IProperties

        public int CharSet {
            get {
                return m_charSetChosen;
            }
        }

        public int WCharSet {
            get {
                return m_wcharSetChosen;
            }
        }
        
        /// <summary>
        /// a client connection manager responsible for client connections (may be null).
        /// </summary>
        internal GiopClientConnectionManager ConnectionManager {
            get {
                return m_conManager;
            }
        }
        
        /// <summary>
        /// the transport handler responsible for the associated connection
        /// </summary>
        internal GiopTransportMessageHandler TransportHandler {
            get {
                if (m_transportHandler != null) {
                    return m_transportHandler;
                } else {
                    throw new omg.org.CORBA.BAD_INV_ORDER(998, omg.org.CORBA.CompletionStatus.Completed_MayBe);
                }
            }
        }
                                
        #endregion IProperties
        #region IMethods
        
        /// <summary>
        /// is the codeset already negotiated?
        /// </summary>
        public bool IsCodeSetNegotiated() {
            return m_codeSetNegotiated;
        }

        /// <summary>
        /// the codeset is already negotiated.
        /// </summary>
        public void SetCodeSetNegotiated() {
            m_codeSetNegotiated = true;
        }
        
        public void SetNegotiatedCodeSets(int charSet, int wcharSet) {
            m_charSetChosen = charSet;
            m_wcharSetChosen = wcharSet;
            SetCodeSetNegotiated();
        }
                
        #endregion IMethods

    }
    
    /// <summary>the connection context for the client side</summary>
    internal class GiopClientConnectionDesc : GiopConnectionDesc {
        
        #region IConstructors
        
        internal GiopClientConnectionDesc(GiopClientConnectionManager conManager, GiopClientConnection connection,
                                          GiopRequestNumberGenerator reqNumberGen, 
                                          GiopTransportMessageHandler transportHandler) : base(conManager, transportHandler) {
            m_reqNumGen = reqNumberGen;
            m_connection = connection;            
        }
        
        #endregion IConstructors
        #region IFields
        
        private GiopRequestNumberGenerator m_reqNumGen;
        
        private GiopClientConnection m_connection;
        
        #endregion IFields
        #region IProperties 
        
        internal GiopRequestNumberGenerator ReqNumberGen {
            get {
                return m_reqNumGen;
            }
        }
        
        internal GiopClientConnection Connection {
            get {
                return m_connection;
            }
        }
                
        #endregion IProporties
    }

    /// <summary>
    /// stores the relevant information of an IIOP client side
    /// connection
    /// </summary>
    internal abstract class GiopClientConnection {

        #region IFields

        private GiopClientConnectionDesc m_assocDesc;

        private string m_connectionKey;
                
        protected GiopTransportMessageHandler m_transportHandler;
        
        #endregion IFields
        #region IConstructors
        
        /// <summary>
        /// used for inheritors, calling initalize themselves.
        /// </summary>
        protected GiopClientConnection() {
        }
                
        #endregion IConstructors
        #region IProperties

        internal GiopClientConnectionDesc Desc {
            get {
                return m_assocDesc;
            }
        }

        internal string ConnectionKey {
            get {
                return m_connectionKey;
            }
        }

        internal GiopTransportMessageHandler TransportHandler {
            get {
                return m_transportHandler;
            }
        }

        #endregion IProperties
        #region IMethods

        protected void Initalize(string connectionKey, GiopTransportMessageHandler transportHandler,
                                 GiopClientConnectionDesc assocDesc) {
            m_connectionKey = connectionKey;
            m_assocDesc = assocDesc;            
            m_transportHandler = transportHandler;            
        }

        internal bool CheckConnected() {            
            return m_transportHandler.Transport.IsConnectionOpen();
        }
        
        /// <summary>
        /// is this connection closable in client role.
        /// </summary>        
        internal abstract bool CanCloseConnection();

        /// <summary>
        /// closes the connection.
        /// </summary>
        /// <remarks>this method must only be called by the ConnectionManager.</remarks>
        internal abstract void CloseConnection();
        
        /// <summary>
        /// is this connection initiated in this appdomain.
        /// </summary>
        internal abstract bool IsInitiatedLocal();
        
        #endregion IMethods

    
    }
    
    
    /// <summary>
    /// a connection, which is initiated in the current appdomain.
    /// </summary>
    internal class GiopClientInitiatedConnection : GiopClientConnection {
        
        #region IConstructors

        /// <param name="connectionKey">the key describing the connection</param>
        /// <param name="transport">an already connected client transport</param>
        internal GiopClientInitiatedConnection(string connectionKey, IClientTransport transport,
                                               MessageTimeout requestTimeOut, GiopClientConnectionManager conManager,
                                               bool supportBidir) {            
            GiopRequestNumberGenerator reqNumberGen =
                (!supportBidir ? new GiopRequestNumberGenerator() : new GiopRequestNumberGenerator(true));
            GiopTransportMessageHandler handler =
                      new GiopTransportMessageHandler(transport, requestTimeOut);
            GiopClientConnectionDesc conDesc = new GiopClientConnectionDesc(conManager, this, reqNumberGen, handler);
            Initalize(connectionKey, handler, conDesc);
            handler.StartMessageReception(); // begin listening for messages
        }                
        
        #endregion IConstructors
        #region IMethods
        
        internal override void CloseConnection() {
            try {
                m_transportHandler.ForceCloseConnection();
            } catch (Exception ex) {
                Debug.WriteLine("exception while closing connection: " + ex);
            }            
        }        
        
        internal override bool CanCloseConnection() {
            return true;
        }
        
        internal override bool IsInitiatedLocal() {
            return true;
        }
        
        
        #endregion IMethods
        
    }
    
    
    /// <summary>
    /// a connection, which is initiated in another appdomain. This connection is used in bidir mode
    /// for callback.
    /// </summary>    
    internal class GiopBidirInitiatedConnection : GiopClientConnection {
        
        #region IConstructors

        /// <param name="connectionKey">the key describing the connection</param>        
        internal GiopBidirInitiatedConnection(string connectionKey, GiopTransportMessageHandler transportHandler, 
                                              GiopClientConnectionManager conManager) {
            GiopRequestNumberGenerator reqNumberGen = 
                    new GiopRequestNumberGenerator(false); // not connection originator -> create non-even req. numbers
            GiopClientConnectionDesc conDesc = new GiopClientConnectionDesc(conManager, this, reqNumberGen,
                                                                            transportHandler);
            Initalize(connectionKey, transportHandler, conDesc);
        }                                
        
        #endregion IConstructors
        #region IMethods
        
        internal override void CloseConnection() {
            throw new omg.org.CORBA.BAD_OPERATION(765, omg.org.CORBA.CompletionStatus.Completed_MayBe);
        }        
        
        internal override bool CanCloseConnection() {
            return false;
        }
        
        internal override bool IsInitiatedLocal() {
            return false;
        }        
        
        #endregion IMethods
        
    }


}
