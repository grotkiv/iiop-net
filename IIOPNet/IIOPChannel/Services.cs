/* Services.cs
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
using System.Diagnostics;
using Ch.Elca.Iiop.Cdr;
using Ch.Elca.Iiop.Idl;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using Ch.Elca.Iiop.CorbaObjRef;


namespace omg.org.IOP {
    
    [IdlStruct]
    public struct ServiceContext {
    
        #region IFields
        
        private int m_serviceId;
        [IdlSequence(0L)]
        private byte[] m_context_data;
        
        #endregion IFields
        #region IConstructors
        
        public ServiceContext(int serviceId, byte[] context_data) {
            m_serviceId = serviceId;
            m_context_data = context_data;
        }
        
        /// <summary>
        /// deserialise from input stream
        /// </summary>
        internal ServiceContext(CdrInputStream inputStream) {
            m_serviceId = (int)inputStream.ReadULong();
            int contextDataLength = (int)inputStream.ReadULong();
            m_context_data = inputStream.ReadOpaque(contextDataLength);
        }
        
        #endregion IConstructors
        #region IProperties
        
        public int ServiceId {
            get {
                return m_serviceId;
            }
        }
        
        public byte[] ContextData {
            get {
                return m_context_data;
            }
        }                        
        
        #endregion IProperties
        #region IMethods
        
        /// <summary>
        /// serialise the service context
        /// </summary>
        internal void Write(CdrOutputStream outputStream) {
            outputStream.WriteULong((uint)m_serviceId);
            outputStream.WriteULong((uint)m_context_data.Length);
            outputStream.WriteOpaque(m_context_data);
        }
        
        #endregion IMethods
        
    }

    /// <summary>
    /// This class represents the collection of service contexts in request / response messages
    /// </summary>    
    internal class ServiceContextList {
        
        #region IConstructors
        
        internal ServiceContextList() {            
        }
        
        /// <summary>
        /// deserialise a service context from 
        /// </summary>
        /// <param name="inputStream"></param>
        internal ServiceContextList(CdrInputStream inputStream) {
            ReadSvcContextList(inputStream);
        }
        
        #endregion IConstructors
        #region IFields
        
        private Hashtable /* int, ServiceContext */ m_contexts = new Hashtable();
        
        #endregion IFields
        #region IMethods
        
        private void ReadSvcContextList(CdrInputStream inputStream) {
            uint nrOfServiceContexts = inputStream.ReadULong();
            for (int i = 0; i < nrOfServiceContexts; i++) {
                ServiceContext context = new ServiceContext(inputStream);
                if (!m_contexts.Contains(context.ServiceId)) {
                    m_contexts[context.ServiceId] = context;
                } else {
                    // ignore multiple contexts with same id; iop interfaces allow access by id
                    Debug.WriteLine("ignoring duplicate context with id: " + context.ServiceId);
                }
            }
        }
        
        internal void WriteSvcContextList(CdrOutputStream outputStream) {
            outputStream.WriteULong((uint)m_contexts.Count);
            foreach (DictionaryEntry entry in m_contexts) {
                ServiceContext context = (ServiceContext)entry.Value;
                context.Write(outputStream);
            }
        }
        
        /// <summary>
        /// is a service context present for the given id.
        /// </summary>
        public bool ContainsServiceContext(int svcContextId) {
            return m_contexts[svcContextId] != null;
        }
        
        /// <summary>
        /// get the service context for the given id.
        /// </summary>
        public ServiceContext GetServiceContext(int svcContextId) {
            return (ServiceContext)m_contexts[svcContextId];
        }
        
        /// <summary>
        /// add a service context to the list.
        /// </summary>
        public void AddServiceContext(ServiceContext context) {
            m_contexts[context.ServiceId] = context;
        }
        
        #endregion IMethods
        
    }
    
}
