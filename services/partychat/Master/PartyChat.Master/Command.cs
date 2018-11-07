namespace PartyChat.Master
{
    internal class Command
    {
        public Command(string name, string text, int id)
        {
            Name = name;
            Text = text;
            Id = id;
        }

        public string Name { get; }
        
        public string Text { get; }
        
        public int Id { get; }
    }
}