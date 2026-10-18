/*
 * Tencent is pleased to support the open source community by making InjectFix available.
 * Copyright (C) 2019 Tencent.  All rights reserved.
 * InjectFix is licensed under the MIT License, except for the third-party components listed in the file 'LICENSE' which may be subject to their corresponding license terms.
 * This file is subject to the terms and conditions defined in file 'LICENSE', which is part of this source code package.
 */

using System;
using System.Reflection;

//ReflectionMethodInvoker allocates an argument array, boxes, and calls MethodInfo.Invoke on every call, which is expensive.
//For ordinary methods with at most 4 arguments (including this), build a strongly typed call with generics + Delegate.CreateDelegate instead of reflection.
//Unsupported cases (constructors, ref/out, value type instance methods, etc.) return null and still use ReflectionMethodInvoker.
//Under IL2CPP, runtime MakeGenericType relies on full generic sharing (Unity 2022.1+).

namespace IFix.Core
{
    unsafe delegate T StackReader<T>(VirtualMachine virtualMachine, Value* evaluationStackBase, Value* ptr,
        object[] managedStack);

    unsafe delegate void StackWriter<T>(Value* evaluationStackBase, Value* ptr, object[] managedStack, T value);

    //Typed reads/writes of the VM stack; common primitive types are not boxed
    unsafe internal static class StackAccessor<T>
    {
        public static readonly StackReader<T> Read;

        public static readonly StackWriter<T> Write;

        static StackAccessor()
        {
            var type = typeof(T);
            if (type == typeof(int))
            {
                Read = (StackReader<T>)(object)new StackReader<int>(StackAccessor.ReadInt32);
                Write = (StackWriter<T>)(object)new StackWriter<int>(StackAccessor.WriteInt32);
            }
            else if (type == typeof(bool))
            {
                Read = (StackReader<T>)(object)new StackReader<bool>(StackAccessor.ReadBoolean);
                Write = (StackWriter<T>)(object)new StackWriter<bool>(StackAccessor.WriteBoolean);
            }
            else if (type == typeof(float))
            {
                Read = (StackReader<T>)(object)new StackReader<float>(StackAccessor.ReadSingle);
                Write = (StackWriter<T>)(object)new StackWriter<float>(StackAccessor.WriteSingle);
            }
            else if (type == typeof(long))
            {
                Read = (StackReader<T>)(object)new StackReader<long>(StackAccessor.ReadInt64);
                Write = (StackWriter<T>)(object)new StackWriter<long>(StackAccessor.WriteInt64);
            }
            else if (type == typeof(double))
            {
                Read = (StackReader<T>)(object)new StackReader<double>(StackAccessor.ReadDouble);
                Write = (StackWriter<T>)(object)new StackWriter<double>(StackAccessor.WriteDouble);
            }
            else
            {
                Read = StackAccessor.ReadBoxed<T>;
                if (type.IsValueType)
                {
                    Write = StackAccessor.WriteBoxed<T>;
                }
                else
                {
                    Write = StackAccessor.WriteReference<T>;
                }
            }
        }
    }

    unsafe internal static class StackAccessor
    {
        //On a type mismatch (e.g. references, enums) use ToObject, same as ReflectionMethodInvoker
        internal static T ReadBoxed<T>(VirtualMachine virtualMachine, Value* evaluationStackBase, Value* ptr,
            object[] managedStack)
        {
            object obj = ptr->Type == ValueType.Object ? managedStack[ptr->Value1]
                : EvaluationStackOperation.ToObject(evaluationStackBase, ptr, managedStack, typeof(T),
                virtualMachine, false);
            //Same as MethodInfo.Invoke: null for a value type argument means the default value
            return obj == null ? default(T) : (T)obj;
        }

        internal static void WriteBoxed<T>(Value* evaluationStackBase, Value* ptr, object[] managedStack, T value)
        {
            EvaluationStackOperation.PushObject(evaluationStackBase, ptr, managedStack, value, typeof(T));
        }

        internal static void WriteReference<T>(Value* evaluationStackBase, Value* ptr, object[] managedStack,
            T value)
        {
            int pos = (int)(ptr - evaluationStackBase);
            ptr->Type = ValueType.Object;
            ptr->Value1 = pos;
            managedStack[pos] = value;
        }

