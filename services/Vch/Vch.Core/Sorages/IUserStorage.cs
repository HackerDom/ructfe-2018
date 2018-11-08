using System;
using System.Threading.Tasks;
using Vch.Core.Meta;

namespace Vch.Core.Sorages
{
    public interface IUserStorage
    {
	    Task<UserInfo> AddUser(UserMeta userInfo);
        UserInfo GetUser(string userId);
    }
}