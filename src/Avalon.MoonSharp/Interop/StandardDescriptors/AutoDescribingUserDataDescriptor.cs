﻿using System;
using MoonSharp.Interpreter.Compatibility;
using MoonSharp.Interpreter.Interop;

namespace MoonSharp.Interpreter
{
    /// <summary>
    /// Descriptor which acts as a non-containing adapter from IUserDataType to IUserDataDescriptor
    /// </summary>
    internal class AutoDescribingUserDataDescriptor : IUserDataDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoDescribingUserDataDescriptor"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="friendlyName">Name of the friendly.</param>
        public AutoDescribingUserDataDescriptor(Type type, string friendlyName)
        {
            this.Name = friendlyName;
            this.Type = type;
        }

        /// <summary>
        /// Gets the name of the descriptor (usually, the name of the type described).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type this descriptor refers to
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Performs an "index" "get" operation.
        /// </summary>
        /// <param name="ecToken">The execution control token of the script processing thread</param>
        /// <param name="script">The script originating the request</param>
        /// <param name="obj">The object (null if a static request is done)</param>
        /// <param name="index">The index.</param>
        /// <param name="isDirectIndexing">If set to true, it's indexed with a name, if false it's indexed through brackets.</param>
        public DynValue Index(ExecutionControlToken ecToken, Script script, object obj, DynValue index,
            bool isDirectIndexing)
        {
            var u = obj as IUserDataType;

            if (u != null)
            {
                return u.Index(ecToken, script, index, isDirectIndexing);
            }

            return null;
        }

        /// <summary>
        /// Performs an "index" "set" operation.
        /// </summary>
        /// <param name="ecToken">The execution control token of the script processing thread</param>
        /// <param name="script">The script originating the request</param>
        /// <param name="obj">The object (null if a static request is done)</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value to be set</param>
        /// <param name="isDirectIndexing">If set to true, it's indexed with a name, if false it's indexed through brackets.</param>
        public bool SetIndex(ExecutionControlToken ecToken, Script script, object obj, DynValue index, DynValue value,
            bool isDirectIndexing)
        {
            var u = obj as IUserDataType;

            if (u != null)
            {
                return u.SetIndex(ecToken, script, index, value, isDirectIndexing);
            }

            return false;
        }

        /// <summary>
        /// Converts this userdata to string
        /// </summary>
        /// <param name="obj">The object.</param>
        public string AsString(object obj)
        {
            if (obj != null)
            {
                return obj.ToString();
            }

            return null;
        }

        /// <summary>
        /// Gets a "meta" operation on this userdata. If a descriptor does not support this functionality,
        /// it should return "null" (not a nil). 
        /// These standard metamethods can be supported (the return value should be a function accepting the
        /// classic parameters of the corresponding metamethod):
        /// __add, __sub, __mul, __div, __div, __pow, __unm, __eq, __lt, __le, __lt, __len, __concat, 
        /// __pairs, __ipairs, __iterator, __call
        /// These standard metamethods are supported through other calls for efficiency:
        /// __index, __newindex, __tostring
        /// </summary>
        /// <param name="ecToken">The execution control token of the script processing thread</param>
        /// <param name="script">The script originating the request</param>
        /// <param name="obj">The object (null if a static request is done)</param>
        /// <param name="metaname">The name of the metamember.</param>
        public DynValue MetaIndex(ExecutionControlToken ecToken, Script script, object obj, string metaname)
        {
            var u = obj as IUserDataType;

            if (u != null)
            {
                return u.MetaIndex(ecToken, script, metaname);
            }

            return null;
        }


        /// <summary>
        /// Determines whether the specified object is compatible with the specified type.
        /// Unless a very specific behaviour is needed, the correct implementation is a 
        /// simple " return type.IsInstanceOfType(obj); "
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="obj">The object.</param>
        public bool IsTypeCompatible(Type type, object obj)
        {
            return Framework.Do.IsInstanceOfType(type, obj);
        }
    }
}