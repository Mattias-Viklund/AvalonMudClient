using System;
using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter.Debugging;

namespace MoonSharp.Interpreter.Execution.VM
{
    // This part is practically written procedural style - it looks more like C than C#.
    // This is intentional so to avoid this-calls and virtual-calls as much as possible.
    // Same reason for the "sealed" declaration.
    internal sealed partial class Processor
    {
        internal bool DebuggerEnabled
        {
            get => _debug.DebuggerEnabled;
            set => _debug.DebuggerEnabled = value;
        }

        internal Instruction FindMeta(ref int baseAddress)
        {
            var meta = _rootChunk.Code[baseAddress];

            // skip nops
            while (meta.OpCode == OpCode.Nop)
            {
                baseAddress++;
                meta = _rootChunk.Code[baseAddress];
            }

            if (meta.OpCode != OpCode.Meta)
            {
                return null;
            }

            return meta;
        }


        internal void AttachDebugger(IDebugger debugger)
        {
            _debug.DebuggerAttached = debugger;
            _debug.LineBasedBreakPoints = (debugger.GetDebuggerCaps() & DebuggerCaps.HasLineBasedBreakpoints) != 0;
            debugger.SetDebugService(new DebugService(_script, this));
        }


        private void ListenDebugger(Instruction instr, int instructionPtr)
        {
            bool isOnDifferentRef = false;

            if (instr.SourceCodeRef != null && _debug.LastHlRef != null)
            {
                if (_debug.LineBasedBreakPoints)
                {
                    isOnDifferentRef = instr.SourceCodeRef.SourceIdx != _debug.LastHlRef.SourceIdx ||
                                       instr.SourceCodeRef.FromLine != _debug.LastHlRef.FromLine;
                }
                else
                {
                    isOnDifferentRef = instr.SourceCodeRef != _debug.LastHlRef;
                }
            }
            else if (_debug.LastHlRef == null)
            {
                isOnDifferentRef = instr.SourceCodeRef != null;
            }


            if (_debug.DebuggerAttached.IsPauseRequested() ||
                (instr.SourceCodeRef != null && instr.SourceCodeRef.Breakpoint && isOnDifferentRef))
            {
                _debug.DebuggerCurrentAction = DebuggerAction.ActionType.None;
                _debug.DebuggerCurrentActionTarget = -1;
            }

            switch (_debug.DebuggerCurrentAction)
            {
                case DebuggerAction.ActionType.Run:
                    if (_debug.LineBasedBreakPoints)
                    {
                        _debug.LastHlRef = instr.SourceCodeRef;
                    }

                    return;
                case DebuggerAction.ActionType.ByteCodeStepOver:
                    if (_debug.DebuggerCurrentActionTarget != instructionPtr)
                    {
                        return;
                    }

                    break;
                case DebuggerAction.ActionType.ByteCodeStepOut:
                case DebuggerAction.ActionType.StepOut:
                    if (_executionStack.Count >= _debug.ExStackDepthAtStep)
                    {
                        return;
                    }

                    break;
                case DebuggerAction.ActionType.StepIn:
                    if ((_executionStack.Count >= _debug.ExStackDepthAtStep) &&
                        (instr.SourceCodeRef == null || instr.SourceCodeRef == _debug.LastHlRef))
                    {
                        return;
                    }

                    break;
                case DebuggerAction.ActionType.StepOver:
                    if (instr.SourceCodeRef == null || instr.SourceCodeRef == _debug.LastHlRef ||
                        _executionStack.Count > _debug.ExStackDepthAtStep)
                    {
                        return;
                    }

                    break;
            }


            this.RefreshDebugger(false, instructionPtr);

            while (true)
            {
                var action = _debug.DebuggerAttached.GetAction(instructionPtr, instr.SourceCodeRef);

                switch (action.Action)
                {
                    case DebuggerAction.ActionType.StepIn:
                    case DebuggerAction.ActionType.StepOver:
                    case DebuggerAction.ActionType.StepOut:
                    case DebuggerAction.ActionType.ByteCodeStepOut:
                        _debug.DebuggerCurrentAction = action.Action;
                        _debug.LastHlRef = instr.SourceCodeRef;
                        _debug.ExStackDepthAtStep = _executionStack.Count;
                        return;
                    case DebuggerAction.ActionType.ByteCodeStepIn:
                        _debug.DebuggerCurrentAction = DebuggerAction.ActionType.ByteCodeStepIn;
                        _debug.DebuggerCurrentActionTarget = -1;
                        return;
                    case DebuggerAction.ActionType.ByteCodeStepOver:
                        _debug.DebuggerCurrentAction = DebuggerAction.ActionType.ByteCodeStepOver;
                        _debug.DebuggerCurrentActionTarget = instructionPtr + 1;
                        return;
                    case DebuggerAction.ActionType.Run:
                        _debug.DebuggerCurrentAction = DebuggerAction.ActionType.Run;
                        _debug.LastHlRef = instr.SourceCodeRef;
                        _debug.DebuggerCurrentActionTarget = -1;
                        return;
                    case DebuggerAction.ActionType.ToggleBreakpoint:
                        this.ToggleBreakPoint(action, null);
                        this.RefreshDebugger(true, instructionPtr);
                        break;
                    case DebuggerAction.ActionType.ResetBreakpoints:
                        this.ResetBreakPoints(action);
                        this.RefreshDebugger(true, instructionPtr);
                        break;
                    case DebuggerAction.ActionType.SetBreakpoint:
                        this.ToggleBreakPoint(action, true);
                        this.RefreshDebugger(true, instructionPtr);
                        break;
                    case DebuggerAction.ActionType.ClearBreakpoint:
                        this.ToggleBreakPoint(action, false);
                        this.RefreshDebugger(true, instructionPtr);
                        break;
                    case DebuggerAction.ActionType.Refresh:
                        this.RefreshDebugger(false, instructionPtr);
                        break;
                    case DebuggerAction.ActionType.HardRefresh:
                        this.RefreshDebugger(true, instructionPtr);
                        break;
                    case DebuggerAction.ActionType.None:
                    default:
                        break;
                }
            }
        }

