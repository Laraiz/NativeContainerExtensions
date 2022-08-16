using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Mathematics;

namespace NativeWrapper
{
    [NativeContainer]
    [BurstCompile]
    public struct NativeHashTableWrapper<Key, Value> : IDisposable
    where Key : unmanaged
    where Value : unmanaged
    {
        public int capacity
        {
            get
            {
                return (2 << powSize);
            }
        }
        public int Count;
        private NativeArray<Element> Data;
        private Allocator allocator;
        private const int GoldenInt = 1327217885;
        private const int AlicePrime = 63;
        private int powSize;
        private float sizeRatio;

        #region  API

        public NativeHashTableWrapper(Allocator allocator, float sizeRatio = .75f)
        {
            //Debug.Log("Constructing");

            this.allocator = allocator;
            Count = 0;
            powSize = 4;
            this.sizeRatio = sizeRatio;
            Data = new NativeArray<Element>((2 << powSize), allocator);

            //Debug.Log(capacity);
        }

        // Amortized Constant runtime
        public void ForceInsert(Key key, Value value)
        {
            //Debug.Log("Force Inserting");
            Insert(key, value);
        }

        // Amortized Constant runtime
        public bool TryInsert(Key key, Value value)
        {
            //Debug.Log("Trying Insert");
            if (Contains(key) == false)
            {
                Insert(key, value);
                return true;
            }
            return false;
        }
        //Amortized Constant runtime,
        public void Extract(Key key, out Value value)
        {
            if (ContainsWithCode(key, out int hashCode))
            {
                Count--;
                value = Data[hashCode].value;
                Data[hashCode] = new Element { key = key, value = value, state = HashState.marked };

            }
            value = default;
        }

        // Amortized Constant runtime
        public bool Contains(Key key)
        {
            //Debug.Log("Checking Contains");
            return ContainsWithCode(key, out int discard);
        }

        // Amortized Constant runtime
        public bool TryGetValue(Key key, out Value value)
        {
            //Debug.Log("Trying to get value");
            if (ContainsWithCode(key, out int code))
            {
                value = Data[code].value;
                return true;
            }
            value = default;
            return false;
        }

        // Linear Runtime
        public void Clear()
        {
            //Debug.Log("Clearing Data");

            Data.Dispose();
            Data = new NativeArray<Element>(capacity, allocator);
            Count = 0;
        }
        #endregion

        #region  Implimentation
        private void Insert(Key key, Value value)
        {
            //Debug.Log("Inserting");

            var hashCode = HashFormula(key.GetHashCode());
            var newElement = new Element
            {
                key = key,
                value = value,
                state = HashState.full
            };

            for (int i = 0; i < capacity; i++)
            {
                if (Data[hashCode].state != HashState.full)
                {
                    //early exit
                    //Debug.Log("Found Free Space");
                    Data[hashCode] = newElement;
                    Count++;
                    break;
                }
                else if (Data[hashCode].key.Equals(key))
                {
                    //Debug.Log("Found Existing Key, Overwritting");
                    Data[hashCode] = newElement;
                    break;
                }

                if (i == capacity - 1)
                {
                    //Debug.Log("Iterated through entire container, Shouldn't Be possible");
                }

                hashCode = (hashCode + 1) % capacity;
            }

            Data[hashCode] = newElement;
            if (Count > capacity * sizeRatio)
            {
                Resize();
            }
            return;
        }
        private bool ContainsWithCode(Key key, out int hashCode)
        {
            //Debug.Log("Checking Contains, passing code");

            hashCode = HashFormula(key.GetHashCode());

            for (int i = 0; i < capacity; i++)
            {
                if (Data[hashCode].state == HashState.Empty)
                {
                    //Debug.Log("Found Empty Space, Key Doesnt Exist");
                    return false;
                }
                else if (Data[hashCode].state == HashState.full && Data[hashCode].key.Equals(key))
                {
                    //Debug.Log("Found Key");
                    return true;
                }

                if (i == capacity - 1)
                {
                    //Debug.Log("Itterated through entire container, shouldnt be possible");
                    return false;
                }

                hashCode = (hashCode + 1) % capacity;
            }
            return false;
        }
        // Square Adds Entropy
        private int HashFormula(int Hash)
        {
            return math.abs(((Hash + AlicePrime) * (Hash + AlicePrime)) * GoldenInt >> (32 - powSize));
        }
        private void Resize()
        {
            //Debug.Log("Resizing Internal Array");

            var newArray = new Element[capacity];
            powSize++;
            Count = 0;

            //Debug.Log("Entering Loop");
            for (int i = 0; i < Data.Length; i++)
            {
                newArray[i] = Data[i]; //Returned BY value, not reference
            }
            //Debug.Log("Exiting Loop");
            Data.Dispose();
            Data = new NativeArray<Element>(capacity, allocator);
            for (int i = 0; i < newArray.Length; i++)
            {
                if (newArray[i].state == HashState.full)
                {
                    Insert(newArray[i].key, newArray[i].value);
                }
            }
        }

        internal enum HashState : byte
        {
            Empty = 0,
            marked = 1,
            full = 2
        };
        internal struct Element
        {
            internal Key key;
            internal Value value;
            internal HashState state;
        }



        #endregion

        public void Dispose()
        {
            Data.Dispose();
            Count = 0;
            powSize = 0;
        }
    }
}