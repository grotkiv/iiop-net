/* IDLToCLS.cs
 * 
 * Project: IIOP.NET
 * IDLToCLSCompiler
 * 
 * WHEN      RESPONSIBLE
 * 30.04.06  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
 * 
 * Copyright 2006 Dominic Ullmann
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
using System.Collections;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Globalization;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using parser;
using Ch.Elca.Iiop.IdlCompiler.Action;
using Ch.Elca.Iiop.IdlPreprocessor;

namespace Ch.Elca.Iiop.IdlCompiler {


    /// <summary>
    /// The class responsible for handling the compiler command line.
    /// </summary>
    public class IDLToCLSCommandLine {
        
        #region IFields
        
        private string m_targetAssemblyName;
        private DirectoryInfo m_outputDirectory = new DirectoryInfo(".");
        private bool m_isInvalid = false;
        private string m_errorMessage = String.Empty;
        private bool m_isHelpRequested = false;
        
        #endregion IFields
        #region IConstructors
        
        public IDLToCLSCommandLine(string[] args) {
            ParseArgs(args);
        }
        
        #endregion IConstructors
        #region IProperties
        
        
        /// <summary>
        /// the name of the target assembly.
        /// </summary>
        public string TargetAssemblyName {
            get {
                return m_targetAssemblyName;
            }
        }
        
        /// <summary>the directory, the output will be written to.</summary>
        public DirectoryInfo OutputDirectory {
            get {
                return m_outputDirectory;
            }
        }
        
        /// <summary>returns true, if an error has been detected.</summary>
        public bool IsInvalid {
            get {
                return m_isInvalid;
            }
        }
        
        /// <summary>
        /// if IsInvalid is true, contains the corresponding error message.
        /// </summary>
        public string ErrorMessage {
            get {
                return m_errorMessage;
            }
        }
        
        /// <summary>returns true, if help is requested.</summary>
        public bool IsHelpRequested {
            get {
                return m_isHelpRequested;
            }
        }
        
        #endregion IProperties
        #region IMethods
        
        private void SetIsInvalid(string message) {
            m_isInvalid = true;
            m_errorMessage = message;
        }
        
        private void ParseArgs(string[] args) {
            int i = 0;

            while ((i < args.Length) && (args[i].StartsWith("-"))) {
                if (args[i].Equals("-h") || args[i].Equals("-help")) {
                    m_isHelpRequested = true;
                    return;
                } else if (args[i].Equals("-o")) {
                    i++;
                    m_outputDirectory = new DirectoryInfo(args[i++]);
                } else if (args[i].StartsWith("-out:")) {                    
                    m_outputDirectory = new DirectoryInfo(args[i].Substring(5));
                    i++;
                } else {
                    SetIsInvalid(String.Format("Error: invalid option {0}", args[i]));
                    return;
                }
            }
            
            if ((i + 1) > args.Length) {
                SetIsInvalid("Error: target assembly name or idl-file missing");
                return;
            }
            
            m_targetAssemblyName = args[i];
            i++;            
        }
        
        #endregion IMethods
                                
    }
    
}


#if UnitTest


namespace Ch.Elca.Iiop.IdlCompiler.Tests {
	
    using System;
    using System.Reflection;
    using System.Collections;
    using System.IO;
    using System.Text;
    using NUnit.Framework;
    using Ch.Elca.Iiop.IdlCompiler;

    /// <summary>
    /// Unit-tests for the IDLToCLS CommandLine handling.
    /// </summary>
    [TestFixture]
    public class IDLToCLSCommandLineTest {
                        
        
        [Test]
        public void TestDefaultOutputDir() {
            DirectoryInfo testDir = new DirectoryInfo(".");
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(new string[1] { "testAsm" });
            Assertion.AssertEquals("OutputDirectory", testDir.FullName,
                                   commandLine.OutputDirectory.FullName);            
        }
        
        [Test]
        public void TestOutDirSpaceSeparator() {            
            DirectoryInfo testDir = new DirectoryInfo(Path.Combine(".", "testOut"));
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-o", testDir.FullName, "testAsm" });
            Assertion.AssertEquals("OutputDirectory", testDir.FullName,
                                   commandLine.OutputDirectory.FullName);
        }
        
        [Test]
        public void TestOutDirColonSeparator() {
            DirectoryInfo testDir = new DirectoryInfo(Path.Combine(".", "testOut"));
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-out:" + testDir.FullName, "testAsm" });
            Assertion.AssertEquals("OutputDirectory", testDir.FullName,
                                   commandLine.OutputDirectory.FullName);
        }
        
        [Test]
        public void TestWrongArgument() {
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-InvalidArg"} );
            Assertion.Assert("Invalid Arg detection",
                             commandLine.IsInvalid);
            Assertion.AssertEquals("invalid arguments message",
                                   "Error: invalid option -InvalidArg",
                                   commandLine.ErrorMessage);
        }
        
        [Test]
        public void TestMissingTargetAssemblyName() {
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[0] );
            Assertion.Assert("Invalid commandLine detection",
                             commandLine.IsInvalid);
            Assertion.AssertEquals("invalid commandLine message",
                                   "Error: target assembly name or idl-file missing",
                                   commandLine.ErrorMessage);
        }
        
        [Test]
        public void TestIsHelpRequested() {
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-h"} );
            Assertion.Assert("Help requested",
                             commandLine.IsHelpRequested);            
            commandLine = new IDLToCLSCommandLine(
                new string[] { "-help"} );
            Assertion.Assert("Help requested",
                             commandLine.IsHelpRequested);            
        }
        
        [Test]
        public void TestTargetAssemblyName() {
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "testAsm"} );
            Assertion.AssertEquals("targetAssemblyName", "testAsm",
                                   commandLine.TargetAssemblyName);
        }
        
        
        
        
    }
}

#endif
 

