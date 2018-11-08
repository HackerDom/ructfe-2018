using System.Collections.Generic;
using Vch.Core.Meta;

namespace Vch.Core.Sorages
{
    public interface IMessageStorage
    {
        Message UpdateMessage(MessageId id, UserInfo userInfo, string text);
        IEnumerable<Message> GetAllMessage();
        IMessage GetMessage(MessageId messageId);
    }
}