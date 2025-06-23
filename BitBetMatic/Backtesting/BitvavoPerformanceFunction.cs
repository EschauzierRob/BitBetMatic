using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitBetMatic.API;
using BitBetMatic.Repositories;
using Skender.Stock.Indicators;
using Microsoft.Azure.Functions.Worker.Http;

public class BitvavoPerformanceProcessor
{
    private readonly IApiWrapper api;
    private readonly DataLoader dataLoader;
    private readonly PortfolioManager portfolioManager;

    public BitvavoPerformanceProcessor(IApiWrapper apiWrapper, CandleRepository candleRepository)
    {
        api = apiWrapper;
        dataLoader = new DataLoader(api, candleRepository);
        portfolioManager = new PortfolioManager();
    }

    public string ProcessPerformance(string market)
    {
        var tradeDataBTC = GetTradeData(market).Result;
        var tradeDataETH = GetTradeData("ETH-EUR").Result;

        var allTrades = tradeDataBTC.Item2;
        allTrades.AddRange(tradeDataETH.Item2);

        portfolioManager.SetCash(300);

        var tokenQuotes = new List<(string market, ChartValues)> { ("BTC-EUR", tradeDataBTC.Item1), ("ETH-EUR", tradeDataETH.Item1) };
        var active = CalculateActivePortfolio(allTrades, tokenQuotes);

        // 4. Generate HTML with comparison chart
        var html = GenerateHtmlWithChart(new List<ChartValues>() { tradeDataBTC.Item1, tradeDataETH.Item1 }, active);
        return html;
    }

    private async Task<(ChartValues, List<(string, TradeData)>)> GetTradeData(string market)
    {
        // 1. Fetch portfolio and trade data
        // var portfolioData = await api.GetPortfolioData();
        var tradeData = await api.GetTradeData(market);
        // 2. Fetch BTC price history

        var startDate = tradeData.OrderBy(x => x.TimestampAsDateTime).First().TimestampAsDateTime;
        var btcPriceData = await dataLoader.LoadHistoricalData(market, "1h", 200, startDate, DateTime.UtcNow);

        // 3. Calculate baseline and active portfolio values
        var baselineValues = CalculateBaseline(btcPriceData, 150);

        return (baselineValues, tradeData.OrderBy(x => x.TimestampAsDateTime).Select(trade => (market, trade)).ToList());
    }


    private ChartValues CalculateBaseline(List<FlaggedQuote> btcPriceData, double initialInvestment)
    {
        btcPriceData.OrderBy(x => x.Date);
        var btcPriceAtStart = Convert.ToDouble(btcPriceData.Last().Close); // Use the close price of the first data point
        var btcQuantity = initialInvestment / btcPriceAtStart;

        var list = new ChartValues();
        list.AddRange(btcPriceData.OrderBy(x => x.Date).Select(data =>
            new ChartValue(data.Date,
             (decimal)(btcQuantity * Convert.ToDouble(data.Close)), data.Close, "BTC-EUR")));
        return list;
    }

    private ChartValues CalculateActivePortfolio(List<(string, TradeData)> allTrades, List<(string market, ChartValues)> tokenQuotes)
    {
        // Simplified logic: Start with portfolio value and adjust for trades
        var activeValues = new ChartValues();

        foreach (var quote in tokenQuotes[0].Item2)
        {
            var time = quote.Time;

            var btcPrice = tokenQuotes[0].Item2.First(x => x.Time == time).TokenPrice;
            var ethPrice = tokenQuotes[1].Item2.First(x => x.Time == time).TokenPrice;

            foreach (var tradeItem in allTrades.Where(t => t.Item2.TimestampAsDateTime >= time && t.Item2.TimestampAsDateTime <= time.AddHours(1)))
            {
                var tokenPrice = (tradeItem.Item2.Market=="BTC-EUR"? tokenQuotes[0]:tokenQuotes[1]).Item2.First(x => x.Time == time).TokenPrice;
                var trade = tradeItem.Item2;
                var tradeAction = new TradeAction { Action = trade.Side == "buy" ? BitBetMatic.BuySellHold.Buy : BitBetMatic.BuySellHold.Sell, CurrentTokenPrice = tokenPrice, AmountInEuro = tokenPrice * trade.Amount, Market = tradeItem.Item1, Timestamp = trade.TimestampAsDateTime };
                portfolioManager.ExecuteTrade(tradeAction);
            }

            if (btcPrice > 100000m)
            {
                int i = 0;
            }

            var graphPrice = 0m;

            var btcBalance = portfolioManager.GetAssetTokenBalance("BTC-EUR");
            var ethBalance = portfolioManager.GetAssetTokenBalance("ETH-EUR");
            var cashBalance = portfolioManager.GetCashBalance();
            var totalBalance = portfolioManager.GetAccountTotal();

            activeValues.Add(new ChartValue(time, graphPrice, btcPrice, "BTC-EUR"));
            activeValues.Add(new ChartValue(time, graphPrice, ethPrice, "ETH-EUR"));
            activeValues.Add(new ChartValue(time, cashBalance, 1.0M, "EUR"));
            activeValues.Add(new ChartValue(time, totalBalance, 1.0M, "TOT"));
        }

        return activeValues;
    }

