using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

internal class Program
{
    public static void Main(string[] args)
    {
        var currentDirectory = Environment.CurrentDirectory;
        var numberRegex = new Regex(@"\d{4,}");
        var tasksList = new List<Task>();
        var countUnnamedMods = 0;
        var countRenamedMods = 0;
        const int maxModeNameLength = 128;
        var locker = new object();
        
        foreach (var directory in Directory.GetDirectories(currentDirectory))
        {
            if (!numberRegex.IsMatch(directory)) continue;

            var renameTask = Task.Run(() =>
            {
                var stringBuilder = new StringBuilder();
                countUnnamedMods++;
                stringBuilder.Append(directory).Append("\\filelist.xml");
                var fileListPath = stringBuilder.ToString();
                var pathSplit = directory.Split('\\');
                stringBuilder.Clear();
                if (!File.Exists(fileListPath))
                {
                    stringBuilder.Append(pathSplit[pathSplit.Length - 1]).Append(": filelist.xml not found");
                    lock (locker)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(stringBuilder);
                    }
                    stringBuilder.Clear();
                }

                var fileListReader = new StreamReader(fileListPath, Encoding.UTF8);
                var rawModName = new char[maxModeNameLength];
                string modName = null;
                fileListReader.BaseStream.Seek(65, SeekOrigin.Begin);
                fileListReader.Read(rawModName, 0, maxModeNameLength);
                for (int i = 0; i < maxModeNameLength; i++)
                {
                    if (rawModName[i] == '"')
                    {
                        modName = new string(rawModName, 0, i);
                        break;
                    }
                }
                fileListReader.Close();
                fileListReader.Dispose();

                stringBuilder.Append(string.Join("\\", pathSplit.Take(pathSplit.Length - 1))).Append('\\').Append(modName);
                //File.SetAttributes(directory, FileAttributes.Directory | FileAttributes.Normal & ~FileAttributes.ReadOnly);
                Directory.Move(directory, stringBuilder.ToString());
                countRenamedMods++;
                lock (locker)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(modName);
                }
            });
            tasksList.Add(renameTask);
        }

        Task.WaitAll(tasksList.ToArray());
        var stringBuilder = new StringBuilder().Append("Mods found: ").Append(countUnnamedMods).AppendLine().Append("Mods renamed: ").Append(countRenamedMods);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(stringBuilder.ToString());
        Console.ReadKey();
    }
}