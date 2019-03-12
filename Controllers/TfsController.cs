using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BasicBot.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using TfsBot.Common.Db;
using TfsBot.Common.Dtos;
using TfsBot.Common.Entities;

namespace BasicBot.Controllers
{
    public class TfsController : ControllerBase
    {
        private const string BotFirstLineHeaderKey = "BotFirstLine";
        private IRepository _repository;
        private SkypeHelper _skypeHelper;

        public TfsController(IRepository repository, SkypeHelper skypeHelper)
        {
            _repository = repository;
            _skypeHelper = skypeHelper;
        }

        [HttpGet]
        [Route("~/tfs/setup/{id}")]
        public async Task<IActionResult> Setup(string id)
        {
            var clients = await _repository.GetServerClients(id);
            return Ok(clients.Select(x => x.ConversationId));
        }

        [HttpGet]
        [HttpPost]
        [Route("~/tfs/commit/{id}")]
        public async Task<IActionResult> CodeCheckedIn(string id, [FromBody] CodeCheckedInRequest req)
        {
            var message = GetCodeCheckedInMessage(req);
            var clients = await _repository.GetServerClients(id);

            foreach (var client in clients)
            {
                await _skypeHelper.SendMessage(client, string.Join(Environment.NewLine, message));
            }

            return Ok();
        }

        private IEnumerable<string> GetCodeCheckedInMessage(CodeCheckedInRequest req)
        {
            var firstLine = string.Empty;
            if (ControllerContext.HttpContext.Request.Headers.TryGetValue(BotFirstLineHeaderKey, out var value))
            {
                firstLine = value.FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(firstLine))
            {
                firstLine = $"{firstLine.TrimEnd()}{Environment.NewLine}";
            }

            var baseMessage = $"{firstLine}**COMMIT {req.Resource.ChangesetId}** {req.DetailedMessage.TrimmedMarkdown}";

            var itemsMessage = req.Resource.WorkItems?.Any() == true
                ? Environment.NewLine + string.Join(Environment.NewLine, req.Resource.WorkItems.Select(x => $"[{x.Id}]({x.WebUrl}) - {x.Title}"))
                : string.Empty;

            yield return baseMessage + itemsMessage;
        }
    }
}
