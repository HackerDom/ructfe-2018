using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Vch.Core.Meta
{
    public class MessageId
    {
        [JsonConstructor]
        public MessageId(string id)
        {
            Id = id;
        }

        public static MessageId Parse(string id)
        {
            return new MessageId(id.ToString());
        }

        public static MessageId From(UInt64 id)
        {
            return new MessageId(id.ToString());
        }

        [BsonId]
        public string Id { get; set; }

        protected bool Equals(MessageId other)
        {
            return string.Equals(Id, other.Id, StringComparison.InvariantCultureIgnoreCase);
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
            return (Id != null ? Id.GetHashCode() : 0);
        }
    }
}