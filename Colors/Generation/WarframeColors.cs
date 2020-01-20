using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Colors.Core;

using Newtonsoft.Json.Linq;

using SevenZip.Compression.LZMA;

namespace Colors.Generation
{
    /// <summary>
    /// Palettes seen in color picker extensions in Warframe game. Updated from Warframe mobile app data bank.
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

        public WarframeColors(Language localization) => language = codes[localization];

        public async ValueTask<IReadOnlyList<Palette>> RetrievePalettesAsync()
        {
            using var client = new HttpClient();
            var content = await client.GetByteArrayAsync(string.Format(SourceFeed, language)).ConfigureAwait(false);
            using var stream = new MemoryStream(content);

            var text = Encoding.UTF8.GetString(Zipper.Decompress(stream));
            var url = text.Split('\n').First(x => x.StartsWith("ExportFlavour")).Trim();

            var flavors = await client.GetStringAsync(string.Format(SourceURL, url)).ConfigureAwait(false);
            flavors = flavors.Replace("\n", @"\n").Replace("\r\n", "\n");

            var colorsList = JToken.Parse(flavors)["ExportFlavour"].SelectTokens(@"$.[?(@.hexColours)]").Select(x => {
                var colors = x["hexColours"].Select(y => new Color(y["value"].ToString().Substring(2))).ToList();
                return new Palette(x["name"].ToString(), colors);
            }).ToList();
            return colorsList.AsReadOnly();
        }
    }
}
