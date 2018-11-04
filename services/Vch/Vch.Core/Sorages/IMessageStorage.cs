using System.Collections.Generic;
using MongoDB.Driver;
using Vch.Core.Meta;

namespace Vch.Core.Sorages
{
    public interface IMessageStorage
    {
        void AddMessage(IMessage message);
        DeleteResult DeleteMessage(MessageId messageId, string ownerCode);
        IEnumerable<IMessage> GetAllMessage();
        IMessage GetMessage(MessageId messageId);
    }
}