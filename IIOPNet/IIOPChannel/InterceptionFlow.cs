/*
 * Created by SharpDevelop.
 * User: Dominic
 * Date: 13.02.2005
 * Time: 13:08
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using omg.org.CORBA;
using omg.org.PortableInterceptor;

namespace Ch.Elca.Iiop.Interception {
	
    
    /// <summary>
	/// Base class of all interception flows, i.e. clientrequest, serverrequest and ior
	/// </summary>
	internal abstract class InterceptionFlow {
		
	    #region SFields
	    
	    private static Interceptor[] s_emptyInterceptionList = new Interceptor[0];
	    
	    #endregion SFields
	    #region IFields
	    
	    private Interceptor[] m_interceptors;
	    private int m_currentInterceptor;
	    private int m_increment;
	    
	    #endregion IFields
	    #region IConstructors
	    
	    internal InterceptionFlow() : this(s_emptyInterceptionList) {
	    }
	    
	    internal InterceptionFlow(Interceptor[] interceptors) {
	        m_currentInterceptor = 0;
	        m_increment = 1;
	        m_interceptors = interceptors;
		}
	    
	    #endregion IConstructors
        #region IProperties
        
        protected int Increment {
            get {
                return m_increment;
            }
            set {
                m_increment = value;
            }
        }
        
        #endregion IProperties
	    #region IMethods
	    
	    protected Interceptor GetCurrent() {
	        return m_interceptors[m_currentInterceptor];
	    }
	    
	    internal bool HasNextInterceptor() {
	        return ((m_currentInterceptor + m_increment >= 0) &&
	                (m_currentInterceptor + m_increment < m_interceptors.Length));
	    }
	    
	    internal void ProceedToNext() {
	        if (HasNextInterceptor()) {
	            m_currentInterceptor +=  m_increment;
	        } else {
	            throw new INTERNAL(1005, CompletionStatus.Completed_MayBe);
	        }
	    }
	    
	    #endregion IMethods
	    
	}
	
	
    /// <summary>
	/// Base class of all request interception flows, i.e. clientrequest, serverrequest
	/// </summary>
	internal abstract class RequestInterceptionFlow : InterceptionFlow {
	    	    
	    #region IFields
	    
	    private RequestInfo m_requestInfo;
	    
	    #endregion IFields
	    #region IConstructors
	    	    
	    /// <summary>
	    /// for empty interception list.
	    /// </summary>
	    internal RequestInterceptionFlow() : base() {
	        m_requestInfo = null;
	    }
	    
	    internal RequestInterceptionFlow(Interceptor[] interceptors,
	                                     RequestInfo requestInfo) : base(interceptors) {
	        m_requestInfo = requestInfo;
	    }	    
	    
	    #endregion IConstructors
	    #region IMethods
	    
	    /// <summary>
	    /// after the last interception starting point has been called,
	    /// swith to the direction for reply processing.
	    /// </summary>
	    internal void SwitchToReplyDirection() {
	        Increment = -1 * Increment;
	    }
	    
	    #endregion IMethods
	    
	}
	
	
	/// <summary>
	/// client request interception flow
	/// </summary>
	internal class ClientRequestInterceptionFlow : RequestInterceptionFlow {
	    
	    #region IConstructors

	    /// <summary>
	    /// for empty interception list.
	    /// </summary>
	    internal ClientRequestInterceptionFlow() : base() {	        
	    }

	    
	    internal ClientRequestInterceptionFlow(ClientRequestInterceptor[] interceptors,
	                                           ClientRequestInfo requestInfo) : base(interceptors, requestInfo) {	        
	    }
	    
	    #endregion IConstructors
	    #region IMethods
	    
	    
	    
	    #endregion IMethods

	    
	}
	
	/// <summary>
	/// server request interception flow
	/// </summary>	
	internal class ServerRequestInterceptionFlow : RequestInterceptionFlow {

	    
	    #region IConstructors

	    /// <summary>
	    /// for empty interception list.
	    /// </summary>
	    internal ServerRequestInterceptionFlow() : base() {	        
	    }

	    
	    internal ServerRequestInterceptionFlow(ServerRequestInterceptor[] interceptors,
	                                           ServerRequestInfo requestInfo) : base(interceptors, requestInfo) {	        
	    }
	    
	    #endregion IConstructors
	    #region IMethods
	    
	    
	    
	    #endregion IMethods	    	    
	    
	}
	
	
	
	
	
}
