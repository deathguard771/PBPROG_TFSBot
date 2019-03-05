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
using TfsBot.Common.Db;
using TfsBot.Common.Dtos;
using TfsBot.Common.Entities;

namespace BasicBot.Controllers
{
    public class TfsController : ControllerBase
    {
        private IDialogsRepository _dialogsRepository;
        private BotConfiguration _botConfiguration;
        private IRepository _repository;

        public TfsController(IDialogsRepository dialogsRepository, Microsoft.Bot.Configuration.BotConfiguration botConfiguration, IRepository repository)
        {
            _dialogsRepository = dialogsRepository;
            _botConfiguration = botConfiguration;
            _repository = repository;
        }

        [HttpGet]
        [Route("~/tfs/setup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Setup()
        {
            return Ok(_dialogsRepository.GetAllDialogs());
        }

        [HttpGet]
        [HttpPost]
        [Route("~/tfs/commit/")]
        public async Task<IActionResult> CodeCheckedIn([FromBody] CodeCheckedInRequest req)
        {
            var message = GetCodeCheckedInMessage(req);
            await SendMessage(string.Join(Environment.NewLine, message));
            return Ok();
        }

        [HttpGet]
        [HttpPost]
        [Route("~/tfs/commit/{id}")]
        public async Task<IActionResult> CodeCheckedIn(string id, [FromBody] CodeCheckedInRequest req)
        {
            var message = GetCodeCheckedInMessage(req);
            var clients = await _repository.GetServerClients(id);

            foreach (var client in clients)
            {
                await SendMessage(client, string.Join(Environment.NewLine, message));
            }

            return Ok();
        }

        private static IEnumerable<string> GetCodeCheckedInMessage(CodeCheckedInRequest req)
        {
            var baseMessage = $"**COMMIT {req.Resource.ChangesetId}** {req.DetailedMessage.Markdown} ([link]({req.Resource.Url})){Environment.NewLine}";

            var itemsMessage = req.Resource.WorkItems.Any()
                ? Environment.NewLine + string.Join(Environment.NewLine, req.Resource.WorkItems.Select(x => $"{x.Id} - {x.Title}"))
                : string.Empty;

            yield return baseMessage + itemsMessage;
        }

        private async Task SendMessage(string messageText)
        {
            var activities = _dialogsRepository.GetAllDialogs();

            if (!activities.Any())
            {
                return;
            }

            if (!(_botConfiguration.Services.FirstOrDefault(x => typeof(EndpointService).Equals(x.GetType()) && x.Name.Equals("production")) is EndpointService endpointService))
            {
                return;
            }

            var account = new MicrosoftAppCredentials(endpointService.AppId, endpointService.AppPassword);
            var token = await account.GetTokenAsync();

            foreach (var activity in activities)
            {
                MicrosoftAppCredentials.TrustServiceUrl(activity.ServiceUrl, DateTime.Now.AddDays(1));

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
                message.Text = messageText ?? "commit";
                message.Locale = "en-us";
                await connector.Conversations.SendToConversationAsync((Activity)message);
            }
        }

        private async Task SendMessage(ServerClient client, string messageText)
        {
            if (!(_botConfiguration.Services.FirstOrDefault(x => typeof(EndpointService).Equals(x.GetType()) && x.Name.Equals("production")) is EndpointService endpointService))
            {
                return;
            }

            var account = new MicrosoftAppCredentials(endpointService.AppId, endpointService.AppPassword);
            var token = await account.GetTokenAsync();

            MicrosoftAppCredentials.TrustServiceUrl(client.BotServiceUrl, DateTime.Now.AddDays(1));

            var userAccount = new ChannelAccount(client.UserId, client.UserName);
            var botAccount = new ChannelAccount(client.BotId, client.BotName);
            var connector = new ConnectorClient(new Uri(client.BotServiceUrl), account, handlers: new TokenHandler(token));

            // Create a new message.
            IMessageActivity message = Activity.CreateMessageActivity();
            string conversationId = null;
            if (!string.IsNullOrEmpty(client.ConversationId) && !string.IsNullOrEmpty(client.ChannelId))
            {
                // If conversation ID and channel ID was stored previously, use it.
                message.ChannelId = client.ChannelId;
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
            message.Conversation = new ConversationAccount(id: conversationId ?? client.ConversationId);
            message.Text = messageText ?? "commit";
            message.Locale = "en-us";
            await connector.Conversations.SendToConversationAsync((Activity)message);

        }
    }
}
