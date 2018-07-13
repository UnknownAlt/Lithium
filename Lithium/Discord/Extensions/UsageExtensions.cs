namespace Lithium.Discord.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text.RegularExpressions;

    using global::Discord.WebSocket;

    using Lithium.Discord.Context;

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
    }
}