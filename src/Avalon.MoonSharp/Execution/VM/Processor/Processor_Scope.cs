using System;

namespace MoonSharp.Interpreter.Execution.VM
{
    internal sealed partial class Processor
    {
        private void ClearBlockData(Instruction I)
        {
            int from = I.NumVal;
            int to = I.NumVal2;

            var array = _executionStack.Peek().LocalScope;

            if (to >= 0 && from >= 0 && to >= from)
            {
                Array.Clear(array, from, to - from + 1);
            }
        }


        public DynValue GetGenericSymbol(SymbolRef symref)
        {
            switch (symref._type)
            {
                case SymbolRefType.DefaultEnv:
                    return DynValue.NewTable(this.GetScript().Globals);
                case SymbolRefType.Global:
                    return this.GetGlobalSymbol(this.GetGenericSymbol(symref._env), symref._name);
                case SymbolRefType.Local:
                    return this.GetTopNonClrFunction().LocalScope[symref._index];
                case SymbolRefType.Upvalue:
                    return this.GetTopNonClrFunction().ClosureScope[symref._index];
                default:
                    throw new InternalErrorException("Unexpected {0} LRef at resolution: {1}", symref._type,
                        symref._name);
            }
        }

        private DynValue GetGlobalSymbol(DynValue dynValue, string name)
        {
            if (dynValue.Type != DataType.Table)
            {
                throw new InvalidOperationException($"_ENV is not a table but a {dynValue.Type}");
            }

            return dynValue.Table.Get(name);
        }

        private void SetGlobalSymbol(DynValue dynValue, string name, DynValue value)
        {
            if (dynValue.Type != DataType.Table)
            {
                throw new InvalidOperationException($"_ENV is not a table but a {dynValue.Type}");
            }

            dynValue.Table.Set(name, value ?? DynValue.Nil);
        }


        public void AssignGenericSymbol(SymbolRef symref, DynValue value)
        {
            switch (symref._type)
            {
                case SymbolRefType.Global:
                    this.SetGlobalSymbol(this.GetGenericSymbol(symref._env), symref._name, value);
                    break;
                case SymbolRefType.Local:
                {
                    var stackframe = this.GetTopNonClrFunction();

                    var v = stackframe.LocalScope[symref._index];
                    if (v == null)
                    {
                        stackframe.LocalScope[symref._index] = v = DynValue.NewNil();
                    }

                    v.Assign(value);
                }
                    break;
                case SymbolRefType.Upvalue:
                {
                    var stackframe = this.GetTopNonClrFunction();

                    var v = stackframe.ClosureScope[symref._index];
                    if (v == null)
                    {
                        stackframe.ClosureScope[symref._index] = v = DynValue.NewNil();
                    }

                    v.Assign(value);
                }
                    break;
                case SymbolRefType.DefaultEnv:
                {
                    throw new ArgumentException("Can't AssignGenericSymbol on a DefaultEnv symbol");
                }
                default:
                    throw new InternalErrorException("Unexpected {0} LRef at resolution: {1}", symref._type,
                        symref._name);
            }
        }

        private CallStackItem GetTopNonClrFunction()
        {
            CallStackItem stackframe = null;

            for (int i = 0; i < _executionStack.Count; i++)
            {
                stackframe = _executionStack.Peek(i);

                if (stackframe.ClrFunction == null)
                {
                    break;
                }
            }

            return stackframe;
        }


        public SymbolRef FindSymbolByName(string name)
        {
            if (_executionStack.Count > 0)
            {
                var stackframe = this.GetTopNonClrFunction();

                if (stackframe != null)
                {
                    if (stackframe.Debug_Symbols != null)
                    {
                        for (int i = stackframe.Debug_Symbols.Length - 1; i >= 0; i--)
                        {
                            var l = stackframe.Debug_Symbols[i];

                            if (l._name == name && stackframe.LocalScope[i] != null)
                            {
                                return l;
                            }
                        }
                    }

                    var closure = stackframe.ClosureScope;

                    if (closure != null)
                    {
                        for (int i = 0; i < closure.Symbols.Length; i++)
                        {
                            if (closure.Symbols[i] == name)
                            {
                                return SymbolRef.Upvalue(name, i);
                            }
                        }
                    }
                }
            }

            if (name != WellKnownSymbols.ENV)
            {
                var env = this.FindSymbolByName(WellKnownSymbols.ENV);
                return SymbolRef.Global(name, env);
            }

            return SymbolRef.DefaultEnv;
        }
    }
}