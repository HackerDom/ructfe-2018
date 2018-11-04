using System;
using Vch.Core.Meta;

namespace Vch.Core.Sorages
{
    public interface IUserStorage
    {
        UserInfo AddUser(UserMeta userInfo);
        UserInfo GetUser(UInt64 userId);
    }
}