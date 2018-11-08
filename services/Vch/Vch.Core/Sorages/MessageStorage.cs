using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Vch.Core.Meta;

namespace Vch.Core.Sorages
{
    public class MessageStorage : BaseStorage, IMessageStorage
    {
        public MessageStorage(IMongoClient mongoClient) : base(mongoClient)
        {
            messages = new ConcurrentDictionary<MessageId, Message>();
            messageCollection = GetCollection<Message>(NameReslover.MessageCollectionName);
            Init().GetAwaiter().GetResult();
            RemoveOldMessage();
        }

        public Message UpdateMessage(MessageId id, UserInfo userInfo, string text)
        {
            return messages.AddOrUpdate(id, messageId => Message.Create(text, userInfo, messageId),
                (messageId, message) =>
                {
                    message.Text = text;
                    return message;
                });
        }

        public DeleteResult DeleteMessage(MessageId messageId)
        {
            messages.Remove(messageId, out var _);
            return messageCollection.DeleteOne(message => messageId.Equals(message.MessageId));
        }

        public IEnumerable<Message> GetAllMessage()
        {
            return messages.Values;
        }

        public IMessage GetMessage(MessageId messageId)
        {
            return messages.TryGetValue(messageId, out var message) ? message : null;
        }

        private async Task Init()
        {
            var loadedMessages = await messageCollection.Find(message => true).ToListAsync();

            foreach (var message in loadedMessages)
            {
                messages[message.MessageId] = message;
            }
        }

        private async Task RemoveOldMessage()
        {
            while (true)
            {
                await Task.Delay(new TimeSpan(0, 0, 5, 0));
                foreach (var message in messages.Where(
                    pair => (pair.Value.CreationTime - DateTime.UtcNow).TotalMinutes > 30))
                {
                    DeleteMessage(message.Value.MessageId);
                }
            }
        }

        private readonly ConcurrentDictionary<MessageId, Message> messages;
        private readonly IMongoCollection<Message> messageCollection;
    }
    
}