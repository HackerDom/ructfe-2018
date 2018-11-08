using System;

namespace Vch.Core.Meta
{
    public interface IMessage
    {
        MessageId MessageId { get; }
        string Text { get; set; }
        DateTime CreationTime { get; }
    }
}