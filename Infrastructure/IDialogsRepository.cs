using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace BasicBot.Infrastructure
{
    public interface IDialogsRepository
    {
        void AddDialog(Activity id);

        IEnumerable<Activity> GetAllDialogs();
    }
}
