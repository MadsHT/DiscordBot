using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Timers;
using Discord.Addons.Interactive;
using Discord.Rest;

namespace Example.Modules
{
    [Name("Utility")]
    public class UtilityModule : ModuleBase<SocketCommandContext>
    {

        private enum operatorEnum
        {
            add,
            sub,
            divide,
            multi
        }

        [Command("say"), Alias("s")]
        [Summary("Make the bot say something")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public Task Say([Remainder]string text)
            => ReplyAsync(text);


        [Command("SelfDestructMsg"), Alias("SD")]
        [Summary("Message self destruct after a given time")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DeleteOOC(int delay, string unit, [Remainder]string msg)
        {
            switch (unit)
            {
                case "min":
                case "m":
                    delay = delay * 60000;
                    break;
                case "hour":
                case "h":
                    delay = delay * 3600000;
                    break;
                default:
                    await ReplyAsync("couldn't conv to a time unit");
                    return;
            }

            await Context.Message.DeleteAsync();
            var botMsg = await ReplyAsync(msg);
            await Task.Delay(delay);
            await botMsg.DeleteAsync();
        }

        [Command("Pin"), Alias("p")]
        [Summary("Pinnes the message")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task Pin([Remainder]string msg)
        {
            var botMsg = msg;

            botMsg = botMsg.Replace("pin", "");
            botMsg = botMsg.Replace("p", "");

            botMsg += $" [Pinned by {Context.User.Mention}]";

            var botMessage = await Context.Channel.SendMessageAsync(botMsg);

            await botMessage.PinAsync();

            var messages = await this.Context.Channel.GetMessagesAsync(3 + 1).Flatten();
            messages = messages.Where(x => x.IsPinned != true);
            await this.Context.Channel.DeleteMessagesAsync(messages);
        }

        [Command("Calculator"), Alias("calc")]
        [Summary("Does maths \nop being fx. '+'" +
                                         "\n'-'" +
                                         "\n'/'" +
                                         "\n'*'" +
                                         "\n'^'" +
                                         "\n'sqrt'")]
        [RequireUserPermission(ChannelPermission.SendMessages)]
        public async Task Calc(float num1, string op, float num2=0)
        {
            switch (op.ToLower())
            {
                case "+":
                    await ReplyAsync($"{num1} + {num2} = {num1 + num2}");
                    break;
                case "-":
                    await ReplyAsync($"{num1} - {num2} = {num1 - num2}");
                    break;
                case "/":
                    await ReplyAsync($"{num1} / {num2} = {num1 / num2}");
                    break;
                case "*":
                    await ReplyAsync($"{num1} * {num2} = {num1 * num2}");
                    break;
                case "%":
                    await ReplyAsync($"{num1} % {num2} = {num1 % num2}");
                    break;
                case "^":
                    await ReplyAsync($"{num1} ^ {num2} = {Math.Pow(num1, num2)}");
                    break;
                case "sqrt":
                    await ReplyAsync($"sqrt of {num1} = {Math.Sqrt(num1)}");
                    break;

            }
        }

        [Command("Timer"), Alias("Countdown")]
        [Summary("Makes a countdown\n" +
                 "add min/m for minutes\n" +
                 "add hour/h for hours")]
        [RequireUserPermission(ChannelPermission.SendMessages)]
        public async Task Timer(int time, string unit)
        {
            Timer aTimer = new Timer();
            var counter = 0;
            switch (unit)
            {
                case "min":
                case "m":
                    counter = time * 60000;
                    aTimer.Interval = 60000;
                    unit = "min";
                    break;
                case "hour":
                case "h":
                    counter = time * 3600000;
                    aTimer.Interval = 3600000;
                    unit = "hour";
                    break;
                default:
                    await ReplyAsync("couldn't conv to a time unit");
                    return;
            }

            var botMessage = await ReplyAsync($"Counting down: {time} {unit} left");

            aTimer.Elapsed += new ElapsedEventHandler((sender, args) =>
            {
                if (time == 0)
                {
                    aTimer.Stop();

                    Task.Delay(10000);
                    var messages = new List<IMessage>()
                    {
                        botMessage,
                        Context.Message
                    };
                    Context.Channel.DeleteMessagesAsync(messages);
                }
                else
                {
                    time--;
                    botMessage.ModifyAsync(x => x.Content = $"Counting down: {time} {unit} left");
                }
            });
            aTimer.Start();
        }
    }
}
