using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace NTPTools
{
    public class TimeProvider : ITimeProvider
    {
        public TimeProvider(INTSourceProvider ntSourceProvider)
        {
            this.ntSourceProvider = ntSourceProvider;
        }

        public byte[] GetTime()
        {
            var addresses = Dns.GetHostEntry(ntSourceProvider.DefaultSource).AddressList.First();
            var time = GetNetworkTime(addresses);
            return BitConverter.GetBytes(time.Item1 + time.Item2);
        }

        public byte[] GetTime(string endpoint)
        {;
            var address = IPAddress.Parse(endpoint);
            var time = GetNetworkTime(address);
            return BitConverter.GetBytes(time.Item1 + time.Item2);
        }



        public static (ulong, ulong) GetNetworkTime(IPAddress endpoint)
        {
            var builder = new NTPDataBuilder();
            builder.SetNTPMode(NTPMode.Client);
            builder.SetLeapIndicator(LeapIndicator.NoWarining);
            builder.SetNTPVersion(NTPVersionNumber.V3);
            builder.SetPeerClockStratum(3);
            builder.SetPollingInterval(TimeSpan.FromSeconds(2));

            var ntpResponse = new byte[48];

            var ipEndPoint = new IPEndPoint(endpoint, 123);
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);
                socket.ReceiveTimeout = 3000;

                socket.Send(builder.Build());
                socket.Receive(ntpResponse);
                socket.Close();
            }

            return  (GetMilliseconds(ntpResponse), GetOrigin(ntpResponse));
        }

        private static ulong GetMilliseconds(byte[] ntpData)
        {
            const byte serverReplyTime = 40;

            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
            return milliseconds;
        }

        private static ulong GetOrigin(byte[] ntpData)
        {
            const byte serverReplyTime = 48;

            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
            return milliseconds;
        }


        enum LeapIndicator
        {
            NoWarining
        }

        enum NTPVersionNumber
        {
            V1, V2, V3, V4
        }

        enum NTPMode
        {
            Client
        }

        class NTPDataBuilder
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

        private readonly INTSourceProvider ntSourceProvider;
    }
}