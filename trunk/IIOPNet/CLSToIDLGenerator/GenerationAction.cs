/* DependencyManager.cs
 * 
 * Project: IIOP.NET
 * CLSToIDLGenerator
 * 
 * WHEN      RESPONSIBLE
 * 31.01.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.Collections;
using System.Reflection;
using System.Diagnostics;

using Ch.Elca.Iiop.Util;

namespace Ch.Elca.Iiop.Idl {

    /// <summary>implementation of the generation action. This class writes the IDL, 
    /// for the specified mapping
    /// </summary>
    /// <remarks>for every mapped type a new instance of this class is generated, which is responsible for writing
    /// the definition for the type</remarks>
    internal class GenerationActionDefineTypes : MappingAction {

        #region IFields

        private string m_outputDirectory;
        /// <summary>the outputstream, the type definition is written into</summary>
        private TextWriter m_currentOutputStream = null;
        /// <summary>the opened modules</summary>
        private string[] m_openModules;
        /// <summary>the dependency information for the type mapped</summary>
        private DependencyInformation m_depInfo;
        /// <summary>the written fwd references </summary>
        private ArrayList m_writtenFwdReferences;

        /// <summary>generates the IDL for referenced types</summary>
        private GenerationActionReference m_refMapper = new GenerationActionReference();

        /// <summary>this instance knows how to map a CLS type</summary>
        private ClsToIdlMapper m_mapper = ClsToIdlMapper.GetSingleton();
        /// <summary>manages the dependencies of the types to map, keep track of already mapped types</summary>
        private DependencyManager m_depManager = new DependencyManager();
        /// <summary>the name of the idl-file this type def is written to</summary>
        private string m_toIDLFile;

        #endregion IFields
        #region IConstructors

        public GenerationActionDefineTypes(string outputDirectory) {
            m_outputDirectory = outputDirectory;
            m_depManager = new DependencyManager();
        }

        public GenerationActionDefineTypes(string outputDirectory, DependencyManager depManager) {
            m_outputDirectory = outputDirectory;
            m_depManager = depManager;
        }

        #endregion IConstructors
        #region IMethods

        /// <summary>
        /// Begins the the idl-file for the Type dotNetType: 
        /// Creates the idl-file, writes the generator information header
        /// </summary>
        /// <param name="dotNetType"></param>
        /// <param name="ToIdlFile"></param>
        private void BeginType(Type dotNetType, out string[] modules, out string unqualName) {
            // determine the dependencies for the type
            m_depInfo = m_depManager.GetDependencyInformation(dotNetType);
            
            // map the namespace:
            modules = IdlNaming.MapNamespaceToIdlModules(dotNetType);
            m_openModules = modules;
            unqualName = IdlNaming.MapShortTypeNameToIdl(dotNetType);

            // write it
            // create output-stream
            m_toIDLFile = CreateIdlFullName(modules, unqualName);
            m_currentOutputStream = OpenFile(m_toIDLFile);
            WriteFileHeader(m_toIDLFile);
            // register this type as mapped
            m_depManager.RegisterMappedType(dotNetType, m_toIDLFile);
            // map types needed, write fwd references
            BeforeTypeDefinition();

            // protect against redefinition:
            m_currentOutputStream.Write("#ifndef ");
            string def = "__";
            foreach (string mod in m_openModules) {
                def = def + mod + "_";
            }
            def = def + unqualName + "__";
            m_currentOutputStream.WriteLine(def);
            m_currentOutputStream.WriteLine("#define " + def);
        }

        /// <summary>ends the type definition</summary>
        private void EndType() {
            // close all opened modules
            CloseOpenScopes(m_openModules.Length);
            // close protection against redef
            m_currentOutputStream.WriteLine("#endif");

            // write includes
            EndTypeDefinition();
            // close the stream
            m_currentOutputStream.Close();
            // check for not mapped types in the deps
            m_depManager.RegisterNotMapped(m_depInfo);
        
            // map non-mapped:
            MapTypeInfo info = m_depManager.GetNextTypeToMap();
            while (info != null) {
                m_mapper.MapClsType(info.m_type, info.m_attributes, 
                                    new GenerationActionDefineTypes(m_outputDirectory, m_depManager));
                info = m_depManager.GetNextTypeToMap();
            }
        }



         private string CreateIdlFullName(string[] modules, string typeName) {
            String path  = String.Join(Path.DirectorySeparatorChar.ToString(), modules);
            return Path.Combine(path, typeName) + ".idl";
        }

       /// <summary>opens the file-output stream</summary>
        private TextWriter OpenFile(string IdlName) {
            string name = Path.Combine(m_outputDirectory, IdlName);
            string dir  = Path.GetDirectoryName(name);

            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
            return new StreamWriter(name);
        }


        /// <summary>
        /// writes a generator file header for an IDL file. This is the same for all idl-files
        /// </summary>
        private void WriteFileHeader(string toIDLFile) {
            m_currentOutputStream.WriteLine("// auto-generated IDL file by CLS to IDL mapper");
            m_currentOutputStream.WriteLine("");
            m_currentOutputStream.WriteLine("// " + toIDLFile);
            m_currentOutputStream.WriteLine("");
        }

        /// <summary>
        /// maps the needed types before the type can be defined, writes the needed forward references and
        /// the includes.
        /// </summary>
        private void BeforeTypeDefinition() {
            Debug.WriteLine("before type definition, type: " + m_depInfo.ForType);
            ArrayList mapBefore = m_depManager.GetTypesToMapBeforeType(m_depInfo);
            // map them
            MapTypes(mapBefore);
            // write forward references
            ArrayList fwdRefs = m_depManager.GetNeededForwardRefs(m_depInfo);
            WriteForwardDecls(fwdRefs);
            m_writtenFwdReferences = fwdRefs;
            // write includes
            ArrayList includesBefore = m_depManager.GetTypesToIncludeBeforeType(m_depInfo);
            WriteIncludeBeforeType(includesBefore);
        }

        private void EndTypeDefinition() {
            // map the types for which fwd references are included:
            Debug.WriteLine("ending type definition, type: " + m_depInfo.ForType);
            MapTypes(m_writtenFwdReferences);
            // write includes
            WriteIncludesAfterType(m_writtenFwdReferences);
        }

        /// <summary>map all the types in the list typesToMap (if not already mapped)</summary>
        private void MapTypes(ArrayList typesToMap) {
            IEnumerator enumerator = typesToMap.GetEnumerator();
            while (enumerator.MoveNext()) {
                MapTypeInfo info = (MapTypeInfo) enumerator.Current;
                if (!m_depManager.CheckMapped(info.m_type)) {
                    MappingAction mapAction = new GenerationActionDefineTypes(m_outputDirectory, m_depManager);
                    m_mapper.MapClsType(info.m_type, info.m_attributes, mapAction);
                }
            }
        }

        
        /// <summary>writes forward declarations</summary>
        private void WriteForwardDecls(ArrayList forwardDeclTypes) {
            GenerationActionWriteFwdDeclarations fwdWriteAction = new GenerationActionWriteFwdDeclarations(m_currentOutputStream);
            IEnumerator enumerator = forwardDeclTypes.GetEnumerator();
            while (enumerator.MoveNext()) {
                MapTypeInfo info = (MapTypeInfo) enumerator.Current;
                Debug.WriteLine("write fwd decl for type: " + info.m_type);
                m_mapper.MapClsType(info.m_type, info.m_attributes, fwdWriteAction);
            }
        }

        /// <summary> writes the includes before the type definition starts </summary>
        /// <param name="mappedBeforeType"></param>
        private void WriteIncludeBeforeType(ArrayList mappedBeforeType) {
            // write default includes for this modules
            m_currentOutputStream.WriteLine("#include \"orb.idl\"");
            m_currentOutputStream.WriteLine("#include \"Predef.idl\"");
            m_currentOutputStream.WriteLine("");
            // write includes for already mapped types
            WriteIncludes(mappedBeforeType);
        }

        private void WriteIncludesAfterType(ArrayList typesMappedAfter) {
            m_currentOutputStream.WriteLine("");
            WriteIncludes(typesMappedAfter);
        }

        /// <summary>write include statements for all types in the list</summary>
        private void WriteIncludes(ArrayList forTypes) {
            IEnumerator enumerator = forTypes.GetEnumerator();
            while (enumerator.MoveNext()) {
                MapTypeInfo info = (MapTypeInfo) enumerator.Current;
                string idlFileName = m_depManager.GetIdlFileForMappedType(info.m_type);
                if (idlFileName == null) { 
                    throw new Exception("internal error in dep-manager, mapped type missing after mapped before: " + info.m_type); 
                }
                m_currentOutputStream.WriteLine("#include \"" + idlFileName + "\"");
            }
        }
        
        /// <summary>writes the module opening for the specified hierarchy</summary>
        private void WriteModuleOpenings(string[] moduleHierarchy) {
            foreach (string module in moduleHierarchy) {
                m_currentOutputStream.WriteLine("module " + module + " {");
            }
            if (moduleHierarchy.Length > 0) { m_currentOutputStream.WriteLine(""); }
        }
        
        /// <summary>writes end-scope for the specified nr of scopes</summary>
        private void CloseOpenScopes(int nrOfScopes) {
            for (int i = 0; i < nrOfScopes; i++) {
                m_currentOutputStream.WriteLine("};");
            }
        }

        /// <summary>writes the repository id for the specified type</summary>
        /// <param name="idlName">the name of the type in IDL</param>
        private void WriteRepositoryID(Type forType, string idlName) {
            string repId = Repository.GetRepositoryID(forType);
            m_currentOutputStream.WriteLine("#pragma ID " + idlName + " \"" + repId + "\"");
            m_currentOutputStream.WriteLine("");
        }

        /// <summary>maps a method to IDL</summary>
        /// <param name="shouldThrowException">if true, generate a raise clause for method: GenericUserException</param>
        private void MapMethod(MethodInfo methodToMap, Type declaringType, bool shouldThrowException) {
            Type returnType = methodToMap.ReturnType;
            string returnTypeMapped = (string)m_mapper.MapClsType(returnType, 
                                                                  Util.AttributeExtCollection.ConvertToAttributeCollection(methodToMap.ReturnTypeCustomAttributes.GetCustomAttributes(true)),
                                                                  m_refMapper);
            m_currentOutputStream.Write(returnTypeMapped + " ");
            
            string mappedMethodName = IdlNaming.MapClsMethodNameToIdlName(methodToMap.Name, declaringType);
            m_currentOutputStream.Write(mappedMethodName + "(");
            
            ParameterInfo[] methodParams = methodToMap.GetParameters();
            for (int i = 0; i < methodParams.Length; i++) {
                if (i > 0) { m_currentOutputStream.Write(", "); }
                ParameterInfo info = methodParams[i];
                WriteParamDirection(info);    
                // type of param
                Type paramType = info.ParameterType;
                string paramTypeMapped = (string)m_mapper.MapClsType(paramType, 
                                                                     Util.AttributeExtCollection.ConvertToAttributeCollection(info.GetCustomAttributes(true)), 
                                                                     m_refMapper);
                m_currentOutputStream.Write(paramTypeMapped + " ");
                // name of param
                m_currentOutputStream.Write(info.Name); // TBD: check if no IDL-keyword ...
            }
            // closing bracket
            m_currentOutputStream.Write(")");
            if (shouldThrowException) {
                m_currentOutputStream.Write(" raises (::Ch::Elca::Iiop::GenericUserException)");
            }
            m_currentOutputStream.Write(";");

            m_currentOutputStream.WriteLine();
        }

        /// <summary>maps all the method of the Type typToMap, which are consistent with the bindingFlags</summary>
        /// <param name="shouldThrowException">if true, generate a raise clause for method: GenericUserException</param>
        private void MapMethods(Type typToMap, bool shouldThrowException, BindingFlags flags) {
            MethodInfo[] methods = typToMap.GetMethods(flags);
            foreach (MethodInfo info in methods) { 
                // private methods are not exposed
                // specialName-methods are not mapped, e.g. property accessor methods are such methods
                if ((!info.IsPrivate) && (!CheckIsMethodInInterfaceOrBase(typToMap, info, flags)) && (!info.IsSpecialName)) { 
                    MapMethod(info, typToMap, shouldThrowException);
                }
            }
        }

        /// <summary>
        /// checks, if the mehtod is already defined in a base class or an interface. If so, don't map it again
        /// </summary>
        /// <returns>true, if contained in a base class or interface, else returns false</returns>
        private bool CheckIsMethodInInterfaceOrBase(Type typeToMap, MethodInfo method, BindingFlags flags) {
            bool result = false;
            Type baseType = typeToMap.BaseType;
            if (baseType != null) {
                result = IsMethodInType(baseType, method, flags);
            }
            Type[] interfaces = typeToMap.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++) {
                result = result || IsMethodInType(interfaces[i], method, flags);
            }
            return result;
        }

        /// <summary>
        /// checks, if the property is already defined in a base class or an interface. If so, don't map it again
        /// </summary>
        /// <returns>true, if contained in a base class or interface, else returns false</returns>
        private bool CheckIsPropertyInInterfaceOrBase(Type typeToMap, PropertyInfo prop, BindingFlags flags) {
            bool result = false;
            Type baseType = typeToMap.BaseType;
            if (baseType != null) {
                result = IsPropertyInType(baseType, prop, flags);
            }
            Type[] interfaces = typeToMap.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++) {
                result = result || IsPropertyInType(interfaces[i], prop, flags);
            }
            return result;
        }

        /// <summary>
        /// checks if a method with the same signature and name as method is in type
        /// </summary>
        private bool IsMethodInType(Type type, MethodInfo method, BindingFlags flags) {
            MethodInfo typeMethod = type.GetMethod(method.Name, flags, null,
                                                   ExtractParamTypesFromParamInfoArray(method.GetParameters()),
                                                   null);
            if (typeMethod != null) {
                return true;
            } else {
                return false;    
            }
        }

        /// <summary>
        /// checks if a method with the same signature and name as method is in type
        /// </summary>
        private bool IsPropertyInType(Type type, PropertyInfo prop, BindingFlags flags) {
            
            PropertyInfo typeProp = type.GetProperty(prop.Name, flags);
            if (typeProp != null) {
                return true;
            } else {
                return false;    
            }
        }

        /// <summary>extract the paramterTypes out of a ParameterInfo-Array</summary>
        private Type[] ExtractParamTypesFromParamInfoArray(ParameterInfo[] paramArray) {
            Type[] result = new Type[paramArray.Length];
            for (int i = 0; i < paramArray.Length; i++) {
                result[i] = paramArray[i].ParameterType;
            }
            return result;
        }

        private void WriteParamDirection(ParameterInfo info) {
            if (Marshalling.ParameterMarshaller.IsInParam(info)) {
                m_currentOutputStream.Write("in");
            } else if (Marshalling.ParameterMarshaller.IsOutParam(info)) {
                m_currentOutputStream.Write("out");
            } else if (Marshalling.ParameterMarshaller.IsRefParam(info)) {
                m_currentOutputStream.Write("inout");
            }
            m_currentOutputStream.Write(" ");
        }

        /// <summary>map the fields of a concrete value type</summary>
        private void MapFields(Type typeToMap, BindingFlags flags) {
            FieldInfo[] fields = typeToMap.GetFields(flags);
            foreach (FieldInfo field in fields) {
                if (!field.IsNotSerialized) { // do not serialize transient fields
                    MapField(field, typeToMap);
                }
            }
        }            
        
        /// <summary>map the properties of a type</summary>
        private void MapProperties(Type typeToMap, BindingFlags flags) {
            PropertyInfo[] properties = typeToMap.GetProperties(flags);
            foreach (PropertyInfo info in properties) {
               if (!CheckIsPropertyInInterfaceOrBase(typeToMap, info, flags)) {
                   MapProperty(info, typeToMap);
               }
            }
        }

        private void MapField(FieldInfo fieldToMap, Type declaringType) {
            Type fieldType = fieldToMap.FieldType;
            string fieldTypeMapped = (string)m_mapper.MapClsType(fieldType, 
                                                                 Util.AttributeExtCollection.ConvertToAttributeCollection(fieldToMap.GetCustomAttributes(true)), 
                                                                 m_refMapper);
            if (fieldToMap.IsPrivate) { 
                m_currentOutputStream.Write("private ");
            } else {
                m_currentOutputStream.Write("public ");
            }
            m_currentOutputStream.Write(fieldTypeMapped + " ");
            // name of field
            m_currentOutputStream.Write(fieldToMap.Name); // TBD: check if no IDL-keyword ...
            m_currentOutputStream.WriteLine(";");
        }                    
        
        private void MapProperty(PropertyInfo propertyToMap, Type declaringType) {
            Type propertyType = propertyToMap.PropertyType;
            if ((propertyToMap.CanWrite) && (!propertyToMap.CanRead)) { return; } // not mappable
            if (!propertyToMap.CanWrite) { // readonly
                m_currentOutputStream.Write("readonly ");
            }
            m_currentOutputStream.Write("attribute ");
            string propTypeMapped = (string)m_mapper.MapClsType(propertyType, 
                                                                Util.AttributeExtCollection.ConvertToAttributeCollection(propertyToMap.GetCustomAttributes(true)),
                                                                m_refMapper);
            m_currentOutputStream.Write(propTypeMapped + " ");
            m_currentOutputStream.Write(propertyToMap.Name);
            m_currentOutputStream.WriteLine(";");
        }

        /// <summary>write the base type</summary>
        /// <returns>true, if basetype is mapped, else false</returns>
        private bool MapBaseType(Type forType) {
            Type baseType = forType.BaseType;
            if ((baseType == null) || (baseType.Equals(typeof(object))) || 
                (baseType.Equals(typeof(System.ComponentModel.MarshalByValueComponent))) ||
                (baseType.Equals(typeof(MarshalByRefObject))) || (baseType.Equals(typeof(ValueType)))) {
                return false;
            }
            m_currentOutputStream.Write(": ");
            string baseTypeMapped = (string)m_mapper.MapClsType(baseType, new Util.AttributeExtCollection(), m_refMapper);
            m_currentOutputStream.Write(baseTypeMapped);
            return true;
        }

        /// <summary>write the inherited interfaces</summary>
        private void MapInterfaces(Type forType) {
            Type[] interfaces = forType.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++) {
                if (i > 0) { m_currentOutputStream.Write(", "); }
                Type interf = interfaces[i];
                string ifMapped = (string)m_mapper.MapClsType(interf, new Util.AttributeExtCollection(), m_refMapper);
                m_currentOutputStream.Write(ifMapped);
            }
        }


        #region Implementation of MappingAction
        

        public object MapToIdlEnum(Type clsType) {    
            if (m_depManager.CheckMapped(clsType)) { return null; }
            // do a sanity check
            // TBD

            string unqualName; string[] modules;
            BeginType(clsType, out modules, out unqualName);

            // write type dependant information
            // begin type definition
            WriteModuleOpenings(modules);

            m_currentOutputStream.WriteLine("enum " + unqualName + "{");
            string[] enumerator = Enum.GetNames(clsType);
            for (int i = 0; i < enumerator.Length; i++) {
                if (i > 0) { m_currentOutputStream.Write(", "); }
                m_currentOutputStream.Write(unqualName + "_" + enumerator[i]);
            }
            m_currentOutputStream.WriteLine("");
            CloseOpenScopes(1);
            // end the type definition
            EndType();
            return null;
        }

        
        public object MapToIdlAbstractInterface(Type clsType) {
            if (m_depManager.CheckMapped(clsType)) { 
                return null; 
            }

            string unqualName; string[] modules;
            BeginType(clsType, out modules, out unqualName);
            
            
            // write type dependant information
            WriteModuleOpenings(modules);

            m_currentOutputStream.Write("abstract interface " + unqualName);
            if (clsType.GetInterfaces().Length > 0) {
                m_currentOutputStream.Write(": ");
                MapInterfaces(clsType);
            }
            m_currentOutputStream.WriteLine(" {");
            
            // map the properties and methods
            MapMethods(clsType, true, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            MapProperties(clsType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            
            CloseOpenScopes(1); // close interface scope
            m_currentOutputStream.WriteLine("");
            // write the PRAGMA ID
            WriteRepositoryID(clsType, unqualName);

            EndType();
            return null;
        }

        public object MapToIdlStruct(Type clsType) {
            if (m_depManager.CheckMapped(clsType)) { 
                return null; 
            }
            // normally, nothing has to be done here, therefore throw a NotSupportedException
            throw new NotSupportedException("only Types procuced from IDL to CLS mapping are mapped to an IDL-struct, therefore Generator doesn't map this type");
        }

        public object MapToIdlConcreateValueType(Type clsType) {
            if (m_depManager.CheckMapped(clsType)) { 
                return null; 
            }
            // do the mapping
            string unqualName; string[] modules;
            BeginType(clsType, out modules, out unqualName);
            
            // write type dependant information
            WriteModuleOpenings(modules);
            
            m_currentOutputStream.Write("valuetype " + unqualName);
            MapBaseType(clsType);
            if (clsType.GetInterfaces().Length > 0) {
                m_currentOutputStream.Write(" supports ");
                MapInterfaces(clsType);
            }
            m_currentOutputStream.WriteLine(" {");
            // map the state members
            MapFields(clsType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | 
                          BindingFlags.DeclaredOnly);
            
            // map the properties and methods
            MapMethods(clsType, false, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            MapProperties(clsType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            CloseOpenScopes(1); // close interface scope
            m_currentOutputStream.WriteLine("");
            // write the repository ID
            WriteRepositoryID(clsType, unqualName);

            EndType();
            return null;
        }

        public object MapToIdlConcreteInterface(Type clsType) {
            if (m_depManager.CheckMapped(clsType)) { 
                return null; 
            }
            // do the mapping
            string unqualName; string[] modules;
            BeginType(clsType, out modules, out unqualName);
            
            // write type dependant information

            WriteModuleOpenings(modules);
            m_currentOutputStream.Write("interface " + unqualName);
            bool baseTypeMapped = MapBaseType(clsType);
            if (clsType.GetInterfaces().Length > 0) {
                if (!baseTypeMapped) {
                    m_currentOutputStream.Write(": ");
                } else {
                    m_currentOutputStream.Write(", ");
                }
                MapInterfaces(clsType);
            }
            m_currentOutputStream.WriteLine(" {");
            m_currentOutputStream.WriteLine("");
            
            // map the properties and methods
            MapMethods(clsType, true, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            MapProperties(clsType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            CloseOpenScopes(1); // close interface scope
            m_currentOutputStream.WriteLine("");
            // write the repository ID
            WriteRepositoryID(clsType, unqualName);
            
            EndType();
            return null;
        }

        public object MapToIdlAbstractValueType(Type clsType) {
            if (m_depManager.CheckMapped(clsType)) { 
                return null; 
            }
            // do the mapping
            string unqualName; string[] modules;
            BeginType(clsType, out modules, out unqualName);

            // write type dependant information
            WriteModuleOpenings(modules);
            m_currentOutputStream.Write("abstract valuetype " + unqualName);
            // map the base types
            MapBaseType(clsType);
            if (clsType.GetInterfaces().Length > 0) {
                m_currentOutputStream.Write(" supports ");
                MapInterfaces(clsType);
            }
            m_currentOutputStream.WriteLine(" {");
            m_currentOutputStream.WriteLine("");
            MapMethods(clsType, false, BindingFlags.Instance | BindingFlags.Public | 
                           BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            
            CloseOpenScopes(1); // close interface scope
            m_currentOutputStream.WriteLine("");
            // write the repository ID
            WriteRepositoryID(clsType, unqualName);

            EndType();            
            return null;
            
        }

        public object MapToIdlBoxedValueType(Type clsType, AttributeExtCollection attributes, 
                                             bool isAlreadyBoxed) {
            if (m_depManager.CheckMapped(clsType)) { 
                return null; 
            }
            // do the mapping
            string unqualName; string[] modules;
            BeginType(clsType, out modules, out unqualName);

            // write type dependant information
            WriteModuleOpenings(modules);
            m_currentOutputStream.Write("valuetype " + unqualName);
            // write the only field:
            FieldInfo[] fields = clsType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | 
                                                   BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if ((fields == null) || (fields.Length != 1)) { 
                throw new ArgumentException("invalid boxed value type: " + clsType + ", only one field is allowed"); 
            }
            // now write the found field
            Type fieldType = fields[0].FieldType;
            string fieldTypeMapped = (string)m_mapper.MapClsType(fieldType, AttributeExtCollection.ConvertToAttributeCollection(fields[0].GetCustomAttributes(true)), m_refMapper);
            m_currentOutputStream.WriteLine(" " + fieldTypeMapped + ";");
            // write the repository ID
            WriteRepositoryID(clsType, unqualName);

            EndType();            
            return null;
        }

        public object MapException(Type clsType) {
            if (clsType.IsSubclassOf(typeof(omg.org.CORBA.AbstractCORBASystemException))) { 
                return null; 
            } // should already be mapped
            if (m_depManager.CheckMapped(clsType)) { 
                return null; 
            }
            // do the mapping
            string unqualName;
            string[] modules;
            BeginType(clsType, out modules, out unqualName);
            
            // write type dependant information
            WriteModuleOpenings(modules);
            
            m_currentOutputStream.Write("exception " + unqualName + "{");
            
            // map the state members
            FieldInfo[] fields = clsType.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                                   BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (FieldInfo fieldToMap in fields) {
                Type fieldType = fieldToMap.FieldType;
                string fieldTypeMapped = (string)m_mapper.MapClsType(fieldType, Util.AttributeExtCollection.ConvertToAttributeCollection(fieldToMap.GetCustomAttributes(true)),
                                                                     m_refMapper);
                m_currentOutputStream.Write(fieldTypeMapped + " ");
                // name of field
                m_currentOutputStream.Write(fieldToMap.Name); // TBD: check if no IDL-keyword ...
                m_currentOutputStream.WriteLine(";");
            }

            CloseOpenScopes(1); // close interface scope
            m_currentOutputStream.WriteLine("");
            // write the repository ID
            WriteRepositoryID(clsType, unqualName);

            EndType();
            return null;
        }

        public object MapToTypeDesc(Type clsType) {
            // nothing to do, type desc already defined
            if (m_depManager.CheckMapped(clsType)) {
                return null; 
            }
            throw new Exception("Dependenciy manager error, this type should be already mapped");
        }

        
        #region unsupported operations for this mapping action
        
        public object MapToIdlBoolean(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToIdlVoid(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToIdlOctet(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToIdlUShort(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToIdlShort(Type clsType)    {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToIdlULong(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToIdlLong(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToIdlLongLong(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToIdlULongLong(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToIdlFloat(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToIdlDouble(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToIdlChar(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToIdlWChar(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToIdlString(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToIdlWString(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToWStringValue(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToStringValue(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToIdlAny(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToTypeCode(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToValueBase(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }
        public object MapToAbstractBase(Type clsType) {
            throw new NotSupportedException("is a standard type, not redifinable");
        }

        public object MapToIdlSequence(Type clsType) {
            throw new NotSupportedException("no type declaration possible for IDL-Sequence");
        }

        #endregion unsupported operations for this mapping action

        #endregion Implementation of MappingAction

        #endregion IMethods

    }


    /// <summary>returns the IDL for a type referenced from another type. This class doesn't write type declarations/definition, this is left to GenerationActionDefineTypes
    /// All methods returns the IDL-string which shoulb be inserted, if the specified type is referenced</summary>
    internal class GenerationActionReference : MappingAction {
    
        #region IFields

        private ClsToIdlMapper m_mapper = ClsToIdlMapper.GetSingleton();

        #endregion IFields
        #region IMethods

        #region Implementation of MappingAction
        
        public object MapToIdlLong(System.Type dotNetType) {
            return "long";
        }
        public object MapToIdlULong(System.Type dotNetType) {
            return "ulong";
        }
        public object MapToIdlLongLong(System.Type dotNetType) {
            return "long long";
        }
        public object MapToIdlULongLong(System.Type dotNetType) {
            return "ulong ulong";
        }
        public object MapToIdlUShort(System.Type dotNetType) {
            return "ushort";
        }
        public object MapToIdlShort(System.Type dotNetType) {
            return "short";
        }
        public object MapToIdlOctet(System.Type dotNetType) {
            return "octet";
        }
        public object MapToIdlVoid(System.Type dotNetType) {
            return "void";
        }        
        public object MapToIdlFloat(System.Type dotNetType) {
            return "float";
        }
        public object MapToIdlDouble(System.Type dotNetType) {
            return "double";
        }
        public object MapToIdlChar(System.Type dotNetType) {
            return "char";
        }
        public object MapToIdlWChar(System.Type dotNetType) {
            return "wchar";
        }
        public object MapToIdlBoolean(System.Type dotNetType) {
            return "boolean";
        }
        public object MapToIdlString(System.Type dotNetType) {
            return "string";
        }
        public object MapToIdlWString(System.Type dotNetType) {
            return "wstring";
        }
        public object MapToIdlAny(System.Type dotNetType) {
            return "any";
        }
        public object MapToStringValue(System.Type dotNetType) {
            return "::CORBA::StringValue";
        }
        public object MapToWStringValue(System.Type dotNetType) {
            return "::CORBA::WStringValue";
        }
        public object MapToIdlEnum(System.Type dotNetType) {
            return IdlNaming.MapFullTypeNameToIdlScoped(dotNetType);
        }
        public object MapToIdlConcreteInterface(System.Type dotNetType) {
            if (!dotNetType.Equals(typeof(MarshalByRefObject))) {
                return IdlNaming.MapFullTypeNameToIdlScoped(dotNetType);
            } else {
            	return "Object";
            }
        }
        public object MapToIdlConcreateValueType(System.Type dotNetType) {
            return IdlNaming.MapFullTypeNameToIdlScoped(dotNetType);
        }
        public object MapToIdlAbstractInterface(System.Type dotNetType) {
            return IdlNaming.MapFullTypeNameToIdlScoped(dotNetType);
        }
        public object MapToIdlStruct(System.Type dotNetType) {
            return IdlNaming.MapFullTypeNameToIdlScoped(dotNetType);
        }
        public object MapToIdlAbstractValueType(System.Type dotNetType) {
            return IdlNaming.MapFullTypeNameToIdlScoped(dotNetType);
        }
        public object MapToIdlSequence(System.Type dotNetType) {
            string refToElemType = (string)m_mapper.MapClsType(dotNetType.GetElementType(), new Util.AttributeExtCollection(), this);
            return "sequence<" + refToElemType + ">";
        }
        public object MapToIdlBoxedValueType(System.Type dotNetType, AttributeExtCollection attributes, bool isAlreadyBoxed) {
            // the dotNetType is a subclass of BoxedValueBase representing the boxed value type
            return IdlNaming.MapFullTypeNameToIdlScoped(dotNetType);
        }
        public object MapToValueBase(System.Type dotNetType) {
            return "::CORBA::ValueBase";
        }
        public object MapToAbstractBase(System.Type dotNetType) {
            return "::CORBA:AbstractBase";
        }
        
        public object MapToTypeDesc(System.Type dotNetType) {
            return null;
        }        
        public object MapToTypeCode(System.Type dotNetType) {
            return "::CORBA::TypeCode";
        }
        public object MapException(System.Type dotNetType) {
            return IdlNaming.MapShortTypeNameToIdl(dotNetType);
        }
        #endregion

        #endregion IMethods

    }

    ///<summary>this action writes forward declarations for interfaces and valuetypes</summary>
    internal class GenerationActionWriteFwdDeclarations : MappingAction {

        #region IFields
        
        private TextWriter m_writeTo;

        private string[] m_modulesOpen;

        #endregion IFields
        #region IConstructors

        public GenerationActionWriteFwdDeclarations(TextWriter writeTo) {
            m_writeTo = writeTo;
        }

        #endregion IConstructors
        #region IMethods

        /// <summary>writes the module opening for the specified hierarchy</summary>
        private void WriteModuleOpenings(string[] moduleHierarchy) {
            foreach (string module in moduleHierarchy) {
                m_writeTo.WriteLine("module " + module + " {");
            }
            if (moduleHierarchy.Length > 0) { m_writeTo.WriteLine(""); }
        }
        
        /// <summary>writes end-scope for the specified nr of scopes</summary>
        private void CloseOpenScopes(int nrOfScopes) {
            for (int i = 0; i < nrOfScopes; i++) {
                m_writeTo.WriteLine("};");
            }
        }

        private string BeginfwdDecl(Type dotNetType) {
            string unqualName = IdlNaming.MapShortTypeNameToIdl(dotNetType);
            m_modulesOpen = IdlNaming.MapNamespaceToIdlModules(dotNetType);

            // protect against redefinition:
            m_writeTo.Write("#ifndef ");
            string def = "__";
            foreach (string mod in m_modulesOpen) {
                def = def + mod + "_";
            }
            def = def + unqualName + "__";
            m_writeTo.WriteLine(def);
            // write modules opening
            WriteModuleOpenings(m_modulesOpen);
            return unqualName;
        }

        private void EndfwdDelc() {
            CloseOpenScopes(m_modulesOpen.Length);
            
            m_writeTo.WriteLine("#endif");
        }
        
        
        #region Implementation of MappingAction
        
        public object MapToIdlAbstractInterface(System.Type dotNetType) {
            string unqualName = BeginfwdDecl(dotNetType);
            m_writeTo.WriteLine("abstract interface " + unqualName + ";");
            EndfwdDelc();    
            return null;
        }

        public object MapToIdlConcreteInterface(System.Type dotNetType) {
            string unqualName = BeginfwdDecl(dotNetType);
            m_writeTo.WriteLine("interface " + unqualName + ";");
            EndfwdDelc();
            return null;
        }
        
        public object MapToIdlAbstractValueType(System.Type dotNetType) {
            string unqualName = BeginfwdDecl(dotNetType);
            m_writeTo.WriteLine("abstract valuetype " + unqualName + ";");
            EndfwdDelc();            
            return null;
        }

        public object MapToIdlConcreateValueType(System.Type dotNetType) {
            string unqualName = BeginfwdDecl(dotNetType);
            m_writeTo.WriteLine("valuetype " + unqualName + ";");
            EndfwdDelc();
            return null;
        }

        public object MapToIdlBoxedValueType(System.Type dotNetType, AttributeExtCollection attributes, bool isAlreadyBoxed) {
            throw new NotSupportedException("a value-box can't have a forward declaration");
        }
        
        #region unsupported mappings for fwd decls

        public object MapToIdlVoid(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToIdlBoolean(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToIdlOctet(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }
        
        public object MapToIdlShort(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToIdlUShort(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToIdlLong(System.Type dotNetType){
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToIdlULong(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToIdlLongLong(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToIdlULongLong(System.Type dotNetType)    {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToIdlFloat(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToIdlDouble(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToIdlChar(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToIdlWChar(System.Type dotNetType)    {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToIdlString(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToIdlWString(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }
        
        public object MapToStringValue(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }
        
        public object MapToWStringValue(System.Type dotNetType)    {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }
        
        public object MapToIdlEnum(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToIdlStruct(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToIdlSequence(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToIdlAny(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToValueBase(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToAbstractBase(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToTypeDesc(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapToTypeCode(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        public object MapException(System.Type dotNetType) {
            throw new NotSupportedException("no fwd declaration possible for this IDL-type");
        }

        #endregion
                
        #endregion

        #endregion IMethods


    }

}
