/* ArgumentsSerializer.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 02.10.05  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.Reflection;
using System.Collections;
using Ch.Elca.Iiop.Cdr;

namespace Ch.Elca.Iiop.Marshalling {

    /// <summary>
    /// interface for generated arguments serializers
    /// </summary>
    [CLSCompliant(false)]
    public abstract class ArgumentsSerializer {
        
        #region Constants
        
        public const string CACHED_METHOD_INFO_PREFIX = "s_mi_";
        
        #endregion Constants
        #region SFields
        
        public static readonly Type ClassType = typeof(ArgumentsSerializer);
        
        public static MethodInfo GET_METHOD_INFO_FOR_INTERNAL_MI =
            typeof(ArgumentsSerializer).GetMethod("GetMethodInfoForInternal", 
                                                  BindingFlags.NonPublic | BindingFlags.Static);
        
        #endregion SFields
        #region IMethods                        
        
        public abstract void SerializeRequestArgs(string targetMethod, object[] actual, CdrOutputStream targetStream);
        
        public abstract object[] DeserializeRequestArgs(string targetMethod, CdrInputStream sourceStream,
                                                        out IDictionary contextElements);
                
        public abstract void SerializeResponseArgs(string targetMethod, object retValue, object[] outArgs,
                                                   CdrOutputStream targetStream);
        
        public abstract object DeserializeResponseArgs(string targetMethod, CdrInputStream sourceStream,
                                                       out object[] outArgs);
        
        public abstract MethodInfo GetMethodInfoFor(string method);
        
        #endregion IMethods        

        /// <summary>
        /// helper method called by generated subclasses
        /// </summary>
        protected static MethodInfo GetMethodInfoForInternal(string method, Type type) {
            string fieldName = CACHED_METHOD_INFO_PREFIX + method;
            FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
            if (field != null) {
                return (MethodInfo)field.GetValue(null);
            } else {
                throw new omg.org.CORBA.BAD_OPERATION(22, omg.org.CORBA.CompletionStatus.Completed_MayBe);
            }
        }        
                
        
    }
    
}
