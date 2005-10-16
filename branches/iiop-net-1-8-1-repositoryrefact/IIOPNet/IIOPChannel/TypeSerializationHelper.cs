/* TypeSerializationHelper.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 04.10.05  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.Collections;
using Ch.Elca.Iiop.Cdr;
using Ch.Elca.Iiop.Util;

namespace Ch.Elca.Iiop.Marshalling {

    /// <summary>
    /// interface for generated type serialization helpers
    /// </summary>
    [CLSCompliant(false)]
    public abstract class TypeSerializationHelper {
        
        #region SFields
        
        public static readonly Type ClassType = typeof(TypeSerializationHelper);
        
        private static ObjRefSerializer s_objRefSerializer = new ObjRefSerializer();
        private static AnySerializer s_anySerializer = new AnySerializer();
        private static TypeCodeSerializer s_tcSerializer = new TypeCodeSerializer();
        private static TypeSerializer s_typeSerializer = new TypeSerializer();
        private static AbstractInterfaceSerializer s_abstractIfSerializer = new AbstractInterfaceSerializer();
        private static AbstractValueSerializer s_abstractVtSerializer = new AbstractValueSerializer();
        
        #endregion SFields
        #region IMethods
                
        public abstract void SerializeInstance(object actual, CdrOutputStream targetStream);
                
        public abstract object DeserializeInstance(CdrInputStream sourceStream);
        
        
        protected void SerialiseObjRef(Type formal, object actual,
                                       CdrOutputStream targetStream) {            
            s_objRefSerializer.Serialise(formal, actual, AttributeExtCollection.EmptyCollection, targetStream);
        }
        
        protected object DeserialiseObjRef(Type formal,
                                           CdrInputStream sourceStream) {            
            return s_objRefSerializer.Deserialise(formal, AttributeExtCollection.EmptyCollection, sourceStream);
        }
        
        protected void SerialiseAny(Type formal, object actual,
                                    CdrOutputStream targetStream) {            
            s_anySerializer.Serialise(formal, actual, AttributeExtCollection.EmptyCollection, targetStream);
        }
        
        protected object DeserialiseAny(Type formal,
                                        CdrInputStream sourceStream) {
            return s_anySerializer.Deserialise(formal, AttributeExtCollection.EmptyCollection, sourceStream);
        }        
        
        protected void SerialiseTC(Type formal, object actual,
                                    CdrOutputStream targetStream) {            
            s_tcSerializer.Serialise(formal, actual, AttributeExtCollection.EmptyCollection, targetStream);
        }
        
        protected object DeserialiseTC(Type formal,
                                        CdrInputStream sourceStream) {
            return s_tcSerializer.Deserialise(formal, AttributeExtCollection.EmptyCollection, sourceStream);
        }                
        
        protected void SerialiseType(Type formal, object actual,
                                     CdrOutputStream targetStream) {            
            s_typeSerializer.Serialise(formal, actual, AttributeExtCollection.EmptyCollection, targetStream);
        }
        
        protected object DeserialiseType(Type formal,
                                         CdrInputStream sourceStream) {
            return s_typeSerializer.Deserialise(formal, AttributeExtCollection.EmptyCollection, sourceStream);
        }                        
        
        protected void SerialiseAbstractIf(Type formal, object actual,
                                           CdrOutputStream targetStream) {            
            s_abstractIfSerializer.Serialise(formal, actual, AttributeExtCollection.EmptyCollection, targetStream);
        }
        
        protected object DeserialiseAbstractIf(Type formal,
                                               CdrInputStream sourceStream) {
            return s_abstractIfSerializer.Deserialise(formal, AttributeExtCollection.EmptyCollection, sourceStream);
        }                                

        protected void SerialiseAbstractVt(Type formal, object actual,
                                           CdrOutputStream targetStream) {            
            s_abstractVtSerializer.Serialise(formal, actual, AttributeExtCollection.EmptyCollection, targetStream);
        }
        
        protected object DeserialiseAbstractVt(Type formal,
                                               CdrInputStream sourceStream) {
            return s_abstractVtSerializer.Deserialise(formal, AttributeExtCollection.EmptyCollection, sourceStream);
        }                
        
        #endregion IMethods
        
    }
    
}
