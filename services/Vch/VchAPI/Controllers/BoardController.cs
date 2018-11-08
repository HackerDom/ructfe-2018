using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public BoardController(IUUIDProvider uuidProvider, IUserStorage userStorage, IMessageStorage messageStorage)
        {
            this.uuidProvider = uuidProvider;
            this.userStorage = userStorage;
            this.messageStorage = messageStorage;
        }

        [HttpGet("messages")]
        public async Task<ActionResult<IEnumerable<IMessage>>> GetAllMessages()
        {
            return messageStorage.GetAllMessage().Cast<IMessage>().ToActionResult();
        }

        [HttpPost("message/post/{userId}")]
        public async Task<ActionResult<Message>> PostMessageAsync(string userId)
        {
            var user = userStorage.GetUser(userId);
            if (user == null)
                return NotFound();

            var text = await ParseContent<string>();

            var message = Message.Create(text, user, uuidProvider);
            messageStorage.UpdateMessage(new MessageId(uuidProvider.GetUUID(user.Meta)), user, text);
            return message.ToActionResult();
        }


        [HttpGet("messages/{userId}")]
        public async Task<ActionResult<IEnumerable<Message>>> GetMessagesAsync(string userId)
        {
            var user = userStorage.GetUser(userId);
            if (user == null)
                return NotFound();

            return messageStorage.GetAllMessage().Where(message => message.userInfo.UserId.Equals(userId)).ToActionResult();
        }

        [HttpPost("user")]
        public async Task<ActionResult<UserInfo>> RegisterUserAsync()
        {
            var meta = await ParseContent<UserMeta>();
            return userStorage.AddUser(meta).ToActionResult();
        }

        public async Task<TValue> ParseContent<TValue>() where TValue : class
        {
            using (var memory = new StreamReader(Request.Body))
            {
                var text = await memory.ReadToEndAsync();

                if (typeof(TValue) == typeof(string))
                {
                    return text as TValue;
                }

                return text.FromJSON<TValue>();
            }
        }

        private readonly IUUIDProvider uuidProvider;
        private readonly IUserStorage userStorage;
        private readonly IMessageStorage messageStorage;
    }
}