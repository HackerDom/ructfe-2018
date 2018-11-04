using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Vch.Core.Helpers;
using Vch.Core.Meta;
using Vch.Core.Sorages;

namespace VchAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BoardController : ControllerBase
    {
        [HttpGet("messages")]
        public ActionResult<IEnumerable<IMessage>> GetAllMessages()
        {
            return messageStorage.GetAllMessage().ToActionResult();
        }

        [HttpPost("message/post/{userId}")]
        public ActionResult<Message> PostMessageAsync(UInt64 userId, [FromBody] string value)
        {
            var user = userStorage.GetUser(userId);
            if (user == null)
                return NotFound();

            var message = Message.Create(value, user.Meta, uuidProvider);
            messageStorage.AddMessage(message);

            return message.ToActionResult();
        }

        [HttpDelete("message/post/{userId}")]
        public ActionResult<DeleteResult> DeleteMessageAsync(UInt64 messageId, [FromBody] string ownerKey)
        {
            var result = messageStorage.DeleteMessage(new MessageId(messageId), ownerKey);

            return result.ToActionResult();
        }


        [HttpPost("user")]
        public ActionResult<UserInfo> RegisterUserAsync()
        {
            using (var memory = new MemoryStream())
            {
                Request.Body.CopyTo(memory);
                var text = BitConverter.ToString(memory.ToArray());
                var meta = text.FromJSON<UserMeta>();
                return userStorage.AddUser(meta).ToActionResult();
            }
        }


        public IUUIDProvider uuidProvider { get; set; }
        public IUserStorage userStorage { get; set; }
        private IMessageStorage messageStorage { get; set; }
    }
}