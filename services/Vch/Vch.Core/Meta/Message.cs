using System;

namespace Vch.Core.Meta
{
    public class Message : IMessage
    {
        private Message(MessageId id)
        {
            Id = id;
        }

        public MessageId Id { get; }
        public string Text { get; set; }
        public DateTime CreationTime { get; set; }
        public UserInfo userInfo { get; set; }


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