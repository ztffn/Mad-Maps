using System.Collections.Generic;
using UnityEngine;

namespace MadMaps.Common
{
    public static class ComputeShaderPool
    {
        private const int MaxBufferDistance = 5000;

        private static Dictionary<string, ComputeShader> _shaderCache = new Dictionary<string, ComputeShader>();
        private static List<ComputeBufferKey> _keyList = new List<ComputeBufferKey>(); 

        private struct ComputeBufferKey
        {
            public int Stride;
            public int Count;

            public ComputeBufferKey(int count, int stride)
            {
                Count = count;
                Stride = stride;
            }
        }

        private class ComputeBufferSorter : IComparer<ComputeBufferKey>
        {
            public int Compare(ComputeBufferKey x, ComputeBufferKey y)
            {
                return x.Count.CompareTo(y.Count);
            }
        }

        private static SortedDictionary<ComputeBufferKey, Queue<ComputeBuffer>> _computeBufferCache 
            = new SortedDictionary<ComputeBufferKey, Queue<ComputeBuffer>>(new ComputeBufferSorter());
        private static List<ComputeBuffer> _bufferList = new List<ComputeBuffer>(); 

        public static void ClearPool()
        {
            foreach (var computeShader in _shaderCache)
            {
                Resources.UnloadAsset(computeShader.Value);
            }
            _shaderCache.Clear();

            foreach (var pair in _computeBufferCache)
            {
                foreach (var buffer in pair.Value)
                {
                    buffer.Release();
                    buffer.Dispose();
                }
            }

            _computeBufferCache.Clear();
            _bufferList.Clear();
            _keyList.Clear();
        }

        public static ComputeShader GetShader(string resourcepath)
        {
            ComputeShader result;
            if (!_shaderCache.TryGetValue(resourcepath, out result))
            {
                result = Resources.Load<ComputeShader>(resourcepath);
                _shaderCache.Add(resourcepath, result);
            }
            return result;
        }

        public static ComputeBuffer GetBuffer(int count, int stride)
        {
            /*Queue<ComputeBuffer> bufferQueue;
            if (_computeBufferCache.TryGetValue(new ComputeBufferKey(count, stride), out bufferQueue) && bufferQueue != null && bufferQueue.Count > 0)
            {
                var buffer = bufferQueue.Dequeue();
                buffer.SetCounterValue(0);
                return buffer;
            }

            foreach (var computeBuffer in _computeBufferCache)
            {
                var lookup = computeBuffer.Key;
                
                if (lookup.Stride != stride)
                {
                    continue;
                }
                if (lookup.Count < count)
                {
                    continue;
                }
                if (lookup.Count > count + MaxBufferDistance)
                {
                    continue;
                }
                if (computeBuffer.Value.Count == 0)
                {
                    continue;
                }
                var result = computeBuffer.Value.Dequeue();
                result.SetCounterValue(0);
                return result;
            }*/

            return  new ComputeBuffer(count, stride);
        }

        public static void ReturnBuffer(ComputeBuffer buffer)
        {
            buffer.Release();
            buffer.Dispose();

            /*Queue<ComputeBuffer> bufferQueue;
            var key = new ComputeBufferKey(buffer.count, buffer.stride);
            if (!_computeBufferCache.TryGetValue(key, out bufferQueue))
            {
                bufferQueue = new Queue<ComputeBuffer>();
                _computeBufferCache[key] = bufferQueue;
            }
            bufferQueue.Enqueue(buffer);*/
        }
    }
}