        private void ResetBreakPoints(DebuggerAction action)
        {
            var src = _script.GetSourceCode(action.SourceID);
            this.ResetBreakPoints(src, new HashSet<int>(action.Lines));
        }

        internal HashSet<int> ResetBreakPoints(SourceCode src, HashSet<int> lines)
        {
            var result = new HashSet<int>();

            foreach (var srf in src.Refs)
            {
                if (srf.CannotBreakpoint)
                {
                    continue;
                }

                srf.Breakpoint = lines.Contains(srf.FromLine);

                if (srf.Breakpoint)
                {
                    result.Add(srf.FromLine);
                }
            }

            return result;
        }

        private bool ToggleBreakPoint(DebuggerAction action, bool? state)
        {
            var src = _script.GetSourceCode(action.SourceID);
            bool found = false;

            foreach (var srf in src.Refs)
            {
                if (srf.CannotBreakpoint)
                {
                    continue;
                }

                if (srf.IncludesLocation(action.SourceID, action.SourceLine, action.SourceCol))
                {
                    found = true;

                    if (state == null)
                    {
                        srf.Breakpoint = !srf.Breakpoint;
                    }
                    else
                    {
                        srf.Breakpoint = state.Value;
                    }

                    if (srf.Breakpoint)
                    {
                        _debug.BreakPoints.Add(srf);
                    }
                    else
                    {
                        _debug.BreakPoints.Remove(srf);
                    }
                }
            }

            if (!found)
            {
                int minDistance = int.MaxValue;
                SourceRef nearest = null;

                foreach (var srf in src.Refs)
                {
                    if (srf.CannotBreakpoint)
                    {
                        continue;
                    }

                    int dist = srf.GetLocationDistance(action.SourceID, action.SourceLine, action.SourceCol);

                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = srf;
                    }
                }

                if (nearest != null)
                {
                    if (state == null)
                    {
                        nearest.Breakpoint = !nearest.Breakpoint;
                    }
                    else
                    {
                        nearest.Breakpoint = state.Value;
                    }

                    if (nearest.Breakpoint)
                    {
                        _debug.BreakPoints.Add(nearest);
                    }
                    else
                    {
                        _debug.BreakPoints.Remove(nearest);
                    }

                    return true;
                }

                return false;
            }

            return true;
        }

