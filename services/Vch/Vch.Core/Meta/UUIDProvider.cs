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

            lastComputedHash = new byte[8];
            Array.Copy(Guid.NewGuid().ToByteArray(), lastComputedHash, 6);
        }

        public async Task<UInt64> GetUUID(UserMeta meta)
        {
            var timestamp = await timeProvider.GetTimestamp(meta.VaultTimeSource.Endpoint());
            var secure = GetNextSecureRandomBytes();

            var timeBits = new BitArray(timestamp.ToBytes());
            var rndBites = new BitArray(secure);

            byte[] result = new byte[8];
            timeBits.Xor(rndBites).CopyTo(result, 0);

            return BitConverter.ToUInt64(result);
        }

        private byte[] GetNextSecureRandomBytes()
        {
            var hash = shaProvider.ComputeHash(lastComputedHash);
            Array.Copy(hash, lastComputedHash, 6);
            return lastComputedHash.ToArray();
        }

        private byte[] lastComputedHash;
        private readonly SHA512 shaProvider;
        private readonly ITimeProvider timeProvider;
    }
}