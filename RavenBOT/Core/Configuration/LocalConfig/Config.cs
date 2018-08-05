namespace RavenBOT.Core.Configuration.LocalConfig
{
    using Discord;
    
    using Passive.Services.DatabaseService;

    public class Config
    {
        public LogSeverity LogLevel { get; set; } = LogSeverity.Info;

        public DatabaseConfig DatabaseConfig { get; set; } = new DatabaseConfig();

        public string DefaultPrefix { get; set; } = "+";
    }
}
