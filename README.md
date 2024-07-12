# MangaCLI.Net
*A Featureful Manga Downloader*

### Command Line Options
- -q, --query
    - **Required.** Search query to use when finding manga
- -m, --manga
    - (Default: First) Which manga to select from search (First/Random/Exact)
- -S, --source
    - (Default: ComicK) The source to download manga from (ComicK/...)
- -G, --group
    - (Default: Official) Preferred group for scanlations
- O, --output
    - (Default: ~/Documents/Manga) The folder to download manga into
- -F, --format
    - (Default: CBZ) The format to download manga as (CBZ/PDF) <sub>(EPUB coming soon)</sub>
- -l, --language
    - (Default: en) Language to use when searching for chapters (en/jp/ko/...)
- --subfolder
    - (Default: Yes) Create a subfolder for manga (Yes/No)
- --overwrite
    - (Default: No) Overwrite existing mangas with the same name (Yes/No)
- --allow-alternate
    - (Default: Yes) Allow the use of alternate scanlation groups for chapter search for missing chapters (Yes/No)
## Supported Sources:
- ComicK
- (Planned) MangaDex
- More to come...