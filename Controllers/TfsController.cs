using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BasicBot.Controllers
{
    public class TfsController : ControllerBase
    {
        [HttpGet]
        [Route("~/tfs/setup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Setup()
        {
            return Ok("Your bunny wrote");
        }
    }
}
