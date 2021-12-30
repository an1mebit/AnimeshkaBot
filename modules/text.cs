using Discord.Commands;
using System;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace AnimeshkaBot.modules
{
    public sealed class text : ModuleBase<SocketCommandContext>
    {
        [Command("hi")]
        [Alias("hello")]
        [Summary("Simple hello command")]

        public async Task Hello()
        {
            SocketUser user = Context.User;
            await ReplyAsync($"Hi {user.Mention} <3");
        }

        [Command("roll")]
        [Alias("spin")]
        [Summary("Command to get a random value in selected range")]

        public async Task Roll([Remainder] int upper = 100)
        {
            if (upper <= 0)
            {
                await ReplyAsync("Нет такой операции(");
                return;
            }

            Random random = new();
            await ReplyAsync($"Ты получил `{random.Next(upper)}/{upper}`");
        }

        [Command("flip")]
        [Alias("coin")]
        [Summary("Command to get a random side of the coin")]

        public async Task Flip()
        {
            Random random = new();
            int flip = random.Next(2);
            if (flip == 0)
            {
                await ReplyAsync("**Heads**");
            }
            else
            {
                await ReplyAsync("**Tails**");
            }
        }
    }
}
