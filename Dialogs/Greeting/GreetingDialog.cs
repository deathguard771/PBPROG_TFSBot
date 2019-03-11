// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using TfsBot.Common.Db;
using TfsBot.Common.Entities;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Demonstrates the following concepts:
    /// - Use a subclass of ComponentDialog to implement a multi-turn conversation
    /// - Use a Waterflow dialog to model multi-turn conversation flow
    /// - Use custom prompts to validate user input
    /// - Store conversation and user state.
    /// </summary>
    public class GreetingDialog : ComponentDialog
    {
        // Prompts names
        private const string CreateOrAddPrompt = nameof(CreateOrAddPrompt);
        private const string PrintOrChangePrompt = nameof(PrintOrChangePrompt);
        private const string AddExistingPrompt = nameof(AddExistingPrompt);

        // Dialog IDs
        private const string SetupDialog = nameof(SetupDialog);
        private const string CreateOrAddDialog = nameof(CreateOrAddDialog);
        private const string PrintOrChangeDialog = nameof(PrintOrChangeDialog);

        private readonly IRepository _repository;
        private readonly BotConfiguration _botConfiguration;
        private readonly Choice _createChoice;
        private readonly Choice _addChoice;
        private readonly IList<Choice> _createOrAddChoices;
        private readonly Choice _printChoice;
        private readonly Choice _changeChoice;
        private readonly IList<Choice> _printOrChangeChoices;

        /// <summary>
        /// Initializes a new instance of the <see cref="GreetingDialog"/> class.
        /// </summary>
        /// <param name="userProfileStateAccessor"><see cref="GreetingStateAccessor"/></param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> that enables logging and tracing.</param>
        /// <param name="repository">Repository for DB.</param>
        public GreetingDialog(IStatePropertyAccessor<GreetingState> userProfileStateAccessor, ILoggerFactory loggerFactory, IRepository repository, BotConfiguration botConfiguration)
            : base(nameof(GreetingDialog))
        {
            _repository = repository;
            _botConfiguration = botConfiguration;
            GreetingStateAccessor = userProfileStateAccessor ?? throw new ArgumentNullException(nameof(userProfileStateAccessor));

            // Add control flow dialogs
            var waterfallSteps = new WaterfallStep[]
            {
                    InitializeStateStepAsync,
                    PromptForSetupStepAsync,
            };
            AddDialog(new WaterfallDialog(SetupDialog, waterfallSteps));

            waterfallSteps = new WaterfallStep[]
            {
                InitializeCreateOrAddStepAsync,
                CreateOrAddExecutionStepAsync,
                AddExistingIdStepAsync,
            };

            AddDialog(new WaterfallDialog(CreateOrAddDialog, waterfallSteps));

            waterfallSteps = new WaterfallStep[]
            {
                InitializePrintOrChangeStepAsync,
                PrintOrChangeExecutionStepAsync,
            };

            AddDialog(new WaterfallDialog(PrintOrChangeDialog, waterfallSteps));

            AddDialog(new ChoicePrompt(CreateOrAddPrompt)
            {
                Style = ListStyle.List,
            });
            AddDialog(new ChoicePrompt(PrintOrChangePrompt)
            {
                Style = ListStyle.List,
            });

            AddDialog(new TextPrompt(AddExistingPrompt, ValidateServerId));

            _createChoice = new Choice { Value = "Create", Synonyms = new List<string> { "new" } };
            _addChoice = new Choice { Value = "Add", Synonyms = new List<string> { "exist", "existing", "my" } };

            _createOrAddChoices = new List<Choice>()
            {
                _createChoice, _addChoice,
            };

            _printChoice = new Choice { Value = "Print", Synonyms = new List<string> { "pechat", "current" } };
            _changeChoice = new Choice { Value = "Change", Synonyms = new List<string> { "make", "do it" } };

            _printOrChangeChoices = new List<Choice>()
            {
                _printChoice, _changeChoice,
            };
        }

        public IStatePropertyAccessor<GreetingState> GreetingStateAccessor { get; }

        private async Task<DialogTurnResult> InitializeStateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var greetingState = await GreetingStateAccessor.GetAsync(stepContext.Context, () => null);
            if (greetingState == null)
            {
                var greetingStateOpt = stepContext.Options as GreetingState;
                if (greetingStateOpt != null)
                {
                    await GreetingStateAccessor.SetAsync(stepContext.Context, greetingStateOpt);
                }
                else
                {
                    await GreetingStateAccessor.SetAsync(stepContext.Context, new GreetingState());
                }
            }

            if (string.IsNullOrWhiteSpace(greetingState.MainServerID))
            {
                var client = await _repository.GetClientAsync(stepContext.Context.Activity.Conversation.Id, stepContext.Context.Activity.Conversation.Name);
                greetingState.MainServerID = client?.ServerId;
            }

            if (!string.IsNullOrWhiteSpace(greetingState.MainServerID))
            {
                var serverClients = await _repository.GetServerClients(greetingState.MainServerID);
                greetingState.ServerIDCollection = serverClients.Select(x => x.UserId).ToArray();
            }

            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> InitializeCreateOrAddStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(CreateOrAddPrompt, new PromptOptions() { Prompt = MessageFactory.Text("You don't have server ID. What would you like to do?"), Choices = _createOrAddChoices, RetryPrompt = MessageFactory.Text("Common, just choose one of them. Don't joke with me.") });
        }

        private async Task<DialogTurnResult> CreateOrAddExecutionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!(stepContext.Result is FoundChoice choice))
            {
                await stepContext.Context.SendActivityAsync("Can't parse result of create or add prompt.");
                return await stepContext.ReplaceDialogAsync(SetupDialog);
            }

            if (choice.Value == _createChoice.Value)
            {
                var greetingState = await GreetingStateAccessor.GetAsync(stepContext.Context);
                await SetServerIdAsync(stepContext.Context);
                await stepContext.Context.SendActivityAsync($"Created server ID = {greetingState.MainServerID}");
                return await stepContext.EndDialogAsync();
            }
            else
            {
                return await stepContext.PromptAsync(AddExistingPrompt, new PromptOptions
                {
                    Prompt = MessageFactory.Text("Send me server id, please."),
                });
            }
        }

        private async Task<DialogTurnResult> AddExistingIdStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var mainID = stepContext.Result as string;
            var greetingState = await GreetingStateAccessor.GetAsync(stepContext.Context);
            greetingState.MainServerID = mainID;

            await SetServerIdAsync(stepContext.Context, mainID);
            await stepContext.Context.SendActivityAsync($"Id {greetingState.MainServerID} successful set");

            return await stepContext.EndDialogAsync();
        }

        private async Task<DialogTurnResult> InitializePrintOrChangeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(PrintOrChangePrompt, new PromptOptions() { Prompt = MessageFactory.Text("You have server ID. What would you like to do?"), Choices = _printOrChangeChoices, RetryPrompt = MessageFactory.Text("Try again, OK?") });
        }

        private async Task<DialogTurnResult> PrintOrChangeExecutionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!(stepContext.Result is FoundChoice choice))
            {
                await stepContext.Context.SendActivityAsync("Can't parse result of print or change prompt.");
                return await stepContext.ReplaceDialogAsync(SetupDialog);
            }

            if (choice.Value == _printChoice.Value)
            {
                var greetingState = await GreetingStateAccessor.GetAsync(stepContext.Context);
                string[] urls;
                if (_botConfiguration.Services.FirstOrDefault(x => typeof(EndpointService).Equals(x.GetType()) && x.Name.Equals("production")) is EndpointService endpointService)
                {
                    var url = endpointService.Endpoint?.Replace("/api/messages", string.Empty);
                    urls = new[]
                    {
                        "*URLs:*",
                        $"[TFS Checked In]({url}/tfs/commit/{greetingState.MainServerID})",
                        $"[TFS Check clients]({url}/tfs/setup/{greetingState.MainServerID})",
                        $"[GitLab Push]({url}/gitlab/push/{greetingState.MainServerID})",
                    };
                }
                else
                {
                    urls = new string[0];
                }

                await stepContext.Context.SendActivityAsync($"Current server ID = {greetingState.MainServerID}{Environment.NewLine}{string.Join(Environment.NewLine, urls)}");
                return await stepContext.EndDialogAsync();
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(CreateOrAddDialog);
            }
        }

        private async Task<DialogTurnResult> PromptForSetupStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var greetingState = await GreetingStateAccessor.GetAsync(stepContext.Context, () => null);

            // if we have everything we need, greet user and return.
            if (greetingState != null)
            {
                if (!string.IsNullOrWhiteSpace(greetingState.MainServerID))
                {
                    return await HaveMainIdStep(stepContext);
                }
                else
                {
                    return await DontHaveMainIdStep(stepContext);
                }
            }

            await stepContext.Context.SendActivityAsync("Sorry, i can't get state of our conversation");
            return await stepContext.EndDialogAsync();
        }

        private async Task<DialogTurnResult> HaveMainIdStep(WaterfallStepContext stepContext)
        {
            return await stepContext.ReplaceDialogAsync(PrintOrChangeDialog);
        }

        private async Task<DialogTurnResult> DontHaveMainIdStep(WaterfallStepContext stepContext)
        {
            return await stepContext.ReplaceDialogAsync(CreateOrAddDialog);
        }

        private async Task<bool> ValidateServerId(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;

            var clients = await _repository.GetServerClients(value);

            if (!clients.Any())
            {
                await promptContext.Context.SendActivityAsync($"Seems like I don't know this ID ({value}). Sorry, I can't use it. Maybe next time, dude. Send another guid.");
                var recipient = promptContext.Context.Activity.Recipient;
                await promptContext.Context.SendActivityAsync($"");
            }
            else
            {
                promptContext.Recognized.Value = value;
            }

            return clients.Any();
        }

        /// <summary>
        /// Set server id for this conversation.
        /// </summary>
        /// <param name="context">Context of conversation.</param>
        private async Task SetServerIdAsync(ITurnContext context, string id = null)
        {
            var activity = context.Activity;

            var serverParams = id == null? ServerParams.New("pcpo") : new ServerParams() { Id = id };

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

            var state = await GreetingStateAccessor.GetAsync(context);
            state.MainServerID = serverParams.Id;
        }
    }
}
