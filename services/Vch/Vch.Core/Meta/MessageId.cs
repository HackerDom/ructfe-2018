using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Vch.Core.Meta
{
    public class MessageId
    {
        [JsonConstructor]
        public MessageId(ulong id)
        {
            Id = id;
        }

        public static MessageId Parse(string id)
        {
            return new MessageId(UInt64.Parse(id));
        }

        public UInt64 Id { get; set; }


        protected bool Equals(MessageId other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MessageId) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}