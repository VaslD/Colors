using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

using Colors.Core;

using HtmlAgilityPack;

using Newtonsoft.Json.Linq;

namespace Colors
{
    /// <summary>
    /// Google's Material Design color palettes.
    /// Updated from <see href="https://www.materialpalette.com/colors">Material Palette</see>.
    /// </summary>
    public class MaterialDesignColors : IPalettesProvider
    {
        private const string SourceURL = "https://www.materialpalette.com/colors";

        public async Task<IReadOnlyList<Palette>> DownloadOnlinePalettes()
        {
            var client = new HttpClient();
            var response = await client.GetAsync(SourceURL).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var html = new HtmlDocument();
            html.Load(contentStream);

            var encoded = html.DocumentNode.SelectSingleNode(@"//div[@data-react-class='Colors']").Attributes["data-react-props"].Value;
            var colorsList = JToken.Parse(HttpUtility.HtmlDecode(encoded))["colors"].Select(x => {
                var colors = x["shades"].Select(y => new Color(y["strength"].ToString(), y["hex"].ToString().Substring(1))).ToList();
                return new Palette(x["name"].ToString().ToUpperInvariant(), colors);
            }).ToList();
            return colorsList.AsReadOnly();
        }
    }
}
