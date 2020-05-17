using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Svelto.Common
{
#if !UNITY_COLLECTIONS
    public enum Allocator
    {
        Invalid ,
        None,
        Temp,
        TempJob,
        Persistent
    }
#else    
    public enum Allocator
    {
        /// <summary>
        ///   <para>Invalid allocation.</para>
        /// </summary>
        Invalid = Unity.Collections.Allocator.Invalid,
        /// <summary>
        ///   <para>No allocation.</para>
        /// </summary>
        None = Unity.Collections.Allocator.None,
        /// <summary>
        ///   <para>Temporary allocation.</para>
        /// </summary>
        Temp = Unity.Collections.Allocator.Temp,
        /// <summary>
        ///   <para>Temporary job allocation.</para>
        /// </summary>
        TempJob = Unity.Collections.Allocator.TempJob,
        /// <summary>
        ///   <para>Persistent allocation.</para>
        /// </summary>
        Persistent = Unity.Collections.Allocator.Persistent
    }
#endif

    public static class MemoryUtilities
    {    
#if UNITY_EDITOR && !UNITY_COLLECTIONS        
        static MemoryUtilities()
        {
            #warning Svelto.Common is depending on the Unity Collection package. Alternatively you can import System.Runtime.CompilerServices.Unsafe.dll
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(IntPtr ptr, Allocator allocator)
        {
            unsafe
            {
#if UNITY_COLLECTIONS
                Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Free((void*) ptr, (Unity.Collections.Allocator) allocator);
#else
                System.Runtime.InteropServices.Marshal.FreeHGlobal((IntPtr) ptr);
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr Alloc(uint newCapacity, Allocator allocator)
        {
            unsafe
            {
#if UNITY_COLLECTIONS
                var newPointer =
                    Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Malloc(newCapacity, (int) OptimalAlignment.alignment, (Unity.Collections.Allocator) allocator);
#else
                var newPointer = System.Runtime.InteropServices.Marshal.AllocHGlobal((int) newCapacity);
#endif
                return (IntPtr) newPointer;
            }
        }
        
        public static void Realloc(ref IntPtr realBuffer, uint oldSize , uint newSize, Allocator allocator)
        {
            unsafe
            {
#if DEBUG && !PROFILE_SVELTO            
                if (newSize <= 0)
                    throw new Exception("new size must be greater than 0");
                if (newSize <= oldSize)
                    throw new Exception("new size must be greater than oldsize");
#endif                
#if UNITY_COLLECTIONS
                IntPtr newPointer =
                    (IntPtr)Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Malloc((long) newSize  , (int) OptimalAlignment.alignment, (Unity.Collections.Allocator) allocator);
                Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpy((void*) newPointer, (void*) realBuffer, oldSize);
                Free(realBuffer, allocator);
#else
                var newPointer = System.Runtime.InteropServices.Marshal.ReAllocHGlobal(realBuffer, (IntPtr) (newSize));
#endif
                realBuffer = newPointer;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MemClear(IntPtr listData, uint sizeOf)
        {
            unsafe 
            {
#if UNITY_COLLECTIONS
                Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemClear((void*) listData, sizeOf);
#else
               Unsafe.InitBlock((void*) listData, 0, sizeOf);
#endif
            }
        }

        static class OptimalAlignment
        {
            internal static readonly uint alignment;

            static OptimalAlignment()
            {
                alignment = (uint) (Environment.Is64BitProcess ? 16 : 8);
            }
        }

        static class CachedSize<T> where T : struct
        {
            public static readonly uint cachedSize = (uint) Unsafe.SizeOf<T>();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //THIS MUST STAY INT. THE REASON WHY EVERYTHING IS INT AND NOT UINT IS BECAUSE YOU CAN END UP
        //DOING SUBTRACT OPERATION EXPECTING TO BE < 0 AND THEY WON'T BE
        public static int SizeOf<T>() where T : struct
        {
            return (int) CachedSize<T>.cachedSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyStructureToPtr<T>(ref T buffer, IntPtr bufferPtr) where T : struct
        {
            unsafe 
            {
                Unsafe.Write((void*) bufferPtr, buffer);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ArrayElementAsRef<T>(IntPtr data, int threadIndex) where T : struct
        {
            unsafe
            {
                return ref Unsafe.AsRef<T>(Unsafe.Add<T>((void*) data, threadIndex));
            }
        }

        public static int GetFieldOffset(FieldInfo field)
        {
#if UNITY_COLLECTIONS
            return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.GetFieldOffset(field);
#else
            int GetFieldOffset(RuntimeFieldHandle h) => 
                System.Runtime.InteropServices.Marshal.ReadInt32(h.Value + (4 + IntPtr.Size)) & 0xFFFFFF;

            return GetFieldOffset(field.FieldHandle);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Align4(uint input) { return (uint) (Math.Ceiling(input / 4.0) * 4); }
    }
}