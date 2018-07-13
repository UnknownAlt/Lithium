namespace Lithium.Models
{
    using System.Collections.Generic;

    using Lithium.Handlers;

    public class PrefixDictionary
    {
        /// <summary>
        /// Gets or sets the prefix list.
        /// The format is GuildId, Prefix
        /// </summary>
        public Dictionary<ulong, string> PrefixList { get; set; } = new Dictionary<ulong, string>();

        /// <summary>
        /// The guild prefix.
        /// </summary>
        /// <param name="guildId">
        /// The guild id.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// This will be null if the guild does not have a set prefix
        /// </returns>
        public string GuildPrefix(ulong guildId)
        {
            PrefixList.TryGetValue(guildId, out var prefix);

            if (prefix == null)
            {
                prefix = DefaultPrefix;
            }

            return prefix;
        }

        public string DefaultPrefix { get; set; }

        /// <summary>
        /// Saves the Dictionary
        /// </summary>
        /// <param name="defaultPrefix">
        /// The default Prefix.
        /// </param>
        /// <returns>
        /// The <see cref="PrefixDictionary"/>.
        /// </returns>
        public static PrefixDictionary Load(string defaultPrefix)
        {
            using (var session = DatabaseHandler.Store.OpenSession())
            {
                var list = session.Load<PrefixDictionary>("PrefixList") ?? new PrefixDictionary();

                session.Dispose();
                list.DefaultPrefix = defaultPrefix;
                return list;
            }
        }

        /// <summary>
        /// Saves the GuildModel
        /// </summary>
        public void Save()
        {
            using (var session = DatabaseHandler.Store.OpenSession())
            {
                session.Store(this, "PrefixList");
                session.SaveChanges();
            }
        }
    }
}
