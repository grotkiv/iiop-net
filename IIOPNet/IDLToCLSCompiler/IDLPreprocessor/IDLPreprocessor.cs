/* IDLPreprocessor.cs
 * 
 * Project: IIOP.NET
 * IDLToCLSCompiler
 * 
 * WHEN      RESPONSIBLE
 * 30.08.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;
using System.Diagnostics;

namespace Ch.Elca.Iiop.IdlPreprocessor {


    /// <summary>
    /// a problem was encountered during preprocessing with the
    /// idl file
    /// </summary>
    public class PreprocessingException : Exception {
        
        /// <summary>creates a preprocessor exception for the given message</summary>
        public PreprocessingException(String message) : base(message) {
        }
        
    }
    
    /// <summary>
    /// exception is thrown, if illegal preprocessor directive is found.
    /// </summary>
    public class IllegalPreprocDirectiveException : PreprocessingException {
    
        /// <summary>creates an exception for the illegal directive</summary>
        public IllegalPreprocDirectiveException(String directive) : 
            base("illegal preprocessor directive: " + directive) {
        }
        
        /// <summary>creates an exception for the illegal directive with the 
        /// given message</summary>
        public IllegalPreprocDirectiveException(String directive, String message) : 
            base(message + "; directive: " + directive) {
        }

    
    }

    /// <summary>
    /// preprocesses IDL,
    /// It can handle the following preprocessor directives:
    /// #ifdef, #ifndef, #if, #else, #endif, #define, #include
    /// </summary>
    public class IDLPreprocessor {
        
        #region Types
        
        /// <summary>stores information on if-blocks</summary>
        private class IfBlock {
        
            private bool m_isConditionTrue;
        
            public IfBlock(bool conditionTrue) {
                m_isConditionTrue = conditionTrue;
            }
            
            public bool IsConditionTrue {
                get {
                    return m_isConditionTrue;
                }
            }            
        
        }
        
        #endregion Types
        #region SFields
        
        /// <summary>
        /// contains the user defined defines from
        /// the command prompt.
        /// </summary>
        private static Hashtable s_userDefined = new Hashtable();
        
        private static Encoding s_latin1 = Encoding.GetEncoding("ISO-8859-1");
        
        /// <summary>
        /// the path to the default IDL files
        /// </summary>
        private static DirectoryInfo s_idlPath; 
        
        private static Regex s_tokenStringEx = new Regex(@"\s+");
        
        #endregion SFields
        #region IFields
        
        /// <summary>the encountered defines</summary>
        private Hashtable m_defined;
        
        private StreamReader m_fileStream;
        
        /// <summary>stores the preprocessor result</summary>
        private StreamWriter m_outputStream;
        private MemoryStream m_outData;
                
        
        /// <summary>keeps track of if/else-blocks open</summary>
        private Stack m_ifBlockStack = new Stack();
       
        #endregion IFields
        #region SConstructor
        
        static IDLPreprocessor() {
            FileInfo locOfPreprocAsm = new FileInfo(typeof(IDLPreprocessor).Assembly.Location);
            string asmDirectory = locOfPreprocAsm.Directory.FullName;
            s_idlPath = new DirectoryInfo(Path.Combine(asmDirectory, 
            	                                       ".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + "IDL"));            
        }
        
        #endregion SConstructor
        #region IConstructors
    
        /// <summary>
        /// create a preprocessor for the specified files
        /// and all dependent files.
        /// </summary>
        /// <param name="toProcess">file to preprocess</param>
        public IDLPreprocessor(FileInfo toProcess) {
            // include the user defined defines in defines
            m_defined = new Hashtable(s_userDefined);
            Init(toProcess);
        }

        /// <summary>internal constructor to resolve included files</summary>
        /// <param name="symbols">already defined symbols in previously 
        /// preprocessed files.</param>
        private IDLPreprocessor(FileInfo toProcess, Hashtable symbols) {
            m_defined = symbols;
            Init(toProcess);
        }

        #endregion IConstructors
        #region SMethods        
        
        /// <summary>defines a preprocessor symbol</summary>
        public static void AddDefine(String define) {
            s_userDefined.Add(define, "");
        }
        
        /// <summary>configures the directory, where to find the default idl files </summary>
        public static void SetIdlDir(DirectoryInfo idlDir) {
            s_idlPath = idlDir;
        }
        
        #endregion SMethods
        #region IMethods
            
        /// <summary>initalizes input and output streams</summary>
        /// <exception cref="System.IO.IOException">Problem with IO, e.g file not found</exception>
        private void Init(FileInfo toProcess) {
            // for IDL files, latin 1 is used            
            m_fileStream = new StreamReader(new FileStream(toProcess.FullName,
                                                           FileMode.Open,
                                                           FileAccess.Read, 
                                                           FileShare.Read),
                                            s_latin1);
            m_outData = new MemoryStream();
            m_outputStream = new StreamWriter(m_outData, s_latin1);
            m_outputStream.AutoFlush = true;
        }



        /// <summary>preprocess the file</summary>        
        public void Process() {
            String currentLine = m_fileStream.ReadLine();
            while (currentLine != null) {
                currentLine = currentLine.Trim();
                if (currentLine.StartsWith("#include"))    {
                    ProcessInclude(currentLine);
                } else if (currentLine.StartsWith("#define")) {
                    ProcessDefine(currentLine);
                } else if (currentLine.StartsWith("#ifndef")) {
                    ProcessIfNDef(currentLine);
                } else if (currentLine.StartsWith("#ifdef")) {
                    ProcessIfDef(currentLine);
                } else if (currentLine.StartsWith("#else")) {
                    ProcessElse(currentLine);
                } else if (currentLine.StartsWith("#endif")) {
                    ProcessEndIf(currentLine);
                } else if (currentLine.StartsWith("#pragma")) {
                    // pragma directives are handles by IDL to CLS compiler
                    m_outputStream.WriteLine(currentLine);
                } else if (currentLine.StartsWith("#")) {
                    // unknown directive
                    throw new PreprocessingException("unknown directive: " + currentLine);
                } else {
                    // write the current line to the output stream
                    m_outputStream.WriteLine(currentLine);
                }                                       
                currentLine = m_fileStream.ReadLine();
            }
            m_fileStream.Close();
            m_outputStream.WriteLine(""); // add a newline at the end, because parser needs at least one line
        }

    

        /// <summary>gets the preprocessed file for further processing</summary>
        public MemoryStream GetProcessed() {
            m_outData.Seek(0, SeekOrigin.Begin);
            return m_outData;
        }

        #region implementation of the preprocessing actions

        /// <summary>processes an include directive</summary>
        /// <exception cref="IllegalPreprocDirective">
        /// illegal include directive encountered
        /// </exception>
        private void ProcessInclude(String currentLine) {
            currentLine = currentLine.Trim();
            String[] tokens = s_tokenStringEx.Split(currentLine);
            if (tokens.Length != 2) { 
                throw new IllegalPreprocDirectiveException(currentLine, 
                                                           "file argument not found / more than one argument");
            }
            
            // fileToInclude enclosed in quotation mark: search in current directory for file
            // fileToInclude enclosed in <>: search in compiler include dir!
            String fileToInclude = tokens[1];
            if (fileToInclude.StartsWith("\"")) { 
                fileToInclude = fileToInclude.Substring(1); 
                if (fileToInclude.EndsWith("\"")) { 
                    fileToInclude = fileToInclude.Substring(0, 
                                                            fileToInclude.Length - 1);
                } else {
                    // illegal include: ending " missing
                    throw new IllegalPreprocDirectiveException(currentLine);
                }
                // check if file exists, else search in compiler inlcude dir
                FileInfo fileInc = new FileInfo(fileToInclude);
                if (!fileInc.Exists) {
                	fileToInclude = Path.Combine(s_idlPath.FullName, fileToInclude);
                }
            } else if (fileToInclude.StartsWith("<")) {
                fileToInclude = fileToInclude.Substring(1); 
                if (fileToInclude.EndsWith(">")) {
                    fileToInclude = fileToInclude.Substring(0, 
                                                            fileToInclude.Length - 1);
                } else {
                    // illegal include: ending > missing
                    throw new IllegalPreprocDirectiveException(currentLine);
                }
                // combine IDL path
                fileToInclude = Path.Combine(s_idlPath.FullName, fileToInclude);
            }            

            FileInfo toInclude = new FileInfo(fileToInclude);
            IDLPreprocessor includePreproc = new IDLPreprocessor(toInclude, 
                                                                 m_defined);
            includePreproc.Process();
            MemoryStream result = includePreproc.GetProcessed();
            // copy result into current output stream
            CopyToOutputStream(result);
        }
    
        private void CopyToOutputStream(MemoryStream input) {
            StreamReader resultReader = new StreamReader(input, s_latin1);
            String currentLine = resultReader.ReadLine();
            while (currentLine != null) {
                m_outputStream.WriteLine(currentLine);
                currentLine = resultReader.ReadLine();
            }
            resultReader.Close();
        }    
    
        /// <summary>processes a define directive</summary>
        /// <exception cref="IllegalPreprocDirective">
        /// illeagal define statemant encountered</exception>
        private void ProcessDefine(String currentLine) {

            currentLine = currentLine.Trim();
            // split by whitespaces
            String[] tokens = s_tokenStringEx.Split(currentLine);
            if (tokens.Length <= 1) { 
                throw new IllegalPreprocDirectiveException(currentLine,
                                                  "define missing argument");
            }
            if (tokens.Length > 3) { 
                throw new IllegalPreprocDirectiveException(currentLine,
                                                  "too much tokens in define directive");

            }            
            String define = tokens[1];
                
            String val = "";
            if (tokens.Length == 3) {
                val = tokens[2];
            }
            if (m_defined.ContainsKey(define)) {
                throw new PreprocessingException("redefinition of a variable: " + define);
            }
            m_defined.Add(define, val);
            Debug.WriteLine("defined symbol in preproc: " + define);
        }    
    
    private void ProcessIfNDef(String currentLine) {        
        currentLine = currentLine.Trim();
        // split by whitespaces
        String[] tokens = s_tokenStringEx.Split(currentLine);
        if (tokens.Length <= 1) { 
            throw new IllegalPreprocDirectiveException(currentLine,
                                              "ifndef missing argument");            
        }
        if (tokens.Length > 2) {
            throw new IllegalPreprocDirectiveException(currentLine,
                                              "too much tokens in ifndef directive");            
        }
        String define = tokens[1];
        if (m_defined.ContainsKey(define)) { // throw everything in block away
            m_ifBlockStack.Push(new IfBlock(false)); // ifndef condition is false
            ReadToEndifOrElse(); // throw away up to endif / else
        } else {
            m_ifBlockStack.Push(new IfBlock(true)); // ifndef condition is true
        }
    }
    
    private void ProcessIfDef(String currentLine) {
        currentLine = currentLine.Trim();
        // split by spaces
        String[] tokens = s_tokenStringEx.Split(currentLine);
        if (tokens.Length <= 1) { 
            throw new IllegalPreprocDirectiveException(currentLine,
                                              "ifdef missing argument");            
        }
        if (tokens.Length > 2) {
            throw new IllegalPreprocDirectiveException(currentLine,
                                              "too much tokens in ifdef directive");            
        }
        String define = tokens[1];
        if (!m_defined.ContainsKey(define)) { // throw everything in block away
            m_ifBlockStack.Push(new IfBlock(false)); // ifdef condition is false
            ReadToEndifOrElse(); // throw away up to endif / else
        } else {
            m_ifBlockStack.Push(new IfBlock(true)); // ifdef condition is true
        }
    }


    private void ProcessElse(String currentLine) {
        if (m_ifBlockStack.Count > 0) {
            IfBlock block = (IfBlock) m_ifBlockStack.Peek();            
            if (block.IsConditionTrue) {
                // if true -> skip else block
                ReadToEndif();
            }
        } else {
            throw new PreprocessingException("else without if encountered"); 
        }
    }    
    
    private void ProcessEndIf(String currentLine) {
        if (m_ifBlockStack.Count > 0) {
            IfBlock block = (IfBlock) m_ifBlockStack.Pop();        
        } else {
            throw new PreprocessingException("too much endif's encountered"); 
        }                
    }

    /// <summary>
    /// search for a matching end-if or else directive, 
    /// throwing away everything in between.
    /// checks if's / endif's in between
    /// </summary> 
    private void ReadToEndifOrElse() {
        int moreIfs = 1; // more if's encountered than endif / else
        String currentLine = "";
        while ((moreIfs > 0) && (currentLine != null)) {
            if (currentLine.StartsWith("#if")) {
                moreIfs++; // an inner if; must be closed by an endif
            }
            if (currentLine.StartsWith("#endif")) {
                moreIfs--; // an inner or a matching endif found; is matching if moreIfs was 1
            }
            if ((moreIfs == 1) && currentLine.StartsWith("#else")) {
                moreIfs--; // matching else found
            }            
            if (moreIfs > 0) { 
                // no matching end directive yet
                currentLine = m_fileStream.ReadLine().Trim();
            }
        }
        if (currentLine.StartsWith("#endif")) {
            // close an if-block
            ProcessEndIf(currentLine);
        }
    }    
    
    /// search for a matching end-if directive,
    /// throwing away everything in between.
    /// checks if's / endif's in between
    private void ReadToEndif() {
        int moreIfs = 1; // more if's encountered than endif / else
        String currentLine = "";
        while ((moreIfs > 0) && (currentLine != null)) {
            if (currentLine.StartsWith("#if")) {
                moreIfs++; 
            }
            if (currentLine.StartsWith("#endif")) { 
                moreIfs--; 
            }            
            if (moreIfs > 0) { 
                currentLine = m_fileStream.ReadLine().Trim(); 
            }
        }
           // close an if-block
           ProcessEndIf(currentLine);
    }

    #endregion implementation of the preprocessing actions

    #endregion IMethods
        
        
        
    }
    
}
