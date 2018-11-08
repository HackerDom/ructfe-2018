using System;
using System.Threading.Tasks;

namespace Vch.Core.Meta
{
    public interface IUUIDProvider
    {
	    Task<UInt64> GetUUID(UserMeta userMeta);
    }
}