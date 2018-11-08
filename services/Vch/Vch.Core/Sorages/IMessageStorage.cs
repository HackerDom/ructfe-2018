using System.Collections.Generic;
using Vch.Core.Meta;

namespace Vch.Core.Sorages
{
    public interface IMessageStorage
    {
        void AddMessage(Message message);
        IEnumerable<Message> GetAllMessage();
        IMessage GetMessage(MessageId messageId);
    }
}