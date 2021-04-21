using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Commands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class Program
{
    public readonly EventId BotEventId = new EventId(42, "Bot-Ex02");

    public DiscordClient Client { get; set; }
    public CommandsNextExtension Commands { get; set; }

    public BadwordsJson BadConfig { get; set; }

    public static void Main(string[] args)
    {
        // since we cannot make the entry method asynchronous,
        // let's pass the execution to asynchronous code
        var prog = new Program();
        prog.RunBotAsync().GetAwaiter().GetResult();
    }

    public async Task RunBotAsync()
    {
        BadConfig = await ReadConfig<BadwordsJson>("badwords.json");

        // next, let's load the values from that file
        // to our client's configuration
        var cfgjson = await ReadConfig<ConfigJson>("config.json");
        var cfg = new DiscordConfiguration
        {
            Token = cfgjson.Token,
            TokenType = TokenType.Bot,

            Intents = DiscordIntents.All,

            AutoReconnect = true,
            MinimumLogLevel = LogLevel.Debug,
        };

        this.Client = new DiscordClient(cfg);

        this.Client.Ready += this.Client_Ready;
        this.Client.GuildAvailable += this.Client_GuildAvailable;
        this.Client.ClientErrored += this.Client_ClientError;
        this.Client.GuildMemberAdded += this.NewMemberAdded;
        this.Client.GuildMemberRemoved += this.MemberRemoved;
        this.Client.UnknownEvent += this.UnknownEvent;
        this.Client.MessageCreated += this.ListenForWords;

        var ccfg = new CommandsNextConfiguration
        {
            StringPrefixes = new[] { ".", "b!" },

            EnableDms = true,

            EnableMentionPrefix = true
        };

        // and hook them up
        this.Commands = this.Client.UseCommandsNext(ccfg);

        this.Commands.CommandExecuted += this.Commands_CommandExecuted;
        this.Commands.CommandErrored += this.Commands_CommandErrored;

        this.Commands.RegisterCommands<GeneralCommands>();
        this.Commands.RegisterCommands<AdministrativeCommands>();

        await this.Client.ConnectAsync();

        // this is to prevent premature quitting
        await Task.Delay(-1);
    }

    private Task Client_Ready(DiscordClient sender, ReadyEventArgs e)
    {
        sender.Logger.LogInformation(BotEventId, "Client is ready to process events.");

        return Task.CompletedTask;
    }

    private async Task ListenForWords(DiscordClient sender, MessageCreateEventArgs e)
    {
        sender.Logger.LogInformation(BotEventId, $"{e.Author} said {e.Message.Content} : in Channel {e.Message.Channel}");

        DiscordMember member = e.Guild.Members.FirstOrDefault(m => m.Value.Username == e.Author.Username).Value;
        string role = "Jail";

        string message = e.Message.Content.ToLower();
        IEnumerable<DiscordRole> Roles = new List<DiscordRole>()
        {
          e.Guild.Roles.FirstOrDefault(r => r.Value.ToString().Contains(role)).Value
        };

        if(message.Contains("fortnite"))
        {
            var emoji = DiscordEmoji.FromName(sender, ":cop:");

            var embed = new DiscordEmbedBuilder
            {
                Title = "Please watch your language!",
                Description = $"{member.Mention} We don't talk about that here! {emoji}.",
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                {
                    Url = member.AvatarUrl,
                    Height = 100,
                    Width = 100
                },
                Color = DiscordColor.Blue //red
            };

            await Client.SendMessageAsync(e.Message.Channel, embed: embed);
        }

        if (BadConfig.Words.Any(w => message.Contains(w)))
        {
            var emoji = DiscordEmoji.FromName(sender, ":cop:");

            var embed = new DiscordEmbedBuilder
            {
                Title = "Those words aren't allowed in here!",
                Description = $"{member.Username} has been sent to jail {emoji}.",
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                {
                    Url = member.AvatarUrl,
                    Height = 100,
                    Width = 100
                },
                Color = DiscordColor.Blue //red
            };
            DiscordChannel Jail = e.Guild.GetChannel(824371798015344659);
            await Client.SendMessageAsync(e.Message.Channel, embed: embed);
            await Client.SendMessageAsync(Jail, $"{e.Author.Mention} has been jailed for the saying the following in {e.Message.Channel.Name}. {Environment.NewLine} {e.Message.Content}");
            await member.ReplaceRolesAsync(Roles);
        }
    }

    
    private async Task UnknownEvent(DiscordClient sender, UnknownEventArgs e)
    {
        switch (e.EventName)
        {
            case "GUILD_JOIN_REQUEST_DELETE":
                await GuildJoinRequestDelted(sender, e);
                break;
            default:
                break;
        }
    }
    private async Task GuildJoinRequestDelted(DiscordClient sender, UnknownEventArgs e)
    {
        sender.Logger.LogInformation(e.Json);
        //DiscordChannel channel = await Client.GetChannelAsync(Constants.Channels.general);

        //var emoji = DiscordEmoji.FromName(sender, ":cry:");

        //var embed = new DiscordEmbedBuilder
        //{
        //    Title = "Access Denied",
        //    Description = $"{e.Member.Username} has left {emoji}.",
        //    Color = DiscordColor.Gray //red
        //};

        //await Client.SendMessageAsync(channel, $" {e.Member.Mention} left!  We hope they enjoyed their stay!");
        await Task.CompletedTask;
    }

    private async Task NewMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
    {
        sender.Logger.LogInformation(BotEventId, $"{e.Member.Username} has Joined {e.Member.Guild}!");
        DiscordChannel channel = await Client.GetChannelAsync(Constants.Channels.general);
        //await Client.SendMessageAsync(channel, $"Welcome {e.Member.Mention}!  We hope you enjoy your stay!");

        var emoji = DiscordEmoji.FromName(sender, ":smiley:");

        var embed = new DiscordEmbedBuilder
        {
            Title = "Welcome!",
            Description = $"{e.Member.Username} has Joined {emoji}.",
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
            {
                Url = e.Member.AvatarUrl,
                Height = 100,
                Width = 100
            },
            Color = DiscordColor.Green //red
        };

        //await Client.SendMessageAsync(channel, $" {e.Member.Mention} left!  We hope they enjoyed their stay!");
        await Client.SendMessageAsync(channel, embed: embed);
    }

    private async Task MemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs e)
    {
        sender.Logger.LogInformation(BotEventId, $"{e.Member.Username} has Left {e.Member.Guild}!");
        DiscordChannel channel = await Client.GetChannelAsync(Constants.Channels.general);

        var emoji = DiscordEmoji.FromName(sender, ":cry:");

        var embed = new DiscordEmbedBuilder
        {
            Title = "Someone left us!",
            Description = $"{e.Member.Username} has left {emoji}.",
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
            {
                Url = e.Member.AvatarUrl,
                Height = 100,
                Width = 100
            },
            Color = DiscordColor.Gray //red
        };

        //await Client.SendMessageAsync(channel, $" {e.Member.Mention} left!  We hope they enjoyed their stay!");
        await Client.SendMessageAsync(channel, embed: embed);
    }
    private Task Client_GuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        sender.Logger.LogInformation(BotEventId, $"Guild available: {e.Guild.Name}");

        return Task.CompletedTask;
    }

    private Task Client_ClientError(DiscordClient sender, ClientErrorEventArgs e)
    {
        sender.Logger.LogError(BotEventId, e.Exception, "Exception occured");

        return Task.CompletedTask;
    }

    private Task Commands_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
    {
        e.Context.Client.Logger.LogInformation(BotEventId, $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'");

        return Task.CompletedTask;
    }

    private async Task Commands_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
    {
        e.Context.Client.Logger.LogInformation(BotEventId, $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}:  {e.Exception.Message ?? "<no message>"}", DateTime.Now);

        if (e.Exception is ChecksFailedException ex)
        {
            //yes, the user lacks required permission, let them know
            var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

            var embed = new DiscordEmbedBuilder
            {
                Title = "Access Denied",
                Description = $"{emoji} You do not have the permissions required to execute this command.",
                Color = DiscordColor.Red //red
            };
            await e.Context.RespondAsync("", embed: embed);
        }
        else if (e.Exception is CommandNotFoundException)
        {
            var emoji = DiscordEmoji.FromName(e.Context.Client, ":interrobang:");

            var embed = new DiscordEmbedBuilder
            {
                Title = "Command Not Found",
                Description = $"Could not find the command you tried to execute.  Please check your spelling and try again. {Environment.NewLine} use .help to see the list of available commands",
                Color = DiscordColor.Red
            };
            await e.Context.RespondAsync("", embed: embed);
        }
    }

    private async Task AssignRole(DiscordClient client, DiscordMember member, DiscordChannel channel, DiscordGuild guild, string role)
    {
        DiscordRole Role = guild.Roles.FirstOrDefault(r => r.Value.ToString() == role).Value;
        await member.GrantRoleAsync(Role);

        var emoji = DiscordEmoji.FromName(client, ":cop:");

        var embed = new DiscordEmbedBuilder
        {
            Title = "Those words aren't allowed in here!",
            Description = $"{member.Username} has been sent to jail {emoji}.",
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
            {
                Url = member.AvatarUrl,
                Height = 100,
                Width = 100
            },
            Color = DiscordColor.Blue //red
        };
    }

    private static async Task<T> ReadConfig<T>(string filename)
    {
        var json = "";
        using (var fs = File.OpenRead(filename))
        using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
            json = await sr.ReadToEndAsync();
        return JsonConvert.DeserializeObject<T>(json);
    }
    private static async Task WriteConfig<T>(T Config, string filename)
    {
        var json = JsonConvert.SerializeObject(Config);
        File.WriteAllText(filename, json);
    }

    public struct ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set; }
    }

    public struct BadwordsJson
    {
        [JsonProperty("badwords")]
        public List<string> Words { get; private set; }
    }
}

