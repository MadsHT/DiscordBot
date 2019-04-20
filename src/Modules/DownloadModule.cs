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

namespace Example.Modules {
    [Name ("Fun")]
    public class DownloadModule : InteractiveBase {
        private readonly IConfigurationRoot _config;
        private static HttpClient http = new HttpClient ();

        private string path = "";

        public DownloadModule (IConfigurationRoot config) {
            _config = config;
            path = _config["DBPath"];
        }

        [Command ("roll"), Alias ("r")]
        [Summary ("roll x number of dice of y number of sides")]
        [RequireUserPermission (ChannelPermission.SendMessages)]
        public async Task Roll (int numDice, string die) {
            await ReplyAsync ($"Rolling {numDice} times :game_die: ....");

            string msg = "";
            await ReplyAsync (msg);
        }

        [Command ("download"), Alias ("dl")]
        [Summary ("I Will search for 1080p movies at reputable sources")]
        [RequireUserPermission (GuildPermission.SendMessages)]
        public async Task GetDownloadLinks (string imdbLink) {
            //TODO make optional parameters
            var User = Context.User as SocketGuildUser;
            var role = Context.Guild.Roles.FirstOrDefault (x => x.Name == "Plexer");

            if (!User.Roles.Contains (role)) {
                await ReplyAsync (
                    "You do not have the required permission to use this command, please contact system admin");
                return;
            }

            JToken toDo = SortList (imdbLink).Result;

            if (toDo != null) {
                await ReplyAsync ($"Starting upload to serve");
                using (var client = new WebClient ()) {
                    //connnects to the ftp serve and uploads the file
                    client.Credentials = new NetworkCredential (_config["ftpUser"], _config["ftpPass"]);
                    client.UploadFile (
                        "ftp://" + _config["serverName"] + "/Download/torrents/" + toDo["release_name"].ToString () +
                        ".torrent",
                        WebRequestMethods.Ftp.UploadFile,
                        "./" + toDo["release_name"].ToString () + ".torrent");
                }

                //Removes the file from bot storage
                System.IO.File.Delete ("./" + toDo["release_name"].ToString () + ".torrent");
                await ReplyAsync ($"upload done.");
            }
        }

        public async Task<JToken> SortList (string imdbLink) {
            JObject jo;

            //Check the link
            if (imdbLink.Contains ("https://www.imdb.com/title/")) {
                imdbLink = imdbLink.Split ("title/") [1].Split ("/") [0];
            } else if (imdbLink.Contains ("tt")) {

            } else {
                await ReplyAsync ($"Given link is not from imdb");
                return null;
            }

            await ReplyAsync ($"Looking for movie..");

            //get the movie list from API
            HttpResponseMessage response = await http.GetAsync (path + imdbLink);
            if (response.IsSuccessStatusCode) {
                jo = JObject.Parse (await response.Content.ReadAsStringAsync ());

                JArray ja = null;

                if (jo.First.First.HasValues) {
                    ja = JArray.FromObject (jo.First.First);
                } else {
                    await ReplyAsync ("Nothing was found");
                    return null;
                }

                await ReplyAsync ($"I found {ja.Count} plausibilities, sorting list");

                List<JToken> tokenList = ja.ToList ();

                //Removes all non 1080p movies or are larger then 20GB or has no seeders
                tokenList.RemoveAll (token => !token["release_name"].ToString ().Contains ("1080p") ||
                    int.Parse (token["size"].ToString ()) > 20000 || int.Parse (token["seeders"].ToString ()) < 0);

                if (tokenList.Count == 0) {
                    await ReplyAsync ("Couldn't find suitable file");
                    return null;
                }

                //Order the list by seeders then by size.
                tokenList = tokenList.OrderBy (size => size["seeders"]).ThenBy (seed => seed["size"]).ToList ();

                //Checks if there is any from unitail or rapidcows
                JToken theOne = tokenList.FirstOrDefault (token =>
                    token["release_name"].ToString ().ToLower ().Contains ("uni") ||
                    token["release_name"].ToString ().ToLower ().Contains ("rapi"));

                string msg = "";

                //Sets the one of reminds me about the ratio
                if (tokenList.Count != 0 && theOne == null) {
                    await ReplyAsync ("Found file. OBS ratio could become a problem");
                    theOne = tokenList.FirstOrDefault ();
                }

                //Sends a msg with the info about the movie that it found
                if (theOne != null) {
                    msg += "Found the One: " + theOne["release_name"] +
                        " Size: " + theOne["size"] +
                        " Seeds: " + theOne["seeders"];

                    await ReplyAsync ($"```{msg}```");

                    //Downloads the file it found
                    using (var client = new WebClient ()) {
                        client.DownloadFile (theOne["download_url"].ToString (),
                            "./" + theOne["release_name"].ToString () + ".torrent");
                    }

                    //Returns the one it found.
                    return theOne;
                } else {
                    await ReplyAsync ("There was a problem with the download");
                    return null;
                }

            } else {
                await ReplyAsync ("Couldn't find any movies with that ID");
                return null;
            }
        }
    }
}