using System;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Mathematics;

namespace NativeWrapper
{
    [NativeContainer]
    [BurstCompile]
    public struct NativeListWrapper<Type> : IDisposable
    where Type : unmanaged
    {
        public int capacity
        {
            get
            {
                return (2 << powSize);
            }
        }
        private int powSize;
        public int Count;
        private NativeArray<Type> Elements;
        private Allocator allocator;
        public NativeListWrapper(Allocator allocator)
        {
            powSize = 2;
            Count = 0;
            Elements = new NativeArray<Type>((2 << powSize), allocator);
            this.allocator = allocator;
        }

        //Const Time
        public void Insert(int index, Type element)
        {
            while (index >= Elements.Length)
            {
                Resize();
            }
            Elements[index] = element;
            if (index > Count) Count = index + 1;
        }
        //Const Time
        public void QuickInsert(Type element)
        {
            Count++;
            if (Count > capacity)
            {
                Resize();
            }
            Elements[Count - 1] = element;
        }
        // Linear Time
        public bool Contains(Type element)
        {
            for (int i = 0; i < Elements.Length; i++)
            {
                if (element.Equals(Elements[i])) return true;
            }
            return false;
        }
        //Linear Time
        public void Remove(Type element)
        {
            for (int i = 0; i < Elements.Length; i++)
            {
                if (element.Equals(Elements[i]))
                {
                    Elements[i] = default;
                }
            }
        }
        // Const time
        public void QuickRemove(int index)
        {
            Elements[index] = default;
        }
        // const time
        public Type Get(int index)
        {
            return Elements[index];
        }
        //linear time
        public bool TryGet(int index, out Type element)
        {
            element = Elements[index];
            if (Elements[index].Equals(default)) return false;
            return true;
        }
        public void Clear()
        {
            Elements.Dispose();
            Count = 0;
            powSize = 6;
            Elements = new NativeArray<Type>(capacity, allocator);
        }
        public Type[] ToArray()
        {
            return Elements.ToArray();
        }
        private void Resize()
        {
            powSize++;
            var oldArray = Elements.ToArray();
            Elements.Dispose();
            Elements = new NativeArray<Type>(capacity, allocator);
            for (int i = 0; i < oldArray.Length; i++)
            {
                Elements[i] = oldArray[i];
            }
        }
        public void Dispose()
        {
            powSize = 0;
            Count = 0;
            Elements.Dispose();
        }
    }
}