using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.BotBuilderSamples;
using Microsoft.Extensions.Logging;

namespace BasicBot.Dialogs.Greeting
{
    public class TeamFoundationServerMonolog : ComponentDialog
    {
        // Dialog IDs
        private const string ProfileDialog = "profileDialog";

        public IStatePropertyAccessor<TfsState> UserProfileAccessor { get; }

        public TeamFoundationServerMonolog(IStatePropertyAccessor<TfsState> userProfileStateAccessor, ILoggerFactory loggerFactory)
            : base(nameof(TeamFoundationServerMonolog))
        {
            UserProfileAccessor = userProfileStateAccessor ?? throw new ArgumentNullException(nameof(userProfileStateAccessor));

            // Add control flow dialogs
            var waterfallSteps = new WaterfallStep[]
            {
                PromptActionStepAsync,
            };
            AddDialog(new WaterfallDialog(ProfileDialog, waterfallSteps));
        }

        private async Task<DialogTurnResult> PromptActionStepAsync(
                                                WaterfallStepContext stepContext,
                                                CancellationToken cancellationToken)
        {
            var state = await UserProfileAccessor.GetAsync(stepContext.Context);

            // if we have everything we need, greet user and return.
            if (state != null && !string.IsNullOrWhiteSpace(state.Text))
            {
                return await GreetUser(stepContext);
            }

            return await stepContext.NextAsync();
        }

        // Helper function to greet user with information in GreetingState.
        private async Task<DialogTurnResult> GreetUser(WaterfallStepContext stepContext)
        {
            var context = stepContext.Context;
            var greetingState = await UserProfileAccessor.GetAsync(context);

            // Display their profile information and end dialog.
            await context.SendActivityAsync($"Your bunny wrote: {greetingState.Text}");
            return await stepContext.EndDialogAsync();
        }
    }
}
