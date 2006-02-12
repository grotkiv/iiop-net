/* TestServer.cs
 *
 * Project: IIOP.NET
 * IntegrationTests
 *
 * WHEN      RESPONSIBLE
 * 27.01.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.Runtime.Remoting.Channels;

namespace Ch.Elca.Iiop.IntegrationTests {


    public class TestServer {

        public static void Main(String[] args) {
            // register the channel
            int port = 8087;
            IiopChannel chan = new IiopChannel(port);
            ChannelServices.RegisterChannel(chan);

            TestService test1 = new TestService();
            string objectURI1 = "test1";
            RemotingServices.Marshal(test1, objectURI1);
            TestService test2 = new TestService();
            string objectURI2 = "test2";
            RemotingServices.Marshal(test2, objectURI2);

            Console.WriteLine("Server running. Press any key to stop....");
            Console.ReadLine();
        }

    }

}