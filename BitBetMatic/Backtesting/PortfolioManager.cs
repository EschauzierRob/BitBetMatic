using System;
using System.Collections.Generic;
using System.Linq;
using BitBetMatic;
public class PortfolioManager
{
    private decimal CashBalance;
    private decimal TradeMargin = 0.9975m;
    private RiskOverlay RiskOverlay;
    private Dictionary<string, (decimal tokenAmount, decimal currentPrice)> AssetBalances;

    public PortfolioManager()
    {
        AssetBalances = new Dictionary<string, (decimal tokenAmount, decimal currentPrice)>();
        RiskOverlay = new RiskOverlay(staticStopLossThreshold: 10m, trailingStopLossPercentage: 5m);
    }

    public void SetCash(decimal cash)
    {
        CashBalance = cash;
    }

    public void ExecuteTrade(TradeAction action)
    {
        SetTokenCurrentPrice(action.Market, action.CurrentTokenPrice);

        if (action.AmountInEuro < 5 || !(action.Action == BuySellHold.Buy || action.Action == BuySellHold.Sell)) return;

        var assetEuroBalance = GetAssetEuroBalance(action.Market);
        var amountInEuro = action.Action == BuySellHold.Buy ?
            Math.Min(CashBalance, action.AmountInEuro) :
            Math.Min(assetEuroBalance, action.AmountInEuro)
        ;

        decimal tokenAmount = amountInEuro / action.CurrentTokenPrice;
        EnsureTokenExists(action);
        if (action.Action == BuySellHold.Buy && CashBalance >= tokenAmount)
        {
            AddToTokenBalance(action, tokenAmount * TradeMargin);
            CashBalance -= amountInEuro;

            // riskOverlay.UpdateEntryPrice(action.CurrentTokenPrice);

            // assetEuroBalance = GetAssetEuroBalance(action.Market);
            // Console.WriteLine($"Buying {amountInEuro:F} {action.Market}, cash balance: {_cashBalance:F}, token balance: {assetEuroBalance:F}, total portfolio: {assetEuroBalance + _cashBalance:F}");
        }
        else if (action.Action == BuySellHold.Sell && AssetBalances.ContainsKey(action.Market) && AssetBalances[action.Market].tokenAmount >= tokenAmount)
        {
            CashBalance += amountInEuro * TradeMargin;
            TakeFromTokenBalance(action, tokenAmount);

            // assetEuroBalance = GetAssetEuroBalance(action.Market);
            // Console.WriteLine($"Selling {amountInEuro:F} {action.Market}, cash balance: {_cashBalance:F}, token balance: {assetEuroBalance:F}, total portfolio: {assetEuroBalance + _cashBalance:F}");
        }
    }

    private void EnsureTokenExists(TradeAction action)
    {
        EnsureTokenExists(action.Market);
    }

    private void EnsureTokenExists(string market)
    {
        if (!AssetBalances.ContainsKey(market))
        {
            AssetBalances[market] = (0, 0);
        }
    }

    public void SetTokenBalance(string market, decimal tokenAmount, decimal currentPrice)
    {
        EnsureTokenExists(market);
        AssetBalances[market] = (tokenAmount, currentPrice);
    }

    private void AddToTokenBalance(TradeAction action, decimal tokenAmount)
    {
        EnsureTokenExists(action);
        AssetBalances[action.Market] = (AssetBalances[action.Market].tokenAmount + tokenAmount, action.CurrentTokenPrice);
    }
    private void TakeFromTokenBalance(TradeAction action, decimal tokenAmount)
    {
        EnsureTokenExists(action);
        AssetBalances[action.Market] = (AssetBalances[action.Market].tokenAmount - tokenAmount, action.CurrentTokenPrice);
    }

    public decimal GetCashBalance() => CashBalance;

    public decimal GetAssetTokenBalance(string market) => AssetBalances.ContainsKey(market) ? AssetBalances[market].tokenAmount : 0;
    public decimal GetAssetEuroBalance(string market) => AssetBalances.ContainsKey(market) ? GetAssetTokenBalance(market) * AssetBalances[market].currentPrice : 0;

    public decimal GetAccountTotal() => AssetBalances.Sum(x => GetAssetEuroBalance(x.Key)) + GetCashBalance();

    public void SetTokenCurrentPrice(string market, decimal close)
    {
        EnsureTokenExists(market);
        AssetBalances[market] = (AssetBalances[market].tokenAmount, close);

        // riskOverlay.UpdatePrice(close);
        // if (riskOverlay.ShouldSell(close))
        // {
        //     Console.WriteLine($"Sell of {market} triggered at price: {close}");
        // }
    }
    public decimal GetCurrentTokenPrice(string market)
    {
        EnsureTokenExists(market);
        return AssetBalances[market].currentPrice;
    }
}
