using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Commands
{
    public class AdministrativeCommands
    {

        [Command("listChannels")]
        [Hidden]
        [RequirePermissions(Permissions.KickMembers)]
        public async Task ListChannels(CommandContext iCommandContext)
        {
            await iCommandContext.TriggerTypingAsync();

            IReadOnlyList<DiscordChannel> Channels = iCommandContext.Guild.Channels;

            string print = string.Join(Environment.NewLine, Channels.Select(c => $"{c.Name} : {c.Id}"));

            await iCommandContext.RespondAsync(print);
        }
        
        [Command("testMembers")]
        [Hidden]
        [RequirePermissions(Permissions.KickMembers)]
        [Description("")]
        public async Task Mute(CommandContext iCommandContext)
        {
            await iCommandContext.TriggerTypingAsync();

            DiscordMember member = iCommandContext.Member;
            var allMembers = await iCommandContext.Member.Guild.GetAllMembersAsync();
            string result = "";
            List<string> AllMembers = new List<string>();

            foreach (var mem in allMembers)
            {
                AllMembers.Add(mem.ToString());
            }

            foreach (string memb in AllMembers)
            {
                result += memb + Environment.NewLine;
            }
            await iCommandContext.RespondAsync(result);
        }    
    }
}