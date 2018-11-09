using System.Collections.Generic;
using Vch.Core.Meta;

namespace Vch.Core.Sorages
{
    public interface IMessageStorage
    {
        Message AddOrUpdateMessage(MessageId id, UserInfo userInfo, string text);
        IEnumerable<Message> GetAllMessages();
        IEnumerable<Message> GetMessagesOrdered(int take);
        IMessage FindMessage(MessageId messageId);
    }
}