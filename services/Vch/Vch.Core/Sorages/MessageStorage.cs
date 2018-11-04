using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Vch.Core.Meta;

namespace Vch.Core.Sorages
{
    public class MessageStorage : BaseStorage, IMessageStorage
    {
        public MessageStorage(IMongoClient mongoClient) : base(mongoClient)
        {
            messages = new ConcurrentDictionary<MessageId, IMessage>();
            messageCollection = GetCollection<IMessage>(NameReslover.MessageCollectionName);
            Init().GetAwaiter().GetResult();
        }

        public void AddMessage(IMessage message)
        {
            messages[message.Id] = message;
            messageCollection.InsertOne(message);
        }

        public DeleteResult DeleteMessage(MessageId messageId, string ownerCode)
        {
            messages.Remove(messageId, out var _);
            try
            {
                return messageCollection.DeleteOne(message => messageId.Equals(message.Id) &&
                                                              message.Owner.SEqual(ownerCode));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }

        public IEnumerable<IMessage> GetAllMessage()
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
            foreach (var message in loadedMessages.Current)
            {
                messages[message.Id] = message;
            }
        }

        private readonly ConcurrentDictionary<MessageId, IMessage> messages;
        private readonly IMongoCollection<IMessage> messageCollection;
    }

    public static class StringExtension
    {
        public static bool SEqual(this string source, string other)
        {
            var equal = true;
            for (int i = 0; i < other.Length; i++)
            {
                equal &= source[i] == other[i];
            }
            return equal;
        }
    }
}