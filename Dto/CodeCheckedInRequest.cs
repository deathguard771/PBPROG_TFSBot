﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicBot.Dto
{
    public class CodeCheckedInRequest : Request
    {
        public CodeCheckedInResource Resource { get; set; }
    }
}
