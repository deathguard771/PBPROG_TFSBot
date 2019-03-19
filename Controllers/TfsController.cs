using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BasicBot.Dto;
using BasicBot.Dto.API;
using BasicBot.Infrastructure;
using BasicBot.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using TfsBot.Common.Db;

namespace BasicBot.Controllers
{
    public class TfsController : ControllerBase
    {
        private const string BotFirstLineHeaderKey = "BotFirstLine";
        private IRepository _repository;
        private SkypeHelper _skypeHelper;
        private TFSApiService _apiService;

        public TfsController(IRepository repository, SkypeHelper skypeHelper, TFSApiService apiService)
        {
            _repository = repository;
            _skypeHelper = skypeHelper;
            _apiService = apiService;
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
        public async Task<IActionResult> CodeCheckedIn(string id, [FromBody] CodeCheckedInRequest req, [FromHeader] string branches)
        {
            IDictionary<string, string> branchesDictionary = null;
            try
            {
                branchesDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<IDictionary<string, string>>(branches);
            }
            catch
            {
                // ignore
            }
            var message = await GetCodeCheckedInMessage(req, branchesDictionary);
            await SendToAllClients(id, message);
            return Ok();
        }

        [HttpGet]
        [HttpPost]
        [Route("~/tfs/itemupdate/{id}")]
        public async Task<IActionResult> ItemStateChanged(string id, [FromBody] ItemUpdatedRequest request, [FromHeader] string[] states, [FromQuery] bool withChangeset = false)
        {
            if (!request.Resource.Fields.TryGetValue("System.State", out var field))
            {
                return Ok("State doesn't updated.");
            }

            if (!(field.NewValue is string newState))
            {
                return Ok("New value isn't string.");
            }

            if (states != null && states.Length > 0 && !states.Contains(newState))
            {
                return Ok($"States from header doesn't contains {newState}.");
            }

            var isUpdatedWithChangeset = request.Resource.Relations?.Added?.Any(x => x.Attributes != null && x.Attributes.TryGetValue("name", out var name) && name == "Fixed in Changeset") == true;

            if (!withChangeset && isUpdatedWithChangeset)
            {
                return Ok($"Item updated with change set and parameter {nameof(withChangeset)} set to false.");
            }

            await SendToAllClients(id, $"{request.DetailedMessage.TrimmedMarkdown}");

            return Ok();
        }

        private async Task<string> GetCodeCheckedInMessage(CodeCheckedInRequest req, IDictionary<string, string> branches)
        {
            var messageParts = new List<string>
            {
                HttpContext.Request.Headers[BotFirstLineHeaderKey].ToString()?.TrimEnd(),
            };

            // строка с комитом и комментарием
            var url = System.Text.RegularExpressions.Regex.Match(req.Message.Markdown, $@"(?<=\[{req.Resource.ChangesetId}\]\()[^)]+").Value;
            var comment = !string.IsNullOrWhiteSpace(req.Resource.Comment)
                ? req.Resource.Comment
                : $"Я МОГУ СЕБЕ ПОЗВОЛИТЬ ВОЗВРАЩАТЬ БЕЗ КОММЕНТАРИЕВ, ЯСНО?! © {req.Resource.Author.DisplayName} (poop) (facepalm)";
            var changesetLink = !string.IsNullOrWhiteSpace(url) ? $"[{req.Resource.ChangesetId}]({url})" : req.Resource.ChangesetId;
            messageParts.Add($"**COMMIT {changesetLink}** {req.Resource.Author.DisplayName} вернул набор изменений с комментарием: {comment}");

            try
            {
                // список измененных файлов
                var changes = await _apiService.GetChangesAsync(req);
                if (changes?.Collection != null)
                {
                    var branchGroups = changes.Collection
                        .GroupBy(x => x?.Info?.BranchName)
                        .OrderByDescending(x => branches?.ContainsKey(x.Key));

                    foreach (var group in branchGroups)
                    {
                        var header = branches.TryGetValue(group.Key, out var alias) ? alias : group.Key;
                        messageParts.Add($"**{header}**");
                        messageParts.AddRange(group.Select(x => $"{x?.Info?.Path} ({x.ChangeType})"));
                    }
                }
            }
            catch
            {
                // ignore
            }

            var itemsMessage = req.Resource.WorkItems?.Any() == true
                ? string.Join(Environment.NewLine, req.Resource.WorkItems.Select(x => $"[{x.Id}]({x.WebUrl}) - {x.Title} ({x.State})"))
                : string.Empty;

            messageParts.Add(itemsMessage);

            return string.Join(Environment.NewLine, messageParts.Where(x => !string.IsNullOrEmpty(x)));
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
                try
                {
                    await _skypeHelper.SendMessage(client, message);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}
