using Unity.Collections;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using System;

namespace VoxelSystem
{
    public unsafe struct Masks
    {
        [NoAlias] public bool* xNegativeMask;
        [NoAlias] public bool* xPositiveMask;
        [NoAlias] public bool* yNegativeMask;
        [NoAlias] public bool* yPositiveMask;
        [NoAlias] public bool* zNegativeMask;
        [NoAlias] public bool* zPositiveMask;
        
        public Masks(int size, Allocator allocator, bool clearMemory, int callstacksToSkip)
        {
            xNegativeMask = (bool*)UnsafeUtility.MallocTracked(size, 4, Allocator.Temp, 0);
            xPositiveMask = (bool*)UnsafeUtility.MallocTracked(size, 4, Allocator.Temp, 0);
            yNegativeMask = (bool*)UnsafeUtility.MallocTracked(size, 4, Allocator.Temp, 0);
            yPositiveMask = (bool*)UnsafeUtility.MallocTracked(size, 4, Allocator.Temp, 0);
            zNegativeMask = (bool*)UnsafeUtility.MallocTracked(size, 4, Allocator.Temp, 0);
            zPositiveMask = (bool*)UnsafeUtility.MallocTracked(size, 4, Allocator.Temp, 0);

            if(clearMemory)
            {
                UnsafeUtility.MemClear(xNegativeMask, size);
                UnsafeUtility.MemClear(xPositiveMask, size);
                UnsafeUtility.MemClear(yNegativeMask, size);
                UnsafeUtility.MemClear(yPositiveMask, size);
                UnsafeUtility.MemClear(zNegativeMask, size);
                UnsafeUtility.MemClear(zPositiveMask, size);
            }
        }

        public bool* GetMask(Face direction)
        {
            switch(direction)
            {
                case Face.XNegative:
                    return xNegativeMask;
                case Face.XPositive:
                    return xPositiveMask;
                case Face.YNegative:
                    return yNegativeMask;
                case Face.YPositive:
                    return yPositiveMask;
                case Face.ZNegative:
                    return zNegativeMask;
                case Face.ZPositive:
                    return zPositiveMask;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}