using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BasicBot.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace BasicBot.Controllers
{
    public class TfsController : ControllerBase
    {
        private IDialogsRepository _dialogsRepository;
        private BotConfiguration _botConfiguration;

        public TfsController(IDialogsRepository dialogsRepository, Microsoft.Bot.Configuration.BotConfiguration botConfiguration)
        {
            _dialogsRepository = dialogsRepository;
            _botConfiguration = botConfiguration;
        }

        [HttpGet]
        [Route("~/tfs/setup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Setup()
        {
            return Ok(_dialogsRepository.GetAllDialogs());
        }

        [Route("~/tfs/commit")]
        [HttpPost]
        public async Task<IActionResult> SendSkypeMessage()
        {
            var activity = _dialogsRepository.GetAllDialogs().LastOrDefault();

            var endpointService = _botConfiguration.Services.FirstOrDefault(x => typeof(EndpointService).Equals(x.GetType()) && x.Name.Equals("production")) as EndpointService;

            var account = new MicrosoftAppCredentials(endpointService.AppId, endpointService.AppPassword);
            var token = await account.GetTokenAsync();

            MicrosoftAppCredentials.TrustServiceUrl(activity.ServiceUrl, DateTime.Now.AddDays(7));

            var userAccount = new ChannelAccount(activity.From.Id, activity.From.Name);
            var botAccount = new ChannelAccount(activity.Recipient.Id, activity.Recipient.Name);
            var connector = new ConnectorClient(new Uri(activity.ServiceUrl), account, handlers: new TokenHandler(token));

            // Create a new message.
            IMessageActivity message = Activity.CreateMessageActivity();
            string conversationId = null;
            if (!string.IsNullOrEmpty(activity.Conversation.Id) && !string.IsNullOrEmpty(activity.ChannelId))
            {
                // If conversation ID and channel ID was stored previously, use it.
                message.ChannelId = activity.ChannelId;
            }
            else
            {
                // Conversation ID was not stored previously, so create a conversation. 
                // Note: If the user has an existing conversation in a channel, this will likely create a new conversation window.
                conversationId = (await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount)).Id;
            }

            // Set the address-related properties in the message and send the message.
            message.From = botAccount;
            message.Recipient = userAccount;
            message.Conversation = new ConversationAccount(id: conversationId ?? activity.Conversation.Id);
            message.Text = "COMMIT";
            message.Locale = "en-us";
            await connector.Conversations.SendToConversationAsync((Activity)message);
            return Ok();
        }
    }
}
