using FortniteReplayReader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using Unreal.Core.Models.Enums;
using System.Collections.Generic;
using FortniteReplayReader.Models.Events;
using System.Linq;

namespace ConsoleReader
{
    using PlatformCountsType = System.Collections.Generic.Dictionary<string, int>;
    using GUIDCountsType = System.Collections.Generic.Dictionary<string, int>;

    class Program
    {
        static PlatformCountsType platformCounts = new PlatformCountsType();

        static void UpdatePlatformCount(FortniteReplayReader.Models.PlayerData item)
        {
            if (item.Platform != null)
            {
                if (platformCounts.ContainsKey(item.Platform))
                {
                    platformCounts[item.Platform]++;
                }
                else
                {
                    platformCounts[item.Platform] = 1;
                }
            }
            else
            {
                if (platformCounts.ContainsKey("unknown"))
                {
                    platformCounts["unknown"]++;
                }
                else
                {
                    platformCounts["unknown"] = 1;
                }
            }
        }

        static void Main(string[] args)
        {
            string replayFilesFolder;
            string myGUID = "";

            if (args.Length < 1)
            {
                System.Console.WriteLine("Command format: ConsoleReader <replay file folder path>");
                return;
            }
            else
            {
                replayFilesFolder = args[0];
            }

            var serviceCollection = new ServiceCollection()
                .AddLogging(loggingBuilder => loggingBuilder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Error));
            var provider = serviceCollection.BuildServiceProvider();
            var logger = provider.GetService<ILogger<Program>>();

            var replayFiles = Directory.EnumerateFiles(replayFilesFolder, "*.replay");

            var gameNumber = 0;
            var guidCounts = new GUIDCountsType();
            var myElimGUIDs = new Dictionary<string, string>();
            foreach (var replayFile in replayFiles)
            {
                var botMap = new Dictionary<string, string>();
                var humanMap = new Dictionary<string, string>();
                float[] deathTimes = new float[100];
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
#if false
                    Console.WriteLine($"timestamp,x,y,z");
#endif
                    int playerIndex = 0;
                    bool headingLine = true;
                    foreach (FortniteReplayReader.Models.PlayerData item in replay.PlayerData)
                    {
                        if (item.EpicId != null)
                        {
#if false
                            Unreal.Core.Models.FVector xxx;
                            if(item.Locations.Count > 0)
                            {
                                if (headingLine)
                                {
                                    Console.WriteLine($"x,y,z,bot,me,timestamp,guid");
                                    headingLine = false;
                                }
                                xxx = item.Locations[0].ReplicatedMovement.Value.Location;
                                int me = 0;
                                var timestamp = item.Locations[0].LastUpdateTime;
                                if (item.IsReplayOwner)
                                {
                                    me = 1;
                                }
                                Console.WriteLine($"{xxx.X},{xxx.Y},{xxx.Z},{0},{me},{item.PlayerId},{timestamp}");
                            }
#endif
                            if (item.IsReplayOwner)
                            {
                                myGUID = item.PlayerId;
#if false
                                for (int i = 0;i < item.Locations.Count;i++)
                                {
                                    Unreal.Core.Models.FVector xxx;
                                    xxx = item.Locations[i].ReplicatedMovement.Value.Location;
                                    var timestamp = item.Locations[i].LastUpdateTime;
                                    Console.WriteLine($"{timestamp},{xxx.X},{xxx.Y},{xxx.Z}");
                                }
#endif
                            }

                            if (item.DeathTime != null)
                            {
                                deathTimes[playerIndex++] = (float)item.DeathTime;
                            }

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

                            UpdatePlatformCount(item);
                        }
                        else if (item.BotId != null)
                        {
                            // EpicId is always a GUID (i.e. 32 hex characters).  BotId is only a GUID for AI players.
                            // Special bots like agents, henchmen, storm troopers etc have BotIds that are blank or alphanumeric names.
                            // Only count AI players, so check valid GUID in BotId.
                            if (Guid.TryParse(item.BotId, out var dummyGuid))
                            {
#if false
                                if(item.Locations.Count > 0)
                                {
                                    if (headingLine)
                                    {
                                        Console.WriteLine($"x,y,z,bot,me,guid,timestamp");
                                        headingLine = false;
                                    }
                                    var xxx = item.Locations[0].ReplicatedMovement.Value.Location;
                                    var timestamp = item.Locations[0].LastUpdateTime;
                                    Console.WriteLine($"{xxx.X},{xxx.Y},{xxx.Z},{1},{0},{0},{timestamp}");
                                }
#endif
                                if (item.DeathTime != null)
                                {
                                    deathTimes[playerIndex++] = (float)item.DeathTime;
                                }

                                //Console.WriteLine($"Id: {item.PlayerName} (BOT)");
                                botMap[item.BotId] = item.PlayerName;
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

                    if (myGUID == "")
                    {
                        Console.WriteLine($"Your player ID not found.  Exiting...");
                        return;
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
            Console.WriteLine("Platforms:");
            var mySortedList = platformCounts.OrderByDescending(d => d.Value);
            foreach (KeyValuePair<string, int> entry in mySortedList)
            {
                Console.WriteLine($"{entry.Key}: {entry.Value}");
            }

            Console.WriteLine();
            Console.WriteLine("Repeated GUIDs:");
            foreach (KeyValuePair<string, int> entry in guidCounts)
            {
                if (entry.Value > 1 && entry.Key != myGUID)
                {
                    Console.WriteLine($"{entry.Key}: {entry.Value}");
                }
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
