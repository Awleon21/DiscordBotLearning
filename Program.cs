using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

internal class Program
{
    /* This is the cancellation token we'll use to end the bot if needed(used for most async stuff). */
    private CancellationTokenSource _cts { get; set; }

    /* We'll load the app config into this when we create it a little later. */
    private IConfigurationRoot _config;

    /* These are the discord library's main classes */
    private DiscordClient _discord;
    private CommandsNextModule _commands;
    private InteractivityModule _interactivity;

    /* Use the async main to create an instance of the class and await it(async main is only available in C# 7.1 onwards). */
    static async Task Main(string[] args) => await new Program().InitBot(args);

    async Task InitBot(string[] args)
    {
        try
        {
            Console.WriteLine("[info] Welcome to my bot!");
            _cts = new CancellationTokenSource();

            // Load the config file(we'll create this shortly)
            Console.WriteLine("[info] Loading config file..");
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .Build();

            // Create the DSharpPlus client
            Console.WriteLine("[info] Creating discord client..");
            _discord = new DiscordClient(new DiscordConfiguration
            {
                Token = _config.GetValue<string>("discord:token"),
                TokenType = TokenType.Bot
            });

            _discord.GuildAvailable += Client_GuildAvailable;
            _discord.ClientErrored += Client_ClientError;
            _discord.GuildMemberUpdated += MemberUpdated_Handler;
            _discord.VoiceStateUpdated += VoiceStateUpdated_Handler;
            _discord.MessageReactionAdded += NewMemberAccept;

            // Create the interactivity module(I'll show you how to use this later on)
            _interactivity = _discord.UseInteractivity(new InteractivityConfiguration()
            {
                PaginationBehaviour = TimeoutBehaviour.Delete, // What to do when a pagination request times out
                PaginationTimeout = TimeSpan.FromSeconds(30), // How long to wait before timing out
                Timeout = TimeSpan.FromSeconds(30) // Default time to wait for interactive commands like waiting for a message or a reaction
            });

            // Build dependancies and then create the commands module.
            var deps = BuildDeps();
            _commands = _discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = _config.GetValue<string>("discord:CommandPrefix"), // Load the command prefix(what comes before the command, eg "!" or "/") from our config file
                Dependencies = deps // Pass the dependancies
            });

            // TODO: Add command loading!
            _commands.RegisterCommands<GeneralCommands>();
            _commands.RegisterCommands<AdministrativeCommands>();
            _commands.CommandExecuted += Commands_CommandExecuted;
            _commands.CommandErrored += Commands_CommandErrored;

            await RunAsync(args);
        }
        catch (Exception ex)
        {
            // This will catch any exceptions that occur during the operation/setup of your bot.

            // Feel free to replace this with what ever logging solution you'd like to use.
            // I may do a guide later on the basic logger I implemented in my most recent bot.
            Console.Error.WriteLine(ex.ToString());
        }
    }

    private Task Client_Ready(ReadyEventArgs e)
    {
        // Let's log the fat that this event occured
        e.Client.DebugLogger.LogMessage(LogLevel.Info, "SinestroBot", "Client is ready to process events.", DateTime.Now);
        return Task.CompletedTask;
    }

    async Task RunAsync(string[] args)
    {
        // Connect to discord's service
        Console.WriteLine("Connecting..");
        await _discord.ConnectAsync();
        Console.WriteLine("Connected!");

        // Keep the bot running until the cancellation token requests we stop
        while (!_cts.IsCancellationRequested)
            await Task.Delay(TimeSpan.FromMinutes(1));
    }

    private async Task NewMemberAccept(MessageReactionAddEventArgs e)
    {
        DiscordRole role = e.Channel.Guild.CurrentMember.Guild.Roles.Where(r => r.Name == "crewmate").FirstOrDefault();

        if (e.Channel.Id == Constants.Channels.da_rules)
        {
            foreach (DiscordMember member in e.Channel.Guild.Members.Where(m => m.Username == e.User.Username))
            {
                await member.GrantRoleAsync(role);
            }
            
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "sinestrobot", $"role applied to {e.User.Username}", DateTime.Now);
        }
        
    }

    private async Task MemberUpdated_Handler(GuildMemberUpdateEventArgs e)
    {

        bool isDead = e.RolesAfter.Any(r => r.Name == "Dead" && !e.RolesBefore.Any(b => r.Name == b.Name));

        if (isDead)
        {
            await e.Member.SetMuteAsync(true);
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "SinestroBot", e.Member.Username, DateTime.Now);
        }
        else if (!e.RolesAfter.Any(r => r.Name == "Dead"))
        {
            await e.Member.SetMuteAsync(false);
        }
    }

    private Task VoiceStateUpdated_Handler(VoiceStateUpdateEventArgs e)
    {
        bool userLeft = e.Channel == null;
        if (userLeft)
        {
            var targetVoiceStates = e.Guild.Members.Where(v => v.VoiceState != null && v.VoiceState.Channel != null).Select(v => v.VoiceState);
            var usedChannels = new HashSet<ulong>(targetVoiceStates.Select(m => m.Channel.Id));
            var channelsToDelete = e.Guild.Channels.Where(c => c.ParentId == Constants.Channels.Games && c.Type == DSharpPlus.ChannelType.Voice && !usedChannels.Contains(c.Id)).ToList();

            channelsToDelete.ForEach(async c =>
            {
                await c.DeleteAsync();
            });
        }
        string result = "";
        e.Client.DebugLogger.LogMessage(LogLevel.Info, "SinestroBot", e.User.Username, DateTime.Now);
        return Task.FromResult(result);
    }

    /* 
     DSharpPlus has dependancy injection for commands, this builds a list of dependancies. 
     We can then access these in our command modules.
    */
    private DependencyCollection BuildDeps()
    {
        using var deps = new DependencyCollectionBuilder();

        deps.AddInstance(_interactivity) // Add interactivity
            .AddInstance(_cts) // Add the cancellation token
            .AddInstance(_config) // Add our config
            .AddInstance(_discord); // Add the discord client

        return deps.Build();
    }

    private Task Client_GuildAvailable(GuildCreateEventArgs e)
    {
        e.Client.DebugLogger.LogMessage(LogLevel.Info, "SinestroBot", $"Guild available: {e.Guild.Name}", DateTime.Now);
        return Task.CompletedTask;
    }

    private Task Client_ClientError(ClientErrorEventArgs e)
    {
        e.Client.DebugLogger.LogMessage(LogLevel.Error, "SinestroBot", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);
        return Task.CompletedTask;
    }

    private Task Commands_CommandExecuted(CommandExecutionEventArgs e)
    {
        e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "SinestroBot", $"{e.Context.User.Username} Successfully executed '{e.Command.QualifiedName}'", DateTime.Now);
        return Task.CompletedTask;
    }

    private async Task Commands_CommandErrored(CommandErrorEventArgs e)
    {
        //Log the name of the guild sent to client
        e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "SinestroBot", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}:  {e.Exception.Message ?? "<no message>"}", DateTime.Now);
        // is error due to lack of permission?
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
}