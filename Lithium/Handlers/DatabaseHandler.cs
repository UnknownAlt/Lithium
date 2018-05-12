using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Discord;
using Lithium.Models;
using Raven.Client;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Lithium.Handlers
{
    public class DatabaseHandler
    {
        public static string DBName { get; set; } = "PassiveBOT";
        public static string ServerURL { get; set; } = "http://127.0.0.1:8080";
        public static string GuildDocName { get; set; } = "Guilds";

        public static void InitDB(IGuild guild)
        {
            using (var ds = new DocumentStore {Urls = new[] {ServerURL}}.Initialize())
            {
                using (var session = ds.OpenSession(DBName))
                {
                    var Model = new GuildModel
                    {
                        Guilds = new List<GuildModel.Guild>
                        {
                            new GuildModel.Guild
                            {
                                GuildID = guild.Id
                            }
                        }
                    };
                    session.Store(Model, GuildDocName);
                    session.SaveChanges();
                }
            }
        }

        public static GuildModel.Guild GetGuild(IGuild guild)
        {
            using (var ds = new DocumentStore { Urls = new[] { ServerURL } }.Initialize())
            {
                using (var session = ds.OpenSession(DBName))
                {
                    var dbGuilds = session.Load<GuildModel>(GuildDocName).Guilds;
                    var currentguild = dbGuilds.FirstOrDefault(x => x.GuildID == guild.Id);
                    return currentguild;
                }
            }
        }

        public static List<GuildModel.Guild> GetFullConfig()
        {
            using (var ds = new DocumentStore { Urls = new[] { ServerURL } }.Initialize())
            {
                using (var session = ds.OpenSession(DBName))
                {
                    var dbGuilds = session.Load<GuildModel>(GuildDocName);
                    return dbGuilds.Guilds;
                }
            }
        }
    }
}
