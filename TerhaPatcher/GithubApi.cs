using Microsoft.VisualBasic.ApplicationServices;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TerhaPatcher
{
    class GithubApi
    {
        public GitHubClient GhClient;

        public ILogger logger;

        public GithubApi(ILogger logger) {
            this.logger = logger;
            //get token from local settings file
            string token = local.Default.githubToken;
            this.GhClient = new GitHubClient(new ProductHeaderValue("TerhaPatcher"));
            var tokenAuth = new Credentials(token);
            this.GhClient.Credentials = tokenAuth;
        }
        public async Task<string> GetLatestRelease()
        {
            var releases = await GhClient.Repository.Release.GetAll("Te-Rha", "TerhaPatcher");
            var latest = "";
            if (releases.Count == 0)
            {
                this.logger.Log("No releases found");
                latest = "1.0.0.0";
            }
            else
            {
                latest = releases[0].Name;
                this.logger.Log($"The latest release is {latest}");
            }
            return latest;
        }

        public async Task CreateReleaseUploadAsset(string version, string zip)
        {
            var newRelease = new NewRelease(version);
            newRelease.Name = version;
            newRelease.Body = "**This** is some *Markdown*";
            newRelease.Prerelease = false;

            var result = await GhClient.Repository.Release.Create("Te-Rha", "TerhaPatcher", newRelease);
            if (!string.IsNullOrEmpty(result.Name))
            {
                this.logger.Log($"Created release {result.Name}");
                using (var archiveContents = File.OpenRead(zip))
                {
                    var assetUpload = new ReleaseAssetUpload()
                    {
                        FileName = $"{result.Name}.zip",
                        ContentType = "application/zip",
                        RawData = archiveContents
                    };
                    await GhClient.Repository.Release.UploadAsset(result, assetUpload);
                }
            }
            
        }


    }
}
