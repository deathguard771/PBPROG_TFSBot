using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BasicBot.Infrastructure;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using TfsBot.Common.Bot;
using TfsBot.Common.Db;
using TfsBot.Common.Dtos;

namespace TfsBot.Controllers
{
    public class WebhooksController : ControllerBase
    {
        private readonly IRepository _repository;
        private readonly IDialogsRepository _dialogsRepository;
        private readonly BotConfiguration _botConfiguration;

        public WebhooksController(IRepository repository, IDialogsRepository dialogsRepository, Microsoft.Bot.Configuration.BotConfiguration botConfiguration)
        {
            _repository = repository;
            _dialogsRepository = dialogsRepository;
            _botConfiguration = botConfiguration;
        }

        [HttpPost]
        [Route("~/api/webhooks/pullrequest/{id}")]
        public async Task<IActionResult> PullRequest(string id, [FromBody] PullRequest req)
        {
            TrackEvent("pullrequest", id, req.EventType);
            var message = GetPullRequestMessage(req);
            await SendMessageIfDefined(id, message);
            return Ok();
        }

        [HttpPost]
        [Route("~/api/webhooks/build/{id}")]
        public async Task<IActionResult> Build(string id, [FromBody] BuildRequest req)
        {
            TrackEvent("build", id, req.EventType);
            var message = GetBuildMessage(req);
            await SendMessageIfDefined(id, message);
            return Ok();
        }

        [HttpGet]
        [HttpPost]
        [Route("~/api/webhooks/test/{id}")]
        public async Task<IActionResult> Test(string id)
        {
            TrackEvent("test", id, string.Empty);
            try
            {
                await SendMessageIfDefined(id, new[] {"webhooks are working"});
                return Ok(new { result = "OK"});
            }
            catch (Exception e)
            {
                return Ok(new { result = "FAILED", error = e.ToString() });
            }
        }

        [HttpGet]
        [HttpPost]
        [Route("~/api/webhooks/commit/{id}")]
        public async Task<IActionResult> CodeCheckedIn(string id, [FromBody] CodeCheckedInRequest req)
        {
            TrackEvent("build", id, req.EventType);
            var message = GetCodeCheckedInMessage(req);
            await SendMessageIfDefined(id, message);
            return Ok();
        }

        private static void TrackEvent(string webhookType, string id, string eventType)
        {
            var telemetry = new TelemetryClient();
            var trackParams = new Dictionary<string, string>
            {
                { "id", id},
                { "eventType", eventType},

                // {"content", contentString}
            };

            telemetry.TrackEvent($"webhooks.{webhookType}", trackParams);
        }

        private static IEnumerable<string> GetPullRequestMessage(PullRequest req)
        {
            yield return $"**PR{req.Resource.PullRequestId}** {req.Message.Markdown} ([link]({req.Resource.Repository.RemoteUrl}/pullrequest/{req.Resource.PullRequestId}?view=files))";
            if (req.EventType == "git.pullrequest.created")
            {
                yield return $"_**{req.Resource.Title}**_";
                yield return $"_{req.Resource.Description}_";
            }
        }

        private static IEnumerable<string> GetBuildMessage(BuildRequest req)
        {
            yield return $"**BUILD {req.Resource.BuildNumber}** {req.Message.Markdown} ([link]({req.Resource.Url}))";
        }

        private static IEnumerable<string> GetCodeCheckedInMessage(CodeCheckedInRequest req)
        {
            yield return $"**COMMIT {req.Resource.ChangesetId}** {req.Message.Markdown} ([link]({req.Resource.Url}))";
        }

        private async Task SendMessageIfDefined(string id, IEnumerable<string> messages)
        {
            if (!messages.Any())
            {
                return;
            }

            var msg = string.Join(Environment.NewLine + Environment.NewLine, messages);

            /*
             * var clients = _repository.GetServerClients(id);
            if (clients.Count == 0)
            {
                throw new ArgumentException($"There are no clients for id: {id}");
            }

            foreach (var client in clients)
            {

                await BotHelper.SendMessageToClient(client, msg);

            }
            */

            await SendMsg(msg);
        }

        private async Task SendMsg(string text)
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
            message.Text = text;
            message.Locale = "en-us";
            await connector.Conversations.SendToConversationAsync((Activity)message);
        }
    }
}