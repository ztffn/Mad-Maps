using System;
using System.IO;
using System.IO.Compression;
using MadMaps.Common.Serialization;
using UnityEngine;

namespace MadMaps.Common.Collections
{
    public abstract class Serializable2DArray<T> : ISerializationCallbackReceiver, IDataInspectorProvider where T : struct
    {
        [NonSerialized]
        public T[] Data;

        [SerializeField]
        private byte[] _compressedData;
        [SerializeField]
        private bool _dirty = false;

        public int Height;
        public int Width;
        
        public Serializable2DArray(int width, int data)
        {
            Width = width;
            Height = data;
            Data = new T[Width*Height];
            _dirty = true;
        }

        public Serializable2DArray(T[,] data)
        {
            _dirty = true;
            if (data == null)
            {
                Width = 0;
                Height = 0;
                Data = null;
                return;
            }
            Width = data.GetLength(0);
            Height = data.GetLength(1);
            Data = new T[Width * Height];
            for (var u = 0; u < Width; u++)
            {
                for (var v = 0; v < Height; v++)
                {
                    var height = data[u, v];
                    var index = v * Width + u;

                    if (index < 0 || index > Data.Length - 1)
                    {
                        Debug.LogError(string.Format("Invalid index: {0} (width was {1}, height was {2}, x was {3}, y was {4}",
                            index, Width, Height, u, v));
                    }

                    Data[index] = height;
                }
            }
        }
        
        public T[,] Deserialize()   // Pure
        {
            var heights = new T[Width, Height];
            for (var u = 0; u < Width; u++)
            {
                for (var v = 0; v < Height; v++)
                {
                    var index = v * Width + u;
                    heights[u, v] = Data[index];
                }
            }
            return heights;
        }

        public T this[int u, int v]
        {
            get
            {
                if (Data == null || Data.Length == 0)
                {
                    return default(T);
                }
                if(u < 0 || u >= Width || v < 0 || v >= Height)
                {
                    throw new IndexOutOfRangeException(string.Format("Coord: ({0}, {1})", u, v));
                }
                var index = v * Width + u;
                if (index >= Data.Length || index < 0)
                {
                    throw new IndexOutOfRangeException("Index: " + index);
                }
                return Data[index];
            }
            set
            {
                var index = v * Width + u;
                if (index >= Data.Length || index < 0)
                {
                    throw new IndexOutOfRangeException("Index: " + index);
                }
                _dirty = true;
                Data[index] = value;
            }
        }

        public void Clear()
        {
            _dirty = true;
            if (Data == null)
            {
                Data = new T[Width * Height];
                return;
            }
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = default(T);
            }
        }

        public void Fill(T val)
        {
            _dirty = true;
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = val;
            }
        }

        public bool HasData()   // Pure
        {
            return Data != null && Data.Length > 0;
        }

        public void ForceDirty()
        {
            _dirty = true;
            OnBeforeSerialize();
        }

        public void OnBeforeSerialize()
        {
            if (!_dirty)
            {
                return;
            }

            _dirty = false;
            if (Data == null || Data.Length == 0)
            {
                _compressedData = null;
                return;
            }

            var rawData = new byte[Data.Length*SerializationUtilites.SizeOf<T>()];
            Buffer.BlockCopy(Data, 0, rawData, 0, rawData.Length);

            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(rawData, 0, rawData.Length);
                zipStream.Close();
                _compressedData = compressedStream.ToArray();
            }
        }

        public void OnAfterDeserialize()
        {
            if (_compressedData == null || _compressedData.Length == 0)
            {
                //Data = new T[Width * Height];
                return;
            }
            using (var compressedMs = new MemoryStream(_compressedData))
            {
                using (var decompressedMs = new MemoryStream())
                {
                    using (var gzs = new GZipStream(compressedMs, CompressionMode.Decompress))
                    {
                        gzs.CopyTo(decompressedMs);
                    }
                    using (var binaryStream = new BinaryReader(decompressedMs))
                    {
                        binaryStream.BaseStream.Position = 0;
                        var pos = 0;
                        var counter = 0;
                        var length = (int)decompressedMs.Length;
                        var typeSize = SerializationUtilites.SizeOf<T>();
                        var arraySize = length/typeSize;
                        if (Data == null || Data.Length != arraySize)
                        {
                            Data = new T[arraySize];
                        }
                        while (pos < length)
                        {
                            try
                            {
                                Data[counter] = ReadFromStream(binaryStream);
                            }
                            catch (Exception)
                            {
                                
                                throw;
                            }
                            
                            counter++;
                            pos += typeSize;
                        }
                    }
                }
            }
        }

        protected abstract T ReadFromStream(BinaryReader br);

        public abstract Texture2D ToTexture2D(bool normalise, Texture2D tex = null);

        public virtual string AuxData {get{return string.Empty;}}
    }
}