using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Example.Modules
{
    [Name("Fun")]
    public class DownloadModule : InteractiveBase
    {
        private readonly IConfigurationRoot _config;
        private static HttpClient http = new HttpClient();

        private string path = "";

        public DownloadModule(IConfigurationRoot config)
        {
            _config = config;
            path = _config["DBPath"];
        }


        [Command("roll"), Alias("r")]
        [Summary("roll x number of dice of y number of sides")]
        [RequireUserPermission(ChannelPermission.SendMessages)]
        public async Task Roll(int numDice, string die)
        {
            await ReplyAsync($"Rolling {numDice} times :game_die: ....");

            string msg = "";
            await ReplyAsync(msg);
        }

        [Command("download"), Alias("dl")]
        [Summary("I Will search for 1080p movies at reputable sources")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task GetDownloadLinks(string imdbLink)
        {
            JObject jo;
            
            Regex uniRegex = new Regex(@".*1080p.WEB-DL.H.*264-UNi.*");
            Regex uniBlurRegex = new Regex(@".*BLUR.1080p.HDRip.x264-UNi.*");
            Regex rapidRegex = new Regex(@".*NORDiC.1080p.WEB-DL.H.264-RAPiDCOWS");

            if (!imdbLink.Contains("https://www.imdb.com/title/"))
            {
                await ReplyAsync($"Given link is not from imdb");
                return;
            }
            
            imdbLink = imdbLink.Split("title/")[1].Split("/")[0];

            await ReplyAsync($"Looking for movie..");

            HttpResponseMessage response = await http.GetAsync(path + imdbLink);
            if (response.IsSuccessStatusCode)
            {
                jo = JObject.Parse(await response.Content.ReadAsStringAsync());
                
                JArray ja = JArray.FromObject(jo.First.First);

                JToken toDo = null;
               
                await ReplyAsync($"I found {ja.Count} plausibilities");
                
                foreach (var token in ja)
                {
                    
                    if (uniRegex.Match(token["release_name"].ToString()).Success
                        || uniBlurRegex.Match(token["release_name"].ToString()).Success
                        || rapidRegex.Match(token["release_name"].ToString()).Success) 
                    {
                        await ReplyAsync($"Found movie");
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(token["download_url"].ToString(),
                                "./" + token["release_name"].ToString() + ".torrent");
                        }

                        toDo = token;
                        break;
                    }
                }

                if (toDo != null)
                {
                    await ReplyAsync($"Starting upload to serve");
                    using (var client = new WebClient())
                    {
                        client.Credentials = new NetworkCredential(_config["ftpUser"], _config["ftpPass"]);
                        client.UploadFile("ftp://" + _config["serverName"] + "/Download/torrents/" + toDo["release_name"].ToString() + ".torrent",
                            WebRequestMethods.Ftp.UploadFile,
                            "./" + toDo["release_name"].ToString() + ".torrent");
                    }
                    System.IO.File.Delete("./" + toDo["release_name"].ToString() + ".torrent");
                    await ReplyAsync($"upload done.");
                }
                else
                {
                    await ReplyAsync($"No file found");
                    return;
                }
            }
            else
            {
                await ReplyAsync("Couldn't find any movies with that ID");
                return;
            }
        }
    }
}