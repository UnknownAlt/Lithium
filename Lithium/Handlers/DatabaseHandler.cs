using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Lithium.Models;
using Lithium.Services;
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

        public static void CheckDB(DiscordSocketClient client)
        {
            using (var ds = new DocumentStore { Urls = new[] { ServerURL } }.Initialize())
            {
                using (var session = ds.OpenSession(DBName))
                {
                    try
                    {
                        //Check to see wether or not we can actually load the Guilds List saved in our RavenDB
                        var dbg = session.Load<GuildModel>(GuildDocName).Guilds;
                    }
                    catch
                    {
                        //In the case that the check fails, ensure we initalise all servers that contain the bot.
                        var glist = client.Guilds.Select(x => new GuildModel.Guild
                        {
                            GuildID = x.Id
                        }).ToList();
                        var Model = new GuildModel
                        {
                            Guilds = glist
                        };
                        session.Store(Model, GuildDocName);
                        session.SaveChanges();
                    }
                }
            }
        }

        public static void SaveGuild(GuildModel.Guild GuildObj)
        {
            using (var ds = new DocumentStore { Urls = new[] { ServerURL } }.Initialize())
            {
                using (var session = ds.OpenSession(DBName))
                {
                    var model = session.Load<GuildModel>(GuildDocName);
                    var gobj = model.Guilds.FirstOrDefault(x => x.GuildID == GuildObj.GuildID);
                    if (gobj == null)
                    {
                        model.Guilds.Add(GuildObj);
                    }
                    else
                    {
                        model.Guilds.Remove(gobj);
                        model.Guilds.Add(GuildObj);
                    }
                    session.Store(model, GuildDocName);
                    session.SaveChanges();
                }
            }
        }

        public async Task DatabaseCheck(DiscordSocketClient client)
        {
            if (Process.GetProcesses().FirstOrDefault(x => x.ProcessName == "Raven.Server") == null)
            {
                Logger.LogInfo("RavenDB: Server isn't running. Please make sure RavenDB is running.\nExiting ...");
                await Task.Delay(5000);
                Environment.Exit(Environment.ExitCode);
            }

            CheckDB(client);
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
