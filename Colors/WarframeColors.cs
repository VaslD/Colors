using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Colors.Core;

using Newtonsoft.Json.Linq;

using SevenZip.Compression.LZMA;

namespace Colors
{
    /// <summary>
    /// Color palettes in Warframe game. Updated from Warframe mobile app data bank.
    /// </summary>
    public class WarframeColors : IPalettesProvider
    {
        private const string SourceFeed = "http://origin.warframe.com/origin/15790001/PublicExport/index_{0}.txt.lzma";
        private const string SourceURL = "http://content.warframe.com/PublicExport/Manifest/{0}";

        private static readonly IReadOnlyDictionary<Language, string> codes = new Dictionary<Language, string> {
            { Language.English, "en" },
            { Language.German, "de" },
            { Language.French, "fr" },
            { Language.Italian, "it" },
            { Language.Korean, "ko" },
            { Language.Spanish, "es" },
            { Language.SimplifiedChinese, "zh" },
            { Language.Russian, "ru" },
            { Language.Japanese, "ja" },
            { Language.Polish, "pl" },
            { Language.Portuguese, "pt" },
            { Language.TraditionalChinese, "tc" },
            { Language.Turkish, "tr" },
            { Language.Ukrainian, "uk" },
        };

        private readonly string language;

        public WarframeColors() : this(Language.English) { }

        public WarframeColors(Language localization)
        {
            language = codes[localization];
        }

        public async ValueTask<IReadOnlyList<Palette>> RetrievePalettesAsync()
        {
            var client = new HttpClient();
            var response = await client.GetAsync(string.Format(SourceFeed, language)).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var text = Encoding.UTF8.GetString(Zipper.Decompress(contentStream));
            _ = contentStream.DisposeAsync();
            var url = text.Split('\n').First(x => x.StartsWith("ExportFlavour")).Trim();

            response = await client.GetAsync(string.Format(SourceURL, url)).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var palettes = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            palettes = palettes.Replace("\n", @"\n").Replace("\r\n", "\n");

            var colorsList = JToken.Parse(palettes)["ExportFlavour"].SelectTokens(@"$.[?(@.hexColours)]").Select(x => {
                var colors = x["hexColours"].Select(y => new Color(y["value"].ToString().Substring(2))).ToList();
                return new Palette(x["name"].ToString(), colors);
            }).ToList();
            return colorsList.AsReadOnly();
        }
    }
}
