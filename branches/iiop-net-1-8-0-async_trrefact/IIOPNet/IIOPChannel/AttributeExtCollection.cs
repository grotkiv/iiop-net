/* AttributeExtCollection.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 14.01.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;
using Ch.Elca.Iiop.Idl;

namespace Ch.Elca.Iiop.Util {

    /// <summary>
    /// A more powerful Attribute collection than AttributeCollection.
    /// </summary>
    public class AttributeExtCollection : ICollection {
        
        #region SFields
        
        private static AttributeExtCollection s_emptyCollection = new AttributeExtCollection();
        
        #endregion
        #region IFields
        
        private object[] m_attributes;
        
        #endregion IFields
        #region IConstructors

        public AttributeExtCollection() {
            m_attributes = new object[0];
        }
        
        public AttributeExtCollection(Attribute[] attrs) {
            m_attributes = new object[attrs.Length];
            attrs.CopyTo(m_attributes, 0);
        }

        public AttributeExtCollection(AttributeExtCollection coll) {
            m_attributes = (object[])coll.m_attributes.Clone();
        }
        
        private AttributeExtCollection(object[] content) {
            m_attributes = content;
        }

        #endregion IConstructors
        #region SProperties
        
        public static AttributeExtCollection EmptyCollection {
            get {
                return s_emptyCollection;
            }
        }
        
        #endregion SProperties
        #region IProperties
        
        public bool IsSynchronized {
            get { 
                return false; 
            }
        }

        public int Count {
            get {
                return m_attributes.Length;
            }
        }

        public object SyncRoot {
            get { 
                return m_attributes.SyncRoot; 
            }
        }
        
        #endregion IProperties
        #region SMethods
        
        /// <summary>
        /// creates an AttibuteExtCollection containing all Attributes in attrs
        /// </summary>
        public static AttributeExtCollection ConvertToAttributeCollection(object[] attrs) {
            if ((attrs != null) && (attrs.Length > 0)) {
                return new AttributeExtCollection(attrs);
            } else {
                return EmptyCollection;
            }
        }

        #endregion
        #region IMethods
    
        public bool Contains(Attribute attr) {
            for (int i = 0; i < m_attributes.Length; i++) {
                if (m_attributes[i].Equals(attr)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// check, if an attribute of the given type is in the collection
        /// </summary>
        public bool IsInCollection(Type attrType) {
            for (int i = 0; i < m_attributes.Length; i++) {
                Attribute attr = (Attribute)m_attributes[i];
                if (attr.GetType() == attrType) { 
                    return true; 
                }
            }
            return false;
        }

        /// <summary>
        /// returns the first attribute in the collection, which is of the specified type
        /// </summary>
        /// <remarks>
        /// for attributes implementing IOrderedAttribute, this returns the attribute
        /// with the highest order number
        /// </remarks>
        public Attribute GetAttributeForType(Type attrType) {
            Attribute result = null;
            bool isOrdered = false;
            if (ReflectionHelper.IOrderedAttributeType.IsAssignableFrom(attrType)) {
                isOrdered = true;
            }
            for (int i = 0; i < m_attributes.Length; i++) {            
                Attribute attr = (Attribute)m_attributes[i];
                if (attr.GetType() == attrType) { 
                    if (!isOrdered) {
                        result = attr;
                        break;
                    } else {
                        if ((result == null) ||
                           (((IOrderedAttribute)result).OrderNr <
                            ((IOrderedAttribute)attr).OrderNr)) {
                            result = attr;        
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// removes the first attribute of the given type
        /// </summary>
        /// <remarks>
        /// for attributes implementing IOrderedAttribute, this removes the attribute
        /// with the highest order number
        /// </remarks>
        /// <returns>The removed attribute, or null if not found</returns>
        public AttributeExtCollection RemoveAttributeOfType(Type attrType, out Attribute foundAttr) {
            foundAttr = GetAttributeForType(attrType);
            if (foundAttr != null) {
                object[] newCollection = new object[m_attributes.Length - 1]; // m_attributes.Length must be >= 0, because attr found
                int newCollectionIndex = 0;
                for (int i = 0; i < m_attributes.Length; i++) {
                    if (m_attributes[i] != foundAttr) {
                        newCollection[newCollectionIndex] = m_attributes[i];
                        newCollectionIndex++;
                    }
                }                
                return new AttributeExtCollection(newCollection);
            } else {
                return this;
            }
        }

        /// <summary>
        /// insert the attribute in the collection at the first position
        /// </summary>
        public AttributeExtCollection MergeAttribute(Attribute attr) {
            object[] newAttributes = new object[m_attributes.Length + 1];
            newAttributes[0] = attr;
            m_attributes.CopyTo(newAttributes, 1);
            return new AttributeExtCollection(newAttributes);
        }
        
        /// <summary>
        /// returns an attribute collection produced by merging this collection and the argument collection.
        /// </summary>
        public AttributeExtCollection MergeAttributeCollections(AttributeExtCollection coll) {
            object[] resultList = new object[coll.m_attributes.Length + m_attributes.Length];            
            // first the new ones
            coll.m_attributes.CopyTo(resultList, 0);
            // append content of this collection
            m_attributes.CopyTo(resultList, coll.m_attributes.Length);
            return new AttributeExtCollection(resultList);
        }

        public AttributeExtCollection MergeMissingAttributes(object[] toAdd) {
            ArrayList resultList = new ArrayList(m_attributes);
            foreach (Attribute attr in toAdd) {
                if (!resultList.Contains(attr)) {
                    resultList.Insert(0, attr);
                }
            }
            return new AttributeExtCollection(resultList.ToArray());
        }

        public override bool Equals(object obj) {
            if (obj == null) { return false; }
            if (!(obj.GetType().Equals(typeof(AttributeExtCollection)))) { return false; }

            IEnumerator enumerator = GetEnumerator();
            while (enumerator.MoveNext()) {
                Attribute attr = (Attribute) enumerator.Current;
                if (!((AttributeExtCollection)obj).Contains(attr)) { return false; }
            }

            enumerator = ((AttributeExtCollection)obj).GetEnumerator();
            while (enumerator.MoveNext()) {
                Attribute attr = (Attribute) enumerator.Current;
                if (!(Contains(attr))) { return false; }
            }
            return true;
        }

        public override int GetHashCode() {
            int result = 0;
            for (int i = 0; i < m_attributes.Length; i++) {
                result = result ^ m_attributes[i].GetHashCode();
            }
            return result;
        }

        #region Implementation of ICollection

        public void CopyTo(System.Array array, int index) {
            m_attributes.CopyTo(array, index);
        }

        #endregion Implementation of ICollection
        #region Implementation of IEnumerable

        public System.Collections.IEnumerator GetEnumerator() {
            return m_attributes.GetEnumerator();
        }

        #endregion Implementation of IEnumerable

        /// <summary>
        /// get attribute at position index
        /// </summary>
        public Attribute GetAttributeAt(int index) {
            return (Attribute)m_attributes[index];
        }

        #endregion IMethods
        
    }
}
