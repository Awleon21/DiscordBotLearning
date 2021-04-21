using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Models;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using static Constants.Channels;
using Imgur.API;

namespace Commands 
{
    public class GeneralCommands : BaseCommandModule
    {
        [Command("Invite")]
        [RequirePrefixes(".")]
        public async Task Invite(CommandContext iCommandContext)
        {
            await iCommandContext.ClearLastMessage();
            await iCommandContext.TriggerTypingAsync();

            var embed = new DiscordEmbedBuilder
            {
                Title = "Discord invite link",
                Url = "https://discord.gg/P7pJ8FFs",
                Color = DiscordColor.DarkGreen,
                Description = $"Don't forget to welcome our new friends {Environment.NewLine} https://discord.gg/P7pJ8FFs"
            };

            await iCommandContext.RespondAsync("", embed: embed);
        }    

   
        [Command("Jail")]
        [RequirePermissions(Permissions.ManageRoles)]
        [Description("type \".Jail username\" to assign the jail role to a member.")]
        public async Task AssignJail(CommandContext iCommandContext, string Username)
        {

            //DiscordRole role = iCommandContext.Channel.Guild.CurrentMember.Guild.Roles.Where(r => r.Value.Name == "Jail").Select(r => r.Value).FirstOrDefault();
            string role = "Jail";

            IEnumerable<DiscordRole> Roles = new List<DiscordRole>()
            {
                iCommandContext.Guild.Roles.FirstOrDefault(r => r.Value.ToString().Contains(role)).Value
            };

            DiscordMember MyMember = null;
            foreach (var mem in iCommandContext.Guild.Members)
            {
                if (mem.Value.Username == Username)
                {
                    MyMember = mem.Value;
                }
            }

            if(MyMember != null)
            {
                await MyMember.ReplaceRolesAsync(Roles);
            }
            else
            {
                await iCommandContext.RespondAsync("Member not found");
            }

            //var allMembers = await iCommandContext.Member.Guild.GetAllMembersAsync();
            //List<DiscordMember> AllMembers = new List<DiscordMember>();

            //foreach (var mem in allMembers)
            //{
            //    AllMembers.Add(mem);
            //}

            //foreach (DiscordMember memb in AllMembers)
            //{
            //    if(memb.Username == Jailed)
            //    {
            //       await memb.GrantRoleAsync(role);
            //    }
            //}
        }

        [Command("Pardon")]
        [RequirePermissions(Permissions.ManageRoles)]
        public async Task PardonMember(CommandContext iCommandContext, string Username)
        {
            DiscordMember MyMember = null;
            foreach(var mem in iCommandContext.Guild.Members)
            {
                if(mem.Value.Username == Username)
                {
                    MyMember = mem.Value;
                }
            }

            string role = "Budgie Initiate";

            DiscordRole Role = iCommandContext.Guild.Roles.FirstOrDefault(r => r.Value.Name == role).Value;
            DiscordRole Jail = iCommandContext.Guild.Roles.FirstOrDefault(r => r.Value.Name == "Jail").Value;
            await MyMember.GrantRoleAsync(Role);
            await MyMember.RevokeRoleAsync(Jail);
        }

        [Command("b")]
        [Aliases("budgie", "budgies", "randombudgie")]
        [RequirePrefixes(".", "b!")]
        [Description("retrieves a random post fro r/budgies")]
        public async Task GetPost(CommandContext iCommandContext)
        {
            await iCommandContext.ClearLastMessage();

            await iCommandContext.RespondAsync($"You got it {iCommandContext.Member.Mention}!  One budgie picture coming right up!");

            var reddit = new RedditSharp.Reddit();
            var subreddit = await reddit.GetSubredditAsync("budgies");
            var posts = subreddit.GetPosts();


            string url = null;
            List<string> urls = new List<string>();

           await foreach(var post in posts.Take(100))
            {
                if(!(post.Url.ToString().Contains("v.redd.it") || post.Url.ToString().Contains("gallery") || post.Url.ToString().Contains("comments")))
                {
                    urls.Add(post.Url.ToString());
                }
            }

            url = urls[RandomNumberGenerator.NumberBetween(1, urls.Count)];
            
            await iCommandContext.RespondAsync(url);
        }

