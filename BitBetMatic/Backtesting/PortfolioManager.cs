using System;
using System.Collections.Generic;
using System.Linq;
using BitBetMatic;
public class PortfolioManager
{
    private decimal _cashBalance;
    private decimal tradeMargin = 0.9975m;
    private Dictionary<string, (decimal tokenAmount, decimal currentPrice)> _assetBalances;

    public PortfolioManager()
    {
        _assetBalances = new Dictionary<string, (decimal tokenAmount, decimal currentPrice)>();
    }

    public void SetCash(decimal cash)
    {
        _cashBalance = cash;
    }

    public void ExecuteTrade(TradeAction action)
    {
        if (action.AmountInEuro < 5 || !(action.Action == BuySellHold.Buy || action.Action == BuySellHold.Sell)) return;

        SetTokenCurrentPrice(action.Market, action.CurrentTokenPrice);

        var assetEuroBalance = GetAssetEuroBalance(action.Market);
        var amountInEuro = action.Action == BuySellHold.Buy ?
            Math.Min(_cashBalance, action.AmountInEuro) :
            Math.Min(assetEuroBalance, action.AmountInEuro)
        ;

        decimal tokenAmount = amountInEuro / action.CurrentTokenPrice;
        EnsureTokenExists(action);
        if (action.Action == BuySellHold.Buy && _cashBalance >= tokenAmount)
        {
            AddToTokenBalance(action, tokenAmount * tradeMargin);
            _cashBalance -= amountInEuro;

            // assetEuroBalance = GetAssetEuroBalance(action.Market);
            // Console.WriteLine($"Buying {amountInEuro:F} {action.Market}, cash balance: {_cashBalance:F}, token balance: {assetEuroBalance:F}, total portfolio: {assetEuroBalance + _cashBalance:F}");
        }
        else if (action.Action == BuySellHold.Sell && _assetBalances.ContainsKey(action.Market) && _assetBalances[action.Market].tokenAmount >= tokenAmount)
        {
            _cashBalance += amountInEuro * tradeMargin;
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
        if (!_assetBalances.ContainsKey(market))
        {
            _assetBalances[market] = (0, 0);
        }
    }

    public void SetTokenBalance(string market, decimal tokenAmount, decimal currentPrice)
    {
        EnsureTokenExists(market);
        _assetBalances[market] = (tokenAmount, currentPrice);
    }

    private void AddToTokenBalance(TradeAction action, decimal tokenAmount)
    {
        EnsureTokenExists(action);
        _assetBalances[action.Market] = (_assetBalances[action.Market].tokenAmount + tokenAmount, action.CurrentTokenPrice);
    }
    private void TakeFromTokenBalance(TradeAction action, decimal tokenAmount)
    {
        EnsureTokenExists(action);
        _assetBalances[action.Market] = (_assetBalances[action.Market].tokenAmount - tokenAmount, action.CurrentTokenPrice);
    }

    public decimal GetCashBalance() => _cashBalance;

    public decimal GetAssetTokenBalance(string market) => _assetBalances.ContainsKey(market) ? _assetBalances[market].tokenAmount : 0;
    public decimal GetAssetEuroBalance(string market) => _assetBalances.ContainsKey(market) ? GetAssetTokenBalance(market) * _assetBalances[market].currentPrice : 0;

    public decimal GetAccountTotal() => _assetBalances.Sum(x => GetAssetEuroBalance(x.Key)) + GetCashBalance();

    public void SetTokenCurrentPrice(string market, decimal close)
    {
        EnsureTokenExists(market);
        _assetBalances[market] = (_assetBalances[market].tokenAmount, close);
    }
    public decimal GetCurrentTokenPrice(string market)
    {
        EnsureTokenExists(market);
        return _assetBalances[market].currentPrice;
    }
}
