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
        decimal tokenAmount = action.AmountInEuro / action.CurrentTokenPrice;
        if (!_assetBalances.ContainsKey(action.Market))
        {
            _assetBalances[action.Market] = (0, 0);
        }
        if (action.Action == BuySellHold.Buy && _cashBalance >= tokenAmount)
        {
            AddToTokenBalance(action, tokenAmount * tradeMargin);
            _cashBalance -= action.AmountInEuro;
        }
        else if (action.Action == BuySellHold.Sell && _assetBalances.ContainsKey(action.Market) && _assetBalances[action.Market].tokenAmount >= tokenAmount)
        {
            _cashBalance += action.AmountInEuro * tradeMargin;
            TakeFromTokenBalance(action, tokenAmount);
        }
    }

    public void SetTokenBalance(string market, decimal tokenAmount, decimal currentPrice)
    {
        _assetBalances[market] = (tokenAmount, currentPrice);
    }

    private void AddToTokenBalance(TradeAction action, decimal tokenAmount)
    {
        _assetBalances[action.Market] = (_assetBalances[action.Market].tokenAmount + tokenAmount, action.CurrentTokenPrice);
    }
    private void TakeFromTokenBalance(TradeAction action, decimal tokenAmount)
    {
        _assetBalances[action.Market] = (_assetBalances[action.Market].tokenAmount - tokenAmount, action.CurrentTokenPrice);
    }

    public decimal GetCashBalance() => _cashBalance;

    public decimal GetAssetTokenBalance(string market) => _assetBalances.ContainsKey(market) ? _assetBalances[market].tokenAmount : 0;
    public decimal GetAssetEuroBalance(string market) => _assetBalances.ContainsKey(market) ? GetAssetTokenBalance(market) * _assetBalances[market].currentPrice : 0;

    public decimal GetAccountTotal() => _assetBalances.Sum(x => GetAssetEuroBalance(x.Key)) + GetCashBalance();

    public void SetTokenCurrentPrice(string market, decimal close)
    {
        _assetBalances[market] = (_assetBalances[market].tokenAmount, close);
    }
}
