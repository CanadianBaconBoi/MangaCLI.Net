#region header
// MangaCLI.Net : A Featureful Manga Downloader
// Copyright (C)  2024 canadian
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

namespace MangaCLI.Net.Logging;

public interface ILogger
{
    public Task Log(string message, bool newLine = true);
    public Task LogWithProgress(string message, int progress, int maximum, bool showAsPercent, bool newLine = true);
    public Task LogError(string message, bool newLine = true);
    public Task<int> LogOptions(string question, string[] options);
    public Task ClearLine();
}