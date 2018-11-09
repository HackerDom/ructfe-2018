using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NTPTools
{
    public class TimeProvider : ITimeProvider
    {
        private static int normalDeviation = 1;

        public TimeProvider()
        {
        }

        public async Task<ulong> GetTimestamp(IPEndPoint endpoint = null)
        {
            var address = endpoint ?? defaultTimeSource;
            return await GetNetworkTime(address);
        }

        public async Task<ulong> GetNetworkTime(IPEndPoint endpoint)
        {
            var builder = new NTPDataBuilder();
            builder.SetNTPMode(NTPMode.Client);
            builder.SetLeapIndicator(LeapIndicator.NoWarining);
            builder.SetNTPVersion(NTPVersionNumber.V3);
            builder.SetPeerClockStratum(3);
            builder.SetPollingInterval(TimeSpan.FromSeconds(2));


            var client = new UdpClient(AddressFamily.InterNetwork);
            client.Client.SendTimeout = 50;
            client.Client.ReceiveTimeout = 50;
            client.Client.ReceiveBufferSize = 48;

            var request = builder.Build();
            await client.SendAsync(request, request.Length, endpoint);


            var result = client.Receive(ref endpoint);

            return BitConverter.ToUInt64(result, 40);
        }


        public enum LeapIndicator
        {
            NoWarining
        }

        public enum NTPVersionNumber
        {
            V1, V2, V3, V4
        }

        public enum NTPMode
        {
            Client
        }

       public class NTPDataBuilder
        {
            private byte flags = 0b00000000;
            private byte peerClockStratum = 0;
            private byte peerPollingInterval = 0;

            public void SetLeapIndicator(LeapIndicator leapIndicator)
            {
                switch (leapIndicator)
                {
                    case LeapIndicator.NoWarining:
                        flags |= 0b00000000;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(leapIndicator), leapIndicator, null);
                }
            }
            public void SetNTPVersion(NTPVersionNumber version)
            {
                switch (version)
                {
                    case NTPVersionNumber.V1:
                        flags |= 0b00001000;
                        break;
                    case NTPVersionNumber.V2:
                        flags |= 0b00010000;
                        break;
                    case NTPVersionNumber.V3:
                        flags |= 0b00011000;
                        break;
                    case NTPVersionNumber.V4:
                        flags |= 0b00100000;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(version), version, null);
                }
            }

            public void SetNTPMode(NTPMode mode)
            {
                switch (mode)
                {
                    case NTPMode.Client:
                        flags |= 0b00000011;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                }
            }

            public void SetPeerClockStratum(byte number)
            {
                peerClockStratum = number;
            }

            public void SetPollingInterval(TimeSpan interval)
            {
                var a = (byte) interval.TotalSeconds;
                peerPollingInterval = a;
            }

            public byte[] Build()
            {
                var ntpData = new byte[48];
                ntpData[0] = flags;
                ntpData[1] = peerClockStratum;
                ntpData[2] = peerPollingInterval;

                return ntpData;
            }
        }

        static uint SwapEndianness(ulong x)
        {
            return (uint) (((x & 0x000000ff) << 24) +
                           ((x & 0x0000ff00) << 8) +
                           ((x & 0x00ff0000) >> 8) +
                           ((x & 0xff000000) >> 24));
        }

        private readonly UdpClient udpClient;
        private readonly IPEndPoint defaultTimeSource = new IPEndPoint(IPAddress.Parse("10.10.10.10"), 123);
    }
}