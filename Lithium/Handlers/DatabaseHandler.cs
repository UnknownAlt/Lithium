using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord.WebSocket;
using Lithium.Models;
using Lithium.Services;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.Backups;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace Lithium.Handlers
{
    public class DatabaseHandler
    {
        //This is out configuration for the database handler, DBName is the database you created in RavenDB when setting it up
        //ServerURL is the URL to the local server. NOTE: This bot has not been configured to use public addresses
        public static string DBName { get; set; } = Config.Load().DBName;
        public static string ServerURL { get; set; } = Config.Load().ServerURL;

        public static async void CheckDB(DiscordSocketClient client)
        {
            using (var ds = new DocumentStore {Urls = new[] {ServerURL}}.Initialize())
            {
                if (ds.Maintenance.Server.Send(new GetDatabaseNamesOperation(0, 5)).All(x => x != DBName))
                {
                    await ds.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord(DBName)));
                }

                using (var session = ds.OpenSession(DBName))
                {
                    try
                    {
                        //Check to see wether or not we can actually load the Guilds List saved in our RavenDB
                        var dbGuilds = session.Query<GuildModel.Guild>().ToList();
                    }
                    catch
                    {
                        //In the case that the check fails, ensure we initalise all servers that contain the bot.
                        var glist = client.Guilds.Select(x => new GuildModel.Guild
                        {
                            GuildID = x.Id
                        }).ToList();
                        foreach (var gobj in glist)
                        {
                            session.Store(gobj, gobj.GuildID.ToString());
                        }
                        session.SaveChanges();
                    }
                    SetDatabasebackupTask(client);
                }
            }
        }

        /// <summary>
        /// This adds a new guild to the RavenDB
        /// </summary>
        /// <param name="Id">The Server's ID</param>
        /// <param name="Name">Optionally say the server name was added to the DB</param>
        public static void AddGuild(ulong Id, string Name = null)
        {
            using (var ds = new DocumentStore {Urls = new[] {ServerURL}}.Initialize())
            {
                using (var Session = ds.OpenSession(DBName))
                {
                    if (Session.Advanced.Exists($"{Id}")) return;
                    Session.Store(new GuildModel.Guild
                    {
                        GuildID = Id
                    }, Id.ToString());
                    Session.SaveChanges();
                }
            }

            Logger.LogInfo(string.IsNullOrWhiteSpace(Name) ? $"Added Server With Id: {Id}" : $"Created Config For {Name}");
        }

        public static void RemoveGuild(ulong Id, string Name = null)
        {
            using (var ds = new DocumentStore { Urls = new[] { ServerURL } }.Initialize())
            {
                using (var Session = ds.OpenSession(DBName))
                {
                    Session.Delete($"{Id}");
                }
            }

            Logger.LogInfo(string.IsNullOrWhiteSpace(Name) ? $"Added Server With Id: {Id}" : $"Created Config For {Name}");
        }

        /// <summary>
        /// Check to see wether or not the RavenDB is running
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static async Task DatabaseCheck(DiscordSocketClient client)
        {
            if (Process.GetProcesses().FirstOrDefault(x => x.ProcessName == "Raven.Server") == null)
            {
                Logger.LogInfo("RavenDB: Server isn't running. Please make sure RavenDB is running.\nExiting ...");
                await Task.Delay(5000);
                Environment.Exit(Environment.ExitCode);
            }

            CheckDB(client);
        }

        public static void SetDatabasebackupTask(DiscordSocketClient client)
        {
            using (var Store = new DocumentStore { Urls = new[] { ServerURL } }.Initialize())
            {
                Logger.LogInfo("Setting up backup operation...");
                Store.Maintenance.ForDatabase(DBName).SendAsync(new UpdatePeriodicBackupOperation(new PeriodicBackupConfiguration
                {
                    Name = "Backup",
                    BackupType = BackupType.Backup,
                    FullBackupFrequency = "*/10 * * * *",
                    IncrementalBackupFrequency = "0 2 * * *",
                    LocalSettings = new LocalSettings { FolderPath = Path.Combine(AppContext.BaseDirectory, "setup/backups/") }
                })).ConfigureAwait(false);

                Logger.LogInfo("Finished backup operation!");
            }
        }

        public static GuildModel.Guild GetGuild(ulong Id)
        {
            using (var ds = new DocumentStore {Urls = new[] {ServerURL}}.Initialize())
            {
                using (var Session = ds.OpenSession(DBName))
                {
                    return Session.Load<GuildModel.Guild>(Id.ToString());
                }
            }
        }

        public static List<GuildModel.Guild> GetFullConfig()
        {
            using (var ds = new DocumentStore {Urls = new[] {ServerURL}}.Initialize())
            {
                using (var session = ds.OpenSession(DBName))
                {
                    List<GuildModel.Guild> dbGuilds;
                    try
                    {
                        dbGuilds = session.Query<GuildModel.Guild>().ToList();
                    }
                    catch
                    {
                        dbGuilds = new List<GuildModel.Guild>();
                    }

                    return dbGuilds;
                }
            }
        }
    }
}