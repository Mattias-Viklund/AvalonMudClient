﻿using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using MoonSharp.Interpreter.Compatibility;
using MoonSharp.Interpreter.Diagnostics;
using MoonSharp.Interpreter.Interop.BasicDescriptors;
using MoonSharp.Interpreter.Interop.Converters;

namespace MoonSharp.Interpreter.Interop
{
    /// <summary>
    /// Class providing easier marshalling of CLR properties
    /// </summary>
    public class PropertyMemberDescriptor : IMemberDescriptor, IOptimizableDescriptor,
        IWireableDescriptor
    {
        private MethodInfo m_Getter, m_Setter;
        private Func<object, object> m_OptimizedGetter;
        private Action<object, object> m_OptimizedSetter;


        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyMemberDescriptor"/> class.
        /// NOTE: This constructor gives get/set visibility based exclusively on the CLR visibility of the 
        /// getter and setter methods.
        /// </summary>
        /// <param name="pi">The pi.</param>
        /// <param name="accessMode">The access mode.</param>
        public PropertyMemberDescriptor(PropertyInfo pi, InteropAccessMode accessMode)
            : this(pi, accessMode, Framework.Do.GetGetMethod(pi), Framework.Do.GetSetMethod(pi))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyMemberDescriptor" /> class.
        /// </summary>
        /// <param name="pi">The PropertyInfo.</param>
        /// <param name="accessMode">The <see cref="InteropAccessMode" /></param>
        /// <param name="getter">The getter method. Use null to make the property writeonly.</param>
        /// <param name="setter">The setter method. Use null to make the property readonly.</param>
        public PropertyMemberDescriptor(PropertyInfo pi, InteropAccessMode accessMode, MethodInfo getter,
            MethodInfo setter)
        {
            if (getter == null && setter == null)
            {
                throw new ArgumentNullException("getter and setter cannot both be null");
            }

            if (Script.GlobalOptions.Platform.IsRunningOnAOT())
            {
                accessMode = InteropAccessMode.Reflection;
            }

            this.PropertyInfo = pi;
            this.AccessMode = accessMode;
            this.Name = pi.Name;

            m_Getter = getter;
            m_Setter = setter;

            this.IsStatic = (m_Getter ?? m_Setter).IsStatic;

            if (this.AccessMode == InteropAccessMode.Preoptimized)
            {
                this.OptimizeGetter();
                this.OptimizeSetter();
            }
        }

        /// <summary>
        /// Gets the PropertyInfo got by reflection
        /// </summary>
        public PropertyInfo PropertyInfo { get; }

        /// <summary>
        /// Gets the <see cref="InteropAccessMode" />
        /// </summary>
        public InteropAccessMode AccessMode { get; }

        /// <summary>
        /// Gets a value indicating whether this instance can be read from
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can be read from; otherwise, <c>false</c>.
        /// </value>
        public bool CanRead => m_Getter != null;

        /// <summary>
        /// Gets a value indicating whether this instance can be written to.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can be written to; otherwise, <c>false</c>.
        /// </value>
        public bool CanWrite => m_Setter != null;

        /// <summary>
        /// Gets a value indicating whether the described property is static.
        /// </summary>
        public bool IsStatic { get; }

        /// <summary>
        /// Gets the name of the property
        /// </summary>
        public string Name { get; }


        /// <summary>
        /// Gets the value of the property
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        public DynValue GetValue(Script script, object obj)
        {
            this.CheckAccess(MemberDescriptorAccess.CanRead, obj);

            if (m_Getter == null)
            {
                throw new ScriptRuntimeException("userdata property '{0}.{1}' cannot be read from.",
                    this.PropertyInfo.DeclaringType.Name, this.Name);
            }

            if (this.AccessMode == InteropAccessMode.LazyOptimized && m_OptimizedGetter == null)
            {
                this.OptimizeGetter();
            }

            object result;

            if (m_OptimizedGetter != null)
            {
                result = m_OptimizedGetter(obj);
            }
            else
            {
                result = m_Getter.Invoke(this.IsStatic ? null : obj,
                    null); // convoluted workaround for --full-aot Mono execution
            }

            return ClrToScriptConversions.ObjectToDynValue(script, result);
        }

        /// <summary>
        /// Sets the value of the property
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        /// <param name="v">The value to set.</param>
        public void SetValue(Script script, object obj, DynValue v)
        {
            this.CheckAccess(MemberDescriptorAccess.CanWrite, obj);

            if (m_Setter == null)
            {
                throw new ScriptRuntimeException("userdata property '{0}.{1}' cannot be written to.",
                    this.PropertyInfo.DeclaringType.Name, this.Name);
            }

            var value = ScriptToClrConversions.DynValueToObjectOfType(v, this.PropertyInfo.PropertyType, null, false);

            try
            {
                if (value is double d)
                {
                    value = NumericConversions.DoubleToType(this.PropertyInfo.PropertyType, d);
                }

                if (this.AccessMode == InteropAccessMode.LazyOptimized && m_OptimizedSetter == null)
                {
                    this.OptimizeSetter();
                }

                if (m_OptimizedSetter != null)
                {
                    m_OptimizedSetter(obj, value);
                }
                else
                {
                    m_Setter.Invoke(this.IsStatic ? null : obj,
                        new[] {value}); // convoluted workaround for --full-aot Mono execution
                }
            }
            catch (ArgumentException)
            {
                // non-optimized setters fall here
                throw ScriptRuntimeException.UserDataArgumentTypeMismatch(v.Type, this.PropertyInfo.PropertyType);
            }
            catch (InvalidCastException)
            {
                // optimized setters fall here
                throw ScriptRuntimeException.UserDataArgumentTypeMismatch(v.Type, this.PropertyInfo.PropertyType);
            }
        }


        /// <summary>
        /// Gets the types of access supported by this member
        /// </summary>
        public MemberDescriptorAccess MemberAccess
        {
            get
            {
                MemberDescriptorAccess access = 0;

                if (m_Setter != null)
                {
                    access |= MemberDescriptorAccess.CanWrite;
                }

                if (m_Getter != null)
                {
                    access |= MemberDescriptorAccess.CanRead;
                }

                return access;
            }
        }

        /// <summary>
        /// Called by standard descriptors when background optimization or preoptimization needs to be performed.
        /// </summary>
        void IOptimizableDescriptor.Optimize()
        {
            this.OptimizeGetter();
            this.OptimizeSetter();
        }

        /// <summary>
        /// Prepares the descriptor for hard-wiring.
        /// The descriptor fills the passed table with all the needed data for hardwire generators to generate the appropriate code.
        /// </summary>
        /// <param name="t">The table to be filled</param>
        public void PrepareForWiring(Table t)
        {
            t.Set("class", DynValue.NewString(this.GetType().FullName));
            t.Set("visibility", DynValue.NewString(this.PropertyInfo.GetClrVisibility()));
            t.Set("name", DynValue.NewString(this.Name));
            t.Set("static", DynValue.NewBoolean(this.IsStatic));
            t.Set("read", DynValue.NewBoolean(this.CanRead));
            t.Set("write", DynValue.NewBoolean(this.CanWrite));
            t.Set("decltype", DynValue.NewString(this.PropertyInfo.DeclaringType.FullName));
            t.Set("declvtype", DynValue.NewBoolean(Framework.Do.IsValueType(this.PropertyInfo.DeclaringType)));
            t.Set("type", DynValue.NewString(this.PropertyInfo.PropertyType.FullName));
        }


        /// <summary>
        /// Tries to create a new StandardUserDataPropertyDescriptor, returning <c>null</c> in case the property is not 
        /// visible to script code.
        /// </summary>
        /// <param name="pi">The PropertyInfo.</param>
        /// <param name="accessMode">The <see cref="InteropAccessMode" /></param>
        /// <returns>A new StandardUserDataPropertyDescriptor or null.</returns>
        public static PropertyMemberDescriptor TryCreateIfVisible(PropertyInfo pi, InteropAccessMode accessMode)
        {
            var getter = Framework.Do.GetGetMethod(pi);
            var setter = Framework.Do.GetSetMethod(pi);

            var pvisible = pi.GetVisibilityFromAttributes();
            var gvisible = getter.GetVisibilityFromAttributes();
            var svisible = setter.GetVisibilityFromAttributes();

            if (pvisible.HasValue)
            {
                return TryCreate(pi, accessMode,
                    (gvisible ?? pvisible.Value) ? getter : null,
                    (svisible ?? pvisible.Value) ? setter : null);
            }

            return TryCreate(pi, accessMode,
                (gvisible ?? getter.IsPublic) ? getter : null,
                (svisible ?? setter.IsPublic) ? setter : null);
        }

        private static PropertyMemberDescriptor TryCreate(PropertyInfo pi, InteropAccessMode accessMode,
            MethodInfo getter, MethodInfo setter)
        {
            if (getter == null && setter == null)
            {
                return null;
            }

            return new PropertyMemberDescriptor(pi, accessMode, getter, setter);
        }

        internal void OptimizeGetter()
        {
            using (PerformanceStatistics.StartGlobalStopwatch(PerformanceCounter.AdaptersCompilation))
            {
                if (m_Getter != null)
                {
                    if (this.IsStatic)
                    {
                        var paramExp = Expression.Parameter(typeof(object), "dummy");
                        var propAccess = Expression.Property(null, this.PropertyInfo);
                        var castPropAccess = Expression.Convert(propAccess, typeof(object));
                        var lambda = Expression.Lambda<Func<object, object>>(castPropAccess, paramExp);
                        Interlocked.Exchange(ref m_OptimizedGetter, lambda.Compile());
                    }
                    else
                    {
                        var paramExp = Expression.Parameter(typeof(object), "obj");
                        var castParamExp = Expression.Convert(paramExp, this.PropertyInfo.DeclaringType);
                        var propAccess = Expression.Property(castParamExp, this.PropertyInfo);
                        var castPropAccess = Expression.Convert(propAccess, typeof(object));
                        var lambda = Expression.Lambda<Func<object, object>>(castPropAccess, paramExp);
                        Interlocked.Exchange(ref m_OptimizedGetter, lambda.Compile());
                    }
                }
            }
        }

        internal void OptimizeSetter()
        {
            using (PerformanceStatistics.StartGlobalStopwatch(PerformanceCounter.AdaptersCompilation))
            {
                if (m_Setter != null && !(Framework.Do.IsValueType(this.PropertyInfo.DeclaringType)))
                {
                    var setterMethod = Framework.Do.GetSetMethod(this.PropertyInfo);

                    if (this.IsStatic)
                    {
                        var paramExp = Expression.Parameter(typeof(object), "dummy");
                        var paramValExp = Expression.Parameter(typeof(object), "val");
                        var castParamValExp = Expression.Convert(paramValExp, this.PropertyInfo.PropertyType);
                        var callExpression = Expression.Call(setterMethod, castParamValExp);
                        var lambda = Expression.Lambda<Action<object, object>>(callExpression, paramExp, paramValExp);
                        Interlocked.Exchange(ref m_OptimizedSetter, lambda.Compile());
                    }
                    else
                    {
                        var paramExp = Expression.Parameter(typeof(object), "obj");
                        var paramValExp = Expression.Parameter(typeof(object), "val");
                        var castParamExp = Expression.Convert(paramExp, this.PropertyInfo.DeclaringType);
                        var castParamValExp = Expression.Convert(paramValExp, this.PropertyInfo.PropertyType);
                        var callExpression = Expression.Call(castParamExp, setterMethod, castParamValExp);
                        var lambda = Expression.Lambda<Action<object, object>>(callExpression, paramExp, paramValExp);
                        Interlocked.Exchange(ref m_OptimizedSetter, lambda.Compile());
                    }
                }
            }
        }
    }
}