using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MadMaps.Common.Serialization
{
    public static class SerializationUtilites
    {
        public static int SizeOf<T>() where T : struct
        {
            return Marshal.SizeOf(default(T));
        }

        public static void CopyTo(this Stream fromStream, Stream toStream)
        {
            if (fromStream == null)
                throw new ArgumentNullException("fromStream");

            if (toStream == null)
                throw new ArgumentNullException("toStream");
            
            var bytes = new byte[8092];
            int dataRead;
            while ((dataRead = fromStream.Read(bytes, 0, bytes.Length)) > 0)
                toStream.Write(bytes, 0, dataRead);
        }
    }
}