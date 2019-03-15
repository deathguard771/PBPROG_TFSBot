using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BasicBot.Dto;
using BasicBot.Dto.API;
using Microsoft.AspNetCore.Http;

namespace BasicBot.Services
{
    public class TFSApiService
    {
        private IHttpContextAccessor _httpContextAccessor;

        public TFSApiService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ChangesCollection> GetChangesAsync(CodeCheckedInRequest req)
        {
            var autorizeHeader = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();

            var match = System.Text.RegularExpressions.Regex.Match(autorizeHeader, @"([^\s]+) ([^\s]+)");

            if (!match.Success)
            {
                return null;
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(match.Groups[1].Value, match.Groups[2].Value);
                var json = await client.GetStringAsync(req.Resource.Url);
                var changesetInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<ChangesetInfo>(json);
                var changesJson = await client.GetStringAsync(changesetInfo.Links.Changes.Href);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<ChangesCollection>(changesJson);
            }
        }
    }
}
