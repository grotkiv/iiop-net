/* CodeSetService.cs
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
using System.Runtime.Remoting.Messaging;
using System.Collections;
using System.IO;
using System.Text;
using Ch.Elca.Iiop.Cdr;
using Ch.Elca.Iiop.CorbaObjRef;
using Ch.Elca.Iiop.Idl;
using omg.org.CORBA;
using omg.org.IOP;
using Ch.Elca.Iiop.CodeSet;
using Ch.Elca.Iiop.Util;

namespace Ch.Elca.Iiop.Services {
    
    
    [IdlStruct]
    public struct CodeSetComponentData {
        
        #region SFields
        
        public readonly static Type ClassType = typeof(CodeSetComponentData);
        
        public readonly static omg.org.CORBA.TypeCode TypeCode =
            Repository.CreateTypeCodeForType(typeof(CodeSetComponentData),
                                             AttributeExtCollection.EmptyCollection);
        
        
        #endregion SFields        
        #region IFields
        
        public int NativeCharSet;
        [IdlSequence(0L)]
        public int[] CharConvSet;
        
        public int NativeWCharSet;
        [IdlSequence(0L)]
        public int[] WCharConvSet;
        
        #endregion IFields
        #region IConstructors        
        
        public CodeSetComponentData(int nativeCharSet, int[] charConvSet,
                                    int nativeWCharSet, int[] wcharConvSet) {
            NativeCharSet = nativeCharSet;            
            NativeWCharSet = nativeWCharSet;
            CharConvSet = charConvSet;
            if (CharConvSet == null) {
                CharConvSet = new int[0];
            }
            WCharConvSet = wcharConvSet;
            if (WCharConvSet == null) {
                WCharConvSet = new int[0];
            }            
        }
        
        #endregion IConstructors
                
    }
    
    /// <summary>
    /// the supported charsets
    /// </summary>
    public enum CharSet {
        LATIN1 = 0x10001, UTF8 = 0x5010001, 
        ISO646IEC_SINGLE = 0x10020 /* compatible with ASCII */        
    }
    
    /// <summary>
    /// the supported wcharsets
    /// </summary>
    public enum WCharSet {
        UTF16 = 0x10109, ISO646IEC_MULTI = 0x10100
    }
    
    /// <summary>
    /// This service handles code-set conversion.
    /// </summary>
    internal sealed class CodeSetService {
    
        #region Constants

        public const int SERVICE_ID = 1;
        
        /// <summary>
        /// default char set used, if not overriden
        /// </summary>
        private const int DEFAULT_CHAR_SET_TO_SET = (int)CharSet.LATIN1;

        /// <summary>
        /// default wchar set used, if not overriden
        /// </summary>
        private const int DEFAULT_WCHAR_SET_TO_SET = (int)WCharSet.UTF16;
        
        private const int UNINITALIZED_CHAR_SET = -1;
        private const int UNINITALIZED_WCHAR_SET = -1;

        #endregion Constants
        #region SFields
        
        private static CodeSetConversionRegistry s_registry = new CodeSetConversionRegistry();
        
        private static readonly Type s_wCharSetType = typeof(WCharSet);
        private static readonly Type s_charSetType = typeof(CharSet);
        
        /// <summary>
        /// the default char set to use; is initalized either by the user or on first use by IIOP.NET to
        /// DEFAULT_CHAR_SET.
        /// </summary>
        private static volatile int s_defaultCharSet = UNINITALIZED_CHAR_SET;

        /// <summary>
        /// the default wchar set to use; is initalized either by the user or on first use by IIOP.NET to
        /// DEFAULT_WCHAR_SET.
        /// </summary>
        private static volatile int s_defaultWCharSet = UNINITALIZED_WCHAR_SET;
        
        private static object s_initLock = new object();
        
        #endregion SFields
        #region SConstructor
        
        static CodeSetService() {
            s_registry = new CodeSetConversionRegistry();
            // add the non-endian dependant encodings here
            s_registry.AddEncodingAllEndian((int)CharSet.LATIN1, new Latin1Encoding());
            s_registry.AddEncodingAllEndian((int)CharSet.ISO646IEC_SINGLE, new ASCIIEncoding());
            s_registry.AddEncodingAllEndian((int)CharSet.UTF8, new UTF8Encoding());
            // big endian
            s_registry.AddEncodingBigEndian((int)WCharSet.UTF16,
                                            new UnicodeEncodingExt(true)); // use big endian encoding here
            s_registry.AddEncodingBigEndian((int)WCharSet.ISO646IEC_MULTI,
                                            new UnicodeEncodingExt(true));
            // little endian
            s_registry.AddEncodingLittleEndian((int)WCharSet.UTF16,
                                               new UnicodeEncodingExt(false)); // use little endian encoding here
            s_registry.AddEncodingLittleEndian((int)WCharSet.ISO646IEC_MULTI,
                                               new UnicodeEncodingExt(false));
        }
        
        #endregion SConstructor
        #region IConstructors
        
        private CodeSetService() {
        }

        #endregion IConstructors
        #region SProperties
        
        /// <summary>
        /// the default char set to use (if nothing different is specified by Code set establishment)
        /// </summary>
        private static int DefaultCharSet {
            get {
                if (s_defaultCharSet == UNINITALIZED_CHAR_SET) {
                    lock(s_initLock) {
                        if (s_defaultCharSet == UNINITALIZED_CHAR_SET) {
                            s_defaultCharSet = DEFAULT_CHAR_SET_TO_SET;
                        }
                    }
                }
                return s_defaultCharSet;
            }
        }

        /// <summary>
        /// the default wchar set to use (if nothing different is specified by Code set establishment)
        /// </summary>        
        private static int DefaultWCharSet {
            get {
                if (s_defaultWCharSet == UNINITALIZED_WCHAR_SET) {
                    lock(s_initLock) {
                        if (s_defaultWCharSet == UNINITALIZED_WCHAR_SET) {
                            s_defaultWCharSet = DEFAULT_WCHAR_SET_TO_SET;
                        }
                    }
                }
                return s_defaultWCharSet;                
            }
        }
        
        #endregion SProperties
        #region SMethods
        
        /// <summary>
        /// overrides the default char set and the default wcharset to use.
        /// </summary>
        internal static void OverrideDefaultCharSets(CharSet defaultCharSet, WCharSet defaultWCharSet) {
            lock(s_initLock) {
                if ((s_defaultCharSet != UNINITALIZED_CHAR_SET) || (s_defaultWCharSet != UNINITALIZED_WCHAR_SET)) {
                    // only settable once, before first used.
                    throw new BAD_INV_ORDER(690, CompletionStatus.Completed_MayBe);
                }
                s_defaultCharSet = (int)defaultCharSet;
                s_defaultWCharSet = (int)defaultWCharSet;
            }
        }
        

                       
        /// <summary>
        /// returns the code set component data or null if not found
        /// </summary>
        internal static object /* CodeSetComponentData */ FindCodeSetComponent(IIorProfile profile, Codec codec) {
            TaggedComponentList list = profile.TaggedComponents;
            object result = 
                list.GetComponentData(TAG_CODE_SETS.ConstVal, codec,
                                      CodeSetComponentData.TypeCode);
            if (result != null) {
                return (CodeSetComponentData)result;
            }            
            return null;
        }        

        internal static int GetServiceId() {
            return SERVICE_ID;
        }
        
        internal static void CheckCodeSetCompatible(int charSet, int wcharSet) {
            // check if acceptable: at the moment, the code-set establishment algorithm is not implemented
            if (!IsCharSetCompatible(charSet)) { 
                throw new CODESET_INCOMPATIBLE(9501, CompletionStatus.Completed_No); 
            }
            if (!IsWCharSetCompatible(wcharSet)) { 
                throw new CODESET_INCOMPATIBLE(9502, CompletionStatus.Completed_No); 
            }            
        }
        
        private static bool IsWCharSetCompatible(int wcharSet) {
            return Enum.IsDefined(s_wCharSetType, wcharSet);
        }
        
        private static bool IsCharSetCompatible(int charSet) {
            return Enum.IsDefined(s_charSetType, charSet);
        }
        
        internal static int ChooseCharSet(CodeSetComponentData codeSetComponent) {
            if (codeSetComponent.NativeCharSet == DefaultCharSet) {
                // the same native char sets
                return codeSetComponent.NativeCharSet;
            }
            if (IsCharSetCompatible(codeSetComponent.NativeCharSet)) {
                // client converts to server's native char set
                return codeSetComponent.NativeCharSet;
            }
            int[] serverConvSets = codeSetComponent.CharConvSet;
            foreach (int serverConvSet in serverConvSets) {
                if (serverConvSet == DefaultCharSet) {
                    // server convert's from client's native char set
                    return serverConvSet;
                }
            }
            foreach (int serverConvSet in serverConvSets) {
                if (IsCharSetCompatible(serverConvSet)) {
                    // use a conversion code set which is available for client and server
                    return serverConvSet;
                }
            }
            throw new CODESET_INCOMPATIBLE(9501, CompletionStatus.Completed_No);
        }
        
        internal static int ChooseWCharSet(CodeSetComponentData codeSetComponent) {
            if (codeSetComponent.NativeWCharSet == DefaultWCharSet) {
                // the same native wchar sets
                return codeSetComponent.NativeWCharSet;
            }
            if (IsWCharSetCompatible(codeSetComponent.NativeWCharSet)) {
                // client converts to server's native wchar set
                return codeSetComponent.NativeWCharSet;
            }
            int[] serverConvSets = codeSetComponent.WCharConvSet;
            foreach (int serverConvSet in serverConvSets) {
                if (serverConvSet == DefaultWCharSet) {
                    // server convert's from client's native wchar set
                    return serverConvSet;
                }
            }
            foreach (int serverConvSet in serverConvSets) {
                if (IsWCharSetCompatible(serverConvSet)) {
                    // use a conversion code set which is available for client and server
                    return serverConvSet;
                }
            }
            throw new CODESET_INCOMPATIBLE(9502, CompletionStatus.Completed_No);
        }        
                        
        /// <summary>
        /// create a code set service context.
        /// </summary>
        internal static omg.org.IOP.ServiceContext CreateServiceContext(int charSet, int wcharSet) {
            CodeSetServiceContext codeSetServiceContext = new CodeSetServiceContext(charSet, wcharSet);
            return codeSetServiceContext.CreateServiceContext();
        }
        
        /// <summary>
        /// finds the code set service context among the collection of received service contexts.
        /// </summary>
        internal static CodeSetServiceContext FindCodeSetServiceContext(omg.org.IOP.ServiceContextList contexts) {                        
            if (contexts.ContainsServiceContext(SERVICE_ID)) {
                omg.org.IOP.ServiceContext context = contexts.GetServiceContext(SERVICE_ID);
                return new CodeSetServiceContext(context);
            } else {
                return null;
            }
        }        
        
        /// <summary>
        /// insert a codeset service context into the service context list.
        /// </summary>
        internal static void InsertCodeSetServiceContext(omg.org.IOP.ServiceContextList contexts,
                                                         int charSet, int wcharSet) {
            omg.org.IOP.ServiceContext context = CreateServiceContext(charSet, wcharSet);
            contexts.AddServiceContext(context);
        }                
        
        /// <summary>
        /// creates the tagged codeset component to insert into an IOR
        /// </summary>
        /// <returns></returns>
        internal static TaggedComponent CreateDefaultCodesetComponent(Codec codec) {
            Array wCharSets = Enum.GetValues(s_wCharSetType);
            Array charSets = Enum.GetValues(s_charSetType);
            int[] wCharSetCodes = new int[wCharSets.Length];
            int[] charSetCodes = new int[charSets.Length];
            for (int i = 0; i < wCharSets.Length; i++) { // Array.CopyTo doesn't work with mono for this case
                wCharSetCodes[i] = (int)wCharSets.GetValue(i);
            }
            for (int i = 0; i < charSets.Length; i++) { // Array.CopyTo doesn't work with mono for this case
                charSetCodes[i] = (int)charSets.GetValue(i);
            }
            Services.CodeSetComponentData codeSetCompData =
                new Services.CodeSetComponentData(Services.CodeSetService.DefaultCharSet,
                                                  charSetCodes,
                                                  Services.CodeSetService.DefaultWCharSet,
                                                  wCharSetCodes);            
            return new TaggedComponent(TAG_CODE_SETS.ConstVal,
                                       codec.encode_value(codeSetCompData));
        }
        
        /// <summary>
        /// get the char encoding to use for a charset id (endian independant)
        /// </summary>
        internal static System.Text.Encoding GetCharEncoding(int charSet, bool isWChar) {
            return s_registry.GetEncodingEndianIndependant(charSet); // get Encoding for charSet
        }
               
        /// <summary>
        /// get the char encoding to use for a charset id (for big endian)
        /// </summary>
        internal static System.Text.Encoding GetCharEncodingBigEndian(int charSet, bool isWChar) {
            return s_registry.GetEncodingBigEndian(charSet); // get Encoding for charSet
        }
        
        /// <summary>
        /// get the char encoding to use for a charset id
        /// </summary>
        internal static System.Text.Encoding GetCharEncodingLittleEndian(int charSet, bool isWChar) {
            return s_registry.GetEncodingLittleEndian(charSet); // get Encoding for charSet
        }
                
        #endregion SMethods
    
    }
    

    /// <summary>
    /// the service context for the code set service
    /// </summary>
    internal class CodeSetServiceContext {
        
        #region IFields
        
        private int m_charSet;
        private int m_wcharSet;

        #endregion IFields
        #region IConstructors

        internal CodeSetServiceContext(int charSet, int wcharSet) {
            m_charSet = charSet;
            m_wcharSet = wcharSet;    
        }

        /// <summary>
        /// create a codeSetServiceContext from the service context with the specified id.
        /// </summary>
        /// <param name="svcContext"></param>
        internal CodeSetServiceContext(omg.org.IOP.ServiceContext svcContext) {
            Deserialise(svcContext.context_data);
        }


        #endregion IConstructors
        #region IProperties
        
        public int CharSet {
            get {
                return m_charSet;
            }
        }

        public int WCharSet {
            get {
                return m_wcharSet;
            }
        }

        #endregion IProperties
        #region IMethods

        public override string ToString() {
            return "CodeSetService: charset: " + m_charSet +
                   ", wcharset: " + m_wcharSet;
        }

        /// <summary>
        /// create an IOP service context from the code set service context
        /// </summary>
        /// <returns></returns>
        public omg.org.IOP.ServiceContext CreateServiceContext() {            
            CdrEncapsulationOutputStream encapStream = new CdrEncapsulationOutputStream(0);
            encapStream.WriteULong((uint)m_charSet);
            encapStream.WriteULong((uint)m_wcharSet); 
            return new omg.org.IOP.ServiceContext(CodeSetService.SERVICE_ID, 
                                                  encapStream.GetEncapsulationData());            
        }

        private void Deserialise(byte[] contextData) {
            CdrEncapsulationInputStream encap = 
                new CdrEncapsulationInputStream(contextData);
            m_charSet = (int)encap.ReadULong();
            m_wcharSet = (int)encap.ReadULong();
        }

        #endregion IMethods
    }

}
