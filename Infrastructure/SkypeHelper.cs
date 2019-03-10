using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using TfsBot.Common.Entities;

namespace BasicBot.Infrastructure
{
    public class SkypeHelper
    {
        private BotConfiguration _botConfiguration;

        public SkypeHelper(BotConfiguration botConfiguration)
        {
            _botConfiguration = botConfiguration;
        }

        public async Task SendMessage(ServerClient client, string messageText)
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
