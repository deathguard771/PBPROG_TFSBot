using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace BasicBot.Infrastructure
{
    public class InMemoryDialogsRepository : IDialogsRepository
    {
        private ICollection<Activity> _dialogs = new List<Activity>();

        public void AddDialog(Activity id)
        {
            _dialogs.Add(id);
        }

        public IEnumerable<Activity> GetAllDialogs()
        {
            return _dialogs;
        }
    }
}
