using System;
using System.Collections;
using Ch.Elca.Iiop.Cdr;

namespace Ch.Elca.Iiop.Marshalling {

    /// <summary>
    /// interface for generated arguments serializers
    /// </summary>
    [CLSCompliant(false)]
    public abstract class ArgumentsSerializer {
        
        #region SFields
        
        public static readonly Type ClassType = typeof(ArgumentsSerializer);
        
        #endregion SFields
        #region IMethods
        
        
        public abstract void SerializeRequestArgs(string targetMethod, object[] actual, CdrOutputStream targetStream);
        
        public abstract object[] DeserializeRequestArgs(string targetMethod, CdrInputStream sourceStream,
                                                        out IDictionary contextElements);
                
        public abstract void SerializeResponseArgs(string targetMethod, object retValue, object[] outArgs,
                                                   CdrOutputStream targetStream);
        
        public abstract object DeserializeResponseArgs(string targetMethod, CdrInputStream sourceStream,
                                                       out object[] outArgs);
        
        #endregion IMethods
        
    }
    
}
