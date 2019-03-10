using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BasicBot.Dialogs.Setup
{
    public class SetupState
    {
        public string Command { get; internal set; }

        public string ServerID { get; set; }

        public bool AskForRepeat { get; set; }
    }
}
