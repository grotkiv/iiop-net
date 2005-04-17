/* Policy.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 17.04.05  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
 using omg.org.CORBA;
 using Ch.Elca.Iiop;
 using Ch.Elca.Iiop.Idl;

 namespace omg.org.IOP {
 
     
    /// <summary>
    /// The IORInfo allows IORInterceptor (on the server side) to components
    /// to an ior profile.
    /// </summary>
    [RepositoryID("IDL:omg.org/IOP/Codec:1.0")]
    [InterfaceType(IdlTypeInterface.LocalInterface)]
    public interface Codec {
        
        /// <summary>
        /// Convert the given any into an octet sequence based on the encoding format effective
        /// for this Codec.
        /// </summary>
        [ThrowsIdlException(typeof(omg.org.IOP.Codec_package.InvalidTypeForEncoding))]
        [return: IdlSequence(0L)]
        byte[] encode (object data);
        
        /// <summary>
        /// Decode the given octet sequence into an any based on the encoding format effective
        /// for this Codec.
        /// </summary>
        [ThrowsIdlException(typeof(omg.org.IOP.Codec_package.FormatMismatch))]
        object decode ([IdlSequence(0L)] byte[] data);

        /// <summary>
        /// Convert the given any into an octet sequence based on the encoding format effective
        /// for this Codec. Only the data from the any is encoded, not the TypeCode.
        /// </summary>
        [ThrowsIdlException(typeof(omg.org.IOP.Codec_package.InvalidTypeForEncoding))]
        [return: IdlSequence(0L)]        
        byte[] encode_value (object data);
        
        /// <summary>
        /// Decode the given octet sequence into an any based on the given TypeCode and the
        /// encoding format effective for this Codec.
        /// </summary>
        [ThrowsIdlException(typeof(omg.org.IOP.Codec_package.FormatMismatch))]
        [ThrowsIdlException(typeof(omg.org.IOP.Codec_package.TypeMismatch))]
        object decode_value ([IdlSequence(0L)] byte[] data,
                             omg.org.CORBA.TypeCode tc);
        
    }     
     
 }
 
 namespace omg.org.IOP.Codec_package {
 
     /// <summary>
     /// This exception is raised by encode or encode_value when the type is invalid for the
     /// encoding.
     /// </summary>
     [Serializable]
     public class InvalidTypeForEncoding : AbstractUserException {
     
         public InvalidTypeForEncoding() {
         }
     
     }

     
     /// <summary>
     /// This exception is raised by decode or decode_value when the data in the octet
     /// sequence cannot be decoded into an any.
     /// </summary>
     [Serializable]
     public class FormatMismatch : AbstractUserException {
     
         public FormatMismatch() {
         }
     
     }

     
     /// <summary>
     /// This exception is raised by decode_value when the given TypeCode does not match
     /// the given octet sequence.
     /// </summary>
     [Serializable]
     public class TypeMismatch : AbstractUserException {
     
         public TypeMismatch() {
         }
     
     }     
 
 }
