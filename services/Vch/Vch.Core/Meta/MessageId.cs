using System;
using System.Collections.Generic;

namespace Vch.Core.Meta
{
    public class MessageId
    {
        public MessageId(ulong id)
        {
            Id = id;
        }

        public static MessageId Parse(string id)
        {
            return new MessageId(UInt64.Parse(id));
        }

        public UInt64 Id { get; set; }

        public static IEqualityComparer<MessageId> IdComparer { get; } = new IdEqualityComparer();

        private sealed class IdEqualityComparer : IEqualityComparer<MessageId>
        {
            public bool Equals(MessageId x, MessageId y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Id == y.Id;
            }

            public int GetHashCode(MessageId obj)
            {
                return obj.Id.GetHashCode();
            }
        }
    }
}