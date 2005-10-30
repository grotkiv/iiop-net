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

        #endregion IFields
        #region IConstructors

        internal GiopConnectionDesc() {
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
        
        #region IFields
        
        private GiopRequestNumberGenerator m_reqNumGen = 
                    new GiopRequestNumberGenerator();
        
        #endregion IFields
        #region IProperties 
        
        internal GiopRequestNumberGenerator ReqNumberGen {
            get {
                return m_reqNumGen;
            }
        }
                
        #endregion IProporties
    }

    /// <summary>
    /// stores the relevant information of an IIOP client side
    /// connection
    /// </summary>
    internal class GiopClientConnection {

        #region IFields

        private GiopClientConnectionDesc m_assocDesc;

        private string m_connectionKey;
                
        private GiopTransportMessageHandler m_transportHandler;
        
        private bool m_isBidir;

        #endregion IFields
        #region IConstructors

        /// <param name="connectionKey">the key describing the connection</param>
        /// <param name="transport">an already connected client transport</param>
        internal GiopClientConnection(string connectionKey, IClientTransport transport,
                                      MessageTimeout requestTimeOut) {            
            Initalize(connectionKey, new GiopTransportMessageHandler(transport, requestTimeOut), false);
        }
        
        /// <param name="connectionKey">the key describing the connection</param>
        /// <param name="transport">an already connected client transport</param>
        internal GiopClientConnection(string connectionKey, IClientTransport transport) : 
            this(connectionKey, transport, MessageTimeout.Infinite) {
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

        private void Initalize(string connectionKey, GiopTransportMessageHandler transportHandler,
                               bool isBidir) {
            m_connectionKey = connectionKey;
            m_assocDesc = new GiopClientConnectionDesc();            
            m_transportHandler = transportHandler;            
            m_transportHandler.StartMessageReception(); // begin listening for messages
            m_isBidir = isBidir;
        }

        internal bool CheckConnected() {            
            return m_transportHandler.Transport.IsConnectionOpen();
        }

        /// <summary>
        /// closes the connection.
        /// </summary>
        /// <remarks>this method must only be called by the ConnectionManager.</remarks>
        internal void CloseConnection() {
            try {
                m_transportHandler.ForceCloseConnection();
            } catch (Exception ex) {
                Debug.WriteLine("exception while closing connection: " + ex);
            }            
        }

        #endregion IMethods

    
    }


}