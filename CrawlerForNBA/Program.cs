using AngleSharp;
using AngleSharp.Dom;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CrawlerForNBA
{
    /* Note:
     * 1. Without parallel processing :     30:00 up
     * 2. With parallel processing :        01:32
     */

    public class CrawlerSettings
    {
        public static readonly string targetUrl = "https://www.basketball-reference.com";
        private static readonly IConfiguration config = Configuration.Default.WithDefaultLoader();
        public static readonly IBrowsingContext context = BrowsingContext.New(config);
    }

    /// <summary>
    /// 球員生涯統計
    /// </summary>
    public class Player
    {
        public Player(string name, string playerResouceName)
        {
            Name = name;
            PlayerResouceName = playerResouceName;
        }

        [Name("Player")]
        public string Name { get; set; }

        [Ignore]
        public string PlayerResouceName { get; set; }

        public int? G { get; set; }

        public float? PTS { get; set; }

        public float? TRB { get; set; }

        public float? AST { get; set; }

        [Name("FG(%)")]
        public float? FGP { get; set; }

        [Name("FG3(%)")]
        public float? FG3P { get; set; }

        [Name("FT(%)")]
        public float? FTP { get; set; }

        [Name("eFG(%)")]
        public float? eFGP { get; set; }

        public float? PER { get; set; }

        public float? WS { get; set; }

        /// <summary>
        /// 取得該球員生涯統計
        /// </summary>
        /// <param name="player"> 球員 </param>
        /// <returns></returns>
        public async Task SetPlayerCareerStatistics()
        {
            var document = await CrawlerSettings.context.OpenAsync($"{CrawlerSettings.targetUrl}{PlayerResouceName}");

            // 取得球員生涯表單
            #region example
            /*
                < div class="stats_pullout">
                    <div>
                    </div>
                    <div class="p1">
                        <div>
                            <h4 class="poptip" data-tip="Games">G</h4>
                            <p></p>
                            <p>256</p>
                        </div>

                        <div>
                            <h4 class="poptip" data-tip="Points">PTS</h4>
                            <p></p>
                            <p>5.7</p>
                        </div>

                        <div>
                        </div>

                        <div>
                        </div>
                    </div>

                    <div class="p2">
                    </div>

                    <div class="p3">
                    </div>
                </div>
            */
            #endregion
            var statisticsFromCrawler = document
                .GetElementsByClassName("stats_pullout");

            // 解析表單
            ParsePlayerStatistics(statisticsFromCrawler[0], this);

            Console.WriteLine($"{Name} {G} {PTS} {TRB} {AST} {FGP} {FG3P} {FTP} {eFGP} {PER} {WS}");
        }

        /// <summary>
        /// 從 html 解析球員生涯統計資料
        /// </summary>
        /// <param name="statistics"> html原始資料 </param>
        /// <param name="player"> 球員 </param>
        private void ParsePlayerStatistics(IElement statistics, Player player)
        {
            var statisticsParts = statistics.QuerySelectorAll("#info > div.stats_pullout > div.p1,.p2,.p3");

            foreach (var part in statisticsParts)
            {
                var statisticsTypes = part.GetElementsByTagName("div");
                foreach (var item in statisticsTypes)
                {
                    switch (item.GetElementsByTagName("h4")[0].Text())
                    {
                        case "G":
                            if (int.TryParse(item.QuerySelector("p:nth-child(3)").Text(), out var _G))
                                player.G = _G;
                            break;
                        case "PTS":
                            if (float.TryParse(item.QuerySelector("p:nth-child(3)").Text(), out var _PTS))
                                player.PTS = _PTS;
                            break;
                        case "TRB":
                            if (float.TryParse(item.QuerySelector("p:nth-child(3)").Text(), out var _TRB))
                                player.TRB = _TRB;
                            break;
                        case "AST":
                            if (float.TryParse(item.QuerySelector("p:nth-child(3)").Text(), out var _AST))
                                player.AST = _AST;
                            break;
                        case "FG%":
                            if (float.TryParse(item.QuerySelector("p:nth-child(3)").Text(), out var _FGP))
                                player.FGP = _FGP;
                            break;
                        case "FG3%":
                            if (float.TryParse(item.QuerySelector("p:nth-child(3)").Text(), out var _FG3P))
                                player.FG3P = _FG3P;
                            break;
                        case "FT%":
                            if (float.TryParse(item.QuerySelector("p:nth-child(3)").Text(), out var _FTP))
                                player.FTP = _FTP;
                            break;
                        case "eFG%":
                            if (float.TryParse(item.QuerySelector("p:nth-child(3)").Text(), out var _eFGP))
                                player.eFGP = _eFGP;
                            break;
                        case "PER":
                            if (float.TryParse(item.QuerySelector("p:nth-child(3)").Text(), out var _PER))
                                player.PER = _PER;
                            break;
                        case "WS":
                            if (float.TryParse(item.QuerySelector("p:nth-child(3)").Text(), out var _WS))
                                player.WS = _WS;
                            break;
                    }
                }
            }
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            for (char alphabet = 'a'; alphabet <= 'z'; alphabet++){
                var players = await GetPlayersByAlphabetAsync(alphabet);
                players = players.OrderBy(p => p.Name).ToList();

                WriteCsv(alphabet, players);
            }

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            // Format and display the TimeSpan value.
            string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);
        }

        /// <summary>
        /// 依據字母取得球員名、球員頁面資源檔名(e.g. /players/a/abdelal01.html)
        /// </summary>
        /// <param name="alphabet"> 字母 </param>
        /// <returns> 該字母類別內所有球員清單 </returns>
        public static async Task<List<Player>> GetPlayersByAlphabetAsync(char alphabet)
        {
            var playerList = new List<Player>();
            var document = await CrawlerSettings.context.OpenAsync($"{CrawlerSettings.targetUrl}/players/{alphabet}");
            // 依據<th>取出球員  
            var players = document
                .QuerySelector("#players > tbody")
                .GetElementsByTagName("th");

            // References
            // https://csharpkh.blogspot.com/2019/09/CSharp-Task-Run-StartNew-Thread-Wait-Cancellation-CancellationToken-Exception.html
            // https://docs.microsoft.com/zh-tw/dotnet/standard/parallel-programming/task-based-asynchronous-programming
            var taskArray = new List<Task>();
            foreach (var playerEle in players)
            {
                var playerName = playerEle.GetElementsByTagName("a")[0].Text();
                var playerResouceName = playerEle.GetElementsByTagName("a")[0].GetAttribute("href");
                var player = new Player(playerName, playerResouceName);

                // 多執行緒取得資料
                taskArray.Add(Task.Run(async () => {
                    await player.SetPlayerCareerStatistics();
                    playerList.Add(player);
                }));
            }
            Task.WhenAll(taskArray).Wait();

            return playerList;
        }


        /// <summary>
        /// 將球員資料寫入csv檔
        /// </summary>
        /// <param name="alphabet"> 字母 </param>
        /// <param name="playerStatisticsList"> 球員生涯統計資料 </param>
        public static void WriteCsv(char alphabet, List<Player> playerStatisticsList)
        {
            Directory.CreateDirectory("Data");

            using var writer = new StreamWriter($"Data/{char.ToUpper(alphabet)}.csv");

            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            
            csv.WriteRecords(playerStatisticsList);
        }

        #region Test
        public static async Task TestGetPlayersByAlphabetAsync()
        {
            var alphabet = 'a';
            var playerList = await GetPlayersByAlphabetAsync(alphabet);
            foreach (var player in playerList)
            {
                Console.WriteLine($"Name: {player.Name}");
                Console.WriteLine($"Resource Name: {player.PlayerResouceName}");
                Console.WriteLine();
            }
        }

        public static void TestWriteCsv()
        {
            var alphabet = 'a';
            var playerStatisticsList = new List<Player>() {
                new Player("Alaa Abdelnaby", "/players/a/abdelal01.html")
                {
                    G = 256,
                    PTS = 5.7F,
                    TRB = 3.3F,
                    AST = 0.3F,
                    FGP = 50.2F,
                    FG3P = 0.0F,
                    FTP = 70.1F,
                    eFGP = 50.2F,
                    PER = 13.0F,
                    WS = 4.8F
                }
            };

            WriteCsv(alphabet, playerStatisticsList);
        }
        #endregion
    }
}
