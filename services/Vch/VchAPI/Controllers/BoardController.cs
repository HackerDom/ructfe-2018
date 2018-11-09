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
        public async Task<ActionResult<IEnumerable<PublicMessage>>> GetAllMessages()
        {
            try
            {
                return messageStorage.GetMessagesOrdered(MaxTakeSize).Select(message => new PublicMessage(message)).ToActionResult();
            }
            catch (Exception e)
            {
                return StatusCode(500, e);
            }
        }

        [HttpPost("message/post/{userId}")]
        public async Task<ActionResult<Message>> PostMessageAsync(string userId)
        {
            try
            {
                var user = userStorage.FindUser(userId);
                if (user == null)
                    return NotFound();

                var text = await ParseContent<string>();
                return messageStorage.AddOrUpdateMessage(MessageId.From(await uuidProvider.GetUUID(user.Meta)), user, text).ToActionResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                return StatusCode(500, e);
            }
        }

        [HttpGet("messages/{userId}")]
        public async Task<ActionResult<IEnumerable<Message>>> GetMessagesAsync(string userId)
        {
            try
            {
                var user = userStorage.FindUser(userId);
                if (user == null)
                    return NotFound();

                return messageStorage.GetAllMessages().Where(message => message.userInfo.UserId.Equals(userId)).ToActionResult();
            }
            catch (Exception e)
            {
                return StatusCode(500, e);
            }
        }

        [HttpPost("user")]
        public async Task<ActionResult<UserInfo>> RegisterUserAsync()
        {
            try
            {
                var meta = await ParseContent<UserMeta>();
                return (await userStorage.AddUser(meta)).ToActionResult();
            }
            catch (Exception e)
            {
                return StatusCode(500, e);
            }
        }

	    private const int MaxTakeSize = 5000;

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