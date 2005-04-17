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
    /// Encoding format: cdr encapsulation.
    /// </summary>
    public sealed class ENCODING_CDR_ENCAPS {
        
        #region Constants
        
        public const short ConstVal = 0;
        
        #endregion Constants
        #region IConstructors
        
        private ENCODING_CDR_ENCAPS() {
        }
        
        #endregion IConstructors        
        
    }
    
     
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
    
    /// <summary>
    /// The Encoding structure defines the encoding format of a Codec. It details the
    /// encoding format, such as CDR Encapsulation encoding, and the major and minor
    /// versions of that format.
    /// </summary>    
    [Serializable()]
    [RepositoryID("IDL:omg.org/IOP/Encoding:1.0")]
    [IdlStruct()]
    public struct Encoding : IIdlEntity {
               
        public short format;
        public byte major_version;
        public byte minor_version;
        
        public Encoding(short format, byte major_version, byte minor_version) {
            this.format = format;
            this.major_version = major_version;
            this.minor_version = minor_version;
        }        
        
    }
    
    /// <summary>
    /// Create a Codec of the given encoding.
    /// </summary>
    [RepositoryID("IDL:omg.org/IOP/CodecFactory:1.0")]
    [InterfaceType(IdlTypeInterface.LocalInterface)]    
    public interface CodecFactory {
        
        [ThrowsIdlException(typeof(omg.org.IOP.CodecFactory_package.UnknownEncoding))]
        Codec create_codec (Encoding enc);
        
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
 
 namespace omg.org.IOP.CodecFactory_package {
 
     /// <summary>
     /// raised, if the codec factory, cannot create a Codec of the given encoding.
     /// </summary>
     [Serializable]
     public class UnknownEncoding : AbstractUserException {
     
         public UnknownEncoding() {
         }
         
     }
 
 }
