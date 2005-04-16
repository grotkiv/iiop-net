/* TaggedComponent.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 16.04.05  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using Ch.Elca.Iiop.Cdr;
using Ch.Elca.Iiop.Idl;


namespace omg.org.IOP {       
    
    [IdlStruct]
    public struct TaggedComponent {
    
        #region IFields
        
        private int m_tag;
        [IdlSequence(0L)]
        private byte[] m_component_data;
        
        #endregion IFields
        #region IConstructors
        
        public TaggedComponent(int tag, byte[] component_data) {
            m_tag = tag;
            m_component_data = component_data;
        }
        
        /// <summary>
        /// deserialise from input stream
        /// </summary>
        internal TaggedComponent(CdrInputStream inputStream) {
            m_tag = (int)inputStream.ReadULong();
            int componentDataLength = (int)inputStream.ReadULong();
            m_component_data = inputStream.ReadOpaque(componentDataLength);
        }
        
        #endregion IConstructors
        #region IProperties
        
        public int Tag {
            get {
                return m_tag;
            }
        }
        
        public byte[] ComponentData {
            get {
                return m_component_data;
            }
        }                        
        
        #endregion IProperties
        #region IMethods
        
        /// <summary>
        /// serialise the service context
        /// </summary>
        internal void Write(CdrOutputStream outputStream) {
            outputStream.WriteULong((uint)m_tag);
            outputStream.WriteULong((uint)m_component_data.Length);
            outputStream.WriteOpaque(m_component_data);
        }
        
        #endregion IMethods
        
    }
    
    
    /// <summary>
    /// This class represents the collection of tagged components in an IOR
    /// </summary>    
    internal class TaggedComponentList {
        
        #region SFields
        
        private static TaggedComponent[] s_emptyList = new TaggedComponent[0];
        
        #endregion SFields
        #region IFields
        
        private TaggedComponent[] m_components;
        
        #endregion IFields
        #region IConstructors
        
        internal TaggedComponentList(params TaggedComponent[] components) {
            m_components = new TaggedComponent[components.Length];
            Array.Copy(components, m_components, components.Length);
        }
        
        internal TaggedComponentList() {
            m_components = s_emptyList;
        }
        
        /// <summary>
        /// deserialise a service context from 
        /// </summary>
        /// <param name="inputStream"></param>
        internal TaggedComponentList(CdrInputStream inputStream) {
            ReadTaggedComponenttList(inputStream);
        }
        
        #endregion IConstructors
        #region IMethods
        
        private void ReadTaggedComponenttList(CdrInputStream inputStream) {
            int nrOfComponents = (int)inputStream.ReadULong();
            m_components = new TaggedComponent[nrOfComponents];
            for (int i = 0; i < nrOfComponents; i++) {
                TaggedComponent component = new TaggedComponent(inputStream);
                m_components[i] = component;
            }
        }
        
        internal void WriteTaggedComponentList(CdrOutputStream outputStream) {
            outputStream.WriteULong((uint)m_components.Length);
            for (int i = 0; i < m_components.Length; i++) {                
                m_components[i].Write(outputStream);
            }
        }
        
        /// <summary>
        /// is a tagged component present for the given id.
        /// </summary>
        public bool ContainsTaggedComponent(int tag) {
            for (int i = 0; i < m_components.Length; i++) {
                if (m_components[i].Tag == tag) {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// serialise the given component data and adds it to the list of components.
        /// </summary>
        public void AddComponent(int tag, object data) {
            TaggedComponent[] result = new TaggedComponent[m_components.Length + 1];
            Array.Copy(m_components, result, m_components.Length);
            // result[m_components.Length] = null; // TODO: serialise
            m_components = result;
        }
        
        /// <summary>
        /// returns the deserialised data of the first component with the given tag or null, if not found.
        /// </summary>
        public object GetComponent(int tag, Type componentDataType) {
            object result = null;            
            for (int i = 0; i < m_components.Length; i++) {
                if (m_components[i].Tag == tag) {
                    TaggedComponent resultComp = m_components[i];
                    // TODO deserialise
                    break;
                }                            
            }
            return result;
        }        
        
        #endregion IMethods
        
    }    

    
}