        private void RefreshDebugger(bool hard, int instructionPtr)
        {
            var sref = this.GetCurrentSourceRef(instructionPtr);
            var context = new ScriptExecutionContext(ExecutionControlToken.Dummy, this, null, sref);

            var watchList = _debug.DebuggerAttached.GetWatchItems();
            var callStack = this.Debugger_GetCallStack(sref);
            var watches = this.Debugger_RefreshWatches(context, watchList);
            var vstack = this.Debugger_RefreshVStack();
            var locals = this.Debugger_RefreshLocals(context);
            var threads = this.Debugger_RefreshThreads(context);

            _debug.DebuggerAttached.Update(WatchType.CallStack, callStack);
            _debug.DebuggerAttached.Update(WatchType.Watches, watches);
            _debug.DebuggerAttached.Update(WatchType.VStack, vstack);
            _debug.DebuggerAttached.Update(WatchType.Locals, locals);
            _debug.DebuggerAttached.Update(WatchType.Threads, threads);

            if (hard)
            {
                _debug.DebuggerAttached.RefreshBreakpoints(_debug.BreakPoints);
            }
        }

        private List<WatchItem> Debugger_RefreshThreads(ScriptExecutionContext context)
        {
            var coroutinesStack = _parent != null ? _parent._coroutinesStack : _coroutinesStack;

            return coroutinesStack.Select(c => new WatchItem
            {
                Address = c.AssociatedCoroutine.ReferenceID,
                Name = "coroutine #" + c.AssociatedCoroutine.ReferenceID
            }).ToList();
        }

        private List<WatchItem> Debugger_RefreshVStack()
        {
            var lwi = new List<WatchItem>();
            for (int i = 0; i < Math.Min(32, _valueStack.Count); i++)
            {
                lwi.Add(new WatchItem
                {
                    Address = i,
                    Value = _valueStack.Peek(i)
                });
            }

            return lwi;
        }

        private List<WatchItem> Debugger_RefreshWatches(ScriptExecutionContext context,
            List<DynamicExpression> watchList)
        {
            return watchList.Select(w => this.Debugger_RefreshWatch(context, w)).ToList();
        }

        private List<WatchItem> Debugger_RefreshLocals(ScriptExecutionContext context)
        {
            var locals = new List<WatchItem>();
            var top = _executionStack.Peek();

            if (top != null && top.Debug_Symbols != null && top.LocalScope != null)
            {
                int len = Math.Min(top.Debug_Symbols.Length, top.LocalScope.Length);

                for (int i = 0; i < len; i++)
                {
                    locals.Add(new WatchItem
                    {
                        IsError = false,
                        LValue = top.Debug_Symbols[i],
                        Value = top.LocalScope[i],
                        Name = top.Debug_Symbols[i]._name
                    });
                }
            }

            return locals;
        }

        private WatchItem Debugger_RefreshWatch(ScriptExecutionContext context, DynamicExpression dynExpr)
        {
            try
            {
                var L = dynExpr.FindSymbol(context);
                var v = dynExpr.Evaluate(context);

                return new WatchItem
                {
                    IsError = dynExpr.IsConstant(),
                    LValue = L,
                    Value = v,
                    Name = dynExpr.ExpressionCode
                };
            }
            catch (Exception ex)
            {
                return new WatchItem
                {
                    IsError = true,
                    Value = DynValue.NewString(ex.Message),
                    Name = dynExpr.ExpressionCode
                };
            }
        }

        internal List<WatchItem> Debugger_GetCallStack(SourceRef startingRef)
        {
            var wis = new List<WatchItem>();

            for (int i = 0; i < _executionStack.Count; i++)
            {
                var c = _executionStack.Peek(i);

                var I = _rootChunk.Code[c.Debug_EntryPoint];

                string callname = I.OpCode == OpCode.Meta ? I.Name : null;

                if (c.ClrFunction != null)
                {
                    wis.Add(new WatchItem
                    {
                        Address = -1,
                        BasePtr = -1,
                        RetAddress = c.ReturnAddress,
                        Location = startingRef,
                        Name = c.ClrFunction.Name
                    });
                }
                else
                {
                    wis.Add(new WatchItem
                    {
                        Address = c.Debug_EntryPoint,
                        BasePtr = c.BasePointer,
                        RetAddress = c.ReturnAddress,
                        Name = callname,
                        Location = startingRef
                    });
                }

                startingRef = c.CallingSourceRef;

                if (c.Continuation != null)
                {
                    wis.Add(new WatchItem
                    {
                        Name = c.Continuation.Name,
                        Location = SourceRef.GetClrLocation()
                    });
                }
            }

            return wis;
        }
    }
}