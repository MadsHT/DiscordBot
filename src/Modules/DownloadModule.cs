using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Example.Modules
{
    [Name("Fun")]
    public class DownloadModule : InteractiveBase
    {
        private readonly IConfigurationRoot _config;
        private static HttpClient http = new HttpClient();

        public DownloadModule(IConfigurationRoot config)
        {
            _config = config;
        }

        [Command("download"), Alias("dl")]
        [Summary("I Will search for 1080p movies at reputable sources")]
        [RequireUserPermission(GuildPermission.SendMessages),
        RequireBotPermission(GuildPermission.SendMessages)]
        public async Task DownloadMovie(string imdbLink)
        {
            //TODO make optional parameters
            var User = Context.User as SocketGuildUser;
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Plexer");

            if (!User.Roles.Contains(role))
            {
                await ReplyAsync("You do not have the required permission to use this command, please contact system admin");
                return;
            }

            JToken movieToken = SortList(imdbLink, _config["ImdbPath"]).Result;

            RetriveAndUploadFile(movieToken);
        }

        [Command("adminDl"), Alias("adl")]
        [Summary("Will download any link given from db")]
        [RequireContext(ContextType.DM), RequireOwner]
        public async Task AdminDownload(string search)
        {
            await ReplyAsync("starting admin dl");
            JToken movieToken = GetMovieList(_config["searchPath"], search).Result.First;

            RetriveAndUploadFile(movieToken);
        }


        public async Task<JArray> GetMovieList(string path, string search)
        {
            await ReplyAsync($"Looking for movie..");

            HttpResponseMessage response = await http.GetAsync(path + search);
            if (response.IsSuccessStatusCode)
            {
                JObject jo = JObject.Parse(await response.Content.ReadAsStringAsync());

                if (jo.First.First.HasValues)
                {
                    var movieList = JArray.FromObject(jo.First.First);

                    await ReplyAsync($"I found {movieList.Count} plausibilities, sorting list");

                    return movieList;
                }
                else
                {
                    await ReplyAsync("Nothing was found");
                    return null;
                }
            }
            else
            {
                await ReplyAsync("There was a problem with the download");
                return null;
            }
        }

        public async Task<JToken> SortList(string imdbLink, string path)
        {
            //Check the link
            if (imdbLink.Contains("https://www.imdb.com/title/") && imdbLink.Contains("tt"))
            {
                imdbLink = imdbLink.Split("title/")[1].Split("/")[0];
            }
            else
            {
                await ReplyAsync($"Given link is not from imdb");
                return null;
            }

            List<JToken> movieListTokens = (await GetMovieList(_config["imdbPath"].ToString(), imdbLink)).ToList();

            //Removes all non 1080p movies or are larger then 20GB or has no seeders
            movieListTokens.RemoveAll(token => !token["release_name"].ToString().Contains("1080p") ||
               int.Parse(token["size"].ToString()) > 20000 || int.Parse(token["seeders"].ToString()) < 1);

            if (movieListTokens.Count == 0)
            {
                await ReplyAsync("Couldn't find suitable file");
                return null;
            }

            //Order the list by seeders then by size.
            movieListTokens = movieListTokens.OrderByDescending(seed => seed["seeders"]).ThenByDescending(size => size["size"]).ToList();

            //Checks if there is any from unitail or rapidcows
            JToken suitableEntry = movieListTokens.FirstOrDefault(token =>
               token["release_name"].ToString().ToLower().Contains("uni") ||
               token["release_name"].ToString().ToLower().Contains("rapi"));

            string msg = "";

            //Sets the one of reminds me about the ratio
            if (movieListTokens.Count != 0 && suitableEntry == null)
            {
                await ReplyAsync("Found file. OBS ratio could become a problem");
                suitableEntry = movieListTokens.FirstOrDefault();
            }

            //Sends a msg with the info about the movie that it found
            if (suitableEntry != null)
            {
                msg += "Found the One: " + suitableEntry["release_name"] +
                    " Size: " + suitableEntry["size"] +
                    " Seeds: " + suitableEntry["seeders"];

                await ReplyAsync($"```{msg}```");

                //Returns the one it found.
                return suitableEntry;
            }
            else
            {
                await ReplyAsync("There was a problem with the download");
                return null;
            }
        }

        private async void RetriveAndUploadFile(JToken suitableEntry)
        {
            await ReplyAsync("downloading file");
            using (var client = new WebClient())
            {
                await client.DownloadFileTaskAsync(suitableEntry["download_url"].ToString(),
                   "./" + suitableEntry["release_name"].ToString() + ".torrent");

                await UploadFile(suitableEntry, client);
                await ReplyAsync("Upload complete");
            }
        }

        private async Task UploadFile(JToken movieToken, WebClient client)
        {
            if (movieToken != null && !client.IsBusy)
            {
                await ReplyAsync($"Starting upload to serve");

                //connnects to the ftp serve and uploads the file
                client.Credentials = new NetworkCredential(_config["ftpUser"], _config["ftpPass"]);
                client.UploadFile(
                    "ftp://" + _config["serverName"] + "/Download/torrents/" + movieToken["release_name"].ToString() +
                    ".torrent",
                    WebRequestMethods.Ftp.UploadFile,
                    "./" + movieToken["release_name"].ToString() + ".torrent");

                //Removes the file from bot storage
                System.IO.File.Delete("./" + movieToken["release_name"].ToString() + ".torrent");
            }
        }

    }
}