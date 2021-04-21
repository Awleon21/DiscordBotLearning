using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;

namespace DiscordBot.Models
{
    public static class ExtensionMethods
    {
        public static async Task ClearLastMessage(this DSharpPlus.CommandsNext.CommandContext iContext)
        {
            IReadOnlyCollection<DiscordMessage> messages = await iContext.Channel.GetMessagesAsync(1);
            await iContext.Channel.DeleteMessageAsync(messages.FirstOrDefault());
        }

        public static async Task ClearMessages(this DSharpPlus.CommandsNext.CommandContext iContext, int count = 25)
        {
            IReadOnlyCollection<DiscordMessage> messages = await iContext.Channel.GetMessagesAsync(count);
            await iContext.Channel.DeleteMessagesAsync(messages);
        }
    }
}
