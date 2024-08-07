# MangaCLI.Net
*A Featureful Manga Downloader*

### Command Line Options

---
#### One of
- -q, --query
    - **Required.** Search query to use when finding manga
- -Q, --query-file
  - **Required** File with line seperated search queries to use when finding manga
---

- -m, --manga
    - (Default: First) Which manga to select from search (First/Random/Exact)
- -S, --source
    - (Default: ComicK) The source to download manga from (ComicK/...)
- -g, --group
    - (Default: Official) Preferred group for scanlations
- -G, --ignore-group
  - Group to ignore for scanlations (can be specified multiple times)
- O, --output
    - (Default: ~/Documents/Manga) The folder to download manga into
- -F, --format
    - (Default: CBZ) The format to download manga as (CBZ/PDF) <sub>(EPUB coming soon)</sub>
- -l, --language
    - (Default: en) Language to use when searching for chapters (en/jp/ko/...)
- --no-subfolder
    - Don't create a subfolder for manga
- --overwrite
    - Overwrite existing mangas with the same name
- --disallow-alternate
    - Disallow the use of alternate scanlation groups for chapter search for missing chapters
## Supported Sources:
- ComicK
- (Planned) MangaDex
- More to come...