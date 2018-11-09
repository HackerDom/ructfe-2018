using System;
using System.Threading.Tasks;
using Vch.Core.Meta;

namespace Vch.Core.Sorages
{
    public interface IUserStorage
    {
	    Task<UserInfo> AddUser(UserMeta userMeta);
        UserInfo FindUser(string userId);
    }
}