using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace MessagePack.Internal
{
    /// <summary>
    /// Provides optimized generation code and helpers.
    /// </summary>
    internal static class ILGeneratorExtensions
    {
        /// <summary>
        /// Loads the local variable at a specific index onto the evaluation stack.
        /// </summary>
        public static void EmitLdloc(this ILGenerator il, int index)
        {
            switch (index)
            {
                case 0:
                    il.Emit(OpCodes.Ldloc_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldloc_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldloc_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldloc_3);
                    break;
                default:
                    if (index <= 255)
                    {
                        il.Emit(OpCodes.Ldloc_S, (byte)index);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldloc, (short)index);
                    }
                    break;
            }
        }

        public static void EmitLdloc(this ILGenerator il, LocalBuilder local)
        {
            il.Emit(OpCodes.Ldloc, local);
        }

        /// <summary>
        /// Pops the current value from the top of the evaluation stack and stores it in a the local variable list at a specified index.
        /// </summary>
        public static void EmitStloc(this ILGenerator il, int index)
        {
            switch (index)
            {
                case 0:
                    il.Emit(OpCodes.Stloc_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Stloc_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Stloc_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Stloc_3);
                    break;
                default:
                    if (index <= 255)
                    {
                        il.Emit(OpCodes.Stloc_S, (byte)index);
                    }
                    else
                    {
                        il.Emit(OpCodes.Stloc, (short)index);
                    }
                    break;
            }
        }

        public static void EmitStloc(this ILGenerator il, LocalBuilder local)
        {
            il.Emit(OpCodes.Stloc, local);
        }

        /// <summary>
        /// Loads the address of the local variable at a specific index onto the evaluation statck.
        /// </summary>
        public static void EmitLdloca(this ILGenerator il, int index)
        {
            if (index <= 255)
            {
                il.Emit(OpCodes.Ldloca_S, (byte)index);
            }
            else
            {
                il.Emit(OpCodes.Ldloca, (short)index);
            }
        }

        public static void EmitLdloca(this ILGenerator il, LocalBuilder local)
        {
            il.Emit(OpCodes.Ldloca, local);
        }

        /// <summary>
        /// Pushes a supplied value of type int32 onto the evaluation stack as an int32.
        /// </summary>
        public static void EmitLdc_I4(this ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    break;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    if (value >= -128 && value <= 127)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, value);
                    }
                    break;
            }
        }

        public static void EmitLdarg(this ILGenerator il, int index)
        {
            switch (index)
            {
                case 0:
                    il.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    if (index <= 255)
                    {
                        il.Emit(OpCodes.Ldarg_S, (byte)index);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldarg, index);
                    }
                    break;
            }
        }

        public static void EmitLoadThis(this ILGenerator il)
        {
            EmitLdarg(il, 0);
        }

        public static void EmitLdarga(this ILGenerator il, int index)
        {
            if (index <= 255)
            {
                il.Emit(OpCodes.Ldarga_S, (byte)index);
            }
            else
            {
                il.Emit(OpCodes.Ldarga, index);
            }
        }

        public static void EmitLoadArg(this ILGenerator il, TypeInfo info, int index)
        {
            if (info.IsClass)
            {
                EmitLdarg(il, index);
            }
            else
            {
                EmitLdarga(il, index);
            }
        }

        public static void EmitStarg(this ILGenerator il, int index)
        {
            if (index <= 255)
            {
                il.Emit(OpCodes.Starg_S, (byte)index);
            }
            else
            {
                il.Emit(OpCodes.Starg, index);
            }
        }

        /// <summary>
        /// Helper for Pop op.
        /// </summary>
        public static void EmitPop(this ILGenerator il, int count)
        {
            for (int i = 0; i < count; i++)
            {
                il.Emit(OpCodes.Pop);
            }
        }

        public static void EmitCall(this ILGenerator il, MethodInfo methodInfo)
        {
            if (methodInfo.IsFinal || !methodInfo.IsVirtual)
            {
                il.Emit(OpCodes.Call, methodInfo);
            }
            else
            {
                il.Emit(OpCodes.Callvirt, methodInfo);
            }
        }

        public static void EmitLdfld(this ILGenerator il, FieldInfo fieldInfo)
        {
            il.Emit(OpCodes.Ldfld, fieldInfo);
        }

        public static void EmitLdsfld(this ILGenerator il, FieldInfo fieldInfo)
        {
            il.Emit(OpCodes.Ldsfld, fieldInfo);
        }

        public static void EmitRet(this ILGenerator il)
        {
            il.Emit(OpCodes.Ret);
        }

        public static void EmitIntZeroReturn(this ILGenerator il)
        {
            il.EmitLdc_I4(0);
            il.Emit(OpCodes.Ret);
        }

        public static void EmitNullReturn(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);
        }

        public static void EmitThrowNotimplemented(this ILGenerator il)
        {
            il.Emit(OpCodes.Newobj, typeof(System.NotImplementedException).GetTypeInfo().DeclaredConstructors.First(x => x.GetParameters().Length == 0));
            il.Emit(OpCodes.Throw);
        }

        /// <summary>for  var i = 0, i ..., i++ </summary>
        public static void EmitIncrementFor(this ILGenerator il, LocalBuilder conditionGreater, Action<LocalBuilder> emitBody)
        {
            var loopBegin = il.DefineLabel();
            var condtionLabel = il.DefineLabel();

            // var i = 0
            var forI = il.DeclareLocal(typeof(int));
            il.EmitLdc_I4(0);
            il.EmitStloc(forI);
            il.Emit(OpCodes.Br, condtionLabel);

            il.MarkLabel(loopBegin);
            emitBody(forI);

            // i++
            il.EmitLdloc(forI);
            il.EmitLdc_I4(1);
            il.Emit(OpCodes.Add);
            il.EmitStloc(forI);

            //// i < ***
            il.MarkLabel(condtionLabel);
            il.EmitLdloc(forI);
            il.EmitLdloc(conditionGreater);
            il.Emit(OpCodes.Blt, loopBegin);
        }
    }
}