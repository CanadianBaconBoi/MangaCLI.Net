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

public class TuiLogger: ILogger
{
    private List<string> _errors = [];
    
    public async Task Log(string message, bool newLine = true)
    {
        await DrawGui();
        await WriteErrors();

        List<string> messages = [message];
        int idx = 0;
        while (messages[idx].Length > Console.BufferWidth - 10)
        {
            
            messages.Add(messages[idx][(Console.BufferWidth-10)..(messages[idx].Length-1)]);
            messages[idx] = messages[idx][..(Console.BufferWidth - 10)];
            idx++;
        }
        
        for (idx = 0; idx < messages.Count; idx++)
        {
            Console.SetCursorPosition(
                (Console.BufferWidth/2)-(messages[0].Length/2),
                (Console.BufferHeight/2)-((messages.Count/2)+idx)
            );
            await Console.Out.WriteAsync(messages[idx]);
        }
    }

    public async Task LogWithProgress(string message, int progress, int maximum, bool showAsPercent, bool newLine = true) {
        await DrawGui();
        await WriteErrors();

        List<string> messages = [message];
        var idx = 0;
        while (messages[idx].Length > Console.BufferWidth - 10)
        {
            messages.Add(messages[idx][(Console.BufferWidth - 10)..(messages[idx].Length-1)]);
            messages[idx] = messages[idx][..(Console.BufferWidth - 10)];
            idx++;
        }
        
        for (idx = 0; idx < messages.Count; idx++)
        {
            Console.SetCursorPosition(
                (Console.BufferWidth/2)-(messages[0].Length/2),
                (Console.BufferHeight/2)-((messages.Count/2)+idx)-1
            );
            await Console.Out.WriteAsync(messages[idx]);
        }
        
        Console.SetCursorPosition((Console.BufferWidth/2)-(Console.BufferWidth/8), (Console.BufferHeight/2)+messages.Count);
        for (idx = 0; idx < ((float)progress / maximum) * Console.BufferWidth/4; idx++)
            await Console.Out.WriteAsync('\u2588');
        for (; idx < Console.BufferWidth/4; idx++)
            await Console.Out.WriteAsync('\u2592');

        var resultMessage = showAsPercent ? $"[{MathF.Floor((float)progress/maximum*100)}%]" : $"[{progress}/{maximum}]";
        Console.SetCursorPosition((Console.BufferWidth/2)-(resultMessage.Length/2), (Console.BufferHeight/2)+messages.Count+1);
        await Console.Out.WriteAsync(resultMessage);
    }

    public async Task LogError(string message, bool newLine = true)
    {
        await DrawGui();
        await WriteErrors();

        List<string> messages = [message];
        int idx = 0;
        while (messages[idx].Length > Console.BufferWidth - 10)
        {
            messages.Add(messages[idx][(Console.BufferWidth - 10)..(messages[idx].Length-1)]);
            messages[idx] = messages[idx][..(Console.BufferWidth - 10)];
            idx++;
        }
        
        for (idx = 0; idx < messages.Count; idx++)
        {
            Console.SetCursorPosition(
                (Console.BufferWidth/2)-(messages[0].Length/2),
                (Console.BufferHeight/2)-((messages.Count/2)+idx)
            );
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            await Console.Out.WriteAsync(messages[idx]);
            Console.ForegroundColor = oldColor;

            _errors.Add(messages[idx]);
        }

        await Task.Delay(2000);
    }

