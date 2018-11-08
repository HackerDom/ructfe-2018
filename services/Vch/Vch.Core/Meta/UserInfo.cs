using System;

namespace Vch.Core.Meta
{
    public class UserInfo
    {
        public UserInfo(UInt64 id)
        {
            Id = id;
        }

        public UInt64 Id { get; }
        public UserMeta Meta { get; set; }
    }
}