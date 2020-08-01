using System;
using System.Threading.Tasks;

using Discord.Commands;

namespace TU20Bot.Commands
{
    public class Ping : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        public async Task ping()
        {
            Console.WriteLine("Ping!");

            await ReplyAsync("Pong!");
        }
    }
}
