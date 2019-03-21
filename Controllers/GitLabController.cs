using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BasicBot.Dto;
using BasicBot.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Configuration;
using TfsBot.Common.Db;

namespace BasicBot.Controllers
{
    public class GitLabController : ControllerBase
    {
        private IRepository _repository;
        private SkypeHelper _skypeHelper;

        public GitLabController(IRepository repository, SkypeHelper skypeHelper)
        {
            _repository = repository;
            _skypeHelper = skypeHelper;
        }

        [HttpGet]
        [HttpPost]
        [Route("~/gitlab/push/{id}")]
        public async Task<IActionResult> CodeCheckedIn(string id, [FromBody] PushRequest req)
        {
            try
            {
                var messages = GetPushMessage(req);
                var clients = await _repository.GetServerClients(id);

                var message = string.Join("\n", messages);

                message = Regex.Replace(message, "<[^>]*>", string.Empty);

                // message = Regex.Replace(message, @"\n?\r?(\r\n){2,}", Environment.NewLine);

                foreach (var client in clients)
                {
                    await _skypeHelper.SendMessage(client, message);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                HttpContext.Response.Headers.Add("ExceptionStackTrace", new Microsoft.Extensions.Primitives.StringValues(ex.StackTrace));
                return Ok(ex.Message);
            }
        }

        private IEnumerable<string> GetPushMessage(PushRequest req)
        {
            yield return $"**PUSHED by {req.UserName} in {req.Ref}**";

            yield return Environment.NewLine;

            if (req?.Commits == null)
            {
                yield break;
            }

            foreach (var commit in req.Commits)
            {
                yield return $"**Commit {commit.ID.Substring(0, 8)}** (author {commit.Author.Name}):";

                if (commit.Added.Any())
                {
                    yield return $"_Added_";
                    foreach (var add in commit.Added)
                    {
                        yield return add;
                    }
                }

                if (commit.Modified.Any())
                {
                    yield return $"_Modified_";
                    foreach (var mod in commit.Modified)
                    {
                        yield return mod;
                    }
                }

                if (commit.Removed.Any())
                {
                    yield return $"_Removed_";
                    foreach (var rem in commit.Removed)
                    {
                        yield return rem;
                    }
                }

                yield return commit.Message?.TrimEnd();

                yield return "\n";
            }
        }
    }
}
