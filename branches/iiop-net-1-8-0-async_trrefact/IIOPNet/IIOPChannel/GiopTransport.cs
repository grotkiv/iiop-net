/* GiopTransport.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 17.01.04  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.IO;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Collections;
using System.Collections.Specialized;
using System.Net;
using Ch.Elca.Iiop.Cdr;
using Ch.Elca.Iiop.Util;
using Ch.Elca.Iiop.MessageHandling;
using omg.org.CORBA;


namespace Ch.Elca.Iiop {
    
    
    /// <summary>
    /// represents a message timeout (can be infinite or finite)
    /// </summary>
    internal class MessageTimeout {
        
    	#region SFields
    	
    	private static MessageTimeout s_infiniteTimeOut = new MessageTimeout();
    	
    	#endregion SFields
    	#region IFields
    	
        private object m_timeOut = null;
        
        #endregion IFields
        #region IConstructors
        
        /// <summary>
        /// infinite connection timeout
        /// </summary>
        private MessageTimeout() {
            m_timeOut = null;
        }
        
        /// <summary>
        /// timeout set to the argument parameter.
        /// </summary>
        internal MessageTimeout(TimeSpan timeOut) {
            m_timeOut = timeOut;
        }

        #endregion IConstructors
        #region SProperties

        /// <summary>
        /// returns an instance of MessageTimeout with infinite timeout.
        /// </summary>
        internal static MessageTimeout Infinite {
        	get {
        		return s_infiniteTimeOut;
        	}
        }
        
        #endregion SProperties        
        #region IProperties
        
        internal TimeSpan TimeOut {
            get {
                if (m_timeOut != null) {
                    return (TimeSpan)m_timeOut;
                } else {
                    throw new BAD_OPERATION(109, CompletionStatus.Completed_MayBe);
                }
            }
        }
        
        /// <summary>
        /// is no timeout defined ?
        /// </summary>
        internal bool IsUnlimited {
            get {
                return m_timeOut == null;
            }
        }                
        
        #endregion IProperties
                                
    }
    
    
    /// <summary>
    /// inteface of a giop request message receiver;    
    /// </summary>
    /// <remarks>the methods of this interface are called in a ThreadPool thread</remarks>
    internal interface IGiopRequestMessageReceiver {
                
        void ProcessRequest(Stream requestStream, GiopClientServerMessageHandler transportHandler);
        
        void ProcessLocateRequest(Stream requestStream, GiopClientServerMessageHandler transportHandler);
        
    }        
    
    /// <summary>
    /// encapsulates a message to send and keeps track of what has already been sent
    /// </summary>
    internal class MessageSendTask {
        
        #region Constants
        
        /// <summary>
        /// specifies, how much should be read in one step from message to send
        /// </summary>
        private const int READ_CHUNK_SIZE = 8192;

        #endregion Constants
        #region Fields
        
        private long m_bytesAlreadySent;
        private long m_bytesToSend;        
        private long m_reqId;
        private Stream m_messageToSend;
        private ITransport m_onTransport;
        private GiopTransportMessageHandler m_messageHandler;
        
        private byte[] m_buffer = new byte[READ_CHUNK_SIZE];
        
        #endregion Fields
        #region IConstructors
        
        public MessageSendTask(Stream message, long bytesToSend, ITransport onTransport,
                               GiopTransportMessageHandler messageHandler) {
            Initalize(message, bytesToSend, -1, onTransport, messageHandler);            
        }

        
        public MessageSendTask(Stream message, long bytesToSend, uint reqId, ITransport onTransport,
                               GiopTransportMessageHandler messageHandler) {
            Initalize(message, bytesToSend, reqId, onTransport, messageHandler);
        }
        
        #endregion IConstructors
        #region IProperties
        
        internal uint RequestId {
            get {
                if (HasRequestId()) {
                    return (uint)m_reqId;
                } else {
                    throw new Exception("no request id present for this message");
                }
            }
        }
        
        #endregion IProperties
        
        private void Initalize(Stream message, long bytesToSend, long reqId, ITransport onTransport,
                               GiopTransportMessageHandler messageHandler) {
            m_messageToSend = message;
            m_bytesToSend = bytesToSend;            
            m_reqId = reqId;
            m_onTransport = onTransport;           
            m_messageHandler = messageHandler;
        }
        
        internal bool HasRequestId() {
            return m_reqId >= 0;
        }
        
        /// <summary>
        /// begins the send of the next message part on the transport;
        /// notifies callback about progress
        /// </summary>
        internal void StartSendNextPart() {
            if (HasNextPartToSend()) {
            // need more data
                long nrOfBytesToRead = m_bytesToSend - m_bytesAlreadySent;
                int toRead = (int)Math.Min(m_buffer.Length,
                                           nrOfBytesToRead);
                
                // read either the whole buffer length or 
                // the remaining nr of bytes: nrOfBytesToRead - bytesRead
                int bytesToSendInProgress = m_messageToSend.Read(m_buffer, 0, toRead);
                if (bytesToSendInProgress <= 0) {
                    // underlying stream not enough data
                    throw new omg.org.CORBA.INTERNAL(88, CompletionStatus.Completed_MayBe);
                }                                                
                m_bytesAlreadySent += bytesToSendInProgress;
                m_onTransport.BeginWrite(m_buffer, 0, bytesToSendInProgress, new AsyncCallback(this.HandleSentCompleted), this);
            } else {
                throw new INTERNAL(89, CompletionStatus.Completed_MayBe);
            }
        }

        private void HandleSentCompleted(IAsyncResult ar) {
            try {
                m_onTransport.EndWrite(ar); // complete the write task                            
                if (HasNextPartToSend()) {
                    StartSendNextPart();
                } else {                    
                    m_messageHandler.MsgSentCallback(this); // doesn't throw exceptions
                }
            } catch (Exception ex) {
                m_messageHandler.MsgSentCallbackException(this, ex);
            }
        }
        
        /// <summary>
        /// returns true, if StartSendNextPart should be called to send another part of the message, else 
        /// returns false to indicate that the message is completely sent
        /// </summary>
        /// <returns></returns>
        internal bool HasNextPartToSend() {
            return m_bytesAlreadySent < m_bytesToSend;
        }
                        
    }            
    
    /// <summary>
    /// encapsulates a message to receive and keeps track of what has already been received
    /// </summary>
    internal class MessageReceiveTask {

        #region Constants
        
        /// <summary>
        /// specifies, how much should be read in one step from message to send
        /// </summary>
        private const int READ_CHUNK_SIZE = 8192;

        #endregion Constants
        #region Fields

        private ITransport m_onTransport;
        private GiopTransportMessageHandler m_messageHandler;
        
        private GiopHeader m_header = null;
        private Stream m_messageToReceive;        
        private int m_expectedMessageLength;
        private int m_bytesRead;
        
        private byte[] m_buffer = new byte[READ_CHUNK_SIZE];
        private byte[] m_giopHeaderBuffer = new byte[GiopHeader.HEADER_LENGTH];
        
        #endregion Fields
        #region IConstructors
        
        public MessageReceiveTask(ITransport onTransport,
                                  GiopTransportMessageHandler messageHandler) {            
            m_onTransport = onTransport;
            m_messageHandler = messageHandler;
        }
        
        #endregion IConstructors
        #region IProperties
                
        public Stream MessageStream {
            get {
                return m_messageToReceive;
            }
        }             
        
        public GiopHeader Header {
            get {
                if (m_header == null) {
                    throw new INTERNAL(100, CompletionStatus.Completed_MayBe);
                }
                return m_header;
            }
        }
                
        #endregion IProperties
        #region IMethods
        
        /// <summary>
        /// begins receiving a new message from transport; can be called again, after message has been completed.
        /// </summary>
        public void StartReceiveMessage() {
            m_messageToReceive = new MemoryStream();
            m_header = null;
            m_expectedMessageLength = GiopHeader.HEADER_LENGTH; // giop header-length
            m_bytesRead = 0;

            StartReceiveNextMessagePart();
        }
        
        private void StartReceiveNextMessagePart() {
            int toRead = Math.Min(READ_CHUNK_SIZE,
                                       m_expectedMessageLength - m_bytesRead);
            m_onTransport.BeginRead(m_buffer, 0, toRead, new AsyncCallback(this.HandleReadCompleted), this);
        }
        

        private void HandleReadCompleted(IAsyncResult ar) {
            try {            
                int read = m_onTransport.EndRead(ar);
                if (read <= 0) {
                    // connection has been closed by the other end
                    m_messageHandler.MsgReceivedConnectionClosedException(this);
                    return;
                }
                int offset = m_bytesRead;
                m_bytesRead += read;
                // copy to message stream
                m_messageToReceive.Write(m_buffer, 0, read);
                // handle header
                if (m_header == null) {
                    // copy to giop-header buffer
                    Array.Copy(m_buffer, 0, m_giopHeaderBuffer, offset, read);
                    if (m_bytesRead == 12) {
                        m_header = new GiopHeader(m_giopHeaderBuffer);
                        m_expectedMessageLength = (int)(m_expectedMessageLength + m_header.ContentMsgLength);
                    }
                }
                if (HasNextMessagePart()) {
                    StartReceiveNextMessagePart();
                } else {
                    // completed
                    m_messageToReceive.Seek(0, SeekOrigin.Begin);
                    m_messageHandler.MsgReceivedCallback(this);
                }
            } catch (Exception ex) {
                m_messageHandler.MsgReceivedCallbackException(this, ex);
            }
        }
                
        private bool HasNextMessagePart() {
            return m_bytesRead < m_expectedMessageLength;
        }
                
        #endregion IMethods                
        
    }
    
    
    /// <summary>
    /// this class is responsible for reading/writing giop messages
    /// </summary>
    public class GiopTransportMessageHandler {                
        
        #region ResponseWaiter-Types
        
        /// <summary>
        /// inteface for a helper class, which waits for a response
        /// </summary>
        internal interface IResponseWaiter {
            Stream Response {
                get;
                set;
            }
            Exception Problem {
                get;
                set;
            }
            
            /// <summary>
            /// is called by MessageHandler, if the suitable response has been received
            /// </summary>
            void Notify();
            
            /// <summary>
            /// prepare for receiving notify; can block the current thread (notify will then unblock)
            /// </summary>
            /// <returns>false, if the wait has been interrupted, otherwise true</returns>
            bool StartWaiting();
            
            
            /// <summary>
            /// is notified, when the response waiter is no longer needed; after completed,
            /// the instance must not be used any more
            /// </summary>
            void Completed();
        }        
        
        internal class SynchronousResponseWaiter : IResponseWaiter {
            
            private Stream m_response;
            private Exception m_problem;
            private ManualResetEvent m_waiter;            
            private GiopTransportMessageHandler m_handler;
            
            public SynchronousResponseWaiter(GiopTransportMessageHandler handler) {   
                m_waiter = new ManualResetEvent(false);
                m_handler = handler;
            }
            
            /// <summary>
            /// the response if successfully completed, otherwise null.
            /// </summary>
            public Stream Response {
                get {
                    return m_response;
                }
                set {
                    m_response = value;
                }
            }
            
            /// <summary>
            /// the problem, if one has occured
            /// </summary>
            public Exception Problem {
                get {
                    return m_problem;    
                }
                set {
                    m_problem = value;
                }
            }
            
            public void Notify() {
                m_waiter.Set();
            }
            
            public bool StartWaiting() {
                return m_handler.WaitForEvent(m_waiter);
            }
            
            public void Completed() {
                // dispose this handle
                m_waiter.Close();
            }
            
        }
    
        internal class AsynchronousResponseWaiter : IResponseWaiter {
            
            private GiopTransportMessageHandler m_transportHandler;
            private Stream m_response;
            private Exception m_problem;
            private AsyncResponseAvailableCallBack m_callback;
            private IClientChannelSinkStack m_clientSinkStack;
            private GiopClientConnection m_clientConnection;
            private Timer m_timer;
            private MessageTimeout m_timeOut;
            private volatile bool m_alreadyNotified;
            private uint m_requestId;
                        
            internal AsynchronousResponseWaiter(GiopTransportMessageHandler transportHandler,
                                                uint requestId,
                                                AsyncResponseAvailableCallBack callback,
                                                IClientChannelSinkStack clientSinkStack,
                                                GiopClientConnection connection, 
                                                MessageTimeout timeOut) {
                Initalize(transportHandler, requestId, callback, clientSinkStack, connection, timeOut);
            }            
                        
            
            /// <summary>
            /// the response if successfully completed, otherwise null.
            /// </summary>
            public Stream Response {
                get {
                    return m_response;
                }
                set {
                    m_response = value;
                }
            }
            
            /// <summary>
            /// the problem, if one has occured
            /// </summary>
            public Exception Problem {
                get {
                    return m_problem;    
                }
                set {
                    m_problem = value;
                }
            }            
            
            private void Initalize(GiopTransportMessageHandler transportHandler,
                                   uint requestId,
                                   AsyncResponseAvailableCallBack callback,
                                   IClientChannelSinkStack clientSinkStack,
                                   GiopClientConnection connection, 
                                   MessageTimeout timeOutMillis) {
                m_alreadyNotified = false;                
                m_transportHandler = transportHandler;
                m_requestId = requestId;
                m_callback = callback;
                m_clientConnection = connection;
                m_clientSinkStack = clientSinkStack;
                m_timeOut = timeOutMillis;                
            }                            
            
            public void Notify() {
                if (!m_alreadyNotified) {
                    lock(this) {
                        if (!m_alreadyNotified) {
                            m_alreadyNotified = true;
                            Completed();
                            m_callback(m_clientSinkStack, m_clientConnection,
                                       Response, Problem);
                        }
                    }
                }
                
            }
            
            public bool StartWaiting() {
                if (!m_timeOut.IsUnlimited) {
                    m_timer = new Timer(new TimerCallback(this.TimeOutCallback),
                                        null,
                                        (int)Math.Round(m_timeOut.TimeOut.TotalMilliseconds),
                                        Timeout.Infinite);                    
                }
                return true;
            }
            
            public void Completed() {
                if (m_timer != null) {                    
                    m_timer.Dispose();
                }
                // nothing special to do
            }            
            
            private void TimeOutCallback(object state) {
                if (!m_alreadyNotified) {
                    lock(this) {
                        if (!m_alreadyNotified) {
                            m_alreadyNotified = true;                            
                            m_transportHandler.CancelWaitForResponseMessage(m_requestId);
                            Completed();
                            m_transportHandler.ForceCloseConnection(); // close the connection after a timeout
                            m_callback(m_clientSinkStack, m_clientConnection,
                                       null, new TIMEOUT(32, CompletionStatus.Completed_MayBe));
                        }
                    }
                }                
            }

            
        }
        
        #endregion ResponseWaiter-Types
        #region IFields
                
        private ITransport m_transport;
        private MessageTimeout m_timeout;
        private AutoResetEvent m_writeLock;
        
        private IDictionary m_waitingForResponse = new ListDictionary();
        private FragmentedMessageAssembler m_fragmentAssembler =
            new FragmentedMessageAssembler();

        private MessageReceiveTask m_messageReceiveTask;
        
        #endregion IFields
        #region IConstructors
        
        /// <summary>creates a giop transport message handler, which doesn't accept request messages</summary>
        internal GiopTransportMessageHandler(ITransport transport) : this(transport, MessageTimeout.Infinite) {            
        }
        
        /// <summary>creates a giop transport message handler, which doesn't accept request messages</summary>
        internal GiopTransportMessageHandler(ITransport transport, MessageTimeout timeout) {            
            Initalize(transport, timeout);
        }
        
        #endregion IConstructors
        #region IProperties
        
        internal ITransport Transport {
            get {
                return m_transport;
            }
        }
        
        #endregion IProperties
        #region IMethods        
        
        private void Initalize(ITransport transport, MessageTimeout timeout) {
            m_transport = transport;            
            m_timeout = timeout;            
            m_writeLock = new AutoResetEvent(true);
        }    
                
        #region Synchronization
        
        /// <summary>
        /// wait for an event, considering timeout.
        /// </summary>
        /// <returns>true, if ok, false if timeout occured</returns>
        private bool WaitForEvent(WaitHandle waiter) {
            if (!m_timeout.IsUnlimited) {
                return waiter.WaitOne(m_timeout.TimeOut, false);
            } else {
                return waiter.WaitOne();
            }
        }
        
        private bool WaitForWriteLock() {
            return WaitForEvent(m_writeLock); // wait for the right to write to the stream
        }
        
        #endregion Synchronization
        #region ConnectionClose
        
        /// <summary>
        /// close a connection, make sure, that no read task is pending.
        /// </summary>
        internal void ForceCloseConnection() {
            try {
                m_transport.CloseConnection();                
            } catch (Exception ex) {
                Trace.WriteLine("problem to close connection: " + ex);
            }
            try {
                StopMessageReception();
            } catch (Exception ex) {
                Trace.WriteLine("problem to stop message reception: " + ex);
            }
        }
        
        #endregion ConnectionClose
        #region Exception handling
        
        private void CloseConnectionAfterTimeout() {
            try {
                Trace.WriteLine("closing connection because of timeout");
                ForceCloseConnection();
            } catch (Exception ex) {
                Debug.WriteLine("exception while trying to close connection after a timeout: " + ex);
            }
        }
        
        private void CloseConnectionAfterUnexpectedException(Exception uex) {
            try {
                Trace.WriteLine("closing connection because of unexpected exception: " + uex);
                ForceCloseConnection();
            } catch (Exception ex) {
                Debug.WriteLine("exception while trying to close connection: " + ex);
            }
        }
        
        #endregion Exception handling
        #region Cancel Pending
        
        /// <summary>abort all requests, which wait for a reply</summary>
        private void AbortAllPendingRequestsWaiting() {
            lock (m_waitingForResponse.SyncRoot) {
                try {
                    foreach (DictionaryEntry entry in m_waitingForResponse) {
                        try {
                            IResponseWaiter waiter = (IResponseWaiter)entry.Value;
                            waiter.Problem = new omg.org.CORBA.COMM_FAILURE(209, CompletionStatus.Completed_MayBe);
                            waiter.Notify();
                        } catch (Exception ex) {
                            Debug.WriteLine("exception while aborting message: " + ex);
                        }
                    }
                    m_waitingForResponse.Clear();
                } catch (Exception) {
                    // ignore
                }
            }
        }
        
        #endregion Cancel Pending
        #region Sending messages

        /// <summary>
        /// sets the stream to offset 0. Returns the length of the stream.
        /// </summary>                
        private long PrepareStreamToSend(Stream stream) {
            stream.Seek(0, SeekOrigin.Begin);
            return stream.Length;
        }
        
        private void StartWriteRequestMessage(Stream stream, uint requestId) {
            long bytesToSend = PrepareStreamToSend(stream);
            MessageSendTask task = new MessageSendTask(stream, bytesToSend, requestId,
                                                       m_transport, this);
            StartSendMessageTask(task);
        }
        
        private void StartWriteMessageWithoutReqId(Stream stream) {
            long bytesToSend = PrepareStreamToSend(stream);
            MessageSendTask task = new MessageSendTask(stream, bytesToSend,
                                                       m_transport, this);
            StartSendMessageTask(task);            
        }
        
        private void StartWriteResponseMessage(Stream stream) {
            long bytesToSend = PrepareStreamToSend(stream);
            MessageSendTask task = new MessageSendTask(stream, bytesToSend,
                                                       m_transport, this);
            StartSendMessageTask(task);
        }   
        
        private void StartSendMessageTask(MessageSendTask task) {            
            if (task.HasNextPartToSend()) {
                task.StartSendNextPart();
            }            
        }
        
        /// <summary>
        /// send a message as a result to an incoming message
        /// </summary>
        internal void SendResponse(Stream responseStream) {
            bool gotLock = WaitForWriteLock();
            if (!gotLock) {
                CloseConnectionAfterTimeout();
                throw new omg.org.CORBA.TIMEOUT(30, CompletionStatus.Completed_No);
            }
            StartWriteResponseMessage(responseStream);
        }
        
        /// <summary>
        /// sends a giop error message as result of a problematic message
        /// </summary>
        internal void SendErrorResponseMessage() {
            GiopVersion version = new GiopVersion(1, 2); // use highest number supported
            GiopMessageHandler handler = GiopMessageHandler.GetSingleton();
            Stream messageErrorStream = handler.PrepareMessageErrorMessage(version);
            SendResponse(messageErrorStream);
        }
        
        /// <summary>
        /// send a close connection message to the peer.
        /// </summary>
        internal void SendConnectionCloseMessage() {
            GiopVersion version = new GiopVersion(1, 0);
            GiopMessageHandler handler = GiopMessageHandler.GetSingleton();
            Stream messageCloseStream = handler.PrepareMessageCloseMessage(version);
            bool gotLock = WaitForWriteLock();
            while (!gotLock) {
                gotLock = WaitForWriteLock();
            }
            StartWriteMessageWithoutReqId(messageCloseStream);
        }
                
        /// <summary>
        /// sends the request and blocks the thread until the response message
        /// has arravied or a timeout has occured.
        /// </summary>
        /// <returns>the response stream</returns>
        internal Stream SendRequestSynchronous(Stream requestStream, uint requestId) {
            bool gotLock = WaitForWriteLock();
            if (!gotLock) {
                CloseConnectionAfterTimeout();
                throw new omg.org.CORBA.TIMEOUT(30, CompletionStatus.Completed_No);
            }
            IResponseWaiter waiter;
            lock (m_waitingForResponse.SyncRoot) {
                // create and register wait handle
                waiter = new SynchronousResponseWaiter(this);
                if (m_waitingForResponse[requestId] == null) {
                    m_waitingForResponse[requestId] = waiter;
                } else {
                    throw new omg.org.CORBA.INTERNAL(40, CompletionStatus.Completed_No);
                }
            }                
            // begin sending of message
            StartWriteRequestMessage(requestStream, requestId);
            // wait for completion or timeout                
            bool received = waiter.StartWaiting();
            waiter.Completed();
            if (received) {
                // get and return the message
                if (waiter.Problem != null) {
                    throw waiter.Problem;
                } else if (waiter.Response != null) {
                    return waiter.Response;
                } else {
                    throw new INTERNAL(41, CompletionStatus.Completed_MayBe);
                }
            } else {
                lock (m_waitingForResponse.SyncRoot) {
                    m_waitingForResponse[requestId] = null;
                }                
                CloseConnectionAfterTimeout();
                throw new omg.org.CORBA.TIMEOUT(31, CompletionStatus.Completed_MayBe);
            }            
        }
        
        internal void SendRequestMessageOneWay(Stream requestStream, uint requestId) {
            bool gotLock = WaitForWriteLock();
            if (!gotLock) {
                CloseConnectionAfterTimeout();
                // don't throw an exception, silently stop try sending
                Debug.WriteLine("failed to send oneway request message due to timeout while trying to start writing");
                return;
            }
            // begin sending of message
            StartWriteRequestMessage(requestStream, requestId);
        }
        
        internal void SendRequestMessageAsync(Stream requestStream, uint requestId,
                                              AsyncResponseAvailableCallBack callback,
                                              IClientChannelSinkStack clientSinkStack,
                                              GiopClientConnection connection) {
            bool gotLock = WaitForWriteLock();
            if (!gotLock) {
                CloseConnectionAfterTimeout();
                Debug.WriteLine("failed to send async request message due to timeout while trying to start writing");
                throw new TIMEOUT(32, CompletionStatus.Completed_No);
            }
            IResponseWaiter waiter;
            lock (m_waitingForResponse.SyncRoot) {
                // create and register wait handle                
                waiter = new AsynchronousResponseWaiter(this, requestId, callback, clientSinkStack, connection,
                                                        m_timeout);
                if (m_waitingForResponse[requestId] == null) {
                    m_waitingForResponse[requestId] = waiter;
                } else {
                    throw new omg.org.CORBA.INTERNAL(40, CompletionStatus.Completed_No);
                }
            }                
            // wait for completion or timeout
            waiter.StartWaiting(); // notify the waiter, that the time for the request starts; is non-blocking
            // begin sending of message
            StartWriteRequestMessage(requestStream, requestId);
        }
        
        /// <summary>
        /// deregister the waiter for the given requestId
        /// </summary>        
        internal void CancelWaitForResponseMessage(uint requestId) {
            lock (m_waitingForResponse.SyncRoot) {
                // deregister waiter
                m_waitingForResponse[requestId] = null;
            }
        }
        
        /// <summary>
        /// is notified, if a complete giop message has been sent.
        /// </summary>
        /// <remarks>doesn't throw exceptiosn</remarks>
        internal void MsgSentCallback(MessageSendTask completedTask) {
            try {
                // message send completed, signal availability of write lock
                m_writeLock.Set();
            } catch (Exception ex) {
                CloseConnectionAfterUnexpectedException(ex);
            }
        }
        
        /// <summary>handles an exception while sending a message</summary>
        internal void MsgSentCallbackException(MessageSendTask problemTask, Exception ex) {            
            try {
                if (problemTask.HasRequestId()) {
                    // check if somebody is waiting for an answer to the message sent
                    lock (m_waitingForResponse.SyncRoot) {
                        IResponseWaiter waiter = (IResponseWaiter)m_waitingForResponse[problemTask.RequestId];
                        if (waiter != null) {
                            m_waitingForResponse[problemTask.RequestId] = null;
                            waiter.Problem = ex;
                            waiter.Notify();
                        }
                    }
                }
            } catch (Exception) {
            } finally {
                try {
                    // close connection
                    CloseConnectionAfterUnexpectedException(ex);                        
                } catch (Exception) {                
                }
            }
        }
        
        #endregion Sending messages
        #region Receiving messages          
                
        /// <summary>
        /// begins receiving messages asynchronously
        /// </summary>
        internal void StartMessageReception() {
            lock(this) {
                if (m_messageReceiveTask == null) {
                    m_messageReceiveTask = new MessageReceiveTask(m_transport, this);
                    m_messageReceiveTask.StartReceiveMessage();
                } // else ignore, already started
            }
        }
        
        /// <summary>
        /// abort receiving messages
        /// </summary>
        private void StopMessageReception() {
            lock(this) {
                m_messageReceiveTask = null;
            }
        }
                
        internal void MsgReceivedCallback(MessageReceiveTask messageReceived) {
            Stream messageStream = messageReceived.MessageStream;
            GiopHeader header = messageReceived.Header;
            if (FragmentedMessageAssembler.IsFragmentedMessage(header)) {
                // defragment
                if (FragmentedMessageAssembler.IsStartFragment(header)) {
                    m_fragmentAssembler.StartFragment(messageStream);
                    messageReceived.StartReceiveMessage(); // receive next message
                    return; // wait for next callback
                } else if (!FragmentedMessageAssembler.IsLastFragment(header)) {
                    m_fragmentAssembler.AddFragment(messageStream);
                    messageReceived.StartReceiveMessage(); // receive next message
                    return; // wait for next callback                    
                } else {
                    messageStream = m_fragmentAssembler.FinishFragmentedMsg(messageStream, out header);
                }                
            }
            
            // here, the message is no longer fragmented, don't check for fragment here
            switch (header.GiopType) {
                case GiopMsgTypes.Request:
                    HandleRequestMessage(messageStream);
                    messageReceived.StartReceiveMessage(); // receive next message
                    break;
                case GiopMsgTypes.LocateRequest:
                    HandleLocateRequestMessage(messageStream);
                    messageReceived.StartReceiveMessage(); // receive next message
                    break;
                case GiopMsgTypes.Reply:
                    // see, if somebody is interested in the response
                    lock (m_waitingForResponse.SyncRoot) {
                        uint replyForRequestId = ExtractRequestIdFromReplyMessage(messageStream);
                        IResponseWaiter waiter = (IResponseWaiter)m_waitingForResponse[replyForRequestId];
                        if (waiter != null) {
                            m_waitingForResponse[replyForRequestId] = null;
                            waiter.Response = messageStream;
                            waiter.Notify();
                        }
                    }
                    
                    messageReceived.StartReceiveMessage(); // receive next message
                    break;
                case GiopMsgTypes.LocateReply:
                    // ignore, not interesting
                    messageReceived.StartReceiveMessage(); // receive next message
                    break;
                case GiopMsgTypes.CloseConnection:
                    m_transport.CloseConnection();
                    AbortAllPendingRequestsWaiting(); // if requests are waiting for a reply, abort them
                    break;
                case GiopMsgTypes.CancelRequest:
                    CdrInputStreamImpl input = new CdrInputStreamImpl(messageStream);
                    GiopHeader cancelHeader = new GiopHeader(input);
                    uint requestIdToCancel = input.ReadULong();
                    m_fragmentAssembler.CancelFragmentsIfInProgress(requestIdToCancel);
                    messageReceived.StartReceiveMessage(); // receive next message
                    break;                
                case GiopMsgTypes.MessageError:                    
                    CloseConnectionAfterUnexpectedException(new MARSHAL(16, CompletionStatus.Completed_MayBe));
                    AbortAllPendingRequestsWaiting(); // if requests are waiting for a reply, abort them
                    break;
                default:
                    // should not occur; 
                    // hint: fragment is also considered as error here,
                    // because fragment should be handled before this loop                    
                    
                    // send message error
                    SendErrorResponseMessage();
                    messageReceived.StartReceiveMessage(); // receive next message
                    break;
            }                                    
        }                
        
        internal void MsgReceivedCallbackException(MessageReceiveTask messageReceived, Exception ex) {
            try {                
                if (ex is omg.org.CORBA.MARSHAL) {
                    // Giop header was not ok
                    // send a message error, something wrong with the message format
                    SendErrorResponseMessage();
                }                
                CloseConnectionAfterUnexpectedException(ex);
            } catch (Exception) {                
            }                 
            AbortAllPendingRequestsWaiting(); // if requests are waiting for a reply, abort them            
        }        
        
        /// <summary>
        /// called, when the connection has been closed while receiving a message
        /// </summary>        
        internal void MsgReceivedConnectionClosedException(MessageReceiveTask messageReceived) {
            Trace.WriteLine("connection closed while trying to read a message");
            try {
                m_transport.CloseConnection();
            } catch (Exception) {                
            }
            AbortAllPendingRequestsWaiting(); // if requests are waiting for a reply, abort them
        }
        
        protected virtual void HandleRequestMessage(Stream messageStream) {
            SendErrorResponseMessage(); // not supported by non-bidirectional handler
        }
        
        protected virtual void HandleLocateRequestMessage(Stream messageStream) {
            SendErrorResponseMessage(); // not supported by non-bidirectional handler
        }

        /// <summary>
        /// extracts the request id from a non-fragmented reply message
        /// </summary>
        /// <param name="replyMessage"></param>
        private uint ExtractRequestIdFromReplyMessage(Stream replyMessage) {
            replyMessage.Seek(0, SeekOrigin.Begin);
            CdrInputStreamImpl reader = new CdrInputStreamImpl(replyMessage);
            GiopHeader msgHeader = new GiopHeader(reader);
            
            if (msgHeader.Version.IsBeforeGiop1_2()) {
                // GIOP 1.0 / 1.1, the service context collection preceeds the id
                SkipServiceContexts(reader);
            }
            return reader.ReadULong();
        }
        
        /// <summary>
        /// skips the service contexts in a request / reply msg 
        /// </summary>
        private void SkipServiceContexts(CdrInputStream cdrIn) {
            uint nrOfContexts = cdrIn.ReadULong();
            // Skip service contexts: not part of this test            
            for (uint i = 0; i < nrOfContexts; i++) {
                uint contextId = cdrIn.ReadULong();
                uint lengthOfContext = cdrIn.ReadULong();
                cdrIn.ReadPadding(lengthOfContext);
            }
        }        
        
        #endregion Receiving messages        
        
        internal IPAddress GetPeerAddress() {
            return m_transport.GetPeerAddress();
        }        
        
        #endregion IMethods
    }
    
    
    
    /// <summary>
    /// a message handler, which can handle client and server messages
    /// </summary>
    public class GiopClientServerMessageHandler : GiopTransportMessageHandler {
        
        #region Types
        
        /// <summary>
        /// the type of request to process
        /// </summary>
        private enum RequestToProcessType {
            Request, LocateRequest
        }
        
        /// <summary>
        /// encapsulates a request to process.
        /// </summary>
        private class RequestToProcess {
            
            private Stream m_messageStream;
            private RequestToProcessType m_type;
            
            internal RequestToProcess(Stream messageStream, RequestToProcessType type) {
                m_messageStream = messageStream;
                m_type = type;
            }
            
            internal Stream MessageStream {
                get {
                    return m_messageStream;
                }
            }
            
            internal RequestToProcessType Type {
                get {
                    return m_type;
                }                    
            }            
        }
        
        #endregion Types
        #region IFields
        
        private IGiopRequestMessageReceiver m_receiver;
        // the connection desc for the handled connection.
        private GiopConnectionDesc m_conDesc = new GiopConnectionDesc();
        
        /// <summary>
        /// the buffered requests.
        /// </summary>
        private Queue m_requestQueue = new Queue();
        
        /// <summary>
        /// is currently a requestProcessing in progress.
        /// </summary>
        private bool m_processing = false;        
        
        #endregion IFields
        #region IProperties
        
        internal GiopConnectionDesc ConnectionDesc {
            get {
                return m_conDesc;
            }
        }
        
        #endregion IProperties
        #region IConstructors
        
        /// <summary>creates a giop transport message handler, which accept request messages by delegating to receiver</summary>
        internal GiopClientServerMessageHandler(ITransport transport, IGiopRequestMessageReceiver receiver) : base(transport) {
            Initalize(receiver);
        }
        
        /// <summary>creates a giop transport message handler, which accept request messages by delegating to receiver</summary>
        internal GiopClientServerMessageHandler(ITransport transport, MessageTimeout timeout, IGiopRequestMessageReceiver receiver) : base(transport, timeout) {
            Initalize(receiver);
        }        
        
        #endregion IConstructors
        #region IMethods
        
        private void Initalize(IGiopRequestMessageReceiver receiver) {
            if (receiver == null) {
                throw new BAD_PARAM(167, CompletionStatus.Completed_No);
            }
            m_receiver = receiver;
        }
        
        private void CheckOrEnableProcessing() {            
            if (!m_processing) {
                // start processing
                m_processing = true;
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.Process), null);
            }
        }        

        protected override void HandleRequestMessage(Stream messageStream) {
            // process in a separate thread to allow reading of next message in parallel.
            lock(this) {
                RequestToProcess req = new RequestToProcess(messageStream, RequestToProcessType.Request);
                m_requestQueue.Enqueue(req);            
                CheckOrEnableProcessing();
            }
        }        
        
        protected override void HandleLocateRequestMessage(Stream messageStream) {
            lock(this) {
                // process in a separate thread to allow reading of next message in parallel.
                RequestToProcess req = new RequestToProcess(messageStream, RequestToProcessType.LocateRequest);
                m_requestQueue.Enqueue(req);
                CheckOrEnableProcessing();
            }
        }
        
        /// <summary>
        /// processes requests incoming from one connection in order (otherwise problems with codeset establishment).
        /// </summary>        
        /// <remarks>
        /// - Reduces the number of thread switches if many requests are concurrently arriving.
        /// - If no more requests are pending, release thread for other tasks.
        /// - called by the thread-pool
        /// </remarks>
        private void Process(object state) {
            try {
                while (true) {
                    RequestToProcess req;
                    lock(this) {
                        // get next request to process.
                        if (!(m_requestQueue.Count > 0)) {
                            m_processing = false; // use a new thread for messages arraving from now on ...
                            break; // nothing more to process
                        }
                        req = (RequestToProcess)m_requestQueue.Dequeue();
                    }
                    // process next request
                    ProcessRequest(req);
                }
            } catch (Exception) {
                // unexpected exception -> processing problem on this connection, close connection...
                try {
                    SendConnectionCloseMessage();
                } finally {
                    ForceCloseConnection();
                }
                // after this, no new thread should be started to process next requests, because
                // connection closed -> don't set m_processing to false here
            }
        }
        
        private void ProcessRequest(RequestToProcess req) {
            switch (req.Type) {
                case RequestToProcessType.Request:
                    m_receiver.ProcessRequest(req.MessageStream, this);
                    break;
                case RequestToProcessType.LocateRequest:
                    m_receiver.ProcessLocateRequest(req.MessageStream, this);
                    break;
                default:
                    Trace.WriteLine("unknown request type in Process: " + req.Type);
                    // unknown, ignore
                    break;
            }
        }
                       
        #endregion IMethods
        
    }    
    
}