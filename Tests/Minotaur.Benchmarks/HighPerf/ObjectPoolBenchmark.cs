using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Minotaur.Pocs.HighPerf;

namespace Minotaur.Benchmarks.HighPerf
{
    [HardwareCounters(HardwareCounter.InstructionRetired)]
    public class ObjectPoolBenchmark
    {
        public class ObjectToPool
        {
            public struct Behavior : IObjectLifecycle<ObjectToPool>
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public ObjectToPool New()
                {
                    return new ObjectToPool();
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Reset(ObjectToPool value) { }
            }
        }

        private static readonly ObjectPool<ObjectToPool> withFactory = new ObjectPool<ObjectToPool>(() => new ObjectToPool());
        private static readonly ObjectPool<ObjectToPool, ObjectToPool.Behavior> withSpecificNew = new ObjectPool<ObjectToPool, ObjectToPool.Behavior>();
        private static readonly DummyObjectPool<ObjectToPool> dummyPool = new DummyObjectPool<ObjectToPool>(() => new ObjectToPool());

        [Benchmark]
        public void UsingFactory()
        {
            var pool = withFactory;
            var obj = pool.Get();
            pool.Free(obj);
            obj = pool.Get();
            pool.Free(obj);
            obj = pool.Get();
            var obj2 = pool.Get();
            pool.Free(obj);
            obj = pool.Get();
            pool.Free(obj);
            pool.Free(obj2);
        }

        [Benchmark]
        public void UsingSpecificNew()
        {
            var pool = withSpecificNew;
            var obj = pool.Get();
            pool.Free(obj);
            obj = pool.Get();
            pool.Free(obj);
            obj = pool.Get();
            var obj2 = pool.Get();
            pool.Free(obj);
            obj = pool.Get();
            pool.Free(obj);
            pool.Free(obj2);
        }

        [Benchmark(Baseline = true)]
        public void Baseline()
        {
            var pool = dummyPool;
            var obj = pool.Get();
            pool.Free(obj);
            obj = pool.Get();
            pool.Free(obj);
            obj = pool.Get();
            var obj2 = pool.Get();
            pool.Free(obj);
            obj = pool.Get();
            pool.Free(obj);
            pool.Free(obj2);
        }
    }

    public class DummyObjectPool<T>
    {
        private readonly Func<T> _factory;
        private readonly Stack<T> _items;

        public DummyObjectPool(Func<T> factory, int capacity = 16)
        {
            _factory = factory;
            _items = new Stack<T>(capacity);
        }

        public T Get() => _items.Count > 0
            ? _items.Pop()
            : _factory();

        public void Free(T value) => _items.Push(value);
    }
}
