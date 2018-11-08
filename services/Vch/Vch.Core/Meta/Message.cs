using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Vch.Core.Meta
{
    public class Message : IMessage
    {
        private Message(MessageId messageId)
        {
            MessageId = messageId;
        }

        [BsonId]
        public ObjectId Id;

        public MessageId MessageId { get; set; }
        public string Text { get; set; }
        public DateTime CreationTime { get; set; }
        public UserInfo userInfo { get; set; }

        public static Message Create(string text, UserInfo info, MessageId id)
        {
            return new Message(id)
            {
                Text = text,
                userInfo = info,
                CreationTime = DateTime.UtcNow
            };
        }

        public static Message Create(string text, UserInfo info, IUUIDProvider uuidProvider)
        {
            return new Message(new MessageId(uuidProvider.GetUUID(info.Meta)))
            {
                Text = text,
                userInfo = info,
                CreationTime = DateTime.UtcNow
            };
        }
    }
}