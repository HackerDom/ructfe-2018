using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Vch.Core.Meta
{
    public class UserInfo
    {

        public UserInfo(string userId)
        {

            UserId = userId;
        }

        [JsonIgnore]
        [BsonId]
        public ObjectId Id { get; set; }

        public string UserId { get; set; }

        public UserMeta Meta { get; set; }
    }
}