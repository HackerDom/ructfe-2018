using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Vch.Core.Helpers
{
    public static class BinaryExtensoins
    {
        public static byte[] ToBytes(this object source)
        {
            if (source == null)
                return null;
            var binaryFormatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, source);
                return memoryStream.ToArray();
            }
        }

        public static byte[] ToBytes(this ulong source)
        {
            return BitConverter.GetBytes(source);
        }

        public static TValue FromBytes<TValue>(this byte[] source)
        {
            using (MemoryStream memoryStream = new MemoryStream(source))
            {
                IFormatter binaryFormatter = new BinaryFormatter();
                return (TValue)binaryFormatter.Deserialize(memoryStream);
            }
        }
    }
}