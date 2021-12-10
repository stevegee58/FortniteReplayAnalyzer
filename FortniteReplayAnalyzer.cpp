using FortniteReplayReader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using Unreal.Core.Models.Enums;
using System.Collections.Generic;
using FortniteReplayReader.Models.Events;

namespace ConsoleReader
{
    class Program
    {
        static void Main(string[] args)
        {
            string replayFilesFolder;
            string myGUID;

            if (args.Length < 2)
            {
                System.Console.WriteLine("Command format: ConsoleReader <your player GUID> <replay file folder path>");
                return;
            }
            else
            {
                myGUID = args[0].ToUpper();
                replayFilesFolder = args[1];
            }

            var serviceCollection = new ServiceCollection()
                .AddLogging(loggingBuilder => loggingBuilder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Error));
            var provider = serviceCollection.BuildServiceProvider();
            var logger = provider.GetService<ILogger<Program>>();

            var replayFiles = Directory.EnumerateFiles(replayFilesFolder, "*.replay");

            var gameNumber = 0;
            var guidCounts = new Dictionary<string, int>();
            var myElimGUIDs = new Dictionary<string, string>();
            foreach (var replayFile in replayFiles)
            {
                var botMap = new Dictionary<string, string>();
                var humanMap = new Dictionary<string, string>();
                int henchmenCount = 0;
                int myHumanElims = 0;
                int myBotElims = 0;
                string killedBy = "******You Won******";
                var sw = new Stopwatch();
                sw.Start();
                try
                {
                    var reader = new ReplayReader(logger, ParseMode.Full);
                    var replay = reader.ReadReplay(replayFile);

                    foreach (FortniteReplayReader.Models.PlayerData item in replay.PlayerData)
                    {
                        if (item.EpicId != null)
                        {
                            //Console.WriteLine($"Id: {item.EpicId} Isbot: {item.IsBot}");
                            humanMap[item.EpicId] = item.PlayerName;
                            //Console.WriteLine($"Id: {item.PlayerName}");
                            if (guidCounts.ContainsKey(item.EpicId))
                            {
                                guidCounts[item.EpicId]++;
                            }
                            else
                            {
                                guidCounts[item.EpicId] = 1;
                            }
                        }
                        else if (item.BotId != null)
                        {
                            // EpicId is always a GUID (i.e. 32 hex characters).  BotId is only a GUID for AI players.
                            // Special bots like agents, henchmen, storm troopers etc have BotIds that are blank or alphanumeric names.
                            // Only count AI players, so check valid GUID in BotId.
                            if (Guid.TryParse(item.BotId, out var dummyGuid))
                            {
                                //Console.WriteLine($"Id: {item.PlayerName} (BOT)");
                                botMap[item.BotId] = item.PlayerName;
#if false
                                if (guidCounts.ContainsKey(item.BotId))
                                {
                                    guidCounts[item.BotId]++;
                                }
                                else
                                {
                                    guidCounts[item.BotId] = 1;
                                }
#endif
                            }
                            else
                            {
                                //Console.WriteLine($"Id: {item.PlayerName} (Henchman)");
                                henchmenCount++;
                            }
                        }
                        else
                        {
                            // Blank EpicId and BotId is a special bot.
                            //Console.WriteLine($"Id: {item.PlayerName} (Henchman)");
                            henchmenCount++;
                        }
                    }

                    foreach (FortniteReplayReader.Models.KillFeedEntry item in replay.KillFeed)
                    {
                        if (item.FinisherOrDownerName == myGUID)
                        {
                            //Console.WriteLine($"YOU killed {item.PlayerName} (bot: {item.PlayerIsBot})");
                            if (item.PlayerIsBot)
                            {
                                myBotElims++;
                            }
                            else
                            {
                                myHumanElims++;
                                myElimGUIDs[item.PlayerName] = humanMap[item.PlayerName];
                            }
                        }
                        if (item.PlayerName == myGUID)
                        {
                            //Console.WriteLine($"{item.FinisherOrDownerName} (bot: {item.FinisherOrDownerIsBot}) killed YOU");
                            killedBy = humanMap[item.FinisherOrDownerName];
                            if (item.FinisherOrDownerIsBot)
                            {
                                killedBy = "Bot";
                            }
                        }
                    }

                    string lineFormat = "|{0,4}|{1,41}|{2,6}|{3,4}|{4,8}|{5,11}|{6,6}|{7,5}|{8,9}|{9,11}|{10,32} |";
                    if (gameNumber == 0)
                    {
                        Console.WriteLine(lineFormat, 
                            "Game","Replay File               ","Humans","Bots","Henchmen","Lobby Total","Placed","Total","Bot Elims","Human Elims","Killed By            ");
                        Console.WriteLine("|----|-----------------------------------------|------|----|--------|-----------|------|-----|---------|-----------|---------------------------------|");
                    }

                    uint position = 999;
                    uint totalPlayers = 999;
                    if(replay.TeamStats != null)
                    {
                        position = replay.TeamStats.Position;
                        totalPlayers = replay.TeamStats.TotalPlayers;
                        // In case my death was erroneously not reported, check my placement to confirm a win
                        if (position != 1 && killedBy == "******You Won******")
                        {
                            killedBy = "******You Died******";
                        }
                    }
                    Console.WriteLine(lineFormat,
                        gameNumber, Path.GetFileName(replayFile), humanMap.Count, botMap.Count, henchmenCount, (humanMap.Count + botMap.Count), position, totalPlayers, myBotElims, myHumanElims, killedBy);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                sw.Stop();
                gameNumber++;
            }

            Console.WriteLine();
            Console.WriteLine("Human Elimination GUIDs:");
            foreach (var guid in myElimGUIDs)
            {
                Console.WriteLine(myElimGUIDs[guid.Key]);
            }

            //Console.ReadLine();
        }
    }
}
