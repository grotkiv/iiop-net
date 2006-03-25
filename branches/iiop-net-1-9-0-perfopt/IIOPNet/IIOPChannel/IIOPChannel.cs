/* IIOPChannel.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 14.01.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Net;
using System.Collections;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using Ch.Elca.Iiop.Util;
using Ch.Elca.Iiop.CorbaObjRef;
using Ch.Elca.Iiop.Interception;
using Ch.Elca.Iiop.MessageHandling;
using omg.org.IOP;

#if DEBUG_LOGFILE
using System.IO;
#endif

namespace Ch.Elca.Iiop {

    /// <summary>
    /// This class represents a .NET Remoting channel for IIOP.
    /// </summary>
    /// <remarks>
    /// It delegates most of the work to the IiopChannelSender, 
    /// IiopChannelReceiver classes
    /// </remarks>
    public class IiopChannel : IChannelSender, IChannelReceiver {

        #region Constants

        internal const string DEFAULT_CHANNEL_NAME = "IIOPChannel";
        internal const int DEFAULT_CHANNEL_PRIORITY = 0;
                
        /// <summary>
        /// key in properties to specify a transport factory
        /// </summary>
        public const string TRANSPORT_FACTORY_KEY = "TransportFactory";
        /// <summary>
        /// key in properties to specify a channel name
        /// </summary>
        public const string CHANNEL_NAME_KEY = "name";
        /// <summary>
        /// key in properties to specify a channel priority
        /// </summary>
        public const string PRIORITY_KEY = "priority";
        
        /// <summary>
        /// key in properties to specify, if channel should be bidirectional. Default is non-bidirectional.
        /// </summary>
        public const string BIDIR_KEY = "isBidirChannel";
        
        #endregion Constants
        #region IFields

        private string m_channelName = DEFAULT_CHANNEL_NAME;
        private int m_channelPriority = DEFAULT_CHANNEL_PRIORITY;

        private IiopClientChannel m_clientChannel;
        private IiopServerChannel m_serverChannel;

        #endregion IFields
        #region IConstructors
        
        public IiopChannel() {
            m_clientChannel = new IiopClientChannel();
            // because no port is specified, server part is not used
        }

        public IiopChannel(int port) : this() {
            m_serverChannel = new IiopServerChannel(port);
        }
        
        public IiopChannel(IDictionary properties) : this(properties, 
                                                          new IiopClientFormatterSinkProvider(),
                                                          new IiopServerFormatterSinkProvider()) {
        }

        /// <summary>this constructor is used by configuration</summary>
        public IiopChannel(IDictionary properties, 
                           IClientChannelSinkProvider clientSinkProvider, 
                           IServerChannelSinkProvider serverSinkProvider) {
            IDictionary clientProp = new Hashtable();
            IDictionary serverProp = new Hashtable();
            bool isServer = false;
            bool isBidir = false;
            // prepare properties for client channel and server channel
            if (properties != null) {
                foreach (DictionaryEntry entry in properties) {
                    switch ((string)entry.Key) {
                        case CHANNEL_NAME_KEY: 
                            m_channelName = (string)entry.Value; 
                            clientProp[CHANNEL_NAME_KEY] = m_channelName;
                            serverProp[CHANNEL_NAME_KEY] = m_channelName;
                            break;
                        case PRIORITY_KEY: 
                            m_channelPriority = Convert.ToInt32(entry.Value); 
                            clientProp[PRIORITY_KEY] = m_channelPriority;
                            serverProp[PRIORITY_KEY] = m_channelPriority;
                            break;
                        case IiopServerChannel.PORT_KEY: 
                            serverProp[IiopServerChannel.PORT_KEY] = Convert.ToInt32(entry.Value); 
                            isServer = true;
                            break;
                        case IiopServerChannel.USE_IPADDRESS_KEY: 
                            serverProp[IiopServerChannel.USE_IPADDRESS_KEY] = Convert.ToBoolean(entry.Value); 
                            break;
                        case IiopServerChannel.BIND_TO_KEY:
                            serverProp[IiopServerChannel.BIND_TO_KEY] = entry.Value; // don't convert here, because conversion is also done in server channel constructor
                            break;
                        case IiopServerChannel.MACHINE_NAME_KEY:
                            serverProp[IiopServerChannel.MACHINE_NAME_KEY] = entry.Value;
                            break;
                        case IiopServerChannel.SERVERTHREADS_MAX_PER_CONNECTION_KEY:
                            serverProp[IiopServerChannel.SERVERTHREADS_MAX_PER_CONNECTION_KEY] = entry.Value;
                            break;
                        case IiopClientChannel.CLIENT_RECEIVE_TIMEOUT_KEY:
                            clientProp[IiopClientChannel.CLIENT_RECEIVE_TIMEOUT_KEY] = Convert.ToInt32(entry.Value);
                            break;
                        case IiopClientChannel.CLIENT_SEND_TIMEOUT_KEY:
                            clientProp[IiopClientChannel.CLIENT_SEND_TIMEOUT_KEY] = Convert.ToInt32(entry.Value);
                            break;
                        case IiopClientChannel.CLIENT_REQUEST_TIMEOUT_KEY:
                            clientProp[IiopClientChannel.CLIENT_REQUEST_TIMEOUT_KEY] = Convert.ToInt32(entry.Value);
                            break;
                        case IiopClientChannel.CLIENT_UNUSED_CONNECTION_KEEPALIVE_KEY:   
                            clientProp[IiopClientChannel.CLIENT_UNUSED_CONNECTION_KEEPALIVE_KEY] = Convert.ToInt32(entry.Value);
                            break;           
                        case IiopClientChannel.CLIENT_CONNECTION_LIMIT_KEY:
                            clientProp[IiopClientChannel.CLIENT_CONNECTION_LIMIT_KEY] = Convert.ToInt32(entry.Value);
                            break;
                        case IiopClientChannel.ALLOW_REQUEST_MULTIPLEX_KEY:
                            clientProp[IiopClientChannel.ALLOW_REQUEST_MULTIPLEX_KEY] = Convert.ToBoolean(entry.Value);
                            break;
                        case IiopClientChannel.MAX_NUMBER_OF_MULTIPLEXED_REQUESTS_KEY:                            
                            clientProp[IiopClientChannel.MAX_NUMBER_OF_MULTIPLEXED_REQUESTS_KEY] = Convert.ToInt32(entry.Value);
                            break;
                        case TRANSPORT_FACTORY_KEY:
                            serverProp[TRANSPORT_FACTORY_KEY] =
                                entry.Value;
                            clientProp[TRANSPORT_FACTORY_KEY] =
                                entry.Value;
                            break;
                        case BIDIR_KEY:
                            isBidir = Convert.ToBoolean(entry.Value);
                            clientProp[BIDIR_KEY] = isBidir;                            
                            break;
                        default: 
                            Debug.WriteLine("unknown property found for IIOP channel: " +
                                            entry.Key);
                            // pass non-default options further on to the client and server-channel for handling by the e.g. transport-factory
                            serverProp[entry.Key] = entry.Value;
                            clientProp[entry.Key] = entry.Value;
                            break;
                    }
                }
            }
            m_clientChannel = new IiopClientChannel(clientProp, clientSinkProvider);
            if (isServer) { 
                if (isBidir) {
                    serverProp[IiopServerChannel.BIDIR_CONNECTION_MANAGER] = 
                        m_clientChannel.ConnectionManager;
                }
                // only create server if port is specified                
                m_serverChannel = new IiopServerChannel(serverProp, serverSinkProvider);
            }
        }

        #endregion IConstructors
        #region IProperties

        public string ChannelName {
            get { 
                return m_channelName;
            }
        }

        public int ChannelPriority {
            get { 
                return m_channelPriority; 
            }
        }

        public object ChannelData {
            get {
                if (m_serverChannel != null) {
                    return m_serverChannel.ChannelData;
                } else {
                    return null;
                }
            }
        }

        #endregion IProperties
        #region IMethods
        #region Implementation of IChannelSender
        public IMessageSink CreateMessageSink(string url, 
                                              object remoteChannelData, 
                                              out string objectURI) {
            Debug.WriteLine("create message sink for client channel");
            return m_clientChannel.CreateMessageSink(url, remoteChannelData,
                                                     out objectURI);
        }

        #endregion Implementation of IChannelSender
        #region Implementation of IChannel
        public string Parse(string url, out string objectURI) {
            Debug.WriteLine("called parse with url: " + url);
            return m_clientChannel.Parse(url, out objectURI);
        }

        #endregion Implementation of IChannel
        #region Implementation of IChannelReceiver

        public void StartListening(object data) {
            if (m_serverChannel != null) {
                m_serverChannel.StartListening(data);
            }
        }

        public void StopListening(object data) {
            if (m_serverChannel != null) {
                m_serverChannel.StopListening(data);
            }
        }

        public string[] GetUrlsForUri(string objectURI) {
            if (m_serverChannel != null) {
                return m_serverChannel.GetUrlsForUri(objectURI);
            } else {
                return null;
            }
        }
        
        #endregion Implementation of IChannelReceiver
        #endregion IMethods

    }


    /// <summary>
    /// this is the client side part of the IiopChannel
    /// </summary>
    public class IiopClientChannel : IChannelSender {
    
        #region Constants

        /// <summary>
        /// the receive timeout in milliseconds
        /// </summary>
        public const string CLIENT_RECEIVE_TIMEOUT_KEY = "clientReceiveTimeOut";

        /// <summary>
        /// the send timeout in milliseconds
        /// </summary>
        public const string CLIENT_SEND_TIMEOUT_KEY = "clientSendTimeOut";

        /// <summary>
        /// the giop request timeout in milliseconds; default is infinite
        /// </summary>
        public const string CLIENT_REQUEST_TIMEOUT_KEY = "clientRequestTimeOut";
        
        /// <summary>
        /// the number of connections concurrently open to the same target.
        /// </summary>
        public const string CLIENT_CONNECTION_LIMIT_KEY = "clientConnectionLimit";
        
        /// <summary>
        /// allows to multiplex requests on the same connection, i.e. allows to send a request before
        /// the response of a previous request has been received.
        /// </summary>
        public const string ALLOW_REQUEST_MULTIPLEX_KEY = "allowRequestMultiplex";
        
        /// <summary>
        /// the number of requests maximally active concurrently on a multiplexed connection.
        /// </summary>
        public const string MAX_NUMBER_OF_MULTIPLEXED_REQUESTS_KEY = "maxNumberOfMultiplexedRequests";

        /// <summary>
        /// the time in milliseconds a unused connection is kept alive on client side; default is 300000ms
        /// </summary>
        public const string CLIENT_UNUSED_CONNECTION_KEEPALIVE_KEY = "unusedConnectionKeepAlive";   
        
        private const int UNUSED_CLIENT_CONNECTION_TIMEOUT = 300000;
        
        /// <summary>allow multiplexing of request, i.e. send a request before the response has been arrived.</summary>
        private const bool ALLOW_MULTIPLEX_REQUEST = true;
        
        /// <summary>the maximum number of multiplexed requests on the same connection</summary>
        private const int NUMBER_OF_MULTIPLEXED_MAX = 1000;
        
        /// <summary>the maximum number of connections open to the same target.</summary>
        private const int NUMBER_OF_CLIENT_CONNECTION_TO_SAME_TARGET = 5;
                
        
        #endregion Constants
        #region IFields

        private string m_channelName = IiopChannel.DEFAULT_CHANNEL_NAME;
        private int m_channelPriority = IiopChannel.DEFAULT_CHANNEL_PRIORITY;
        
        private IClientChannelSinkProvider m_providerChain;                
        private GiopClientConnectionManager m_conManager;

        #endregion IFields
        #region SConstructor

        #if DEBUG_LOGFILE
        static IiopClientChannel() {
            Stream log = File.Create("IIOPNET_DebugOutputClientChannel_"+
                             DateTime.Now.ToString("yyyyMMdd_HHmmss")+
                         ".txt");
 
            TextWriterTraceListener logListener = new TextWriterTraceListener(log);
            
            Trace.Listeners.Add(logListener);
            Trace.AutoFlush = true;
            Debug.AutoFlush = true;
        }
        #endif

        #endregion SConstructor
        #region IConstructors
        
        public IiopClientChannel() {
            InitChannel(new TcpTransportFactory(), MessageTimeout.Infinite, UNUSED_CLIENT_CONNECTION_TIMEOUT, false,
                        InterceptorManager.EmptyInterceptorOptions, NUMBER_OF_CLIENT_CONNECTION_TO_SAME_TARGET, 
                        ALLOW_MULTIPLEX_REQUEST, NUMBER_OF_MULTIPLEXED_MAX);
        }
        
        public IiopClientChannel(IDictionary properties) : 
            this(properties, new IiopClientFormatterSinkProvider()) {            
        }

        /// <summary>the constructor used by the config file</summary>
        public IiopClientChannel(IDictionary properties, IClientChannelSinkProvider sinkProvider) {
            if (!CheckSinkProviderChain(sinkProvider)) { 
                throw new ArgumentException(
                     "IIOPClientSideFormatter provider not found in chain, this channel is only usable with the IIOPFormatters"); 
            }
            m_providerChain = sinkProvider;
            IClientTransportFactory clientTransportFactory = new TcpTransportFactory();
            IDictionary nonDefaultOptions = new Hashtable();
            int receiveTimeOut = 0;
            int sendTimeOut = 0;
            MessageTimeout requestTimeOut = MessageTimeout.Infinite;
            int unusedClientConnectionTimeout = UNUSED_CLIENT_CONNECTION_TIMEOUT;
            bool isBidir = false;
            ArrayList interceptionOptions = new ArrayList();
            int maxNumberOfConnections = NUMBER_OF_CLIENT_CONNECTION_TO_SAME_TARGET;
            bool allowMultiplex = ALLOW_MULTIPLEX_REQUEST;
            int maxNumberOfMultplexedRequests = NUMBER_OF_MULTIPLEXED_MAX;
            
            if (properties != null) {
                foreach (DictionaryEntry entry in properties) {
                    switch ((string)entry.Key) {
                        case IiopChannel.CHANNEL_NAME_KEY: 
                            m_channelName = (string)entry.Value; 
                            break;
                        case IiopChannel.PRIORITY_KEY: 
                            m_channelPriority = Convert.ToInt32(entry.Value);
                            break;
                        case IiopChannel.TRANSPORT_FACTORY_KEY:
                            Type transportFactoryType = Type.GetType((string)entry.Value, true);
                            clientTransportFactory = (IClientTransportFactory)
                                Activator.CreateInstance(transportFactoryType);
                            break;
                        case IiopClientChannel.CLIENT_RECEIVE_TIMEOUT_KEY:
                            receiveTimeOut = Convert.ToInt32(entry.Value);
                            break;
                        case IiopClientChannel.CLIENT_SEND_TIMEOUT_KEY:
                            sendTimeOut = Convert.ToInt32(entry.Value);
                            break;
                        case IiopClientChannel.CLIENT_REQUEST_TIMEOUT_KEY:
                            int requestTimeOutMilllis = Convert.ToInt32(entry.Value);
                            requestTimeOut = new MessageTimeout(TimeSpan.FromMilliseconds(requestTimeOutMilllis));
                            break;
                        case IiopClientChannel.CLIENT_UNUSED_CONNECTION_KEEPALIVE_KEY:
                            unusedClientConnectionTimeout = Convert.ToInt32(entry.Value);
                            break;
                        case IiopClientChannel.CLIENT_CONNECTION_LIMIT_KEY:
                            maxNumberOfConnections = Convert.ToInt32(entry.Value);
                            break;
                        case IiopClientChannel.ALLOW_REQUEST_MULTIPLEX_KEY:
                            allowMultiplex = Convert.ToBoolean(entry.Value);
                            break;
                        case IiopClientChannel.MAX_NUMBER_OF_MULTIPLEXED_REQUESTS_KEY:
                            maxNumberOfMultplexedRequests = Convert.ToInt32(entry.Value);
                            break;
                        case IiopChannel.BIDIR_KEY:
                            isBidir = Convert.ToBoolean(entry.Value);
                            interceptionOptions.Add(new BiDirIiopInterceptionOption());
                            break;                             
                        default: 
                            Debug.WriteLine("non-default property found for IIOPClient channel: " + entry.Key);
                            nonDefaultOptions[entry.Key] = entry.Value;
                            break;
                    }
                }
            }
            
            // handle the options now by transport factory
            clientTransportFactory.SetClientTimeOut(receiveTimeOut, sendTimeOut);
            clientTransportFactory.SetupClientOptions(nonDefaultOptions);
            InitChannel(clientTransportFactory, requestTimeOut, 
                        unusedClientConnectionTimeout, isBidir, 
                        (IInterceptionOption[])interceptionOptions.ToArray(typeof(IInterceptionOption)),
                        maxNumberOfConnections, allowMultiplex, maxNumberOfMultplexedRequests);
        }

        #endregion IConstructors
        #region IProperties

        public string ChannelName {
            get { 
                return m_channelName; 
            }
        }

        public int ChannelPriority {
            get {
                return m_channelPriority; 
            }
        }
        
        /// <summary>
        /// the connection manager, which is reponsible for assigning outgoing connections.
        /// </summary>
        internal GiopClientConnectionManager ConnectionManager {
            get {
                return m_conManager;
            }
        }

        #endregion IProperties
        #region IMethods

        /// <summary>
        /// check if the custom provided provider chain contains the IIOPClientsideFormatter. This channel is not
        /// usable with another formatter ...
        /// </summary>
        private bool CheckSinkProviderChain(IClientChannelSinkProvider prov) {
            if (prov == null) { 
                return true; 
            }
            while (prov != null) {
                if (prov is IiopClientFormatterSinkProvider) { 
                    return true; 
                }
                prov = prov.Next;
            }
            return false;
        }
        
        /// <summary>
        /// Configures the installed IIOPClientSideFormatterProivder
        /// </summary>
        /// <param name="interceptionOptions"></param>
        private void ConfigureSinkProviderChain(GiopClientConnectionManager conManager,
                                                GiopMessageHandler messageHandler,
                                                IInterceptionOption[] interceptionOptions) {
            IClientChannelSinkProvider prov = m_providerChain;
            while (prov != null) {
                if (prov is IiopClientFormatterSinkProvider) {
                    ((IiopClientFormatterSinkProvider)prov).Configure(conManager, messageHandler,
                                                                      interceptionOptions);
                    break;
                }
                prov = prov.Next;
            }
        }        
        
        /// <summary>initalize this channel</summary>
        private void InitChannel(IClientTransportFactory transportFactory, MessageTimeout requestTimeOut,
                                 int unusedClientConnectionTimeOut, bool isBidir, IInterceptionOption[] interceptionOptions,
                                 int maxNumberOfConnections, bool allowMultiplex, int maxNumberOfMultplexedRequests ) {
            
            if (!isBidir) {
                m_conManager = new GiopClientConnectionManager(transportFactory, requestTimeOut,
                                                               unusedClientConnectionTimeOut, maxNumberOfConnections,
                                                               allowMultiplex, maxNumberOfMultplexedRequests);
            } else {
                m_conManager = new GiopBidirectionalConnectionManager(transportFactory, requestTimeOut,
                                                                      unusedClientConnectionTimeOut, maxNumberOfConnections,
                                                                      allowMultiplex, maxNumberOfMultplexedRequests);
            }
            IiopClientTransportSinkProvider transportProvider =
                new IiopClientTransportSinkProvider(m_conManager);
            if (m_providerChain != null) {
                // append transport provider to the chain
                IClientChannelSinkProvider prov = m_providerChain;
                while (prov.Next != null) { prov = prov.Next; }
                prov.Next = transportProvider; // append the transport provider at the end
            } else {
                // create the default provider chain
                IClientFormatterSinkProvider formatterProv = new IiopClientFormatterSinkProvider();
                formatterProv.Next = transportProvider;
                m_providerChain = formatterProv;
            }            
            GiopMessageHandler messageHandler = 
                new GiopMessageHandler(omg.org.CORBA.OrbServices.GetSingleton().ArgumentsSerializerFactory);
            ConfigureSinkProviderChain(m_conManager, messageHandler, interceptionOptions);
        }

        #region Implementation of IChannelSender

        /// <summary>
        /// create the sink chain for the url and return a reference to the first sink in the chain
        /// </summary>
        public IMessageSink CreateMessageSink(string url, object remoteChannelData, out string objectURI) {
            objectURI = null;
            if ((url != null) && IiopUrlUtil.IsUrl(url) && 
                (m_conManager.CanConnectToIor(IiopUrlUtil.CreateIorForUrl(url, "")))) {
                GiopVersion version = new GiopVersion(1, 0);
                IiopUrlUtil.ParseUrl(url, out objectURI, out version);
            
                IClientChannelSink sink = m_providerChain.CreateSink(this, url, remoteChannelData);
                if (!(sink is IMessageSink)) { 
                    throw new Exception("first sink in the client side channel must be a message-sink"); 
                }
                return (IMessageSink) sink;                
            } else if ((url == null) && (remoteChannelData is IiopChannelData)) {
                // check remoteChannelData
                Trace.WriteLine("url null, remote channel data: " + remoteChannelData);
//                IiopChannelData chanData = (IiopChannelData)remoteChannelData;
//                IClientChannelSink sink = m_providerChain.CreateSink(this, url, chanData);
//                if (!(sink is IMessageSink)) { 
//                    throw new Exception("first sink in the client side channel must be a message-sink"); 
//                }
//                return (IMessageSink) sink;
                return null; // TODO
            } else {
                return null;
            }
        }
    
        #endregion Implementation of IChannelSender
        #region Implementation of IChannel
        public string Parse(string url, out string objectURI) {
            string result;
            if (IiopUrlUtil.IsUrl(url) && (m_conManager.CanConnectToIor(IiopUrlUtil.CreateIorForUrl(url, "")))) {
                GiopVersion version;
                objectURI = null;
                Uri uri = IiopUrlUtil.ParseUrl(url, out objectURI, out version);
                result = uri.ToString();
            } else {
                // is either no corba url or is not usable by transport factory, 
                // because it doesn't support the transport protocol
                objectURI = null;
                result = null;
            }
            return result;            
        }

        #endregion Implementation of IChannel
        #endregion IMethods
                
    }
    

    /// <summary>
    /// this is the server side of the IiopChannel
    /// </summary>
    public class IiopServerChannel : IChannelReceiver {

        #region Constants
        
        /// <summary>
        /// the listening port
        /// </summary>
        public const string PORT_KEY = "port";
        /// <summary>
        /// the ip-address to use to bind-to and to. is also used as basis for machineName, if machineName not specified.
        /// </summary>
        public const string BIND_TO_KEY = "bindTo";
        /// <summary>
        /// use ip-address instead of hostname (only useful, if bindTo is not specified)
        /// </summary>
        public const string USE_IPADDRESS_KEY = "useIpAddress";
        /// <summary>
        /// used to specify the machine name, which should be used by remote clients to connect to this server side;
        /// if not specified, it is automatically determined or bindTo.ToString() is used if specified.
        /// </summary>
        public const string MACHINE_NAME_KEY = "machineName";
        
        /// <summary>
        /// the maximum number of server threads used for processing requests on a multiplexed client connection.
        /// If client serialise requests, only one thread is used. If requests arrive, before the server has
        /// sent the answer, more threads are used to process the requests in parallel up to the given limit.
        /// </summary>        
        public const string SERVERTHREADS_MAX_PER_CONNECTION_KEY = "serverThreadsMaxPerConnection";
        
        /// <summary>
        /// key used to specify the bidirectional connection manager in the server-props.
        /// </summary>
        /// <remarks>for use by IiopChannel only</remarks>
        internal const string BIDIR_CONNECTION_MANAGER = "bidirConnectionManager";                
        
        #endregion Constants
        #region IFields

        private string m_channelName = IiopChannel.DEFAULT_CHANNEL_NAME;
        private int m_channelPriority = IiopChannel.DEFAULT_CHANNEL_PRIORITY;

        private int m_port = 8085;        
        private string m_hostNameToUse;
        private IiopChannelData m_channelData;

        private bool m_useIpAddr = true;
        private IPAddress m_forcedBind;
        private string m_forcedHostNameToUse; // the configured hostname to use by remote connectors, may be null
        
        /// <summary>
        /// the maximum number of server threads used for processing requests on a multiplexed client connection.
        /// </summary>
        private int m_serverThreadsMaxPerConnection = 25;

        private IServerChannelSinkProvider m_providerChain;
        /// <summary>the standard transport sink for this channel</summary>
        private IiopServerTransportSink m_transportSink;
        
        private IServerConnectionListener m_connectionListener;
        
        private IList /* GiopClientServerMessageHandler */ m_transportHandlers = new ArrayList(); // the active transport handlers
        
        private GiopBidirectionalConnectionManager m_bidirConnectionManager = null;
        private IServerTransportFactory m_transportFactory;


        #endregion IFields
        #region SConstructor

        #if DEBUG_LOGFILE
        static IiopServerChannel() {
            Stream log = File.Create("IIOPNET_DebugOutputServerChannel_"+
                                     DateTime.Now.ToString("yyyyMMdd_HHmmss")+
                                     ".txt");
 
            TextWriterTraceListener logListener = new TextWriterTraceListener(log);
            
            Trace.Listeners.Add(logListener);
            Trace.AutoFlush = true;
            Debug.AutoFlush = true;
        }
        #endif

        #endregion SConstructor
        #region IConstructors

        public IiopServerChannel() : this(0) {            
        }

        public IiopServerChannel(int port) {
            m_port = port;
            InitChannel(new TcpTransportFactory(), InterceptorManager.EmptyInterceptorOptions);
        }
        
        public IiopServerChannel(IDictionary properties) : this(properties, new IiopServerFormatterSinkProvider()) {            
        }

        /// <summary>Constructor used by configuration</summary>
        public IiopServerChannel(IDictionary properties, IServerChannelSinkProvider sinkProvider) {
            if (!CheckSinkProviderChain(sinkProvider)) {
                throw new ArgumentException(
                    "IIOPServerSideFormatter provider not found in chain, this channel is only usable with the IIOPFormatters"); 
            }

            m_providerChain = sinkProvider;
            IServerTransportFactory serverTransportFactory =
                new TcpTransportFactory();
            IDictionary nonDefaultOptions = new Hashtable();
            ArrayList interceptionOptions = new ArrayList();

            // parse properties
            if (properties != null) {
                foreach (DictionaryEntry entry in properties) {
                    switch ((string)entry.Key) {
                        case IiopChannel.CHANNEL_NAME_KEY: 
                            m_channelName = (string)entry.Value; 
                            break;
                        case IiopChannel.PRIORITY_KEY: 
                            m_channelPriority = Convert.ToInt32(entry.Value); 
                            break;
                        case PORT_KEY: 
                            m_port = Convert.ToInt32(entry.Value); 
                            break;
                        case BIND_TO_KEY:
                            m_forcedBind = IPAddress.Parse((string)entry.Value);
                            break;
                        case USE_IPADDRESS_KEY: 
                            m_useIpAddr = Convert.ToBoolean(entry.Value); 
                            break;
                        case MACHINE_NAME_KEY:
                            m_forcedHostNameToUse = (string)entry.Value;
                            break;
                        case IiopChannel.TRANSPORT_FACTORY_KEY:
                            Type transportFactoryType = Type.GetType((string)entry.Value, true);
                            serverTransportFactory = (IServerTransportFactory)
                                Activator.CreateInstance(transportFactoryType);
                            break;
                        case SERVERTHREADS_MAX_PER_CONNECTION_KEY:
                            m_serverThreadsMaxPerConnection = Convert.ToInt32(entry.Value);
                            break;
                        case IiopServerChannel.BIDIR_CONNECTION_MANAGER:
                            m_bidirConnectionManager = (GiopBidirectionalConnectionManager)entry.Value;
                            interceptionOptions.Add(new BiDirIiopInterceptionOption());                            
                            break;
                        default: 
                            Debug.WriteLine("non-default property found for IIOPServer channel: " + entry.Key);
                            nonDefaultOptions[entry.Key] = entry.Value;
                            break;
                    }
                }
            }
            // handle non-default options now by transport factory
            serverTransportFactory.SetupServerOptions(nonDefaultOptions);
            InitChannel(serverTransportFactory, 
                        (IInterceptionOption[])interceptionOptions.ToArray(typeof(IInterceptionOption)));
        }
        
        #endregion IConstructors
        #region IProperties
        
        public string ChannelName {
            get { 
                return m_channelName; 
            }
        }

        public int ChannelPriority {
            get {
                return m_channelPriority; 
            }
        }

        public object ChannelData {
            get { 
                return m_channelData;
            }
        }

        #endregion IProperties
        #region IMethods

        /// <summary>
        /// check if the custom provided provider chain contains the IIOPServersideFormatter. This channel is not
        /// usable with another formatter!
        /// </summary>
        private bool CheckSinkProviderChain(IServerChannelSinkProvider prov) {
            if (prov == null) { 
                return true; 
            }
            while (prov != null) {
                if (prov is IiopServerFormatterSinkProvider) { 
                    return true; 
                }
                prov = prov.Next;
            }
            return false;
        }
        
        /// <summary>
        /// Configures the installed IIOPServerSideFormatterProivder
        /// </summary>        
        private void ConfigureSinkProviderChain(GiopMessageHandler messageHandler,
                                                IInterceptionOption[] interceptionOptions) {
            IServerChannelSinkProvider prov = m_providerChain;
            while (prov != null) {
                if (prov is IiopServerFormatterSinkProvider) {
                    ((IiopServerFormatterSinkProvider)prov).Configure(messageHandler,
                                                                      interceptionOptions);
                    break;
                }
                prov = prov.Next;
            }
        }
        
        /// <summary>initalize the channel</summary>
        private void InitChannel(IServerTransportFactory transportFactory, IInterceptionOption[] interceptionOptions) {
            if (m_port < 0) {
                throw new ArgumentException("illegal port to listen on: " + m_port); 
            }
            m_transportFactory = transportFactory;
            m_hostNameToUse = DetermineMachineNameToUse();
            SetupChannelData(m_hostNameToUse, m_port, null);
            m_connectionListener =
                transportFactory.CreateConnectionListener(new ClientAccepted(this.ProcessClientMessages));
            
            // create the default provider chain, if no chain specified
            if (m_providerChain == null) {
                m_providerChain = new IiopServerFormatterSinkProvider();
            }
            GiopMessageHandler messageHandler = 
                new GiopMessageHandler(omg.org.CORBA.OrbServices.GetSingleton().ArgumentsSerializerFactory);
            ConfigureSinkProviderChain(messageHandler, interceptionOptions);            
            
            IServerChannelSink sinkChain = ChannelServices.CreateServerChannelSinkChain(m_providerChain, this);
            m_transportSink = new IiopServerTransportSink(sinkChain);            

            if (m_bidirConnectionManager != null) {
                // bidirectional entry point into server channel sink chain.
                m_bidirConnectionManager.RegisterMessageReceptionHandler(m_transportSink, 
                                                                         m_serverThreadsMaxPerConnection);
            }
            
            // ready to wait for messages
            StartListening(null);
            // publish init-service
            Services.CORBAInitServiceImpl.Publish();
            // public the handler for generic corba operations
            StandardCorbaOps.SetUpHandler();                       
        }
        
        private string DetermineMachineNameToUse() {
            string hostNameToUse;
            if (m_forcedHostNameToUse != null) {
                hostNameToUse = m_forcedHostNameToUse;
            } else if (m_forcedBind != null) {
                hostNameToUse = m_forcedBind.ToString();
            } else {            
                string hostName = Dns.GetHostName();
                if (m_useIpAddr) {
                    IPHostEntry ipEntry = Dns.GetHostByName(hostName);
                    IPAddress[] ipAddrs = ipEntry.AddressList;
                    if ((ipAddrs == null) || (ipAddrs.Length == 0)) { 
                        throw new ArgumentException("can't determine ip-addr of local machine, abort channel creation"); 
                    }                    
                    hostNameToUse = ipAddrs[0].ToString();
                } else {
                    hostNameToUse = hostName;
                }
            }
            return hostNameToUse;
        }

        private void SetupChannelData(string hostName, int port, TaggedComponent[] additionalComponents) {
            IiopChannelData newChannelData = new IiopChannelData(hostName, port);
            newChannelData.AddAdditionalTaggedComponent(
                Services.CodeSetService.CreateDefaultCodesetComponent());
            if ((additionalComponents != null) && (additionalComponents.Length > 0)){
                newChannelData.AddAdditionalTaggedComponents(additionalComponents);
            }
            m_channelData = newChannelData;
        }

        #region Implementation of IChannelReceiver
        public void StartListening(object data) {
            // start Listening
            if (!m_connectionListener.IsListening()) {
                TaggedComponent[] additionalComponents;
                // use IPAddress.Any and not a specific ip-address, to allow connections to loopback and normal ip; but if forcedBind use the specified one
                IPAddress bindTo = (m_forcedBind == null ? IPAddress.Any : m_forcedBind);
                int listeningPort = m_connectionListener.StartListening(bindTo, m_port, out additionalComponents);
                SetupChannelData(m_hostNameToUse, listeningPort, additionalComponents);
                // register endpoints for bidirectional connections (if bidir enabled)
                if (m_bidirConnectionManager != null) {                
                    object[] listenPoints = m_transportFactory.GetListenPoints(m_channelData);
                    m_bidirConnectionManager.SetOwnListenPoints(listenPoints);
                }                
            }
        }

        /// <summary>
        /// this method handles the incoming messages; it's called by the IServerListener
        /// </summary>
        private void ProcessClientMessages(IServerTransport transport) {
            GiopTransportMessageHandler handler = new GiopTransportMessageHandler(transport);
            GiopConnectionDesc conDesc = new GiopConnectionDesc(m_bidirConnectionManager, handler);            
            handler.InstallReceiver(m_transportSink, conDesc, m_serverThreadsMaxPerConnection);
            m_transportHandlers.Add(handler);
            handler.StartMessageReception();            
        }
            
        public void StopListening(object data) {
            if (m_connectionListener.IsListening()) {                                
                // don't accept new connections any more.
                try {
                    m_connectionListener.StopListening();
                } catch (Exception ex) {
                    Debug.WriteLine("exception while stopping accept: " + ex);
                }
                // close connections to this server endpoint
                try {
                    foreach (GiopTransportMessageHandler handler in m_transportHandlers) {
                        try {
                            try {
                                handler.SendConnectionCloseMessage();
                            } finally {
                                handler.ForceCloseConnection();
                            }
                        } catch (Exception ex) {
                            Debug.WriteLine("exception while trying to close connection: " + ex);
                        }
                    }
                } finally {
                    m_transportHandlers.Clear();                
                }
                // bidir connection data no longer usable/used -> clean up at the end
                if (m_bidirConnectionManager != null) {
                    try {
                        // for bidir use case (1), no more connections initiated by a client to this server endpoint
                        // should be used for callbacks
                        m_bidirConnectionManager.RemoveAllBidirInitiated();
                    } catch (Exception ex) {
                        Debug.WriteLine("exception while trying to remove registered bidir connections: " + ex);
                    }
                    try {
                        // for bidir use case (2), don't send any more listen points to server, because no longer listening -> 
                        // server can't connect back using the bidir connections registered
                        m_bidirConnectionManager.SetOwnListenPoints(new object[0]);
                    } catch (Exception ex) {
                        Debug.WriteLine("exception while trying to remove registered bidir connections: " + ex);
                    }
                }                
            }
        }

        public string[] GetUrlsForUri(string objectURI) {
            return new string[] {
                IiopUrlUtil.GetUrl(m_channelData.HostName, m_channelData.Port, objectURI) };
        }

        #endregion Implementation of IChannelReceiver
        #region Implementation of IChannel
            
        public string Parse(string url, out string objectURI) {
            objectURI = null;
            GiopVersion version;
            return IiopUrlUtil.ParseUrl(url, out objectURI, out version).ToString();
        }

        #endregion Implementation of IChannel
        #endregion IMethods
        
    }


    /// <summary>
    /// This class is used to hold the IiopChannel specific data
    /// </summary>
    /// <remarks>
    /// RemotingServices.Marshal queries all remoting channels registered for their channel-data.
    /// The IIOPChannel returns an instance of this class.
    /// </remarks>
    [Serializable] // must be serializable for the .NET framework
    public class IiopChannelData : ChannelDataStore {

        #region SFields
        
        private Type s_taggedComponentType = typeof(TaggedComponent);
        
        #endregion SFields
        #region IFields
        private string m_hostName;
        private int m_port;
        
        private ArrayList m_additionTaggedComponents = new ArrayList();
        #endregion IFields
        #region IConstructors

        public IiopChannelData(string hostName, int port) : base(new String[] { "iiop://"+hostName+":"+port } ) {
            m_hostName = hostName;
            m_port = port;
        }
        #endregion IConstructors
        #region IProperties

        public string HostName {
            get { return m_hostName; }
        }
        
        public int Port {
            get { return m_port; }
        }
        
        /// <summary>allows to add additional tagged component to an IOR marshalled over this channel.</summary>
        public TaggedComponent[] AdditionalTaggedComponents {
            get {
                return (TaggedComponent[])m_additionTaggedComponents.ToArray(s_taggedComponentType);
            }
        }

        #endregion
        #region IMethods

        public override String ToString() {
            StringBuilder result = new StringBuilder();
            result.Append("IIOP-channel data, hostname: " + m_hostName +
                          ", port: " + m_port);
            foreach (TaggedComponent taggedComp in m_additionTaggedComponents) {
                result.Append("; tagged component with id: " + taggedComp.tag);
            }
            return result.ToString();
        }
        
        /// <summary>add passed additional tagged component to all IOR for objects hosted by this appdomain.</summary>
        public void AddAdditionalTaggedComponent(TaggedComponent taggedComponent) {
            m_additionTaggedComponents.Add(taggedComponent);
        }
        
        /// <summary>adds passed additional tagged components to all IOR for objects hosted by this appdomain.</summary>
        public void AddAdditionalTaggedComponents(TaggedComponent[] newTaggedComponents) {
            m_additionTaggedComponents.AddRange(newTaggedComponents);
        }
        
        #endregion IMethods

    }
    
}

