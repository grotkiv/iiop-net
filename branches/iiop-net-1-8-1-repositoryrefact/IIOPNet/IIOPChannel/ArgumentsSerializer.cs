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
using System.Runtime.Remoting.Messaging;
using Ch.Elca.Iiop.Cdr;

namespace Ch.Elca.Iiop.Marshalling {

    /// <summary>
    /// interface for generated arguments serializers
    /// </summary>
    [CLSCompliant(false)]
    public abstract class ArgumentsSerializer {
        
        #region Types

        public delegate void SerializeRequestArgsFor(object[] actual, CdrOutputStream targetStream,
                                                     LogicalCallContext callContext);

        public delegate object[] DeserializeRequestArgsFor(CdrInputStream sourceStream,
                                                           out IDictionary contextElements);
        
        public delegate void SerializeResponseArgsFor(object retValue, object[] outArgs,
                                                      CdrOutputStream targetStream);
        
        public delegate object DeserializeResponseArgsFor(CdrInputStream sourceStream,
                                                          out object[] outArgs);
        
        #endregion Types               
        #region Constants
        
        public const string SER_REQ_ARGS_METHOD_PREFIX = "SerReqArgsFor_";
        public const string DESER_REQ_ARGS_METHOD_PREFIX = "DeserReqArgsFor_";
        public const string SER_RESP_ARGS_METHOD_PREFIX = "SerRespArgsFor_";
        public const string DESER_RESP_ARGS_METHOD_PREFIX = "DeserRespArgsFor_";
        
        #endregion Constants
        #region SFields
        
        public static readonly Type ClassType = typeof(ArgumentsSerializer);
        
        internal static readonly Type SerializeRequestArgsForType =
            typeof(SerializeRequestArgsFor);

        internal static readonly Type DeserializeRequestArgsForType =
            typeof(DeserializeRequestArgsFor);

        internal static readonly Type SerializeResponseArgsForType =
            typeof(SerializeResponseArgsFor);

        internal static readonly Type DeserializeResponseArgsForType =
            typeof(DeserializeResponseArgsFor);
                
        #endregion SFields
        #region IMethods                        
        
        public void SerializeRequestArgs(string targetMethod, object[] actual, CdrOutputStream targetStream,
                                         LogicalCallContext callContext) {
            SerializeRequestArgsFor del = (SerializeRequestArgsFor)Delegate.CreateDelegate(SerializeRequestArgsForType, this,
                                                   SER_REQ_ARGS_METHOD_PREFIX + targetMethod);
            del(actual, targetStream, callContext);
        }
        
        public object[] DeserializeRequestArgs(string targetMethod, CdrInputStream sourceStream,
                                               out IDictionary contextElements) {
            DeserializeRequestArgsFor del = (DeserializeRequestArgsFor)Delegate.CreateDelegate(DeserializeRequestArgsForType, this,
                                                   DESER_REQ_ARGS_METHOD_PREFIX + targetMethod);
            return del(sourceStream, out contextElements);
        }
                
        public void SerializeResponseArgs(string targetMethod, object retValue, object[] outArgs,
                                          CdrOutputStream targetStream) {
            SerializeResponseArgsFor del = (SerializeResponseArgsFor)Delegate.CreateDelegate(SerializeResponseArgsForType, this,
                                                   SER_RESP_ARGS_METHOD_PREFIX + targetMethod);
            del(retValue, outArgs, targetStream);
        }
        
        public object DeserializeResponseArgs(string targetMethod, CdrInputStream sourceStream,
                                              out object[] outArgs) {
            DeserializeResponseArgsFor del = (DeserializeResponseArgsFor)Delegate.CreateDelegate(DeserializeResponseArgsForType, this,
                                                   DESER_RESP_ARGS_METHOD_PREFIX + targetMethod);
            return del(sourceStream, out outArgs);
        }
        
        public abstract MethodInfo GetMethodInfoFor(string method);
        
        public abstract string GetRequestNameFor(MethodInfo method);
        
        #endregion IMethods        
                
        
    }
    
}
