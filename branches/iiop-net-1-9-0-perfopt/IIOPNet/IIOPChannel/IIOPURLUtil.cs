/* IIOPURLUtil.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 16.01.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using omg.org.CORBA;
using Ch.Elca.Iiop.CorbaObjRef;

namespace Ch.Elca.Iiop.Util {

    /// <summary>
    /// This class is able to handle urls for the IIOP-channel
    /// </summary>
    /// <remarks>
    /// This class is used to parse url's.
    /// This is a helper class for the IIOP-channel
    /// </remarks>
    public sealed class IiopUrlUtil {

        #region Constants

        #endregion Constants
        #region SFields
                      
        private readonly static object[] s_defaultAdditionalTaggedComponents =
            new object[] { 
                Services.CodeSetService.CreateDefaultCodesetComponent(
                    OrbServices.GetSingleton().CodecFactory.create_codec(
                        new omg.org.IOP.Encoding(omg.org.IOP.ENCODING_CDR_ENCAPS.ConstVal, 
                                                 1, 2))) };
        
        private readonly static omg.org.IOP.Codec s_codec =
            OrbServices.GetSingleton().CodecFactory.create_codec(
                        new omg.org.IOP.Encoding(omg.org.IOP.ENCODING_CDR_ENCAPS.ConstVal, 
                                                 1, 2));
            
        
        #endregion SFields
        #region IConstructors
        
        private IiopUrlUtil() {
        }

        #endregion IConstructors
        #region SMethods
        
        /// <summary>checks if data is an URL for the IIOP-channel </summary>
        public static bool IsUrl(string data) {
            return (data.StartsWith("iiop") || IsIorString(data) ||
                    data.StartsWith("corbaloc"));
        }

        public static bool IsIorString(string url) {
            return url.StartsWith("IOR");
        }

        /// <summary>creates an IOR for the object described by the Url url</summary>
        /// <param name="url">an url of the form IOR:--hex-- or iiop://addr/key</param>
        /// <param name="targetType">if the url contains no info about the target type, use this type</param>
        public static Ior CreateIorForUrl(string url, string repositoryId) {
            Ior ior = null;
            if (IsIorString(url)) {
                ior = new Ior(url);
            } else if (url.StartsWith("iiop")) {
                // iiop1.0, iiop1.1, iiop1.2 (=iiop); extract version in protocol tag
                IiopLoc iiopLoc = new IiopLoc(url, s_codec,
                                              s_defaultAdditionalTaggedComponents);
                // now create an IOR with the above information
                ior = new Ior(repositoryId, iiopLoc.GetProfiles());
            } else if (url.StartsWith("corbaloc")) {
                Corbaloc loc = new Corbaloc(url, s_codec,
                                            s_defaultAdditionalTaggedComponents);
                IorProfile[] profiles = loc.GetProfiles();
                ior = new Ior(repositoryId, profiles);
            } else {
                throw new INV_OBJREF(1963, CompletionStatus.Completed_MayBe);
            }
            return ior;
        }
        
        /// <summary>
        /// This method parses an url for the IIOP channel. 
        /// It extracts the channel URI and the objectURI
        /// </summary>
        /// <param name="url">the url to parse</param>
        /// <param name="objectURI">the objectURI</param>
        /// <returns>the channel-Uri</returns>
        internal static Uri ParseUrl(string url, out string objectUri, 
                                     out GiopVersion version) {
            Uri uri = null;
            if (url.StartsWith("iiop")) {
                IiopLoc iiopLoc = new IiopLoc(url, s_codec,
                                              s_defaultAdditionalTaggedComponents);
                uri = iiopLoc.ParseUrl(out objectUri, out version);
            } else if (url.StartsWith("IOR")) {
                Ior ior = new Ior(url);
                IInternetIiopProfile profile = ior.FindInternetIiopProfile();
                if (profile != null) {
                    uri = new Uri("iiop" + profile.Version.Major + "." + profile.Version.Minor + 
                              Uri.SchemeDelimiter + profile.HostName+":"+profile.Port);
                    objectUri = IorUtil.GetObjectUriForObjectKey(profile.ObjectKey);
                    version = profile.Version;
                } else {
                    uri = null;
                    objectUri = null;
                    version = new GiopVersion(1,0);
                }                
            } else if (url.StartsWith("corbaloc")) {
                Corbaloc loc = new Corbaloc(url, s_codec,
                                            s_defaultAdditionalTaggedComponents);
                uri = loc.ParseUrl(out objectUri, out version);
            } else {
                // not possible
                uri = null;
                objectUri = null;
                version = new GiopVersion(1,0);
            }
            return uri;
        }

        /// <summary>
        /// creates an URL from host, port and objectURI
        /// </summary>
        internal static string GetUrl(string host, int port, string objectUri) {
            return "iiop" + Uri.SchemeDelimiter + host + ":" + port + "/" + objectUri;
        }

        #endregion SMethods

    }
        
 
     
}


#if UnitTest

namespace Ch.Elca.Iiop.Tests {
    
    using System.IO;
    using NUnit.Framework;
    using Ch.Elca.Iiop;
    using Ch.Elca.Iiop.Idl;
    using Ch.Elca.Iiop.Util;
    using Ch.Elca.Iiop.Services;
    using Ch.Elca.Iiop.Marshalling;
    using Ch.Elca.Iiop.Interception;
    using omg.org.CORBA;

    
    /// <summary>
    /// Unit-tests for testing request/reply serialisation/deserialisation
    /// </summary>
    [TestFixture]    
    public class IiopUrlUtilTest {
        
        private omg.org.IOP.Codec m_codec;
        
    	[SetUp]
    	public void SetUp() {
    	    SerializerFactory serFactory =
    	        new SerializerFactory();
            omg.org.IOP.CodecFactory codecFactory =
                new CodecFactoryImpl(serFactory);
            m_codec = 
                codecFactory.create_codec(
                    new omg.org.IOP.Encoding(omg.org.IOP.ENCODING_CDR_ENCAPS.ConstVal, 1, 2));
    	}        
        
        private void CheckIorForUrl(Ior iorForUrl, int expectedNumberOfComponents,
                                    bool shouldHaveCodeSetComponent) {
            Assertion.AssertEquals("number of profiles", 1, iorForUrl.Profiles.Length);
            Assertion.AssertEquals("type", typeof(MarshalByRefObject), 
                                   iorForUrl.Type);
            IIorProfile profile = iorForUrl.FindInternetIiopProfile();
            Assertion.AssertNotNull("internet iiop profile",
                                    profile);
            ArrayAssertion.AssertByteArrayEquals("profile object key",
                                                 new byte[] { 116, 101, 115, 116 },
                                                 profile.ObjectKey);
            Assertion.AssertEquals("profile giop version", 
                                   new GiopVersion(1, 2), 
                                   profile.Version);
            
            if (shouldHaveCodeSetComponent) {
                Assertion.AssertEquals("number of components",
                                       expectedNumberOfComponents, 
                                       profile.TaggedComponents.Count);
                Assertion.Assert("code set component present",
                                 profile.ContainsTaggedComponent(
                                     CodeSetService.SERVICE_ID));                
                CodeSetComponentData data = (CodeSetComponentData)
                    profile.TaggedComponents.GetComponentData(CodeSetService.SERVICE_ID,
                                                              m_codec,
                                                              CodeSetComponentData.TypeCode);
                Assertion.AssertEquals("code set component: native char set",
                                       (int)CharSet.LATIN1,
                                       data.NativeCharSet);
                Assertion.AssertEquals("code set component: native char set",
                                       (int)WCharSet.UTF16,
                                       data.NativeWCharSet);
            } else {
                Assertion.Assert("code set component present",
                                 !profile.ContainsTaggedComponent(
                                     CodeSetService.SERVICE_ID));                
            }
        }
        
        [Test]
        public void CreateIorForCorbaLocUrlWithCodeSetComponent() {
            string testCorbaLoc = "corbaloc:iiop:1.2@elca.ch:1234/test";
            Ior iorForUrl = 
                IiopUrlUtil.CreateIorForUrl(testCorbaLoc, String.Empty);
            CheckIorForUrl(iorForUrl, 1, true);
        }
        
        [Test]
        public void CreateIorForIiopLocUrlWithCodeSetComponent() {
            string testIiopLoc = "iiop1.2://localhost:1234/test";
            Ior iorForUrl = 
                IiopUrlUtil.CreateIorForUrl(testIiopLoc, String.Empty);
            CheckIorForUrl(iorForUrl, 1, true);
        }
        
        
        [Test]
        public void CreateIorForIorUrl() {
            string testIorLoc = 
                "IOR:000000000000000100000000000000010000000000000050000102000000000A6C6F63616C686F73740004D2000000047465737400000001000000010000002800000000000100010000000300010001000100200501000100010109000000020001010000010109";            
            Ior iorForUrl = 
                IiopUrlUtil.CreateIorForUrl(testIorLoc, String.Empty);
            CheckIorForUrl(iorForUrl, 1, true);
        }                
        
    }

}

#endif

