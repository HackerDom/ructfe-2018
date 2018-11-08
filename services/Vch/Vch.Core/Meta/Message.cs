using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Vch.Core.Meta
{
    public class Message : IMessage
    {
	    [JsonConstructor]
		private Message(MessageId messageId)
        {
            MessageId = messageId;
        }

        [BsonId]
		[JsonIgnore]
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

	public class PublicMessage : IMessage
	{
		public PublicMessage(IMessage message)
		{
			MessageId = message.MessageId;
			Text = message.Text;
			CreationTime = message.CreationTime;
		}

		public MessageId MessageId { get; set; }
		public string Text { get; set; }
		public DateTime CreationTime { get; set; }
	}
}