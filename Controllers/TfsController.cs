using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BasicBot.Dto;
using BasicBot.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using TfsBot.Common.Db;

namespace BasicBot.Controllers
{
    public class TfsController : ControllerBase
    {
        private const string BotFirstLineHeaderKey = "BotFirstLine";
        private IRepository _repository;
        private SkypeHelper _skypeHelper;
        private string[] _statesToReport = new[]
        {
            "Done", "Removed",
        };

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
            await SendToAllClients(id, message);

            return Ok();
        }

        [HttpGet]
        [HttpPost]
        [Route("~/tfs/itemupdate/{id}")]
        public async Task<IActionResult> ItemStateChanged(string id, [FromBody] ItemUpdatedRequest request)
        {
            var shouldReturn = request.Resource == null
                || !request.Resource.Fields.TryGetValue("System.State", out var field)
                || !(field.NewValue is string newState)
                || !_statesToReport.Contains(newState);

            if (shouldReturn)
            {
                return Ok();
            }

            await SendToAllClients(id, $"{request.DetailedMessage.TrimmedMarkdown}");

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

        private async Task SendToAllClients(string id, IEnumerable<string> message)
        {
            await SendToAllClients(id, string.Join(Environment.NewLine, message));
        }

        private async Task SendToAllClients(string id, string message)
        {
            var clients = await _repository.GetServerClients(id);

            foreach (var client in clients)
            {
                await _skypeHelper.SendMessage(client, message);
            }
        }
    }
}
