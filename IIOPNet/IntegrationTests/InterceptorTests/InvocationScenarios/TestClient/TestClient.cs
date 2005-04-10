/* TestClient.cs
 *
 * Project: IIOP.NET
 * IntegrationTests
 *
 * WHEN      RESPONSIBLE
 * 08.07.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Collections;
using NUnit.Framework;
using Ch.Elca.Iiop;
using Ch.Elca.Iiop.Idl;
using Ch.Elca.Iiop.Services;
using Ch.Elca.Iiop.Interception;
using omg.org.CosNaming;
using omg.org.CORBA;
using omg.org.PortableInterceptor;

namespace Ch.Elca.Iiop.IntegrationTests {


    [TestFixture]
    public class TestClient {

        #region IFields

        private IiopClientChannel m_channel;

        private TestService m_testService;

        private TestInterceptorInit m_testInterceptorInit;

        #endregion IFields
        #region IMethods


        private void RegisterInterceptors() {
            IOrbServices orb = OrbServices.GetSingleton();
            m_testInterceptorInit = new TestInterceptorInit();
            orb.RegisterPortableInterceptorInitalizer(m_testInterceptorInit);
            orb.CompleteInterceptorRegistration();
        }


        [SetUp]
        public void SetupEnvironment() {
            // register the channel
            if (m_channel == null) {
                m_channel = new IiopClientChannel();
                ChannelServices.RegisterChannel(m_channel);

                RegisterInterceptors();

                // get the reference to the test-service
        	m_testService = (TestService)RemotingServices.Connect(typeof(TestService), "corbaloc:iiop:1.2@localhost:8087/test");
            }
        }

        [TearDown]
        public void TearDownEnvironment() {
        }
        
        [Test]
        public void TestNonExceptionScenario() {
            try {
                System.Byte arg = 1;
                System.Byte result = m_testService.TestIncByte(arg);
                Assertion.AssertEquals((System.Byte)(arg + 1), result);

                Assertion.Assert("expected: a on out path called", m_testInterceptorInit.A.InvokedOnOutPath);
                Assertion.Assert("expected: b on out path called", m_testInterceptorInit.B.InvokedOnOutPath);
                Assertion.Assert("expected: c on out path called", m_testInterceptorInit.C.InvokedOnOutPath);

                Assertion.AssertEquals("a on in path called (reply)", 
                                       InPathResult.Reply, m_testInterceptorInit.A.InPathResult);
                Assertion.AssertEquals("b on in path called (reply)",
                                       InPathResult.Reply, m_testInterceptorInit.B.InPathResult);
                Assertion.AssertEquals("c on in path called (reply)", 
                                       InPathResult.Reply, m_testInterceptorInit.C.InPathResult);
            } finally {
                m_testInterceptorInit.A.ClearInvocationHistory();
                m_testInterceptorInit.B.ClearInvocationHistory();
                m_testInterceptorInit.C.ClearInvocationHistory();
            }            
        }

        [Test]
        public void TestServerUserExceptionScenario() {
            try {
                try {
                    m_testService.TestThrowException();
                    Assertion.Fail("no exception");
                } catch (TestServerSideException) {
                    // ok, expected
                }

                Assertion.Assert("expected: a on out path called", m_testInterceptorInit.A.InvokedOnOutPath);
                Assertion.Assert("expected: b on out path called", m_testInterceptorInit.B.InvokedOnOutPath);
                Assertion.Assert("expected: c on out path called", m_testInterceptorInit.C.InvokedOnOutPath);

                Assertion.AssertEquals("a on in path called (exception)", 
                                       InPathResult.Exception, m_testInterceptorInit.A.InPathResult);
                Assertion.AssertEquals("b on in path called (exception)",
                                       InPathResult.Exception, m_testInterceptorInit.B.InPathResult);
                Assertion.AssertEquals("c on in path called (exception)", 
                                       InPathResult.Exception, m_testInterceptorInit.C.InPathResult);
            } finally {
                m_testInterceptorInit.A.ClearInvocationHistory();
                m_testInterceptorInit.B.ClearInvocationHistory();
                m_testInterceptorInit.C.ClearInvocationHistory();
            }            
        }

        [Test]
        public void TestExceptionOutPath() {
            try {
                m_testInterceptorInit.B.SetExceptionOnOutPath(new BAD_OPERATION(1000, CompletionStatus.Completed_No));
                try {
                    System.Byte arg = 1;
                    System.Byte result = m_testService.TestIncByte(arg);
                    Assertion.Fail("no exception");
                } catch (BAD_OPERATION) {
                    // ok, expected
                }

                Assertion.Assert("expected: a on out path called", m_testInterceptorInit.A.InvokedOnOutPath);
                Assertion.Assert("expected: b on out path called", m_testInterceptorInit.B.InvokedOnOutPath);
                Assertion.Assert("expected: c on out path not called", !m_testInterceptorInit.C.InvokedOnOutPath);

                Assertion.AssertEquals("a on in path called (exception)", 
                                       InPathResult.Exception, m_testInterceptorInit.A.InPathResult);
                Assertion.AssertEquals("b on in path called",
                                       InPathResult.NotCalled, m_testInterceptorInit.B.InPathResult);
                Assertion.AssertEquals("c on in path called", 
                                       InPathResult.NotCalled, m_testInterceptorInit.C.InPathResult);
            } finally {
                m_testInterceptorInit.A.ClearInvocationHistory();
                m_testInterceptorInit.B.ClearInvocationHistory();
                m_testInterceptorInit.C.ClearInvocationHistory();
            }            
        }

        #endregion IMethods


    }

}
