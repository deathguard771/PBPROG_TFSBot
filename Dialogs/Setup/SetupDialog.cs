using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using TfsBot.Common.Db;
using TfsBot.Common.Entities;

namespace BasicBot.Dialogs.Setup
{
    public class SetupDialog : ComponentDialog
    {
        // Dialog IDs
        public const string SetupDialogID = "setupDialog";
        private const string SetupCommand = "/setup";
        private const string TfsCheckedInUriCommand = "/checkedin_uri";
        private const string PrintGuidPrompt = "printGuidPrompt";

        private readonly string[] _activeCommands = new[] { SetupCommand };
        private readonly IRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetupDialog"/> class.
        /// </summary>
        /// <param name="setupStateAccessor">Accessor for user profile.</param>
        /// <param name="loggerFactory">Logging.</param>
        /// <param name="repository">Set server and conversation id.</param>
        public SetupDialog(IStatePropertyAccessor<SetupState> setupStateAccessor, ILoggerFactory loggerFactory, IRepository repository)
            : base(nameof(SetupDialog))
        {
            _repository = repository;
            SetupStateAccessor = setupStateAccessor ?? throw new ArgumentNullException(nameof(setupStateAccessor));

            // Add control flow dialogs
            var waterfallSteps = new WaterfallStep[]
            {
                CommandsCheckStepAsync,
                SetupPrintOrRepeatGuidStepAsync,
            };
            AddDialog(new WaterfallDialog(SetupDialogID, waterfallSteps));
            AddDialog(new ConfirmPrompt(PrintGuidPrompt));
        }

        public IStatePropertyAccessor<SetupState> SetupStateAccessor { get; }

        private async Task<DialogTurnResult> CommandsCheckStepAsync(
                                                WaterfallStepContext stepContext,
                                                CancellationToken cancellationToken)
        {
            var activity = stepContext.Context.Activity;
            var state = await SetupStateAccessor.GetAsync(stepContext.Context, () => new SetupState());

            if (string.IsNullOrWhiteSpace(state.ServerID))
            {
                var client = await _repository.GetClientAsync(activity.Conversation.Id, activity.Conversation.Name);
                state.ServerID = client?.ServerId;
            }

            state.Command = state.Command ?? (activity.TextFormat == "plain" && activity.Type == "message" ? activity.Text : null);

            // if we have everything we need, greet user and return.
            if (state != null && !string.IsNullOrWhiteSpace(state.Command) && state.Command.StartsWith('/'))
            {
                return await CommandExecuteAsync(stepContext);
            }

            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> SetupPrintOrRepeatGuidStepAsync(
                                                WaterfallStepContext stepContext,
                                                CancellationToken cancellationToken)
        {
            var result = stepContext.Result as bool?;

            if (!result.HasValue)
            {
                return await stepContext.NextAsync();
            }

            var setupState = await SetupStateAccessor.GetAsync(stepContext.Context);

            // generate new id
            if (!result.Value)
            {
                await SetServerIdAsync(stepContext.Context);
            }

            await stepContext.Context.SendActivityAsync($"Server guid={setupState.ServerID}");

            return await stepContext.EndDialogAsync();
        }

        // Helper function to greet user with information in GreetingState.
        private async Task<DialogTurnResult> CommandExecuteAsync(WaterfallStepContext stepContext)
        {
            var context = stepContext.Context;
            var setupState = await SetupStateAccessor.GetAsync(context);

            string answer;

            switch (setupState.Command)
            {
                case SetupCommand:
                    if (!string.IsNullOrWhiteSpace(setupState.ServerID))
                    {
                        setupState.AskForRepeat = true;
                        return await stepContext.PromptAsync(PrintGuidPrompt, new PromptOptions()
                        {
                            Prompt = (Activity)MessageFactory.Text("Would you like to print previous server guid? If you say NO I will generate new pretty awesome guid for you"),
                        });
                    }

                    await SetServerIdAsync(context);
                    answer = $"Server guid: {setupState.ServerID}";
                    break;
                default:
                    answer = $"Can't understand what do you meen by {setupState.Command}. List of active commands:"
                        + string.Join(Environment.NewLine, _activeCommands);
                    break;
            }

            // Display their profile information and end dialog.
            await context.SendActivityAsync(answer);
            return await stepContext.EndDialogAsync();
        }

        /// <summary>
        /// Set server id for this conversation.
        /// </summary>
        /// <param name="context">Context of conversation.</param>
        private async Task SetServerIdAsync(ITurnContext context)
        {
            var activity = context.Activity;

            var serverParams = ServerParams.New("pcpo");

            var serverClient = new ServerClient(serverParams.Id, activity.Conversation.Id)
            {
                UserName = activity.Conversation.Name,
                BotServiceUrl = activity.ServiceUrl,
                BotId = activity.Recipient.Id,
                BotName = activity.Recipient.Name,
                ReplaceFrom = serverParams.ReplaceFrom,
                ReplaceTo = serverParams.ReplaceTo,
                ConversationId = activity.Conversation.Id,
                ChannelId = activity.ChannelId,
            };
            await _repository.SaveServiceClient(serverClient);
            var client = new Client(serverParams.Id, activity.Conversation.Id, activity.Conversation.Name);
            await _repository.SaveClient(client);

            var state = await SetupStateAccessor.GetAsync(context);
            state.ServerID = serverParams.Id;
        }
    }
}
