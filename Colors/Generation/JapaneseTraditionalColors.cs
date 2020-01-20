using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Colors.Core;

using HtmlAgilityPack;

namespace Colors.Generation
{
    /// <summary>
    /// <see href="https://www.colordic.org/w">ColorDic.org</see> から更新された日本の伝統色の色見本。
    /// </summary>
    public class JapaneseTraditionalColors : IPalettesProvider
    {
        private const string SourceURL = "https://www.colordic.org/w";

        private static readonly Regex extractor = new Regex(@"(.*)<span>.*<\/span><br>(.*)", RegexOptions.Compiled);

        public async ValueTask<IReadOnlyList<Palette>> RetrievePalettesAsync()
        {
            using var client = new HttpClient();
            using var content = await client.GetStreamAsync(SourceURL).ConfigureAwait(false);

            var html = new HtmlDocument();
            html.Load(content);

            var colors = new List<Color>();
            var nodes = html.DocumentNode.SelectNodes(@"//td/a");
            foreach (var node in nodes)
            {
                var match = extractor.Match(node.InnerHtml);
                if (!match.Success) continue;
                colors.Add(new Color(match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim().Substring(1)));
            }

            return new List<Palette> { new Palette("日本の伝統色", colors) };
        }
    }
}
