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

        public string Owner { get; set; }

        public static Message Create(string text, UserMeta meta, IUUIDProvider uuidProvider)
        {
            return new Message(new MessageId(uuidProvider.GetUUID(meta)))
            {
                Text = text,
                Owner = meta.VaultAuthCode
            };
        }
    }
}