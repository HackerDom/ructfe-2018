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

        public void AddMessage(Message message)
        {
            messages[message.Id] = message;
            messageCollection.InsertOne(message);
        }

        public DeleteResult DeleteMessage(MessageId messageId)
        {
            messages.Remove(messageId, out var _);
            try
            {
                return messageCollection.DeleteOne(message => messageId.Equals(message.Id));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
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
            var loadedMessages = await messageCollection.FindAsync(message => true);

            if (loadedMessages.Current == null)
                return;

            foreach (var message in loadedMessages.Current)
            {
                messages[message.Id] = message;
            }
        }

        private async Task RemoveOldMessage()
        {
            while (true)
            {
                await Task.Delay(new TimeSpan(0, 0, 5, 0));
                foreach (var message in messages.Where(pair => (pair.Value.CreationTime - DateTime.UtcNow).TotalMinutes > 30))
                {
                    try
                    {
                        DeleteMessage(message.Value.Id);
                    }
                    catch (Exception e)
                    {
                        //TODO: log this
                        Console.WriteLine(e);
                    }
                }
            }
        }

        private readonly ConcurrentDictionary<MessageId, Message> messages;
        private readonly IMongoCollection<Message> messageCollection;
    }
    
}