    public async Task<int> LogOptions(string question, string[] options)
    {
        var pager = 0;
        string buffer = String.Empty;
        var ret = 0;
        
        await DrawGui();

        var optionsLengthDigits = (int)Math.Floor(Math.Log10(options.Length) + 1);
        for (var i = 0; i < options.Length; i++)
        {
            options[i] = $"[{new string(' ', optionsLengthDigits-(int)Math.Floor(Math.Log10(i+1)+1))}{i + 1}] {options[i]}";
        }

        var optionsParsed = new List<string>();
        
        for (var i = 0; i < options.Length; i++)
        {
            if (options[i].Length <= Console.BufferWidth - 10)
            {
                optionsParsed.Add(options[i]);
                continue;
            }
            while (options[i].Length > Console.BufferWidth - 10)
            {
                optionsParsed.Add(options[i][..(Console.BufferWidth - 10)]);
                options[i] = options[i][(Console.BufferWidth - 10)..];
            }
        }

        
        for (var y = 3; y < int.Min(Console.BufferHeight - 3, optionsParsed.Count+3); y++)
        {
            Console.SetCursorPosition(6, y);
            if(optionsParsed.Count >= (y-3)+pager)
                Console.Write(optionsParsed[(y-3)+pager]);
            else
                break;
        }
        
        if (optionsParsed.Count > Console.BufferHeight - 6)
        {
            Console.SetCursorPosition(Console.BufferWidth/2-1, Console.BufferHeight - 3);
            Console.Write("\\/");
        }
        Console.SetCursorPosition(6, Console.BufferHeight - 2);
        Console.Write("Select an Option: ");

        ConsoleKeyInfo key;
        (int, int) cursorPos;
        bool returned = false;
        while (!returned)
        {
            key = Console.ReadKey();
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    cursorPos = Console.GetCursorPosition();
                    if(pager > 0) pager--;

                    for (int y = 2; y < Console.BufferHeight - 2; y++)
                    {
                        Console.SetCursorPosition(6, y);
                        Console.Write(new string(' ', Console.BufferWidth - 10));
                    }
                    
                    if (pager > 0)
                    {
                        Console.SetCursorPosition(Console.BufferWidth/2-1, 2);
                        Console.Write("/\\");
                    }

                    for (var y = 3; y < int.Min(Console.BufferHeight - 3, optionsParsed.Count+3); y++)
                    {
                        Console.SetCursorPosition(6, y);
                        if(optionsParsed.Count >= (y-3)+pager)
                            Console.Write(optionsParsed[(y-3)+pager]);
                        else
                            break;
                    }
        
                    if (optionsParsed.Count > Console.BufferHeight - 6)
                    {
                        Console.SetCursorPosition(Console.BufferWidth/2-1, Console.BufferHeight - 3);
                        Console.Write("\\/");
                    }
                    Console.SetCursorPosition(cursorPos.Item1, cursorPos.Item2);
                    break;
                case ConsoleKey.DownArrow:
                    cursorPos = Console.GetCursorPosition();
                    if(pager < optionsParsed.Count-(Console.BufferHeight-6)) pager++;
                    
                    for (int y = 2; y < Console.BufferHeight - 2; y++)
                    {
                        Console.SetCursorPosition(6, y);
                        Console.Write(new string(' ', Console.BufferWidth - 10));
                    }
                    
                    if (pager > 0)
                    {
                        Console.SetCursorPosition(Console.BufferWidth/2-1, 2);
                        Console.Write("/\\");
                    }

                    for (var y = 3; y < int.Min(Console.BufferHeight - 3, optionsParsed.Count+3); y++)
                    {
                        Console.SetCursorPosition(6, y);
                        if(optionsParsed.Count >= (y-3)+pager)
                            Console.Write(optionsParsed[(y-3)+pager]);
                        else
                            break;
                    }
        
                    if (pager < optionsParsed.Count-(Console.BufferHeight-6))
                    {
                        Console.SetCursorPosition(Console.BufferWidth/2-1, Console.BufferHeight - 3);
                        Console.Write("\\/");
                    }
                    Console.SetCursorPosition(cursorPos.Item1, cursorPos.Item2);
                    break;
                case ConsoleKey.Enter:
                    if(int.TryParse(buffer, out ret) && ret > 0 && ret <= options.Length)
                        returned = true;
                    else
                    {
                        cursorPos = Console.GetCursorPosition();
                        Console.SetCursorPosition(cursorPos.Item1 - buffer.Length, cursorPos.Item2);
                        await Console.Out.WriteAsync(new string(' ', buffer.Length));
                        Console.SetCursorPosition(cursorPos.Item1 - buffer.Length, cursorPos.Item2);
                        buffer = String.Empty;
                    }

                    break;
                default:
                    if (key.KeyChar != '\0')
                    {
                        cursorPos = Console.GetCursorPosition();
                        Console.SetCursorPosition(cursorPos.Item1 - 1, cursorPos.Item2);
                        await Console.Out.WriteAsync(' ');
                        Console.SetCursorPosition(cursorPos.Item1 - 1, cursorPos.Item2);
                        switch (key.Key)
                        {
                            case ConsoleKey.D0: case ConsoleKey.D1: case ConsoleKey.D2: case ConsoleKey.D3: case ConsoleKey.D4:
                            case ConsoleKey.D5: case ConsoleKey.D6: case ConsoleKey.D7: case ConsoleKey.D8: case ConsoleKey.D9:
                                await Console.Out.WriteAsync((char)key.Key);
                                buffer += key.KeyChar;

                                if (int.TryParse(buffer, out var bufferInt) && bufferInt > options.Length)
                                {
                                    cursorPos = Console.GetCursorPosition();
                                    Console.SetCursorPosition(cursorPos.Item1 - buffer.Length, cursorPos.Item2);
                                    await Console.Out.WriteAsync(new string(' ', buffer.Length));
                                    Console.SetCursorPosition(cursorPos.Item1 - buffer.Length, cursorPos.Item2);
                                    buffer = options.Length.ToString();
                                    await Console.Out.WriteAsync(buffer);
                                }
                                break;
                            case ConsoleKey.Backspace:
                                if (buffer.Length > 0)
                                {
                                    cursorPos = Console.GetCursorPosition();
                                    Console.SetCursorPosition(cursorPos.Item1 - 1, cursorPos.Item2);
                                    await Console.Out.WriteAsync(' ');
                                    Console.SetCursorPosition(cursorPos.Item1 - 1, cursorPos.Item2);
                                    buffer = buffer[..(buffer.Length - 1)];
                                }
                                break;
                        }
                    }

                    break;
            }
        }

        return ret-1;
    }

    public async Task ClearLine()
    {
        await DrawGui();
    }


    private async Task DrawGui()
    {
        Console.Clear();
        Console.SetCursorPosition(0, 0);
        for (var x = 0; x < Console.BufferWidth; x++)
            await Console.Out.WriteAsync('=');
        for (var y = 1; y < Console.BufferHeight; y++)
        {
            Console.SetCursorPosition(0, y);
            await Console.Out.WriteAsync('|');
            Console.SetCursorPosition(Console.BufferWidth, y);
            await Console.Out.WriteAsync('|');
        }
        Console.SetCursorPosition(0, Console.BufferHeight);
        for (var x = 0; x < Console.BufferWidth; x++)
            await Console.Out.WriteAsync('=');
    }

    private async Task WriteErrors()
    {
        int y = 3;
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        foreach (var error in _errors)
        {
            Console.SetCursorPosition(3, y++);
            await Console.Out.WriteAsync(error);
        }
        Console.ForegroundColor = oldColor;
    }
}