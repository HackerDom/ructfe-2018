namespace Vch.Core.Meta
{
    public interface IUUIDProvider
    {
        ulong GetUUID(UserMeta userMeta);
    }
}