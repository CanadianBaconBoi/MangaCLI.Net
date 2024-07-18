#!/bin/sh

dotnet restore

dotnet publish MangaCLI.Net -c Release -r linux-x64 -o publish/cli/linux-x64/ --no-restore
dotnet publish MangaCLI.Net -c Release -r linux-arm64 -o publish/cli/linux-aarch64/ --no-restore
dotnet publish MangaCLI.Net -c Release -r win-x64 -o publish/cli/windows-x64/ --no-restore
dotnet publish MangaCLI.Net -c Release -r win-arm64 -o publish/cli/windows-aarch64/ --no-restore

dotnet pack MangaLib.Net.Base -o publish/pluginbase --no-restore
dotnet pack MangaLib.Net -o publish/mangalib --no-restore
