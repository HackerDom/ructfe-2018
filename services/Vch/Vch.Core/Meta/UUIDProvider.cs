using System;
using System.Collections;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NTPTools;
using Vch.Core.Helpers;

namespace Vch.Core.Meta
{
    public class UUIDProvider : IUUIDProvider
    {
        public UUIDProvider(ITimeProvider timeProvider)
        {
            this.timeProvider = timeProvider;
            shaProvider = new SHA512Managed();

			lastComputed = Guid.NewGuid().ToByteArray().Take(6).Concat(new byte[] { 0, 0 }).ToArray();
		}

        public async Task<UInt64> GetUUID(UserMeta meta)
        {
	        var timestamp = await timeProvider.GetTimestamp(meta.VaultTimeSource.Endpoint());
	        var timeBits = new BitArray(timestamp.ToBytes());
            var rndBites = new BitArray(GetNextSecureRandomBytes());

            byte[] result = new byte[8];
            timeBits.Xor(rndBites).CopyTo(result, 0);
            return BitConverter.ToUInt64(result);
        }

        private byte[] GetNextSecureRandomBytes()
        {
            var hash = shaProvider.ComputeHash(lastComputed);
            Array.Copy(hash, lastComputed, 6);
            return lastComputed.ToArray();
        }

        private byte[] lastComputed;
        private readonly SHA512 shaProvider;
        private readonly ITimeProvider timeProvider;
    }
}