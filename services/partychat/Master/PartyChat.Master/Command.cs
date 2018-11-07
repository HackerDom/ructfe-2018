namespace PartyChat.Master
{
    internal class Command
    {
        public Command(string name, string args, int id)
        {
            Name = name;
            Args = args;
            Id = id;
        }

        public string Name { get; }
        
        public string Args { get; }
        
        public int Id { get; }

        public override string ToString() => $"{Id} {Name} {Args}";
    }
}