namespace Vch.Core.Meta
{
    public class UserMeta
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public IPEndpointWrapper VaultTimeSource { get; set; }
        public string TrackingCode { get; set; }
    }
}