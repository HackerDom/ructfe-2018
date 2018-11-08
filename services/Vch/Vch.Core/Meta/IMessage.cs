using System;

namespace Vch.Core.Meta
{
    public interface IMessage
    {
        MessageId Id { get; }
        string Text { get; set; }
        DateTime CreationTime { get; }
    }
}