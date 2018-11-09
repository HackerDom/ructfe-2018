using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Newtonsoft.Json;

namespace Vch.Core.Meta
{
    public class UserInfo
    {
        public UserInfo(string userId)
        {
            UserId = userId;
        }

        [BsonId]
        public string UserId { get; set; }

        public UserMeta Meta { get; set; }

        public DateTime RegistrationDate { get; set; }
    }
}