#if UnitTest

namespace Ch.Elca.Iiop.Tests {
    
    using NUnit.Framework;
    using Ch.Elca.Iiop.Services;
    using Ch.Elca.Iiop;
    
    /// <summary>
    /// Unit-test for class IiopChannelData
    /// </summary>
    [TestFixture]    
    public class IiopChannelDataTest {
        
        private const string HOST = "localhost";
        private const int PORT = 8087;
        
        private IiopChannelData m_channelData;
        
        [SetUp]
        public void Setup() {
            m_channelData = new IiopChannelData(HOST, PORT);
        }
        
        [Test]
        public void TestCorrectCreation() {
            Assertion.AssertEquals("Host", HOST, m_channelData.HostName);
            Assertion.AssertEquals("Port", PORT, m_channelData.Port);
            Assertion.AssertEquals("No components by default", 0, 
                                   m_channelData.AdditionalTaggedComponents.Length);
            Assertion.AssertEquals("chan uris length", 1,
                                   m_channelData.ChannelUris.Length);
            Assertion.AssertEquals("chan uri 1", "iiop://" + HOST + ":" + PORT,
                                   m_channelData.ChannelUris[0]);
        }
        
        [Test]
        public void AddComponent() {
            TaggedComponent comp = 
                TaggedComponent.CreateTaggedComponent(TAG_CODE_SETS.ConstVal,
                                                      new Services.CodeSetComponentData(10000,
                                                                                        new int[0],
                                                                                        20000,
                                                                                        new int[0]));                                    
            m_channelData.AddAdditionalTaggedComponent(comp);
            Assertion.AssertEquals("Component not added correctly", 1, 
                                   m_channelData.AdditionalTaggedComponents.Length);
            Assertion.AssertEquals("Component not added correctly", comp.tag, 
                                   m_channelData.AdditionalTaggedComponents[0].tag);
        }
        
    }
    
}

#endif