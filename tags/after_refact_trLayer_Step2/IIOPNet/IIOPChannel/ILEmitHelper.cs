/* UnionGenerationHelper.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 02.10.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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
using System.Reflection;
using System.Reflection.Emit;

namespace Ch.Elca.Iiop.Idl {

   
    /// <summary>
    /// helper class to collect data about a parameter of an operation
    /// </summary>
    public class ParameterSpec {
        
        #region Types
        
        /// <summary>helper class for specifying parameter directions</summary>
        public class ParameterDirection {
        
            #region Constants

            private const int ParamDir_IN = 0;
            private const int ParamDir_OUT = 1;
            private const int ParamDir_INOUT = 2;

            #endregion Constants
            #region SFields
        	
            public static readonly ParameterDirection s_inout = new ParameterDirection(ParamDir_INOUT);
            public static readonly ParameterDirection s_in = new ParameterDirection(ParamDir_IN);
            public static readonly ParameterDirection s_out = new ParameterDirection(ParamDir_OUT);
        	
            #endregion SFields
            #region IFields
        	
            private int m_direction;
        	
            #endregion IFields
            #region IConstructors
        		
            private ParameterDirection(int direction) {
                m_direction = direction;
            }
        	
            #endregion IConstructors
            #region IMethods
			
            public bool IsInOut() {
                return (m_direction == ParamDir_INOUT);
            }

            public bool IsIn() {
                return (m_direction == ParamDir_IN);
            }

            public bool IsOut() {
                return (m_direction == ParamDir_OUT);
            }
			
            #endregion IMethods
        	
        }
        
        #endregion Types
        #region IFields

        private TypeContainer m_paramType;
        private String m_paramName;
        private ParameterDirection m_direction;

        #endregion IFields
        #region IConstructors
        
        public ParameterSpec(String paramName, TypeContainer paramType, 
            ParameterDirection direction) {
            m_paramName = paramName;
            m_paramType = paramType;
            m_direction = direction;
        }
        
        /// <summary>creates an in parameterspec</summary>
        public ParameterSpec(String paramName, Type clsType) {
            m_paramName = paramName;
            m_paramType = new TypeContainer(clsType);
            m_direction = ParameterDirection.s_in;
        }

        #endregion IConstructors
        #region IMethods

        public String GetPramName() {
            return m_paramName;
        }
        
        public TypeContainer GetParamType() {
            return m_paramType;
        }

        /// <summary>
        /// merges the separated cls type with param direction:
        /// for inout/out parameters a ....& type is needed.
        /// </summary>
        /// <returns></returns>
        public Type GetParamTypeMergedDirection() {
            // get correct type for param direction:
            if (IsIn()) {
                return m_paramType.GetSeparatedClsType();
            } else { // out or inout parameter
                // need a type which represents a reference to the parametertype
                Assembly declAssembly = m_paramType.GetSeparatedClsType().Assembly;
                return declAssembly.GetType(m_paramType.GetSeparatedClsType().FullName + "&"); // not nice, better solution ?
            }
        }

        public ParameterDirection GetParamDirection() {
            return m_direction;
        }

        public bool IsInOut() {
            return m_direction.IsInOut();
        }

        public bool IsIn() {
            return m_direction.IsIn();
        }

        public bool IsOut() {
            return m_direction.IsOut();
        }

        #endregion IMethods

    }

    
    /// <summary>
    /// provides some help in generating methods / fields / ...
    /// </summary>
    public class IlEmitHelper {

        #region SFields    

        private static IlEmitHelper s_singleton = new IlEmitHelper();

        /** reference to one of the internal constructor of class ParameterInfo. Used for assigning custom attributes to the return parameter */
        private static ConstructorInfo s_paramBuildConstr;
    
        #endregion SFields
        #region IFields

        
        #endregion IFields
        #region SConstructor

        static IlEmitHelper() {
            // work around: need a way to define attributes on return parameter
            // TBD: search better way
            Type paramBuildType = typeof(ParameterBuilder);
            s_paramBuildConstr = paramBuildType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                                                               new Type[] { typeof(MethodBuilder), 
                                                                            typeof(System.Int32), 
                                                                            typeof(ParameterAttributes),
                                                                            typeof(String) }, 
                                                               null);
        }

        #endregion SConstructor
        #region IConsturctors
    
        private IlEmitHelper() {
        }
    
        #endregion IConstructors
        #region SMethods
    
        public static IlEmitHelper GetSingleton() {
            return s_singleton;
        }
    
        #endregion SMethods
        #region IMethods

        /// <summary>adds a method to a type, setting the attributes on the parameters</summary>
        /// <remarks>forgeign method: should be better on TypeBuilder, but not possible</remarks>
        /// <returns>the MethodBuilder for the method created</returns>
        public MethodBuilder AddMethod(TypeBuilder builder, string methodName, ParameterSpec[] parameters, 
                                       TypeContainer returnType, MethodAttributes attrs) {
        
            Type[] paramTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) { 
                paramTypes[i] = parameters[i].GetParamTypeMergedDirection();
            }
        
            MethodBuilder methodBuild = builder.DefineMethod(methodName, attrs, 
                                                             returnType.GetSeparatedClsType(),
                                                             paramTypes);
            // define the paramter-names / attributes
            for (int i = 0; i < parameters.Length; i++) {
                DefineParamter(methodBuild, parameters[i], i+1);
            }
            // add custom attributes for the return type
            ParameterBuilder paramBuild = CreateParamBuilderForRetParam(methodBuild);
            for (int i = 0; i < returnType.GetSeparatedAttrs().Length; i++) {
                paramBuild.SetCustomAttribute(returnType.GetSeparatedAttrs()[i]);
            }
            return methodBuild;
        }
        
        /// <summary>adds a constructor to a type, setting the attributes on the parameters</summary>
        /// <remarks>forgeign method: should be better on TypeBuilder, but not possible</remarks>
        /// <returns>the ConstructorBuilder for the method created</returns>
        public ConstructorBuilder AddConstructor(TypeBuilder builder, ParameterSpec[] parameters, 
                                                 MethodAttributes attrs) {
        
            Type[] paramTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) { 
                paramTypes[i] = parameters[i].GetParamTypeMergedDirection();
            }
        
            ConstructorBuilder constrBuild = 
                builder.DefineConstructor(attrs, CallingConventions.Standard,
                                          paramTypes);
            // define the paramter-names / attributes
            for (int i = 0; i < parameters.Length; i++) {
                ParameterAttributes paramAttr = ParameterAttributes.None;
                ParameterBuilder paramBuild = 
                    constrBuild.DefineParameter(i + 1, paramAttr, 
                                                parameters[i].GetPramName());
                // custom attribute spec
                TypeContainer specType = parameters[i].GetParamType();
                for (int j = 0; j < specType.GetSeparatedAttrs().Length; j++) {
                    paramBuild.SetCustomAttribute(specType.GetSeparatedAttrs()[j]);    
                }                
            }
            
            return constrBuild;
        }

        
        /// <summary>adds a field to a type, including the custom attributes needed</summary>
        /// <remarks>forgeign method: should be better on TypeBuilder, but not possible</remarks>
        /// <returns>the FieldBuilder for the field created</returns>
        public FieldBuilder AddFieldWithCustomAttrs(TypeBuilder builder, string fieldName, 
                                                    TypeContainer fieldType, FieldAttributes attrs) {
            // consider custom mappings
            Type clsType = fieldType.GetSeparatedClsType();
            FieldBuilder fieldBuild = builder.DefineField(fieldName, clsType, attrs);
            // add custom attributes
            for (int j = 0; j < fieldType.GetSeparatedAttrs().Length; j++) {
                fieldBuild.SetCustomAttribute(fieldType.GetSeparatedAttrs()[j]);
            }
            return fieldBuild;
        }


        /// <summary>
        /// adds a property setter method.
        /// </summary>
        /// <param name="attrs">MethodAttributes, automatically adds HideBySig and SpecialName</param>
        public MethodBuilder AddPropertySetter(TypeBuilder builder, string propertyName,
                                               TypeContainer propertyType, MethodAttributes attrs) {
            Type propTypeCls = propertyType.GetSeparatedClsType();
            MethodBuilder setAccessor = builder.DefineMethod("__set_" + propertyName, 
                                                             attrs | MethodAttributes.HideBySig | MethodAttributes.SpecialName, 
                                                             null, new System.Type[] { propTypeCls });
            
            ParameterBuilder valParam = setAccessor.DefineParameter(1, ParameterAttributes.None, "value"); 
            // add custom attributes
            for (int j = 0; j < propertyType.GetSeparatedAttrs().Length; j++) {
                valParam.SetCustomAttribute(propertyType.GetSeparatedAttrs()[j]);
            }
            return setAccessor;
        }

        /// <summary>
        /// adds a property getter method.
        /// </summary>
        /// <param name="attrs">MethodAttributes, automatically adds HideBySig and SpecialName</param>
        public MethodBuilder AddPropertyGetter(TypeBuilder builder, string propertyName,
                                               TypeContainer propertyType, MethodAttributes attrs) {
            Type propTypeCls = propertyType.GetSeparatedClsType();
            MethodBuilder getAccessor = builder.DefineMethod("__get_" + propertyName, 
                                                             attrs | MethodAttributes.HideBySig | MethodAttributes.SpecialName, 
                                                             propTypeCls, System.Type.EmptyTypes);
            
            ParameterBuilder retParamGet = CreateParamBuilderForRetParam(getAccessor);
            // add custom attributes
            for (int j = 0; j < propertyType.GetSeparatedAttrs().Length; j++) {                
                retParamGet.SetCustomAttribute(propertyType.GetSeparatedAttrs()[j]);
            }
            return getAccessor;
        }

        /// <summary>
        /// adds a property to a type, including the custom attributes needed.
        /// </summary>
        public PropertyBuilder AddProperty(TypeBuilder builder, string propertyName, 
                                           TypeContainer propertyType, 
                                           MethodBuilder getAccessor, MethodBuilder setAccessor) {
            Type propTypeCls = propertyType.GetSeparatedClsType();
            PropertyBuilder propBuild = builder.DefineProperty(propertyName, PropertyAttributes.None, 
                                                               propTypeCls, System.Type.EmptyTypes);
            // add accessor methods
            if (getAccessor != null) {
                propBuild.SetGetMethod(getAccessor);
            }            
            if (setAccessor != null) {
                propBuild.SetSetMethod(setAccessor);
            }
            // define custom attributes
            for (int j = 0; j < propertyType.GetSeparatedAttrs().Length; j++) {
                propBuild.SetCustomAttribute(propertyType.GetSeparatedAttrs()[j]);                
            }
            return propBuild;
        }

        /// <summary>
        /// reefines a prameter; not possible for return parameter, ...? TODO: refact ...
        /// </summary>
        /// <param name="methodBuild"></param>
        /// <param name="spec"></param>
        /// <param name="paramNr"></param>
        public void DefineParamter(MethodBuilder methodBuild, ParameterSpec spec, int paramNr) {
            ParameterAttributes paramAttr = ParameterAttributes.None;
            if (spec.IsOut()) { 
                paramAttr = paramAttr | ParameterAttributes.Out; 
            }
            ParameterBuilder paramBuild = methodBuild.DefineParameter(paramNr, paramAttr, spec.GetPramName());
            // custom attribute spec
            TypeContainer specType = spec.GetParamType();
            for (int i = 0; i < specType.GetSeparatedAttrs().Length; i++) {
                paramBuild.SetCustomAttribute(specType.GetSeparatedAttrs()[i]);    
            }
        }

        /// <summary>
        /// need this, because define-parameter prevent creating a parameterbuilder for param-0, the ret param.
        /// For defining custom attributes on the ret-param, a parambuilder is however needed
        /// TBD: search nicer solution for this 
        /// </summary>
        /// <remarks>should be on MethodBuilder, but not possible to change MethodBuilder-class</remarks>
        public ParameterBuilder CreateParamBuilderForRetParam(MethodBuilder forMethod) {
            return (ParameterBuilder) s_paramBuildConstr.Invoke(new Object[] { forMethod, (System.Int32) 0, ParameterAttributes.Retval, "" } );
        }

        #endregion IMethods
    
    }


}