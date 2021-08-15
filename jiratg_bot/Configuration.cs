using System;

namespace jiratg_bot
{
    public class Configuration
    {
        public readonly static string BotToken = Environment.GetEnvironmentVariable("BOT_TOKEN");
    }
}