        [Command("Clear")]
        [Hidden]
        public async Task ClearMessages(CommandContext iCommandContext, int count)
        {
            int num = count + 1;
            await iCommandContext.TriggerTypingAsync();
            await iCommandContext.ClearMessages(num);
        }
        //[Command("mute")]
        //[Description("Mutes everyone in your current voice channel. This is a server mute, so you have to use the unmute function to unmute them.")]
        //public async Task mute(CommandContext iCommandContext)
        //{
        //    await iCommandContext.TriggerTypingAsync();

        //    DiscordGuild guild = iCommandContext.Member.Guild;

        //    var host = iCommandContext.Member.Username;
        //    var hisVoiceChannel = iCommandContext.Member.VoiceState.Channel.Id;
        //    var otherMembers = guild.VoiceStates.Where(vs => vs.Channel.Id == hisVoiceChannel).ToList();
        //    var memberNames = new HashSet<string>(otherMembers.Select(m => m.User.Username));
        //    var memberObjects = iCommandContext.Member.Guild.Members.Where(m => memberNames.Contains(m.Username)).ToList();
        //    memberObjects.ForEach(async m =>
        //    {
        //        await m.SetMuteAsync(true);
        //    });

        //    await iCommandContext.RespondAsync("Muted");
        //}

        //[Command("unmute")]
        //[Description("Will unmute players muted by the mute function.")]
        //public async Task unmute(CommandContext iCommandContext)
        //{
        //    await iCommandContext.TriggerTypingAsync();

        //    DiscordGuild guild = iCommandContext.Member.Guild;
        //    DiscordRole role = iCommandContext.Member.Guild.Roles.Where(r => r.Name == "Dead").FirstOrDefault();
        //    // Who am I looking for
        //    // Who voice channel he is in
        //    var hisVoiceChannel = iCommandContext.Member.VoiceState.Channel.Id;
        //    // What other members are in that channel.
        //    var otherMembers = guild.VoiceStates.Where(vs => vs.Channel.Id == hisVoiceChannel).ToList();

        //    var memberNames = new HashSet<string>(otherMembers.Select(m => m.User.Username));

        //    var memberObjects = iCommandContext.Member.Guild.Members.Where(m => memberNames.Contains(m.Username)).ToList();
        //    memberObjects.ForEach(async m =>
        //    {
        //        if (!m.Roles.Contains(role))
        //        {
        //            await m.SetMuteAsync(false);
        //        }
        //    });

        //    await iCommandContext.RespondAsync("Unmuted");
        //}

        //[Command("newGame")]
        //[Description("Creates a new voice channel and puts you in it.  Must be executed from the Game Lobby voice channel.  Include the game code for your game.  for example .newGame ABCDEF")]
        ////public async Task createChannel(CommandContext iCommandContext,[Description("Game code so people can join")] string ChannelName)
        //{
        //    await iCommandContext.TriggerTypingAsync();

        //    var Parent = iCommandContext.Guild.Channels.Where(c => c.Id == Games).FirstOrDefault();
        //    bool inChannel = iCommandContext?.Member?.VoiceState?.Channel != null;
            
        //    try
        //    {
        //        if (!inChannel)
        //        {
        //            await iCommandContext.RespondAsync("You should be in the lobby channel if you want to create a new game channel");
        //        }
        //        else
        //        {
        //            var newChannel = await iCommandContext.Guild.CreateChannelAsync(ChannelName, ChannelType.Voice, Parent);
        //            await iCommandContext.Member.PlaceInAsync(newChannel);
        //        }
        //    }
        //    catch (Exception e)
        //    {

        //        throw;
        //    }
        //}

        //[Command("undead")]
        //public async Task undead(CommandContext iCommandContext)
        //{
        //    await iCommandContext.TriggerTypingAsync();

        //    DiscordRole role = iCommandContext.Member.Guild.Roles.Where(r => r.Name == "Dead").FirstOrDefault();
        //    DiscordGuild guild = iCommandContext.Member.Guild;
        //    // Who am I looking for
        //    // Who voice channel he is in
        //    var hisVoiceChannel = iCommandContext.Member.VoiceState.Channel.Id;
        //    // What other members are in that channel.
        //    var otherMembers = guild.VoiceStates.Where(vs => vs.Channel.Id == hisVoiceChannel).ToList();

        //    var memberNames = new HashSet<string>(otherMembers.Select(m => m.User.Username));

        //    var memberObjects = iCommandContext.Member.Guild.Members.Where(m => memberNames.Contains(m.Username)).ToList();

        //    memberObjects.ForEach(async m =>
        //    {
        //        await guild.RevokeRoleAsync(m, role, "new game");
        //    });
        //}
    }
}
