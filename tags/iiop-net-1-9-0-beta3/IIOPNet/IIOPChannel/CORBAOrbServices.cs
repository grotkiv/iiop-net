/* CORBAOrbServices.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 11.04.04  Dominic Ullmann (DUL), dul@elca.ch
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
using System.Runtime.Remoting;
using System.Collections;
using System.Diagnostics;
using Ch.Elca.Iiop.Idl;
using Ch.Elca.Iiop;
using Ch.Elca.Iiop.CorbaObjRef;
using Ch.Elca.Iiop.Util;
using Ch.Elca.Iiop.Interception;
using omg.org.PortableInterceptor;
using omg.org.IOP;


namespace omg.org.CORBA {
    
            
    /// <remarks>contains only the CORBA Orb operations supported by IIOP.NET</remarks>
    public interface ORB {
        
        /// <summary>takes an IOR or a corbaloc and returns a proxy</summary>
        object string_to_object([StringValue] string obj);
        
        /// <summary>takes a proxy and returns the IOR / corbaloc / ...</summary>
        string object_to_string(object obj);

		
        /// <summary>allows to access a small set of well defined local objects.></summary>
        /// <remarks>currently supported are: CodecFactory and PICurrent.</remarks>
        [ThrowsIdlException(typeof(omg.org.CORBA.ORB_package.InvalidName))]
        object resolve_initial_references ([StringValue()][WideChar(false)] string identifier);
        
        #region Typecode creation operations

        TypeCode create_interface_tc ([StringValue] [WideChar(false)] string id,
                                      [StringValue] [WideChar(false)] string name);
        
        TypeCode create_abstract_interface_tc([StringValue] [WideChar(false)] string id,
                                              [StringValue] [WideChar(false)] string name);
        
        TypeCode create_local_interface_tc([StringValue] [WideChar(false)] string id,
                                           [StringValue] [WideChar(false)] string name);        
        
        TypeCode create_string_tc(int bound);
        
        TypeCode create_wstring_tc(int bound);
        
        TypeCode create_array_tc (int length,
                                  TypeCode element_type);

        TypeCode create_alias_tc ([StringValue] [WideChar(false)] string id, [StringValue] [WideChar(false)] string name, 
                                  TypeCode original_type);
        
        TypeCode create_sequence_tc (int bound, TypeCode element_type);

        TypeCode create_value_box_tc ([StringValue] [WideChar(false)] string id, 
                                      [StringValue] [WideChar(false)] string name,
                                      TypeCode boxed_type);
        
        TypeCode create_enum_tc ([StringValue] [WideChar(false)] string id, 
                                 [StringValue] [WideChar(false)] string name,
                                 [IdlSequence(0L)][StringValue] [WideChar(false)] string[] members);
                
        TypeCode create_struct_tc ([StringValue] [WideChar(false)] string id, 
                                   [StringValue] [WideChar(false)] string name,
                                   [IdlSequence(0L)] StructMember[] members);
        
        TypeCode create_value_tc ([StringValue] [WideChar(false)] string id, 
                                  [StringValue] [WideChar(false)] string name,
                                  short type_modifier,
                                  omg.org.CORBA.TypeCode concrete_base,
                                  [IdlSequence(0L)] ValueMember[] members);
        
        // TypeCode create_native_tc (
        //    [StringValue] [WideChar(false)] string id, 
        //    [StringValue] [WideChar(false)] string name);

        // TypeCode create_recursive_tc(
        //    [StringValue] [WideChar(false)] string id);
        
        // TypeCode create_recursive_sequence_tc (// deprecated
        // long bound, long offset
        // );
        
        // TypeCode create_fixed_tc (
        // short digits,
        // short scale);
        
        TypeCode create_exception_tc([StringValue] [WideChar(false)] string id, 
                                     [StringValue] [WideChar(false)] string name,
                                     [IdlSequence(0L)] StructMember[] members);
        
        // TypeCode create_union_tc (
        //    [StringValue] [WideChar(false)] string id, 
        //    [StringValue] [WideChar(false)] string name,        
        //    omg.org.CORBA.TypeCode discriminator_type,
        //    [IdlSequence(0L)] UnionMember[] members);
                
        #endregion TypeCode creation operations
        
    }
    
    public interface IOrbServices : ORB {
        
        /// <summary>creates the typecode NullTC</summary>
        TypeCode create_null_tc();
        
        /// <summary>creates the typecode VoidTC</summary>
        TypeCode create_void_tc();
        
        TypeCode create_ulong_tc();
        
        TypeCode create_ushort_tc();
        
        TypeCode create_ulonglong_tc();      
        
        TypeCode create_long_tc();
        
        TypeCode create_short_tc();
        
        TypeCode create_longlong_tc();
        
        TypeCode create_float_tc();
        
        TypeCode create_double_tc();
        
        TypeCode create_boolean_tc();
        
        TypeCode create_octet_tc();
        
        TypeCode create_char_tc();
        
        TypeCode create_wchar_tc();     
        
        TypeCode create_any_tc();
        
        TypeCode create_typecode_tc();       
        
        /// <summary>takes an object an returns the typecode for it</summary>
        TypeCode create_tc_for(object forObject);
        
        /// <summary>takes a type an returns the typecode for it</summary>
        TypeCode create_tc_for_type(Type forType);      
        
        /// <summary>
        /// retrieves a type corresponding to the given typecode.
        /// </summary>
        Type get_type_for_tc(TypeCode tc);
        
        #region Pseudo object operation helpers
                
        /// <summary>checks, if object supports the specified interface</summary>
        bool is_a(object proxy, Type type);
      
        /// <summary>checks, if object supports the specified interface</summary>
        bool is_a(object proxy, string repId);
        
        /// <summary>checks, if the object is existing</summary>
        bool non_existent(object proxy);
        
        #endregion Pseude object operation helpers
        #region Portable Interceptors
	    
        /// <summary>registers an initalizer for portable interceptors. The interceptors are
        /// enabled by calling CompleteInterceptorRegistration.</summary>
        void RegisterPortableInterceptorInitalizer(ORBInitalizer initalizer);
	    
        /// <summary>
        /// completes registration of interceptors. 
        /// Afterwards, the interceptors are enabled and are called during processing.
        /// </summary>
        void CompleteInterceptorRegistration();
        
        /// <summary>
        /// Overrides the IIOP.NET default for charset and wcharset. If this method is
        /// not invoked specifying a user default, IIOP.NET uses it's default of LATIN1 / UTF16.
        /// </summary>
        /// <remarks>This method must be called, before the first call is performed. 
        /// Otherwise this method throws a BAD_INV_ORDER exception.</remarks>
        void OverrideDefaultCharSets(Ch.Elca.Iiop.Services.CharSet charSet, 
                                     Ch.Elca.Iiop.Services.WCharSet wcharSet);
	    
        #endregion Protable Interceptors

    }
    
    
    /// <summary>implementation of the Orb interface methods supported by IIOP.NET</summary>
    public sealed class OrbServices : IOrbServices {
    
        #region SFields
        
        private static OrbServices s_singleton = new OrbServices();     
        
        #endregion SFields
        #region IFields
		
        private IList m_orbInitalizers; 
        private InterceptorManager m_interceptorManager;
        private CodecFactory m_codecFactory;
        private Ch.Elca.Iiop.Interception.PICurrentManager m_piCurrentManager;
        private Ch.Elca.Iiop.Marshalling.ArgumentsSerializerFactory m_argSerializerFactory;
        private Ch.Elca.Iiop.Marshalling.SerializerFactory m_serializerFactory;
		
        #endregion IFields
        #region IConstructors
        
        private OrbServices() {         
            m_orbInitalizers = new ArrayList();
            m_codecFactory = new CodecFactoryImpl();
            m_piCurrentManager = new PICurrentManager();
            m_interceptorManager = new InterceptorManager(this);
            m_serializerFactory = new Ch.Elca.Iiop.Marshalling.SerializerFactory();
            m_argSerializerFactory = 
                new Ch.Elca.Iiop.Marshalling.ArgumentsSerializerFactory(m_serializerFactory);
        }
        
        #endregion IConstructors
        #region SMethods
        
        public static OrbServices GetSingleton() {
            return s_singleton;
        }
        
        #endregion SMethods
        #region IProperties
		
        /// <summary>
        /// the manager responsible for managing the interceptors.
        /// </summary>
        internal InterceptorManager InterceptorManager {
            get {
                return m_interceptorManager;
            }
        }
		
        /// <summary>
        /// returns the instance of the codec factory.
        /// </summary>
        internal CodecFactory CodecFactory {
            get {
                return m_codecFactory;
            }
        }
		
        /// <summary>
        /// returns the thread-scoped instance of picurrent.
        /// </summary>
        internal Ch.Elca.Iiop.Interception.PICurrentImpl PICurrent {
            get {
                return m_piCurrentManager.GetThreadScopedCurrent();
            }
        }
		
        /// <summary>
        /// returns the manager responsible for PICurrents.
        /// </summary>
        internal Ch.Elca.Iiop.Interception.PICurrentManager PICurrentManager {
            get {
                return m_piCurrentManager;
            }
        }
		
        /// <summary>
        /// returns the factory responsible for creating ArgumentsSerializer
        /// </summary>
        internal Ch.Elca.Iiop.Marshalling.ArgumentsSerializerFactory ArgumentsSerializerFactory {
            get {
                return m_argSerializerFactory;
            }        
        }

        /// <summary>
        /// returns the factory responsible for creating Serializers for serializing/deserializing
        /// instances.
        /// </summary>
        internal Ch.Elca.Iiop.Marshalling.SerializerFactory SerializerFactory {
            get {
                return m_serializerFactory;
            }
        }        
		
        #endregion IProperties
        #region IMethods
        
        
        private void CheckIsValidUri(string uri) {
            if (!IiopUrlUtil.IsUrl(uri)) {
                throw new BAD_PARAM(264, CompletionStatus.Completed_Yes);
            }
        }
        
        private void CheckIsProxy(MarshalByRefObject mbrProxy) {
            if ((mbrProxy == null) || (!RemotingServices.IsTransparentProxy(mbrProxy))) {
                // argument is not a proxy
                throw new BAD_PARAM(265, CompletionStatus.Completed_Yes);
            }
        }

        
        /// <summary>takes an IOR or a corbaloc and returns a proxy</summary>
        public object string_to_object([StringValue] string uri) {
            CheckIsValidUri(uri);
            
            Ior ior = IiopUrlUtil.CreateIorForUrl(uri, String.Empty);
            // performance opt: if an ior passed in, use it
            string iorString = uri;         
            if (!IiopUrlUtil.IsIorString(uri)) {
                iorString = ior.ToString();
            }
                
            return RemotingServices.Connect(ior.Type, iorString);
        }
        
        /// <summary>takes a proxy and returns the IOR / corbaloc / ...</summary>
        public string object_to_string(object obj) {
            MarshalByRefObject mbr = obj as MarshalByRefObject;
            if (mbr == null) {
                throw new BAD_PARAM(265, CompletionStatus.Completed_Yes);
            }
            if (RemotingServices.IsTransparentProxy(mbr)) {
            
                string uri = RemotingServices.GetObjectUri(mbr);
                CheckIsValidUri(uri);
                if (IiopUrlUtil.IsIorString(uri)) {
                    return uri;
                } else {
                    // create an IOR assuming type is CORBA::Object
                    return IiopUrlUtil.CreateIorForUrl(uri, "").ToString();
                }
            } else {
                // local object
                return IiopUrlUtil.CreateIorForObjectFromThisDomain(mbr).ToString();
            }
        }

        /// <summary>
        /// <see cref="omg.org.CORBA.ORB.resolve_initial_references"/>
        /// </summary>
        public object resolve_initial_references ([StringValue()][WideChar(false)] string identifier) {
            if (identifier == "CodecFactory") {
                return CodecFactory;
            } else if (identifier == "PICurrent") {
                return PICurrent;
            } else {
                throw new omg.org.CORBA.ORB_package.InvalidName();
            }
        }
        
        #region Typecode creation operations
        
        public TypeCode create_null_tc() {
            return new NullTC();
        }
        
        public TypeCode create_void_tc() {
            return new VoidTC();
        }
            
        public TypeCode create_interface_tc ([StringValue] [WideChar(false)] string id,
                                             [StringValue] [WideChar(false)] string name) {
            return new ObjRefTC(id, name);
        }
        
        public TypeCode create_abstract_interface_tc([StringValue] [WideChar(false)] string id,
                                                     [StringValue] [WideChar(false)] string name) {
            return new AbstractIfTC(id, name);
        }
        
        public TypeCode create_local_interface_tc([StringValue] [WideChar(false)] string id,
                                                  [StringValue] [WideChar(false)] string name) {
            return new LocalIfTC(id, name);
        }

        public TypeCode create_ulong_tc() {
            return new ULongTC();
        }
        
        public TypeCode create_ushort_tc() {
            return new UShortTC();
        }
        
        public TypeCode create_ulonglong_tc() {
            return new ULongLongTC();
        }
        
        public TypeCode create_long_tc() {
            return new LongTC();
        }
        
        public TypeCode create_short_tc() {
            return new ShortTC();
        }
        
        public TypeCode create_longlong_tc() {
            return new LongLongTC();
        }
        
        public TypeCode create_float_tc() {
            return new FloatTC();
        }
        
        public TypeCode create_double_tc() {
            return new DoubleTC();
        }
        
        public TypeCode create_boolean_tc() {
            return new BooleanTC();
        }
        
        public TypeCode create_octet_tc() {
            return new OctetTC();
        }
        
        public TypeCode create_char_tc() {
            return new CharTC();
        }
        
        public TypeCode create_wchar_tc() {
            return new WCharTC();
        }
        
        public TypeCode create_string_tc(int bound) {
            return new StringTC(bound);
        }
        
        public TypeCode create_wstring_tc(int bound) {
            return new WStringTC(bound);
        }

        public TypeCode create_array_tc (int length,
                                         TypeCode element_type) {
            return new ArrayTC(element_type, length);
        }
        
        public TypeCode create_sequence_tc (int bound, TypeCode element_type) {
            return new SequenceTC(element_type, bound);
        }        

        public TypeCode create_alias_tc ([StringValue] [WideChar(false)] string id, [StringValue] [WideChar(false)] string name, 
                                         TypeCode original_type) {
            return new AliasTC(id, name, original_type);
        }

        public TypeCode create_value_box_tc ([StringValue] [WideChar(false)] string id, 
                                             [StringValue] [WideChar(false)] string name,
                                             TypeCode boxed_type) {
            return new ValueBoxTC(id, name, boxed_type);
        }
        
        public TypeCode create_enum_tc ([StringValue] [WideChar(false)] string id, 
                                        [StringValue] [WideChar(false)] string name,
                                        [IdlSequence(0L)][StringValue] [WideChar(false)] string[] members) {
            return new EnumTC(id, name, members);
        }
        
        public TypeCode create_any_tc() {
            return new AnyTC();
        }
        
        public TypeCode create_typecode_tc() {
            return new TypeCodeTC();
        }
                        
        /// <summary>takes an object an returns the typecode for it</summary>
        public TypeCode create_tc_for(object forObject) {
            if (!(forObject == null)) {
                return Repository.CreateTypeCodeForType(forObject.GetType(), AttributeExtCollection.EmptyCollection);
            } else {
                return new NullTC();
            }
        }

        public TypeCode create_tc_for_type(Type forType) {
            return Repository.CreateTypeCodeForType(forType, AttributeExtCollection.EmptyCollection);
        }

        public Type get_type_for_tc(TypeCode tc) {
            if (!(tc is NullTC)) {
                return Repository.GetTypeForTypeCode(tc);
            } else {
                return null;
            }
        }
        
        public TypeCode create_struct_tc ([StringValue] [WideChar(false)] string id, 
                                          [StringValue] [WideChar(false)] string name,
                                          [IdlSequence(0L)] StructMember[] members) {
            return new StructTC(id, name, members);
        }
        
        public TypeCode create_value_tc ([StringValue] [WideChar(false)] string id, 
                                         [StringValue] [WideChar(false)] string name,
                                         short type_modifier,
                                         omg.org.CORBA.TypeCode concrete_base,
                                         [IdlSequence(0L)] ValueMember[] members) {
            return new ValueTypeTC(id, name,
                                   members, concrete_base, type_modifier);
        }
        
        public TypeCode create_exception_tc([StringValue] [WideChar(false)] string id, 
                                            [StringValue] [WideChar(false)] string name,
                                            [IdlSequence(0L)] StructMember[] members) {
            return new ExceptTC(id, name, members);
        }                
                
        #endregion TypeCode creation operations     
        
        #region Pseudo object operation helpers
                
        public bool is_a(object proxy, Type type) {
            if (type == null) {
                throw new ArgumentException("type must be != null");
            }
            string repId = Repository.GetRepositoryID(type);
            return is_a(proxy, repId);
            
        }
        
        public bool is_a(object proxy, string repId) {
            if (proxy == null) {
                throw new ArgumentException("proxy must be != null");
            } 
            CheckIsProxy(proxy as MarshalByRefObject);
            if (repId == null) {
                throw new ArgumentException("repId must be != null");
            }           
            
            if (repId.Equals("IDL:omg.org/CORBA/Object:1.0") ||
                repId.Equals(String.Empty)) {
                // always true
                return true;
            }
            
            // perform remote call to check for is_a
            return ((IObject)proxy)._is_a(repId);           
        }
        
        public bool non_existent(object proxy) {
            CheckIsProxy(proxy as MarshalByRefObject);
            
            return ((IObject)proxy)._non_existent();
        }
        
        #endregion Pseude object operation helpers
        #region Portable Interceptors
	    
        /// <summary>see <see cref="omg.org.CORBA.IOrbServices.RegisterPortableInterceptorInitalizer"</summary>
        public void RegisterPortableInterceptorInitalizer(ORBInitalizer initalizer) {
            lock(m_orbInitalizers.SyncRoot) {
                m_orbInitalizers.Add(initalizer);
            }
        }
	    
        /// <summary>see <see cref="omg.org.CORBA.IOrbServices.CompleteInterceptorRegistration"</summary>
        public void CompleteInterceptorRegistration() {
            lock(m_orbInitalizers.SyncRoot) {
                try {
                    m_interceptorManager.CompleteInterceptorRegistration(m_orbInitalizers);
                } finally {
                    // not needed any more
                    m_orbInitalizers.Clear();
                }
            }	        
        }
	    
        #endregion Protable Interceptors
        
        /// <summary>see <see cref="omg.org.CORBA.IOrbServices.OverrideDefaultCharSets"</summary>
        public void OverrideDefaultCharSets(Ch.Elca.Iiop.Services.CharSet charSet,
                                            Ch.Elca.Iiop.Services.WCharSet wcharSet) {
            Ch.Elca.Iiop.Services.CodeSetService.OverrideDefaultCharSets(charSet, wcharSet);
        }
        
        
        #endregion IMethods
    
    }        						

}


namespace omg.org.CORBA.ORB_package {
    
    [RepositoryIDAttribute("IDL:omg.org/CORBA/ORB/InvalidName:1.0")]
    [Serializable]
    public class InvalidName : AbstractUserException {
        
        #region IConstructors

        /// <summary>constructor needed for deserialisation</summary>
        public InvalidName() { }
        
        protected InvalidName(System.Runtime.Serialization.SerializationInfo info,
                              System.Runtime.Serialization.StreamingContext context) : base(info, context) {            
        }
        
        public InvalidName(string reason) : base(reason) {
        }                

        #endregion IConstructors

    }    
    
}



#if UnitTest

namespace Ch.Elca.Iiop.Tests {
    
    using System.IO;    
    using NUnit.Framework;    
    using omg.org.CORBA;
    using Ch.Elca.Iiop.Services;
    
    /// <summary>
    /// Unit-tests for orb services code set
    /// </summary>
    [TestFixture]
    public class OrbServicesCodeSetTest {
        
        public OrbServicesCodeSetTest() {
        }
                       
        [Test]
        public void TestOverrideCodeSetsWhenAlreadySet() {
            Assertion.Assert(Enum.IsDefined(typeof(CharSet), CodeSetService.DefaultCharSet));
            Assertion.Assert(Enum.IsDefined(typeof(WCharSet), CodeSetService.DefaultWCharSet));
            
            IOrbServices orbServices = OrbServices.GetSingleton();
            try {
                orbServices.OverrideDefaultCharSets(CharSet.UTF8, WCharSet.UTF16);
                Assertion.Fail("expected bad_inv_order exception");
            } catch (BAD_INV_ORDER) {
                // expected, because already set
            }
        }
        
    }
    
    
    /// <summary>
    /// Unit-tests for type code creation using orb services
    /// </summary>
    [TestFixture]
    public class OrbServicesTCTest {

        private IOrbServices m_orb;
        
        [SetUp]
        public void SetUp() {
            m_orb = OrbServices.GetSingleton();            
        }
        
        [Test]
        public void TestCreateTCForLongType() {                   
            int longArg = 1;            
            omg.org.CORBA.TypeCode long_TC = m_orb.create_tc_for(longArg);
            Assertion.AssertEquals("created tc kind", TCKind.tk_long,
                                   long_TC.kind());
        }
        
        [Test]
        public void TestAliasTC() {
            string name = "OrbServices_TestAlias";
            string aliasRepId = "IDL:Ch/Elca/Iiop/Tests/" + name + ":1.0";
            TypeCodeImpl aliasedTC = (TypeCodeImpl)m_orb.create_long_tc();
            omg.org.CORBA.TypeCode alias_TC =
                m_orb.create_alias_tc(aliasRepId, name, aliasedTC);
            Assertion.AssertEquals("alias id", aliasRepId, alias_TC.id());
            Assertion.AssertEquals("alias kind", TCKind.tk_alias, alias_TC.kind());
            Assertion.AssertEquals("alias cls type", aliasedTC.GetClsForTypeCode(),
                                   ((TypeCodeImpl)alias_TC).GetClsForTypeCode());            
        }
        
        [Test]
        public void TestSequenceTC() {
            TypeCodeImpl seqMemberType = (TypeCodeImpl)m_orb.create_octet_tc();            
            omg.org.CORBA.TypeCode seqOfOctet_TC = 
                m_orb.create_sequence_tc(0, seqMemberType);
            Assertion.AssertEquals("sequence kind", TCKind.tk_sequence, seqOfOctet_TC.kind());
            Assertion.AssertEquals("sequence member type", seqMemberType.GetClsForTypeCode(),
                                   ((TypeCodeImpl)seqOfOctet_TC.content_type()).GetClsForTypeCode());
        }
        
        [Test]
        public void TestStructTC() {
            string name = "OrbServices_TestStruct";
            string repId = "IDL:Ch/Elca/Iiop/Tests/" + name + ":1.0";
            
            StructMember m1 = new StructMember("M1", m_orb.create_long_tc());            
            omg.org.CORBA.TypeCode tc = 
                m_orb.create_struct_tc(repId, name,
                                       new StructMember[] { m1 });
            Assertion.AssertEquals("id", repId, tc.id());
            Assertion.AssertEquals("king", TCKind.tk_struct, tc.kind());
            Assertion.AssertEquals("nr of members", 1, tc.member_count());
            Assertion.AssertEquals("member m1 name", m1.name, tc.member_name(0));
            Assertion.AssertEquals("member m1 type", m1.type.kind(), tc.member_type(0).kind());
        }
        
        [Test]
        public void TestValueTypeTC() {
            string name = "OrbServices_TestValueType";
            string repId = "IDL:Ch/Elca/Iiop/Tests/" + name + ":1.0";
            
            ValueMember m1 = new ValueMember("M1", m_orb.create_long_tc(), 0);
            omg.org.CORBA.TypeCode tc = 
                m_orb.create_value_tc(repId, name, 0, m_orb.create_null_tc(),
                                      new ValueMember[] { m1 });
            Assertion.AssertEquals("id", repId, tc.id());
            Assertion.AssertEquals("king", TCKind.tk_value, tc.kind());
            Assertion.AssertEquals("nr of members", 1, tc.member_count());
            Assertion.AssertEquals("member m1 name", m1.name, tc.member_name(0));
            Assertion.AssertEquals("member m1 type", m1.type.kind(), tc.member_type(0).kind());
        }
        
        [Test]
        public void TestExceptTC() {
            string name = "OrbServices_TestException";
            string repId = "IDL:Ch/Elca/Iiop/Tests/" + name + ":1.0";
            
            StructMember m1 = new StructMember("M1", m_orb.create_long_tc());            
            omg.org.CORBA.TypeCode tc = 
                m_orb.create_exception_tc(repId, name,
                                       new StructMember[] { m1 });
            Assertion.AssertEquals("id", repId, tc.id());
            Assertion.AssertEquals("king", TCKind.tk_except, tc.kind());
            Assertion.AssertEquals("nr of members", 1, tc.member_count());
            Assertion.AssertEquals("member m1 name", m1.name, tc.member_name(0));
            Assertion.AssertEquals("member m1 type", m1.type.kind(), tc.member_type(0).kind());
        }
        
    }


}

#endif
