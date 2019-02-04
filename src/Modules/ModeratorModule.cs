using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Example.Modules
{
    [Name("Moderator")]
    [RequireContext(ContextType.Guild)]
    public class ModeratorModule : ModuleBase<SocketCommandContext>
    {
        [Command("kick")]
        [Summary("Kick the specified user.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Kick([Remainder]SocketGuildUser user)
        {
            await ReplyAsync($"cya {user.Mention} :wave:");
            await user.KickAsync();
        }

        [Command("Purge")]
        [Summary("Deletes all messegs")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Purge(int amount)
        {
            var atp = Math.Abs(amount);

            if (atp > 100)
            {
                await ReplyAsync("Amount must be below 100");
                return;
            }

            var messages = await this.Context.Channel.GetMessagesAsync(atp + 1).Flatten();
            messages = messages.Where(x => x.IsPinned != true);
            await this.Context.Channel.DeleteMessagesAsync(messages);
        }

        [Command("nick"), Priority(0)]
        [Summary("Change another user's nickname to the specified text")]
        [RequireUserPermission(GuildPermission.Administrator), 
         RequireUserPermission(GuildPermission.ChangeNickname), 
         RequireUserPermission(GuildPermission.ManageNicknames)]
        public async Task Nick(SocketGuildUser user, [Remainder]string name)
        {
            await user.ModifyAsync(x => x.Nickname = name);
            await ReplyAsync($"{user.Mention} I changed your name to **{name}**");
        }
    }
}