        internal static int ReadInt32(VirtualMachine virtualMachine, Value* evaluationStackBase, Value* ptr,
            object[] managedStack)
        {
            if (ptr->Type == ValueType.Integer)
            {
                return ptr->Value1;
            }
            return ReadBoxed<int>(virtualMachine, evaluationStackBase, ptr, managedStack);
        }

        internal static void WriteInt32(Value* evaluationStackBase, Value* ptr, object[] managedStack, int value)
        {
            ptr->Type = ValueType.Integer;
            ptr->Value1 = value;
        }

        internal static bool ReadBoolean(VirtualMachine virtualMachine, Value* evaluationStackBase, Value* ptr,
            object[] managedStack)
        {
            if (ptr->Type == ValueType.Integer)
            {
                return ptr->Value1 == 1;
            }
            return ReadBoxed<bool>(virtualMachine, evaluationStackBase, ptr, managedStack);
        }

        internal static void WriteBoolean(Value* evaluationStackBase, Value* ptr, object[] managedStack, bool value)
        {
            ptr->Type = ValueType.Integer;
            ptr->Value1 = value ? 1 : 0;
        }

        internal static float ReadSingle(VirtualMachine virtualMachine, Value* evaluationStackBase, Value* ptr,
            object[] managedStack)
        {
            if (ptr->Type == ValueType.Float)
            {
                return *(float*)&ptr->Value1;
            }
            return ReadBoxed<float>(virtualMachine, evaluationStackBase, ptr, managedStack);
        }

        internal static void WriteSingle(Value* evaluationStackBase, Value* ptr, object[] managedStack, float value)
        {
            ptr->Type = ValueType.Float;
            *(float*)&ptr->Value1 = value;
        }

        internal static long ReadInt64(VirtualMachine virtualMachine, Value* evaluationStackBase, Value* ptr,
            object[] managedStack)
        {
            if (ptr->Type == ValueType.Long)
            {
                return *(long*)&ptr->Value1;
            }
            return ReadBoxed<long>(virtualMachine, evaluationStackBase, ptr, managedStack);
        }

        internal static void WriteInt64(Value* evaluationStackBase, Value* ptr, object[] managedStack, long value)
        {
            ptr->Type = ValueType.Long;
            *(long*)&ptr->Value1 = value;
        }

        internal static double ReadDouble(VirtualMachine virtualMachine, Value* evaluationStackBase, Value* ptr,
            object[] managedStack)
        {
            if (ptr->Type == ValueType.Double)
            {
                return *(double*)&ptr->Value1;
            }
            return ReadBoxed<double>(virtualMachine, evaluationStackBase, ptr, managedStack);
        }

        internal static void WriteDouble(Value* evaluationStackBase, Value* ptr, object[] managedStack,
            double value)
        {
            ptr->Type = ValueType.Double;
            *(double*)&ptr->Value1 = value;
        }
    }

    unsafe internal abstract class GenericMethodInvoker
    {
        MethodBase method;

        bool hasThis;

        bool hasReturn;

        int argCount; //including this

        static readonly Type[] actionTypes = new Type[]
        {
            typeof(Action), typeof(Action<>), typeof(Action<,>), typeof(Action<,,>), typeof(Action<,,,>)
        };

        static readonly Type[] funcTypes = new Type[]
        {
            typeof(Func<>), typeof(Func<,>), typeof(Func<,,>), typeof(Func<,,,>), typeof(Func<,,,,>)
        };

        static readonly Type[] actionInvokerTypes = new Type[]
        {
            typeof(ActionInvoker), typeof(ActionInvoker<>), typeof(ActionInvoker<,>), typeof(ActionInvoker<,,>),
            typeof(ActionInvoker<,,,>)
        };

        static readonly Type[] funcInvokerTypes = new Type[]
        {
            typeof(FuncInvoker<>), typeof(FuncInvoker<,>), typeof(FuncInvoker<,,>), typeof(FuncInvoker<,,,>),
            typeof(FuncInvoker<,,,,>)
        };

        //Prevent unity il2cpp code stripping
        static void PreventStripping(object obj)
        {
            if (obj != null)
            {
                new ActionInvoker();
                new ActionInvoker<object>();
                new ActionInvoker<object, object>();
                new ActionInvoker<object, object, object>();
                new ActionInvoker<object, object, object, object>();
                new FuncInvoker<object>();
                new FuncInvoker<object, object>();
                new FuncInvoker<object, object, object>();
                new FuncInvoker<object, object, object, object>();
                new FuncInvoker<object, object, object, object, object>();
            }
        }

