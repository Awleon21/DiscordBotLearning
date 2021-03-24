using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using static Constants.Channels;

namespace Commands 
{
    public class GeneralCommands 
    {
        [Command("Invite")]
        public async Task Invite(CommandContext iCommandContext)
        {
            await iCommandContext.TriggerTypingAsync();

            var embed = new DiscordEmbedBuilder
            {
                Title = "Discord invite link",
                Url = "https://discord.gg/BQuG2xm",
                Color = DiscordColor.DarkGreen,
                Description = $"Don't forget to welcome our new friends {Environment.NewLine} https://discord.gg/BQuG2xm "
            };

            await iCommandContext.RespondAsync("", embed: embed);
        }    

        [Command("mute")]
        [Description("Mutes everyone in your current voice channel. This is a server mute, so you have to use the unmute function to unmute them.")]
        public async Task mute(CommandContext iCommandContext)
        {
            await iCommandContext.TriggerTypingAsync();

            DiscordGuild guild = iCommandContext.Member.Guild;

            var host = iCommandContext.Member.Username;
            var hisVoiceChannel = iCommandContext.Member.VoiceState.Channel.Id;
            var otherMembers = guild.VoiceStates.Where(vs => vs.Channel.Id == hisVoiceChannel).ToList();
            var memberNames = new HashSet<string>(otherMembers.Select(m => m.User.Username));
            var memberObjects = iCommandContext.Member.Guild.Members.Where(m => memberNames.Contains(m.Username)).ToList();
            memberObjects.ForEach(async m =>
            {
                await m.SetMuteAsync(true);
            });

            await iCommandContext.RespondAsync("Muted");
        }

        [Command("unmute")]
        [Description("Will unmute players muted by the mute function.")]
        public async Task unmute(CommandContext iCommandContext)
        {
            await iCommandContext.TriggerTypingAsync();

            DiscordGuild guild = iCommandContext.Member.Guild;
            DiscordRole role = iCommandContext.Member.Guild.Roles.Where(r => r.Name == "Dead").FirstOrDefault();
            // Who am I looking for
            // Who voice channel he is in
            var hisVoiceChannel = iCommandContext.Member.VoiceState.Channel.Id;
            // What other members are in that channel.
            var otherMembers = guild.VoiceStates.Where(vs => vs.Channel.Id == hisVoiceChannel).ToList();

            var memberNames = new HashSet<string>(otherMembers.Select(m => m.User.Username));

            var memberObjects = iCommandContext.Member.Guild.Members.Where(m => memberNames.Contains(m.Username)).ToList();
            memberObjects.ForEach(async m =>
            {
                if (!m.Roles.Contains(role))
                {
                    await m.SetMuteAsync(false);
                }
            });

            await iCommandContext.RespondAsync("Unmuted");
        }

        [Command("newGame")]
        [Description("Creates a new voice channel and puts you in it.  Must be executed from the Game Lobby voice channel.  Include the game code for your game.  for example .newGame ABCDEF")]
        public async Task createChannel(CommandContext iCommandContext,[Description("Game code so people can join")] string ChannelName)
        {
            await iCommandContext.TriggerTypingAsync();

            var Parent = iCommandContext.Guild.Channels.Where(c => c.Id == Games).FirstOrDefault();
            bool inChannel = iCommandContext?.Member?.VoiceState?.Channel != null;
            
            try
            {
                if (!inChannel)
                {
                    await iCommandContext.RespondAsync("You should be in the lobby channel if you want to create a new game channel");
                }
                else
                {
                    var newChannel = await iCommandContext.Guild.CreateChannelAsync(ChannelName, ChannelType.Voice, Parent);
                    await iCommandContext.Member.PlaceInAsync(newChannel);
                }
            }
            catch (Exception e)
            {

                throw;
            }
        }

        [Command("undead")]
        public async Task undead(CommandContext iCommandContext)
        {
            await iCommandContext.TriggerTypingAsync();

            DiscordRole role = iCommandContext.Member.Guild.Roles.Where(r => r.Name == "Dead").FirstOrDefault();
            DiscordGuild guild = iCommandContext.Member.Guild;
            // Who am I looking for
            // Who voice channel he is in
            var hisVoiceChannel = iCommandContext.Member.VoiceState.Channel.Id;
            // What other members are in that channel.
            var otherMembers = guild.VoiceStates.Where(vs => vs.Channel.Id == hisVoiceChannel).ToList();

            var memberNames = new HashSet<string>(otherMembers.Select(m => m.User.Username));

            var memberObjects = iCommandContext.Member.Guild.Members.Where(m => memberNames.Contains(m.Username)).ToList();

            memberObjects.ForEach(async m =>
            {
                await guild.RevokeRoleAsync(m, role, "new game");
            });
        }
    }
}
