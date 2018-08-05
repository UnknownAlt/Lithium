namespace RavenBOT.Core.Configuration.RavenDB
{
    using System.Linq;
    using System.Threading.Tasks;
    
    using Passive.Services.DatabaseService;

    using Raven.Client.ServerWide;
    using Raven.Client.ServerWide.Operations;

    using RavenBOT.Core.Configuration.LocalConfig;

    public class Management
    {
        private DatabaseService DatabaseService { get; }

        private Config Config => Initialization.GetConfig();

        public Management(DatabaseService dbService)
        {
            DatabaseService = dbService;
        }

        public Task CheckDatabaseCreationAsync()
        {
            // This creates the database
            if (DatabaseService.Store.Maintenance.Server.Send(new GetDatabaseNamesOperation(0, 20)).All(x => x != Config.DatabaseConfig.DatabaseName))
            {
                DatabaseService.Store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(Config.DatabaseConfig.DatabaseName)));
            }

            return Task.CompletedTask;
        }

        /*
        public Task CheckBackupOperation(string backupDirectory = null)
        {
            try
            {
                var record = DatabaseService.Store.Maintenance.ForDatabase(Config.DatabaseConfig.DatabaseName).Server.Send(new GetDatabaseRecordOperation(Config.DatabaseConfig.DatabaseName));
                var backup = record.PeriodicBackups.FirstOrDefault(x => x.Name == "Backup");

                if (backup == null)
                {
                    var newbackup = new PeriodicBackupConfiguration { Name = "Backup", BackupType = BackupType.Backup, FullBackupFrequency = Settings.FullBackup, IncrementalBackupFrequency = Settings.IncrementalBackup, LocalSettings = new LocalSettings { FolderPath = Settings.BackupFolder } };
                    DatabaseService.Store.Maintenance.ForDatabase(Config.DatabaseConfig.DatabaseName).Send(new UpdatePeriodicBackupOperation(newbackup));
                }
                else
                {
                    // In the case that we already have a backup operation setup, ensure that we update the backup location accordingly
                    backup.LocalSettings = new LocalSettings { FolderPath = Settings.BackupFolder };
                    Store.Maintenance.ForDatabase(Settings.Name).Send(new UpdatePeriodicBackupOperation(backup));
                }
            }
            catch
            {
                LogHandler.LogMessage("RavenDB: Failed to set Backup operation. Backups may not be saved", LogSeverity.Warning);
            }
        }
        */
    }
}
