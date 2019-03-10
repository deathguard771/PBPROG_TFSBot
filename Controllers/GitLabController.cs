using System;
using System.Collections.Generic;
using System.Linq;
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
            var messages = GetPushMessage(req);
            var clients = await _repository.GetServerClients(id);

            foreach (var client in clients)
            {
                await _skypeHelper.SendMessage(client, string.Join(Environment.NewLine, messages));
            }

            return Ok();
        }

        private IEnumerable<string> GetPushMessage(PushRequest req)
        {
            yield return $"*PUSH* by {req.UserName}";

            yield return Environment.NewLine;

            foreach (var commit in req.Commits)
            {
                yield return $"*Commit* {commit.ID.Substring(0, 8)} by {commit.Author.Name}:";

                yield return $"_Added_";
                foreach (var add in commit.Added)
                {
                    yield return $"{{code}}{add}{{code}}";
                }

                yield return $"_Modified_";
                foreach (var mod in commit.Modified)
                {
                    yield return $"{{code}}{mod}{{code}}";
                }

                yield return $"_Removed_";
                foreach (var rem in commit.Removed)
                {
                    yield return $"{{code}}{rem}{{code}}";
                }

                yield return commit.Message;

                yield return Environment.NewLine;
            }
        }
    }
}
