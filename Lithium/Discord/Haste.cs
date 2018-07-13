using System;
using System.Collections.Generic;
using System.Text;

namespace Lithium.Discord
{
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class Haste
    {
        private static readonly Regex _HasteKeyRegex = new Regex(@"{""key"":""(?<key>[a-z].*)""}", RegexOptions.Compiled);

        public static async Task<string> HasteBin(string code, string fileExtension = null, bool raw = false)
        {
            using (var client = new WebClient())
            {
                if (code.Length >= 40000)
                {
                    throw new Exception("Code length must be less than 40,000 characters to upload");
                }

                var response = await client.UploadStringTaskAsync("https://hastebin.com" + "/documents", code);
                var match = _HasteKeyRegex.Match(response);

                if (!match.Success)
                {
                    Console.WriteLine(response);
                    throw new Exception();
                }

                return $"https://hastebin.com/{(raw ? "raw/" : null)}{match.Groups["key"]}{fileExtension}";
            }
        }
    }
}
