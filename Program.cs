using FortniteReplayReader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using Unreal.Core.Models.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;
using Unreal.Core;
using Unreal.Core.Contracts;

namespace ConsoleReader
{
    using PlatformCountsType = System.Collections.Generic.Dictionary<string, int>;
    using GUIDCountsType = System.Collections.Generic.Dictionary<string, int>;
    using ElimCountsType = System.Collections.Generic.Dictionary<string, int>;

    class Program
    {
        static PlatformCountsType platformCounts = new PlatformCountsType();
        static ElimCountsType botElims = new ElimCountsType();
        static ElimCountsType humanElims = new ElimCountsType();

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

        static void UpdateElimCounts(FortniteReplayReader.Models.KillFeedEntry item)
        {
            if (!item.FinisherOrDownerIsBot && item.FinisherOrDownerName != null)
            {
                if (item.PlayerIsBot)
                {
                    if (item.FinisherOrDownerName != null)
                    {

                    }
                    if (botElims.ContainsKey(item.FinisherOrDownerName))
                    {
                        botElims[item.FinisherOrDownerName]++;
                    }
                    else
                    {
                        botElims[item.FinisherOrDownerName] = 1;
                    }
                }
                else
                {
                    if (humanElims.ContainsKey(item.FinisherOrDownerName))
                    {
                        humanElims[item.FinisherOrDownerName]++;
                    }
                    else
                    {
                        humanElims[item.FinisherOrDownerName] = 1;
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            string cs = @"URI=file:fortnite_game_stats.sqlite";
            using var con = new SQLiteConnection(cs);
            con.Open();

            using var cmd = new SQLiteCommand(con);

#if false
            cmd.CommandText = "DROP TABLE IF EXISTS players";
            cmd.ExecuteNonQuery();
#endif
            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS players(timestamp TEXT, guid TEXT, name TEXT, placed INT, bot_kills INT, human_kills INT, platform TEXT, killed_by TEXT, PRIMARY KEY (timestamp, guid))";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS games(timestamp TEXT, humans INT, bots INT, henchmen INT, PRIMARY KEY (timestamp))";
            cmd.ExecuteNonQuery();

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
            serviceCollection.AddSingleton<INetGuidCache, NetGuidCache>();
            serviceCollection.AddSingleton<INetFieldParser, NetFieldParser>();
            serviceCollection.AddSingleton<ReplayReader>();

            var provider = serviceCollection.BuildServiceProvider();
            var logger = provider.GetService<ILogger<Program>>();
            var reader = provider.GetRequiredService<ReplayReader>();

            var replayFiles = Directory.EnumerateFiles(replayFilesFolder, "*.replay");

            var gameNumber = 0;
            var guidCounts = new GUIDCountsType();
            var myElimGUIDs = new Dictionary<string, string>();
            foreach (var replayFile in replayFiles)
            {
                int firstDash = replayFile.IndexOf('-') + 1;
                int lastPeriod = replayFile.LastIndexOf('.');
                var timestamp = replayFile.Substring(firstDash, lastPeriod - firstDash);

                cmd.CommandText = $"select count(*) from games where timestamp='{timestamp}'";
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count > 0)
                {
                    continue;
                }

                var botMap = new Dictionary<string, string>();
                var humanMap = new Dictionary<string, string>();
                float[] deathTimes = new float[100];
                int henchmenCount = 0;
                botElims.Clear();
                humanElims.Clear();
                string killedBy = "******You Won******";
                var sw = new Stopwatch();
                sw.Start();
                try
                {
                    var replay = reader.ReadReplay(replayFile, ParseMode.Full);
#if false
                    Console.WriteLine($"timestamp,x,y,z");
                    bool headingLine = true;
#endif
                    int playerIndex = 0;
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

                            int placement = 0;
                            if (item.Placement != null)
                            {
                                placement = (int)item.Placement;
                            }

                            int kills = 0;
                            if (item.Kills != null)
                            {
                                kills = (int)item.Kills;
                            }

                            cmd.CommandText = $"INSERT OR REPLACE INTO players(name, guid, timestamp, placed, bot_kills, human_kills, platform) VALUES('{item.PlayerName}','{item.EpicId}','{timestamp}',{placement},{0},{0},'{item.Platform}')";
                            cmd.ExecuteNonQuery();
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
                        UpdateElimCounts(item);

                        if (item.FinisherOrDownerName == myGUID)
                        {
                            //Console.WriteLine($"YOU killed {item.PlayerName} (bot: {item.PlayerIsBot})");
                            if (item.PlayerIsBot)
                            {
                            }
                            else
                            {
                                myElimGUIDs[item.PlayerName] = humanMap[item.PlayerName];
                            }
                        }
                        if (item.PlayerName == myGUID)
                        {
                            //Console.WriteLine($"{item.FinisherOrDownerName} (bot: {item.FinisherOrDownerIsBot}) killed YOU");
                            if (item.FinisherOrDownerIsBot)
                            {
                                killedBy = "Bot";
                            }
                            else
                            {
                                killedBy = humanMap[item.FinisherOrDownerName];
                            }
                        }

                        if (!item.PlayerIsBot)
                        {
                            string killedBy1;
                            if (item.FinisherOrDownerIsBot)
                            {
                                killedBy1 = "Bot";
                            }
                            else
                            {
                                killedBy1 = humanMap[item.FinisherOrDownerName];
                            }
                            cmd.CommandText = $"UPDATE players SET killed_by = '{killedBy1}' WHERE guid = '{item.PlayerName}' AND timestamp = '{timestamp}'";
                            cmd.ExecuteNonQuery();
                        }
                    }

                    foreach(var human in humanMap)
                    {
                        int kills = 0;
                        if (botElims.ContainsKey(human.Key))
                        {
                            kills = botElims[human.Key];
                        }
                        else
                        {
                            kills = 0;
                        }
                        cmd.CommandText = $"UPDATE players SET bot_kills = '{kills}' WHERE guid = '{human.Key}' AND timestamp = '{timestamp}'";
                        cmd.ExecuteNonQuery();

                        if (humanElims.ContainsKey(human.Key))
                        {
                            kills = humanElims[human.Key];
                        }
                        else
                        {
                            kills = 0;
                        }
                        cmd.CommandText = $"UPDATE players SET human_kills = '{kills}' WHERE guid = '{human.Key}' AND timestamp = '{timestamp}'";
                        cmd.ExecuteNonQuery();
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
                    int myBotElims = 0;
                    int myHumanElims = 0;
                    if(botElims.ContainsKey(myGUID))
                    {
                        myBotElims = botElims[myGUID];
                    }
                    if (humanElims.ContainsKey(myGUID))
                    {
                        myHumanElims = humanElims[myGUID];
                    }
                    Console.WriteLine(lineFormat,
                        gameNumber, Path.GetFileName(replayFile), humanMap.Count, botMap.Count, henchmenCount, (humanMap.Count + botMap.Count), position, totalPlayers, myBotElims, myHumanElims, killedBy);

                    cmd.CommandText = $"INSERT OR REPLACE INTO games(timestamp, humans, bots, henchmen) VALUES('{timestamp}', {humanMap.Count()}, {botMap.Count()}, {henchmenCount})";
                    cmd.ExecuteNonQuery();
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

            con.Close();
        }
    }
}
