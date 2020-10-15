﻿using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Kebler.Services
{
    public static class UpdaterApi
    {
        private static JObject latestReleaseJson;

        public static async Task<(bool, Version)> CheckAsync(string user, string repository, Version currentVersion)
        {
            try
            {
                var gitHub = new GitHubApi();
                latestReleaseJson = await gitHub.GetLatestReleaseJSONAsync(user, repository);
                var version = GitHubApi.ExtractVersion(latestReleaseJson);

                return (currentVersion < version, version);
            }
            catch
            {
                return (false, new Version());
            }
        }

        public static Task<Tuple<bool, Version>> Check(string user, string repository, Version currentVersion)
        {
            return Task.Run(async ()=>
            {
                try
                {
                    var gitHub = new GitHubApi();
                    latestReleaseJson = await gitHub.GetLatestReleaseJSONAsync(user, repository);
                    var version = GitHubApi.ExtractVersion(latestReleaseJson);

                    return new Tuple<bool, Version>(currentVersion < version, version);
                }
                catch (Exception ex)
                {
                    return new Tuple<bool, Version>(false, new Version());
                }
            });
           
        }

        public static string GetlatestUri()
        {
            var updateUrl = GitHubApi.ExtractDownloadUrl(latestReleaseJson);
            return updateUrl;
        }
    }
}