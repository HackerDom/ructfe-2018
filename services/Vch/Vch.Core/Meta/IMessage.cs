namespace Vch.Core.Meta
{
    public interface IMessage
    {
        MessageId Id { get; }

        string Text { get; set; }
        string Owner { get; }
    }
}