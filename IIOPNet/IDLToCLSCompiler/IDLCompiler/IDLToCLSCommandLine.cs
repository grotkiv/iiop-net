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
        private IList /* <FileInfo> */ m_inputFiles = new ArrayList();
        private DirectoryInfo m_outputDirectory = new DirectoryInfo(".");
        private IList /* <FileInfo> */ m_customMappingFiles = new ArrayList();
        private FileInfo m_signKeyFile = null;
        private bool m_delaySign = false;
        private string m_asmVersion = null;
        private bool m_mapAnyToAnyContainer = false;
        private DirectoryInfo m_baseDirectory = null;
        private Type m_baseInterface = null;
        private bool m_generateVtSkeletons = false;
        private bool m_overwriteVtSkeletons = false;
        private DirectoryInfo m_vtSkeletonsTargetDir = null;
        private Type m_vtSkelcodeDomProviderType;
        private IList /* <DirectoryInfo> */ m_idlSourceDirs = new ArrayList();
        private IList /* <Assembly> */ m_refAssemblies = new ArrayList();
        
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
        
        /// <summary>
        /// the list of input file infos, i.e. IList of FileInfo
        /// </summary>
        public IList /* <FileInfo> */ InputFiles {
            get {
                return m_inputFiles;
            }
        }
        
        /// <summary>the directory, the output will be written to.</summary>
        public DirectoryInfo OutputDirectory {
            get {
                return m_outputDirectory;
            }
        }

        /// <summary>the custom mapping files.</summary>
        public IList /* <FileInfo> */ CustomMappingFiles {
            get {
                return m_customMappingFiles;
            }
        }
        
        /// <summary>the key file used to sign the resulting assembly</summary>
        public FileInfo SignKeyFile {
            get {
                return m_signKeyFile;
            }
        }
        
        /// <summary>delay sign the assembly</summary>
        public bool DelaySign {
            get {
                return m_delaySign;
            }
        }
        
        /// <summary>the version of the target assembly.</summary>
        public string AssemblyVersion {
            get {
                return m_asmVersion;
            }
        }
        
        /// <summary>
        /// returns true, if any should be map to the any container type instead of object.
        /// </summary>
        public bool MapAnyToAnyContainer {
            get {
                return m_mapAnyToAnyContainer;
            }
        }
        
        /// <summary>
        /// the directory to change to, before doing any processing.
        /// </summary>
        public DirectoryInfo BaseDirectory {
            get {
                return m_baseDirectory;
            }
        }
        
        /// <summary>
        /// option to specify, that a generated concrete / abstract interface should inherit from
        /// a certain base interface.
        /// </summary>
        public Type BaseInterface {
            get {
                return m_baseInterface;                
            }                
        }
        
        /// <summary>
        /// Generate ValueType skeletons or not.
        /// </summary>
        public bool GenerateValueTypeSkeletons {
            get {
                return m_generateVtSkeletons;
            }
        }
        
        /// <summary>
        /// Overwrite already generated value type skeletons or not.
        /// </summary>
        public bool OverwriteValueTypeSkeletons {
            get {
                return m_overwriteVtSkeletons;
            }
        }        
        
        /// <summary>
        /// The target directory for the generated value type skeletons.
        /// </summary>
        public DirectoryInfo ValueTypeSkeletonsTargetDir {
            get {
                return m_vtSkeletonsTargetDir;
            }
        }
        
        /// <summary>
        /// the codedom provider to use for Valuetype skeleton generation.
        /// </summary>
        public Type ValueTypeSkeletonCodeDomProviderType {
            get {
                return m_vtSkelcodeDomProviderType;
            }
        }
        
        /// <summary>
        /// directories to search in for idl files.
        /// </summary>
        public IList /* <DirectoryInfo> */ IdlSourceDirectories {
            get {
                return m_idlSourceDirs;
            }
        }
        
        /// <summary>
        /// the referenced assemblies.
        /// </summary>
        public IList /* <Assembly> */ ReferencedAssemblies {
            get {
                return m_refAssemblies;
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
        
        private bool ContainsFileInfoAlready(IList list, FileInfo info) {
            for (int i = 0; i < list.Count; i++) {
                if (((FileInfo)list[i]).FullName == info.FullName) {
                    return true;
                }
            }
            return false;
        }
        
        private bool AddRefAssembly(string asmName) {
            try {                    
                Assembly refAsm = Assembly.LoadFrom(asmName);
                m_refAssemblies.Add(refAsm);
                return true;
            } catch (Exception ex) {
                SetIsInvalid("can't load assembly: " + asmName + "\n" + ex);
                return false;
            }                                
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
                    m_outputDirectory = new DirectoryInfo(args[i++].Substring(5));                    
                } else if (args[i].Equals("-r")) {
                    i++;
                    if (!AddRefAssembly(args[i++])) {
                        return;
                    }
                } else if (args[i].StartsWith("-r:")) {
                    if (!AddRefAssembly(args[i++].Substring(3))) {
                        return;
                    }
                } else if (args[i].Equals("-c")) {
                    i++;
                    FileInfo customMappingFile = new System.IO.FileInfo(args[i++]);
                    if (!ContainsFileInfoAlready(m_customMappingFiles, customMappingFile)) {
                        m_customMappingFiles.Add(customMappingFile);
                    } else {
                        SetIsInvalid("tried to add a custom mapping file multiple times: " + customMappingFile.FullName);
                        return;
                    }
                    
                } else if (args[i].Equals("-snk")) {
                    i++;
                    m_signKeyFile = new FileInfo(args[i++]);
                } else if (args[i].Equals("-delaySign")) {
                    i++;
                    m_delaySign = true;
                } else if (args[i].Equals("-asmVersion")) {
                    i++;
                    m_asmVersion = args[i++];                    
                } else if (args[i].Equals("-mapAnyToCont")) {
                    i++;
                    m_mapAnyToAnyContainer = true;
                } else if (args[i].Equals("-basedir")) {
                    i++;
                    m_baseDirectory = new DirectoryInfo(args[i++]);
                    if (!Directory.Exists(m_baseDirectory.FullName)) {
                        SetIsInvalid(String.Format("Error: base directory {0} does not exist!", 
                                                   m_baseDirectory.FullName ) );
                        return;
                    }                    
                } else if (args[i].Equals("-idir")) {
                    i++;
                    m_idlSourceDirs.Add(new DirectoryInfo(args[i++]));
                } else if (args[i].Equals("-b")){                    
                    i++;
                    string baseInterfaceName = args[i++];
                    m_baseInterface = Type.GetType(baseInterfaceName, false);
                    if (m_baseInterface == null) {
                        SetIsInvalid(String.Format("Error: base interface {0} does not exist!", 
                                                   baseInterfaceName));
                        return;
                    }
                } else if (args[i].Equals("-vtSkel")) {
                    i++;
                    m_generateVtSkeletons = true;
                } else if (args[i].Equals("-vtSkelProv")) {
                    i++;
                    string providerTypeName = args[i++].Trim();                    
                    m_vtSkelcodeDomProviderType = Type.GetType(providerTypeName, false);
                    if (m_vtSkelcodeDomProviderType == null) {
                        SetIsInvalid(String.Format("provider {0} not found!",
                                            providerTypeName));
                        return;
                    }
                } else if (args[i].Equals("-vtSkelTd")) {
                    i++;
                    m_vtSkeletonsTargetDir = new DirectoryInfo(args[i++]);
                } else if (args[i].Equals("-vtSkelO")) {
                    i++;
                    m_overwriteVtSkeletons = true;                    
                } else {
                    SetIsInvalid(String.Format("Error: invalid option {0}", args[i]));
                    return;
                }                
            }
            
            if ((i + 2) > args.Length) {
                SetIsInvalid("Error: target assembly name or idl-file missing");
                return;
            }
            
            m_targetAssemblyName = args[i];
            i++;            
            
            for (int j = i; j < args.Length; j++) {
                m_inputFiles.Add(new FileInfo(args[j]));
            }            
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
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "testAsm", "test.idl" });
            Assertion.AssertEquals("OutputDirectory", testDir.FullName,
                                   commandLine.OutputDirectory.FullName);            
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }
        
        [Test]
        public void TestOutDirSpaceSeparator() {            
            DirectoryInfo testDir = new DirectoryInfo(Path.Combine(".", "testOut"));
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-o", testDir.FullName, "testAsm", "test.idl" });
            Assertion.AssertEquals("OutputDirectory", testDir.FullName,
                                   commandLine.OutputDirectory.FullName);
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }
        
        [Test]
        public void TestOutDirColonSeparator() {
            DirectoryInfo testDir = new DirectoryInfo(Path.Combine(".", "testOut"));
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-out:" + testDir.FullName, "testAsm", "test.idl" });
            Assertion.AssertEquals("OutputDirectory", testDir.FullName,
                                   commandLine.OutputDirectory.FullName);
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
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
        public void TestMissingIdlFileName() {
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "testAsm" } );
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
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }
        
        [Test]
        public void TestTargetAssemblyName() {
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "testAsm", "test.idl" } );
            Assertion.AssertEquals("targetAssemblyName", "testAsm",
                                   commandLine.TargetAssemblyName);
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }
        

        [Test]
        public void TestSingleIdlFile() {
            string file1 = "test1.idl";
            
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "testAsm", file1 } );
            Assertion.AssertEquals("idl files", 1,
                                   commandLine.InputFiles.Count);
            Assertion.AssertEquals("idl file1", 
                                   file1,
                                   ((FileInfo)commandLine.InputFiles[0]).Name);
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }        
        
        [Test]
        public void TestIdlFiles() {            
            string file1 = "test1.idl";
            string file2 = "test2.idl";
            string file3 = "test3.idl";
            
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "testAsm", file1, file2, file3 } );
            Assertion.AssertEquals("idl files", 3,
                                   commandLine.InputFiles.Count);
            Assertion.AssertEquals("idl file1", 
                                   file1,
                                   ((FileInfo)commandLine.InputFiles[0]).Name);
            Assertion.AssertEquals("idl file2", 
                                   file2,
                                   ((FileInfo)commandLine.InputFiles[1]).Name);
            Assertion.AssertEquals("idl file3", 
                                   file3,
                                   ((FileInfo)commandLine.InputFiles[2]).Name);
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }           
        
        [Test]
        public void TestCustomMappingFiles() {
            string customMappingFile1 = "customMapping1.xml";
            string customMappingFile2 = "customMapping2.xml";
            
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-c", customMappingFile1, "-c", customMappingFile2,
                               "testAsm", "test.idl" });
            Assertion.AssertEquals("CustomMappingFiles", 2,
                                   commandLine.CustomMappingFiles.Count);
            Assertion.AssertEquals("CustomMappingFile 1", customMappingFile1,
                                   ((FileInfo)commandLine.CustomMappingFiles[0]).Name);
            Assertion.AssertEquals("CustomMappingFile 2", customMappingFile2,
                                   ((FileInfo)commandLine.CustomMappingFiles[1]).Name);
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }
        
        [Test]
        public void TestCustomMappingFilesMultipleTheSame() {
            string customMappingFile1 = "customMapping1.xml";
            string customMappingFile2 = "customMapping1.xml";
            
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-c", customMappingFile1, "-c", customMappingFile2,
                               "testAsm", "test.idl" });
            Assertion.Assert("Invalid commandLine detection",
                             commandLine.IsInvalid);
            Assertion.Assert("invalid commandLine message",
                             commandLine.ErrorMessage.StartsWith(
                                "tried to add a custom mapping file multiple times: "));
        }
        
        [Test]
        public void TestSnkFile() {
            string snkFile = "test.snk";
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-snk", snkFile, "testAsm", "test.idl" });
            Assertion.AssertEquals("Key file", snkFile,
                                   commandLine.SignKeyFile.Name);
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }
        
        [Test]
        public void TestDelaySign() {            
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-delaySign", "testAsm", "test.idl" });
            Assertion.Assert("DelaySign", commandLine.DelaySign);
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }        

        [Test]
        public void TestAsmVersion() {
            string asmVersion = "1.0.0.0";
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-asmVersion", asmVersion, "testAsm", "test.idl" });
            Assertion.AssertEquals("Target Assembly Version", 
                                   asmVersion,
                                   commandLine.AssemblyVersion);
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }        
        
        [Test]
        public void TestMapToAnyContainer() {
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-mapAnyToCont", "testAsm", "test.idl" });
            Assertion.Assert("Map any to any container", commandLine.MapAnyToAnyContainer);
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }
        
        [Test]
        public void TestBaseDirectory() {            
            DirectoryInfo testDir = new DirectoryInfo(".");
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-basedir", testDir.FullName, "testAsm", "test.idl" });
            Assertion.AssertEquals("BaseDirectory", testDir.FullName,
                                   commandLine.BaseDirectory.FullName);
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }        
        
        [Test]
        public void TestBaseDirectoryNonExisting() {            
            DirectoryInfo testDir = new DirectoryInfo(Path.Combine(".", "NonExistantBaseDir"));
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-basedir", testDir.FullName, "testAsm", "test.idl" });
            Assertion.Assert("Invalid Base directory",
                             commandLine.IsInvalid);
            Assertion.AssertEquals("invalid arguments message",
                                   String.Format(
                                       "Error: base directory {0} does not exist!", testDir.FullName),
                                   commandLine.ErrorMessage);
        }        
        
        [Test]
        public void TestInheritBaseInterface() {
            Type type = typeof(IDisposable);
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-b", type.FullName, "testAsm", "test.idl" });
            Assertion.AssertEquals("BaseInterface", type.FullName,
                                   commandLine.BaseInterface.FullName);
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }
        
        [Test]
        public void TestBaseInterfaceNonExisting() {                        
            string baseInterfaceName = "System.IDisposableNonExisting";
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-b", baseInterfaceName, "testAsm", "test.idl" });
            Assertion.Assert("Invalid base interface",
                             commandLine.IsInvalid);
            Assertion.AssertEquals("invalid arguments message",
                                   String.Format(
                                       "Error: base interface {0} does not exist!", baseInterfaceName),
                                   commandLine.ErrorMessage);
        }
                
        [Test]
        public void TestVtSkel() {
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-vtSkel", "testAsm", "test.idl" });
            Assertion.Assert("Value Type Skeleton generation", 
                             commandLine.GenerateValueTypeSkeletons);
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }
        
        [Test]
        public void TestVtSkelOverwrite() {
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-vtSkelO", "testAsm", "test.idl" });
            Assertion.Assert("Value Type Skeleton overwrite", 
                             commandLine.OverwriteValueTypeSkeletons);
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }
        
        [Test]
        public void TestVtTargetDir() {
            DirectoryInfo testDir = new DirectoryInfo(Path.Combine(".", "testGenVtDir"));
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-vtSkelTd", testDir.FullName, "testAsm", "test.idl" });
            Assertion.AssertEquals("Valuetype Skeletons Target Directory", testDir.FullName,
                                   commandLine.ValueTypeSkeletonsTargetDir.FullName);
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }                
        
        [Test]
        public void TestVtGenerationProvider() {
            Type provider = typeof(Microsoft.CSharp.CSharpCodeProvider);
            string providerName = provider.AssemblyQualifiedName;
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-vtSkelProv", providerName, "testAsm", "test.idl" });
            Assertion.Assert("Command Line Validity", !commandLine.IsInvalid);
            Assertion.AssertEquals("Valuetype Skeletons Generation Provider", provider,
                                   commandLine.ValueTypeSkeletonCodeDomProviderType);
        }                        
        
        [Test]
        public void TestVtGenerationProviderInvalid() {                        
            string providerName = "System.NonExistingProvider";
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-vtSkelProv", providerName, "testAsm", "test.idl" });
            Assertion.Assert("Invalid codedom provider",
                             commandLine.IsInvalid);
            Assertion.AssertEquals("invalid arguments message",
                                   String.Format(
                                       "provider {0} not found!", providerName),
                                   commandLine.ErrorMessage);
        }
        
        [Test]
        public void TestIdlSourceDirectories() {
            DirectoryInfo dir1 = new DirectoryInfo(".");
            DirectoryInfo dir2 = new DirectoryInfo(Path.Combine(".", "testIdlDir"));
                        
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-idir", dir1.FullName, "-idir", dir2.FullName, "testAsm", "test.idl" } );
            Assertion.AssertEquals("idl source dirs", 2,
                                   commandLine.IdlSourceDirectories.Count);
            Assertion.AssertEquals("idl source dir 1", 
                                   dir1.FullName,
                                   ((DirectoryInfo)commandLine.IdlSourceDirectories[0]).FullName);
            Assertion.AssertEquals("idl source dir 2", 
                                   dir2.FullName,
                                   ((DirectoryInfo)commandLine.IdlSourceDirectories[1]).FullName);
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }
        
        [Test]
        public void TestRefAssembliesSpaceSeparator() {
            Assembly asm1 = this.GetType().Assembly;
            Assembly asm2 = typeof(TestAttribute).Assembly;
                        
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-r", asm1.CodeBase, "-r", asm2.CodeBase, "testAsm", "test.idl" } );
            Assertion.AssertEquals("referenced assemblies", 2,
                                   commandLine.ReferencedAssemblies.Count);
            Assertion.AssertEquals("ref assembly 1", 
                                   asm1.FullName,
                                   ((Assembly)commandLine.ReferencedAssemblies[0]).FullName);
            Assertion.AssertEquals("ref assembly 2", 
                                   asm2.FullName,
                                   ((Assembly)commandLine.ReferencedAssemblies[1]).FullName);            
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }
        
        [Test]
        public void TestRefAssembliesColonSeparator() {
            Assembly asm1 = this.GetType().Assembly;
            Assembly asm2 = typeof(TestAttribute).Assembly;
                        
            IDLToCLSCommandLine commandLine = new IDLToCLSCommandLine(
                new string[] { "-r:" + asm1.CodeBase, "-r:" + asm2.CodeBase, "testAsm", "test.idl" } );
            Assertion.AssertEquals("referenced assemblies", 2,
                                   commandLine.ReferencedAssemblies.Count);
            Assertion.AssertEquals("ref assembly 1", 
                                   asm1.FullName,
                                   ((Assembly)commandLine.ReferencedAssemblies[0]).FullName);
            Assertion.AssertEquals("ref assembly 2", 
                                   asm2.FullName,
                                   ((Assembly)commandLine.ReferencedAssemblies[1]).FullName);            
            
            Assertion.Assert("Command line validity", !commandLine.IsInvalid);
        }
        
    }
}

#endif
 