    private string GenerateHtmlWithChart(List<ChartValues> baselineValues, ChartValues activeValues)
    {
        var baseline1 = baselineValues[0].OrderBy(x => x.Time).ToList().Where(x => x.Time > DateTime.Now.AddMonths(-2)).ToList();
        var baseline2 = baselineValues[1].OrderBy(x => x.Time).ToList().Where(x => x.Time > DateTime.Now.AddMonths(-2)).ToList();

        var portfolioData = activeValues.Where(x => x.Time > DateTime.Now.AddMonths(-2)).ToList();

        var timestamps = string.Join(",", baseline1.Select(v => $"'{v.Time:dd-MM-yyyy}'"));
        var baselineData = string.Join(",", baseline1.Select((v, index) => Math.Round(v.Value + baseline2[index].Value)));
        var activeBtcData = string.Join(",", portfolioData.Where(v => v.Market == "BTC-EUR").Select(v => Math.Round(v.Value)));
        var activeEthData = string.Join(",", portfolioData.Where(v => v.Market == "ETH-EUR").Select(v => Math.Round(v.Value)));
        var activeEurData = string.Join(",", portfolioData.Where(v => v.Market == "EUR").Select(v => Math.Round(v.Value)));
        var totalBalanceData = string.Join(",", portfolioData.Where(v => v.Market == "TOT").Select(v => Math.Round(v.Value)));

        var graph = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <script src='https://cdn.jsdelivr.net/npm/chart.js'></script>
        </head>
        <body>
        <h1>HOI!!!!<h1>
            <canvas id='chart'></canvas>
            <script>
                var ctx = document.getElementById('chart').getContext('2d');
                var chart = new Chart(ctx, {{
                    type: 'line',
                    data: {{
                        labels: [{timestamps}],
                        datasets: [
                            {{
                                label: 'Baseline (HODL)',
                                data: [{baselineData}],
                                borderColor: 'rgb(54, 162, 235)',
                                fill: false
                            }},
                            {{
                                label: 'BTC',
                                data: [{activeBtcData}],
                                borderColor: 'rgb(255, 99, 132)',
                                fill: false
                            }},
                            {{
                                label: 'ETH',
                                data: [{activeEthData}],
                                borderColor: 'rgb(252, 99, 255)',
                                fill: false
                            }},
                            {{
                                label: 'EUR',
                                data: [{activeEurData}],
                                borderColor: 'rgb(99, 255, 177)',
                                fill: false
                            }},
                            {{
                                label: 'Total portfolio',
                                data: [{totalBalanceData}],
                                borderColor: 'rgb(255, 206, 99)',
                                fill: false
                            }}
                        ]
                    }}
                }});
            </script>
        </body>
        </html>
        ";

        return graph;
    }
}

public class ChartValues : List<ChartValue>
{

}
public class ChartValue
{
    public ChartValue(DateTime Time, decimal Value, decimal TokenPrice, string Market)
    {
        this.Time = Time;
        this.Value = Value;
        this.TokenPrice = TokenPrice;
        this.Market = Market;
    }

    public DateTime Time { get; set; }
    public decimal Value { get; set; }
    public decimal TokenPrice { get; set; }
    public string Market { get; set; }
}