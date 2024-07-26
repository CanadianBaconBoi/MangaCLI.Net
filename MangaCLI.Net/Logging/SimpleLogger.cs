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

public class SimpleLogger: ILogger
{
    public Task Log(string message, bool newLine = true) => newLine ? Console.Out.WriteLineAsync(message) : Console.Out.WriteAsync(message);
    public Task LogWithProgress(string message, int progress, int maximum, bool showAsPercent, bool newLine = true) => newLine
        ? showAsPercent
            ? Console.Out.WriteLineAsync($"{message} [{MathF.Floor((float)progress/maximum*100)}%]")
            : Console.Out.WriteLineAsync($"{message} [{progress}/{maximum}]")
        : showAsPercent
            ? Console.Out.WriteAsync($"{message} [{MathF.Floor((float)progress/maximum*100)}%]")
            : Console.Out.WriteAsync($"{message} [{progress}/{maximum}]");

    public Task LogError(string message, bool newLine = true) => newLine ? Console.Error.WriteLineAsync(message) : Console.Error.WriteAsync(message);

    public async Task<int> LogOptions(string question, string[] options)
    {
        await Console.Out.WriteLineAsync(question);

        var idx = 0;
        for (; idx < options.Length; idx++)
        {
            await Console.Out.WriteLineAsync($"{idx+1}. {options[idx]}");
        }

        do
        {
            Console.Write($"{question} [1-{options.Length}]: ");
        } while (!int.TryParse(await Console.In.ReadLineAsync(), out idx) && idx > options.Length);

        return idx - 1;
    }

    public Task ClearLine() => Console.Out.WriteAsync("\r" + new string(' ', Console.BufferWidth) + "\r");
}