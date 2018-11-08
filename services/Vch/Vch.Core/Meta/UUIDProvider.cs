using System;
using System.Collections;
using System.Linq;
using System.Security.Cryptography;
using NTPTools;
using Vch.Core.Helpers;

namespace Vch.Core.Meta
{
    public class UUIDProvider : IUUIDProvider
    {
        public UUIDProvider(ITimeProvider timeProvider)
        {
            this.timeProvider = timeProvider;
            shaProvider = SHA512.Create();
            shaProvider.Initialize();
            lastComputed = Guid.NewGuid().ToByteArray();
        }

        public UInt64 GetUUID(UserMeta meta)
        {
            var timeBits = new BitArray(timeProvider.GetTimestamp(meta.VaultTimeSource).ToBytes());
            var rndBites = new BitArray(GetNextBytes());
            byte[] result = new byte[8];
            timeBits.Xor(rndBites).CopyTo(result, 0);
            return BitConverter.ToUInt64(result);
        }

        private byte[] GetNextBytes()
        {
            lastComputed = shaProvider.ComputeHash(lastComputed);
            return lastComputed.Take(6).Union(new byte[2]).ToArray();
        }

        private byte[] lastComputed;
        private readonly SHA512 shaProvider;
        private readonly ITimeProvider timeProvider;
    }
}