using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Colors.Core;

namespace Colors.Generation
{
    /// <summary>
    /// 中国色，取自 <see href="https://coolfishstudio.github.io/cfs-color/" />。
    /// </summary>
    public class ChineseColors : IPalettesProvider
    {
        private const string SourceURL = "https://coolfishstudio.github.io/cfs-color/";

        private const string ScriptRegEx = @"<script type=[^\s]* src=([^\s>]*)>";
        private const string ColorRegEx = @"{\s*name:\s*\""([^\""]*)\"",\s*hex:\s*\""([^\""]*)\""\s*}";

        public async ValueTask<IReadOnlyList<Palette>> RetrievePalettesAsync()
        {
            using var client = new HttpClient();
            var html = await client.GetStringAsync(SourceURL).ConfigureAwait(false);

            var colors = new List<Color>();
            foreach (Match match in Regex.Matches(html, ScriptRegEx))
            {
                var script = await client.GetStringAsync(SourceURL + match.Groups[1].Value).ConfigureAwait(false);
                foreach (Match color in Regex.Matches(script, ColorRegEx))
                {
                    colors.Add(new Color(color.Groups[1].Value.Trim(), color.Groups[2].Value.Trim().Substring(1)));
                }
            }

            return new List<Palette> { new Palette("中国色", colors) };
        }
    }
}
