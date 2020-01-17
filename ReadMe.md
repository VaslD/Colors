# Colors

## Introduction

This is a personal project created to store and update a collection of color palettes, also to demonstrate various common coding patterns and logic for future reference.

Simply put, there are 3 main concepts here:

- **Models**: a *Color*, which is an A/R/G/B integer-encoded real-world color; and, a collection of *Color*s, which is called a *Palette*.
- **Providers**: each contains a mechanism that retrieves at least one collection of *Color*s.
- **Printers**: that print, (i.e. serialize, visualize, transform) *Color*s from a machine-readable format to a human-readable format.

All concepts have implementations. Currently, four providers (`JapaneseTraditionalColors`, `MaterialDesignColors`, `WarframeColors`, `LocalStorageReader`) and two printers (`ConsolePrinter` and `LocalStorageWriter`) are fully functional; a `WordDocumentPrinter` is planned.

## Usage

Consuming this project as a library is pretty much straightforward. Included .NET Core console app shows example usage.

## Development

This is only a side-project/utility and I do not see it as a library/dependency that will be used by others. Therefore it is not under active development, in a sense. I will probably update it occasionally entirely for personal reasons.

If you are interested, you're welcome to add something new and PR it, or fork/modify for any purpose.

## License

The project is currently released without a specific license. However, third-party libraries included in the project may dictate a different license, see *ReadMe*s in subdirectories and license of referenced packages for details.

## Disclaimer

The program probes various websites for color collections. This procedure is non-intrusive and is equivalent to a normal web browser visit. Special thanks to all website owners and maintainers.

I'm not affiliated with any of these sites. And I take no responsibility if this program is being used to serve ill-intent.

If you are owners of these websites and you think this project visits your site in an inappropriate way, please open an issue and the *provider* for your site will be removed from public access.
