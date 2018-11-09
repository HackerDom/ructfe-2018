using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Vch.Core.Meta;

namespace Vch.Core.Sorages
{
    public class MessageStorage : BaseMongoStorage, IMessageStorage
    {
        public MessageStorage(IMongoClient mongoClient) : base(mongoClient)
        {
            messages = new ConcurrentDictionary<MessageId, Message>();
            messagesCollection = GetOrCreateCollection<Message>(NameReslover.MessagesCollectionName);
            Init().Wait();
            InfiniteRemoveOldMessagesAsync();
        }

        public Message AddOrUpdateMessage(MessageId id, UserInfo userInfo, string text)
        {
            return messages.AddOrUpdate(id, messageId =>
                {
                    var message = Message.Create(text, userInfo, messageId);
                    messagesCollection.InsertOneAsync(message).Wait();
                    return message;
                },
                (messageId, message) =>
                {
                    message.Text = text;
                    var update = Builders<Message>.Update.Set(oldMessage => oldMessage.Text, text);
                    messagesCollection.UpdateOneAsync(oldMessage => oldMessage.MessageId.Equals(messageId), update).Wait();
                    return message;
                });
        }

        public IEnumerable<Message> GetAllMessages()
	    {
		    return messages.Values;
	    }

		public IEnumerable<Message> GetMessagesOrdered(int take)
        {
            return messages.Values.OrderByDescending(message => message.CreationTime).Take(take);
        }

        public IMessage FindMessage(MessageId messageId)
        {
            return messages.TryGetValue(messageId, out var message) ? message : null;
        }

        private async Task Init()
        {

            //messagesCollection.DeleteMany(info => true);
            var loadedMessages = await messagesCollection.Find(_ => true).ToListAsync();
            foreach (var message in loadedMessages)
                messages[message.MessageId] = message;
        }

        private async Task InfiniteRemoveOldMessagesAsync()
        {
            while (true)
            {
	            await Task.Delay(TimeSpan.FromSeconds(5));
				try
	            {
		            foreach(var message in messages.Where(pair => (DateTime.UtcNow - pair.Value.CreationTime).TotalMinutes > TTLinMinutes))
			            DeleteMessage(message.Value.MessageId);
				}
	            catch(Exception e)
	            {
	            }
			}
        }

	    public void DeleteMessage(MessageId messageId)
	    {
		    messagesCollection.DeleteOneAsync(message => messageId.Equals(message.MessageId)).Wait();
		    messages.Remove(messageId, out var _);
	    }

        private const int TTLinMinutes = 30;
        private readonly ConcurrentDictionary<MessageId, Message> messages;
        private readonly IMongoCollection<Message> messagesCollection;
    }
    
}