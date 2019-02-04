using System;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Discord.Addons.Interactive;

namespace Example.Modules
{
    [Name("Fun")]
    public class FunModule : InteractiveBase
    {
        private Dictionary<string, int> diceDictionary = new Dictionary<string, int>();

        public FunModule()
        {
            diceDictionary.Add("d4", 4);
            diceDictionary.Add("d6", 6);
            diceDictionary.Add("d8", 8);
            diceDictionary.Add("d10", 10);
            diceDictionary.Add("d12", 20);
            diceDictionary.Add("d20", 20);
        }

        [Command("roll"), Alias("r")]
        [Summary("roll x number of dice of y number of sides")]
        [RequireUserPermission(ChannelPermission.SendMessages)]
        public async Task Roll(int numDice, string die)
        {
            await ReplyAsync($"Rolling d{diceDictionary[die]}, {numDice} times :game_die: ....");
            var rnd = new Random();

            var msg = $"{Context.User.Mention} Rolled: ";

            for (int i = 0; i < numDice; i++)
            {
                msg += $"{rnd.Next(1, diceDictionary[die] + 1)}, ";
            }

            msg = msg.Trim();
            msg = msg.Remove(msg.Length - 1, 1);
            msg += $" :game_die:";

            await ReplyAsync(msg);
        }

        [Command("coinFlip"), Alias("cf")]
        [Summary("Flips a coin")]
        [RequireUserPermission(ChannelPermission.SendMessages)]
        public async Task CoinFlip()
        {
            var msg = $"";

            var rnd = new Random();

            var num = rnd.Next(0, 2);

            Console.WriteLine(num);

            switch (num)
            {
                case 0:
                    msg += "Heads";
                    break;
                case 1:
                    msg += "Tailes";
                    break;
                default:
                    break;
            }

            await ReplyAsync(msg);
        }

        [Command("ping")]
        [Summary("pong")]
        [RequireUserPermission(GuildPermission.SendMessages)]
        public async Task Ping()
        {
            await ReplyAsync("pong");
        }

        //[Command("next", RunMode = RunMode.Async)]
        //public async Task Test_NextMessageAsync()
        //{
        //    await ReplyAsync("What is 2+2?");
        //    var response = await NextMessageAsync();
        //    if (response != null)
        //        await ReplyAsync($"You replied: {response.Content}");
        //    else
        //        await ReplyAsync("You did not reply before the timeout");
        //}
    }
}