        static bool isSupportedType(Type type)
        {
            if (type.IsByRef || type.IsPointer || type.ContainsGenericParameters)
            {
                return false;
            }
            //ref structs (Span etc.) can't be generic arguments
            if (type.IsValueType)
            {
                foreach (var attr in type.GetCustomAttributes(false))
                {
                    if (attr.GetType().FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute")
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        //Only used by probe(), so no generic instantiation over it exists ahead of time
        struct ProbeValue
        {
            public int Value;
        }

        static ProbeValue probeAdd(ProbeValue a, int b)
        {
            a.Value += b;
            return a;
        }

        //0: not probed yet, 1: supported, -1: not supported
        static int supportState = 0;

        //Without IL2CPP full generic sharing (before Unity 2022), invoker types made with MakeGenericType over value
        //types have no AOT code and fail when called, not when created. So build and call one over a value type
        //that no code instantiates statically; if that fails, never use this path.
        static bool probe()
        {
            try
            {
                var invoker = create(new Func<ProbeValue, int, ProbeValue>(probeAdd).Method);
                if (invoker == null)
                {
                    return false;
                }
                Value* stack = stackalloc Value[2];
                var managedStack = new object[2];
                stack[0].Type = ValueType.ValueType;
                stack[0].Value1 = 0;
                managedStack[0] = new ProbeValue { Value = 40 };
                stack[1].Type = ValueType.Integer;
                stack[1].Value1 = 2;
                var call = new Call()
                {
                    argumentBase = stack,
                    currentTop = stack,
                    evaluationStackBase = stack,
                    managedStack = managedStack
                };
                invoker(null, ref call, false);
                return call.currentTop == stack + 1 && stack[0].Type == ValueType.ValueType
                    && managedStack[0] is ProbeValue && ((ProbeValue)managedStack[0]).Value == 42;
            }
            catch
            {
                return false;
            }
        }

        //Returns null if unsupported
        internal static ExternInvoker Create(MethodBase method)
        {
            if (supportState == 0)
            {
                supportState = probe() ? 1 : -1;
            }
            return supportState > 0 ? create(method) : null;
        }

        static ExternInvoker create(MethodBase method)
        {
            var methodInfo = method as MethodInfo;
            if (methodInfo == null || methodInfo.ContainsGenericParameters)
            {
                return null;
            }
            //this of a value type instance method is a reference; this also excludes Nullable
            if (!method.IsStatic && method.DeclaringType.IsValueType)
            {
                return null;
            }
            var parameters = method.GetParameters();
            int argCount = parameters.Length + (method.IsStatic ? 0 : 1);
            if (argCount > 4)
            {
                return null;
            }
            bool hasReturn = methodInfo.ReturnType != typeof(void);
            var typeArgs = new Type[argCount + (hasReturn ? 1 : 0)];
            int idx = 0;
            if (!method.IsStatic)
            {
                typeArgs[idx++] = method.DeclaringType;
            }
            foreach (var parameter in parameters)
            {
                typeArgs[idx++] = parameter.ParameterType;
            }
            if (hasReturn)
            {
                typeArgs[idx] = methodInfo.ReturnType;
            }
            foreach (var type in typeArgs)
            {
                if (!isSupportedType(type))
                {
                    return null;
                }
            }

            try
            {
                PreventStripping(null);
                Type delegateType, invokerType;
                if (typeArgs.Length == 0)
                {
                    delegateType = actionTypes[0];
                    invokerType = actionInvokerTypes[0];
                }
                else if (hasReturn)
                {
                    delegateType = funcTypes[argCount].MakeGenericType(typeArgs);
                    invokerType = funcInvokerTypes[argCount].MakeGenericType(typeArgs);
                }
                else
                {
                    delegateType = actionTypes[argCount].MakeGenericType(typeArgs);
                    invokerType = actionInvokerTypes[argCount].MakeGenericType(typeArgs);
                }
                //Instance methods get an open instance delegate; virtual methods are still dispatched virtually, same as MethodInfo.Invoke
                var del = Delegate.CreateDelegate(delegateType, methodInfo, false);
                if (del == null)
                {
                    return null;
                }
                var invoker = (GenericMethodInvoker)Activator.CreateInstance(invokerType);
                invoker.method = method;
                invoker.hasThis = !method.IsStatic;
                invoker.hasReturn = hasReturn;
                invoker.argCount = argCount;
                invoker.SetDelegate(del);
                return invoker.Invoke;
            }
            catch
            {
                return null;
            }
        }

        protected abstract void SetDelegate(Delegate del);

        //Read the arguments, call, and write the return value to argumentBase
        protected abstract void Execute(VirtualMachine virtualMachine, Value* evaluationStackBase,
            Value* argumentBase, object[] managedStack);

        public void Invoke(VirtualMachine virtualMachine, ref Call call, bool isInstantiate)
        {
            var managedStack = call.managedStack;
            var evaluationStackBase = call.evaluationStackBase;
            var argumentBase = call.argumentBase;
            var pushResult = false;
            try
            {
                if (hasThis)
                {
                    object instance = argumentBase->Type == ValueType.Object ? managedStack[argumentBase->Value1]
                        : EvaluationStackOperation.ToObject(evaluationStackBase, argumentBase, managedStack,
                        method.DeclaringType, virtualMachine, false);
                    if (instance == null)
                    {
                        throw new NullReferenceException(string.Format("can not invoke method [{0}.{1}], Non-static method require instance but got null.", method.DeclaringType, method.Name));
                    }
                }
                Execute(virtualMachine, evaluationStackBase, argumentBase, managedStack);
                if (hasReturn)
                {
                    call.currentTop = argumentBase + 1;
                    pushResult = true;
                }
            }
            finally
            {
                for (int i = (pushResult ? 1 : 0); i < argCount; i++)
                {
                    managedStack[argumentBase + i - evaluationStackBase] = null;
                }
            }
        }
    }

    unsafe internal class ActionInvoker : GenericMethodInvoker
    {
        Action action;

        protected override void SetDelegate(Delegate del)
        {
            action = (Action)del;
        }

        protected override void Execute(VirtualMachine virtualMachine, Value* evaluationStackBase,
            Value* argumentBase, object[] managedStack)
        {
            action();
        }
    }

    unsafe internal class ActionInvoker<T1> : GenericMethodInvoker
    {
        Action<T1> action;

        protected override void SetDelegate(Delegate del)
        {
            action = (Action<T1>)del;
        }

        protected override void Execute(VirtualMachine virtualMachine, Value* evaluationStackBase,
            Value* argumentBase, object[] managedStack)
        {
            action(StackAccessor<T1>.Read(virtualMachine, evaluationStackBase, argumentBase, managedStack));
        }
    }

    unsafe internal class ActionInvoker<T1, T2> : GenericMethodInvoker
    {
        Action<T1, T2> action;

        protected override void SetDelegate(Delegate del)
        {
            action = (Action<T1, T2>)del;
        }

        protected override void Execute(VirtualMachine virtualMachine, Value* evaluationStackBase,
            Value* argumentBase, object[] managedStack)
        {
            action(StackAccessor<T1>.Read(virtualMachine, evaluationStackBase, argumentBase, managedStack),
                StackAccessor<T2>.Read(virtualMachine, evaluationStackBase, argumentBase + 1, managedStack));
        }
    }

    unsafe internal class ActionInvoker<T1, T2, T3> : GenericMethodInvoker
    {
        Action<T1, T2, T3> action;

        protected override void SetDelegate(Delegate del)
        {
            action = (Action<T1, T2, T3>)del;
        }

        protected override void Execute(VirtualMachine virtualMachine, Value* evaluationStackBase,
            Value* argumentBase, object[] managedStack)
        {
            action(StackAccessor<T1>.Read(virtualMachine, evaluationStackBase, argumentBase, managedStack),
                StackAccessor<T2>.Read(virtualMachine, evaluationStackBase, argumentBase + 1, managedStack),
                StackAccessor<T3>.Read(virtualMachine, evaluationStackBase, argumentBase + 2, managedStack));
        }
    }

    unsafe internal class ActionInvoker<T1, T2, T3, T4> : GenericMethodInvoker
    {
        Action<T1, T2, T3, T4> action;

        protected override void SetDelegate(Delegate del)
        {
            action = (Action<T1, T2, T3, T4>)del;
        }

        protected override void Execute(VirtualMachine virtualMachine, Value* evaluationStackBase,
            Value* argumentBase, object[] managedStack)
        {
            action(StackAccessor<T1>.Read(virtualMachine, evaluationStackBase, argumentBase, managedStack),
                StackAccessor<T2>.Read(virtualMachine, evaluationStackBase, argumentBase + 1, managedStack),
                StackAccessor<T3>.Read(virtualMachine, evaluationStackBase, argumentBase + 2, managedStack),
                StackAccessor<T4>.Read(virtualMachine, evaluationStackBase, argumentBase + 3, managedStack));
        }
    }

    unsafe internal class FuncInvoker<TResult> : GenericMethodInvoker
    {
        Func<TResult> func;

        protected override void SetDelegate(Delegate del)
        {
            func = (Func<TResult>)del;
        }

        protected override void Execute(VirtualMachine virtualMachine, Value* evaluationStackBase,
            Value* argumentBase, object[] managedStack)
        {
            var ret = func();
            StackAccessor<TResult>.Write(evaluationStackBase, argumentBase, managedStack, ret);
        }
    }

    unsafe internal class FuncInvoker<T1, TResult> : GenericMethodInvoker
    {
        Func<T1, TResult> func;

        protected override void SetDelegate(Delegate del)
        {
            func = (Func<T1, TResult>)del;
        }

        protected override void Execute(VirtualMachine virtualMachine, Value* evaluationStackBase,
            Value* argumentBase, object[] managedStack)
        {
            var ret = func(StackAccessor<T1>.Read(virtualMachine, evaluationStackBase, argumentBase, managedStack));
            StackAccessor<TResult>.Write(evaluationStackBase, argumentBase, managedStack, ret);
        }
    }

    unsafe internal class FuncInvoker<T1, T2, TResult> : GenericMethodInvoker
    {
        Func<T1, T2, TResult> func;

        protected override void SetDelegate(Delegate del)
        {
            func = (Func<T1, T2, TResult>)del;
        }

        protected override void Execute(VirtualMachine virtualMachine, Value* evaluationStackBase,
            Value* argumentBase, object[] managedStack)
        {
            var ret = func(StackAccessor<T1>.Read(virtualMachine, evaluationStackBase, argumentBase, managedStack),
                StackAccessor<T2>.Read(virtualMachine, evaluationStackBase, argumentBase + 1, managedStack));
            StackAccessor<TResult>.Write(evaluationStackBase, argumentBase, managedStack, ret);
        }
    }

    unsafe internal class FuncInvoker<T1, T2, T3, TResult> : GenericMethodInvoker
    {
        Func<T1, T2, T3, TResult> func;

        protected override void SetDelegate(Delegate del)
        {
            func = (Func<T1, T2, T3, TResult>)del;
        }

        protected override void Execute(VirtualMachine virtualMachine, Value* evaluationStackBase,
            Value* argumentBase, object[] managedStack)
        {
            var ret = func(StackAccessor<T1>.Read(virtualMachine, evaluationStackBase, argumentBase, managedStack),
                StackAccessor<T2>.Read(virtualMachine, evaluationStackBase, argumentBase + 1, managedStack),
                StackAccessor<T3>.Read(virtualMachine, evaluationStackBase, argumentBase + 2, managedStack));
            StackAccessor<TResult>.Write(evaluationStackBase, argumentBase, managedStack, ret);
        }
    }

    unsafe internal class FuncInvoker<T1, T2, T3, T4, TResult> : GenericMethodInvoker
    {
        Func<T1, T2, T3, T4, TResult> func;

        protected override void SetDelegate(Delegate del)
        {
            func = (Func<T1, T2, T3, T4, TResult>)del;
        }

        protected override void Execute(VirtualMachine virtualMachine, Value* evaluationStackBase,
            Value* argumentBase, object[] managedStack)
        {
            var ret = func(StackAccessor<T1>.Read(virtualMachine, evaluationStackBase, argumentBase, managedStack),
                StackAccessor<T2>.Read(virtualMachine, evaluationStackBase, argumentBase + 1, managedStack),
                StackAccessor<T3>.Read(virtualMachine, evaluationStackBase, argumentBase + 2, managedStack),
                StackAccessor<T4>.Read(virtualMachine, evaluationStackBase, argumentBase + 3, managedStack));
            StackAccessor<TResult>.Write(evaluationStackBase, argumentBase, managedStack, ret);
        }
    }
}
