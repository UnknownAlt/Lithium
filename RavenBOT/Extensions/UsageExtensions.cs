namespace RavenBOT.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;

    using RavenBOT.Core.Bot.Context;

    public static class UsageExtensions
    {
        /// <summary>
        /// Replaces key parts of text based off Custom context
        /// </summary>
        /// <param name="input">
        /// The input.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string DoReplacements(this string input, Context context)
        {
            var result = input;
            if (!string.IsNullOrEmpty(input))
            {
                result = Regex.Replace(input, "{user}", context.User.Username, RegexOptions.IgnoreCase);
                result = Regex.Replace(result, "{user.mention}", context.User.Mention, RegexOptions.IgnoreCase);
                result = Regex.Replace(result, "{guild}", context.Guild.Name, RegexOptions.IgnoreCase);
                result = Regex.Replace(result, "{channel}", context.Channel.Name, RegexOptions.IgnoreCase);
                result = Regex.Replace(result, "{channel.mention}", ((SocketTextChannel)context.Channel).Mention, RegexOptions.IgnoreCase);
            }

            return result;
        }

        /// <summary>
        /// Shortens a string to the specified length if it is too long.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="length">
        /// The length.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string FixLength(this string message, int length = 1024)
        {
            if (message.Length > length)
            {
                message = message.Substring(0, length - 4) + "...";
            }

            return message;
        }

        /// <summary>
        ///     Split a list into a group of lists of a specified size.
        /// </summary>
        /// <typeparam name="T">Type of item held within the list</typeparam>
        /// <param name="fullList">Input list</param>
        /// <param name="groupSize">Size of Groups to output</param>
        /// <returns>A list of lists of the specified size</returns>
        public static List<List<T>> SplitList<T>(this List<T> fullList, int groupSize)
        {
            var splitList = new List<List<T>>();
            for (var i = 0; i < fullList.Count; i += groupSize)
            {
                splitList.Add(fullList.Skip(i).Take(groupSize).ToList());
            }

            return splitList;
        }

        /// <summary>
        /// The get description.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string GetDescription(this Enum value)
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            if (name != null)
            {
                var field = type.GetField(name);
                if (field != null)
                {
                    if (Attribute.GetCustomAttribute(field, 
                            typeof(DescriptionAttribute)) is DescriptionAttribute attr)
                    {
                        return attr.Description;
                    }
                }
            }

            return $"{value}";
        }

        /// <summary>
        /// Returns true if the current string contains the given string
        /// </summary>
        /// <param name="source"></param>
        /// <param name="toCheck"></param>
        /// <param name="comp"></param>
        /// <returns></returns>
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        public static EmbedBuilder GenerateErrorEmbed(this IResult result, CommandService commandService, Context context, int argPos)
        {
            string errorMessage;
            if (result.Error == CommandError.UnknownCommand)
            {
                errorMessage = "**Command:** N/A";
            }
            else
            {
                // Search the commandservice based on the Message, then respond accordingly with information about the command.
                var search = commandService.Search(context, argPos);
                var cmd = search.Commands.FirstOrDefault();

                errorMessage = $"**Command Name:** `{cmd.Command.Name}`\n" +
                               $"**Summary:** `{cmd.Command?.Summary ?? "N/A"}`\n" +
                               $"**Remarks:** `{cmd.Command?.Remarks ?? "N/A"}`\n" +
                               $"**Aliases:** {(cmd.Command.Aliases.Any() ? string.Join(" ", cmd.Command.Aliases.Select(x => $"`{x}`")) : "N/A")}\n" +
                               $"**Parameters:** {(cmd.Command.Parameters.Any() ? string.Join(" ", cmd.Command.Parameters.Select(x => x.IsOptional ? $" `<(Optional){x.Name}>` " : $" `<{x.Name}>` ")) : "N/A")}\n" +
                               "**Error Reason**\n" +
                               $"{result.ErrorReason}";
            }

            return new EmbedBuilder { Title = "ERROR", Description = errorMessage };
        }
    }
}