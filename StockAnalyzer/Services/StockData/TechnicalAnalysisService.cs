using StockAnalyzer.Models;

namespace StockAnalyzer.Services.StockData;

/// <summary>
/// Technical analysis service for calculating stock indicators.
/// Implements MA, RSI, MACD, Bollinger Bands, Stochastic, ATR, and pattern detection.
/// </summary>
public class TechnicalAnalysisService : ITechnicalAnalysisService
{
    // ==================== Moving Averages ====================

    public decimal CalculateMA(List<decimal> prices, int period)
    {
        if (prices.Count < period) return prices.LastOrDefault();
        return prices.TakeLast(period).Average();
    }

    public List<decimal> CalculateMA5(List<decimal> prices)
    {
        return CalculateMovingAverage(prices, 5);
    }

    public List<decimal> CalculateMA10(List<decimal> prices)
    {
        return CalculateMovingAverage(prices, 10);
    }

    public List<decimal> CalculateMA20(List<decimal> prices)
    {
        return CalculateMovingAverage(prices, 20);
    }

    public List<decimal> CalculateMA50(List<decimal> prices)
    {
        return CalculateMovingAverage(prices, 50);
    }

    public List<decimal> CalculateMA200(List<decimal> prices)
    {
        return CalculateMovingAverage(prices, 200);
    }

    private List<decimal> CalculateMovingAverage(List<decimal> prices, int period)
    {
        var result = new List<decimal>();
        for (int i = 0; i < prices.Count; i++)
        {
            if (i < period - 1)
            {
                result.Add(0);
            }
            else
            {
                var sum = prices.Skip(i - period + 1).Take(period).Sum();
                result.Add(sum / period);
            }
        }
        return result;
    }

    // ==================== RSI ====================

    public List<decimal> CalculateRSI(List<decimal> prices, int period = 14)
    {
        var rsi = new List<decimal>();
        if (prices.Count < period + 1)
        {
            return Enumerable.Repeat(50m, prices.Count).ToList();
        }

        for (int i = 0; i < period; i++)
            rsi.Add(50m);

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        for (int i = 1; i < prices.Count; i++)
        {
            var diff = prices[i] - prices[i - 1];
            gains.Add(diff > 0 ? diff : 0);
            losses.Add(diff < 0 ? -diff : 0);
        }

        decimal avgGain = gains.Take(period).Average();
        decimal avgLoss = losses.Take(period).Average();

        for (int i = period; i < gains.Count; i++)
        {
            avgGain = ((avgGain * (period - 1)) + gains[i]) / period;
            avgLoss = ((avgLoss * (period - 1)) + losses[i]) / period;

            if (avgLoss == 0)
                rsi.Add(100m);
            else
            {
                var rs = avgGain / avgLoss;
                rsi.Add(100m - (100m / (1m + rs)));
            }
        }

        return rsi;
    }

    // ==================== MACD ====================

    public (List<decimal> macd, List<decimal> signal, List<decimal> histogram) CalculateMACD(
        List<decimal> prices, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
    {
        var ema12 = CalculateEMA(prices, fastPeriod);
        var ema26 = CalculateEMA(prices, slowPeriod);

        var macdLine = new List<decimal>();
        for (int i = 0; i < prices.Count; i++)
        {
            if (i < slowPeriod - 1)
                macdLine.Add(0);
            else
                macdLine.Add(ema12[i] - ema26[i]);
        }

        var signalLine = CalculateEMA(macdLine, signalPeriod);
        var histogram = new List<decimal>();

        for (int i = 0; i < prices.Count; i++)
        {
            histogram.Add(macdLine[i] - signalLine[i]);
        }

        return (macdLine, signalLine, histogram);
    }

    private List<decimal> CalculateEMA(List<decimal> prices, int period)
    {
        var ema = new List<decimal>();
        decimal multiplier = 2m / (period + 1);

        for (int i = 0; i < prices.Count; i++)
        {
            if (i == 0)
                ema.Add(prices[i]);
            else if (i < period - 1)
                ema.Add(0);
            else if (i == period - 1)
                ema.Add(prices.Take(period).Average());
            else
                ema.Add((prices[i] - ema[i - 1]) * multiplier + ema[i - 1]);
        }

        return ema;
    }

    // ==================== Bollinger Bands ====================

    public (List<decimal> upper, List<decimal> middle, List<decimal> lower) CalculateBollingerBands(
        List<decimal> prices, int period = 20, double multiplier = 2.0)
    {
        var middle = CalculateMovingAverage(prices, period);
        var upper = new List<decimal>();
        var lower = new List<decimal>();

        for (int i = 0; i < prices.Count; i++)
        {
            if (i < period - 1)
            {
                upper.Add(0);
                lower.Add(0);
            }
            else
            {
                var slice = prices.Skip(i - period + 1).Take(period).ToList();
                var avg = slice.Average();
                var sumSquaredDiff = slice.Sum(p => (double)((p - avg) * (p - avg)));
                var stdDev = (decimal)Math.Sqrt(sumSquaredDiff / period);

                upper.Add(middle[i] + (stdDev * (decimal)multiplier));
                lower.Add(middle[i] - (stdDev * (decimal)multiplier));
            }
        }

        return (upper, middle, lower);
    }

    // ==================== Stochastic Oscillator ====================

    public (List<decimal> k, List<decimal> d) CalculateStochastic(
        List<decimal> high, List<decimal> low, List<decimal> close, int period = 14, int smoothK = 3, int smoothD = 3)
    {
        var kRaw = new List<decimal>();

        for (int i = 0; i < close.Count; i++)
        {
            if (i < period - 1)
            {
                kRaw.Add(50m);
            }
            else
            {
                var highestHigh = high.Skip(i - period + 1).Take(period).Max();
                var lowestLow = low.Skip(i - period + 1).Take(period).Min();
                if (highestHigh == lowestLow)
                    kRaw.Add(50m);
                else
                    kRaw.Add(((close[i] - lowestLow) / (highestHigh - lowestLow)) * 100m);
            }
        }

        var k = CalculateMovingAverage(kRaw, smoothK);
        var d = CalculateMovingAverage(k, smoothD);

        return (k, d);
    }

    // ==================== ATR ====================

    public List<decimal> CalculateATR(List<decimal> high, List<decimal> low, List<decimal> close, int period = 14)
    {
        var trueRanges = new List<decimal>();

        for (int i = 0; i < close.Count; i++)
        {
            if (i == 0)
            {
                trueRanges.Add(high[i] - low[i]);
            }
            else
            {
                var tr1 = high[i] - low[i];
                var tr2 = Math.Abs(high[i] - close[i - 1]);
                var tr3 = Math.Abs(low[i] - close[i - 1]);
                trueRanges.Add(Math.Max(tr1, Math.Max(tr2, tr3)));
            }
        }

        var atr = new List<decimal>();
        for (int i = 0; i < trueRanges.Count; i++)
        {
            if (i < period - 1)
                atr.Add(0);
            else if (i == period - 1)
                atr.Add(trueRanges.Take(period).Average());
            else
                atr.Add(((atr[i - 1] * (period - 1)) + trueRanges[i]) / period);
        }

        return atr;
    }

    // ==================== Trend Analysis ====================

    public string DetermineTrend(List<decimal> prices)
    {
        if (prices.Count < 50) return "Insufficient Data";

        var ma20 = CalculateMA(prices, 20);
        var ma50 = CalculateMA(prices, 50);
        var currentPrice = prices.Last();

        if (currentPrice > ma20 && ma20 > ma50) return "Bullish";
        if (currentPrice < ma20 && ma20 < ma50) return "Bearish";
        if (currentPrice > ma50 && ma20 < ma50) return "Sideways-Bullish";
        if (currentPrice < ma50 && ma20 > ma50) return "Sideways-Bearish";

        return "Sideways";
    }

    public string DetectCandlestickPattern(decimal open, decimal high, decimal low, decimal close,
        decimal prevOpen, decimal prevClose)
    {
        var body = Math.Abs(close - open);
        var upperShadow = high - Math.Max(open, close);
        var lowerShadow = Math.Min(open, close) - low;
        var totalRange = high - low;

        if (totalRange > 0 && body / totalRange < 0.1m)
            return "Doji";
        if (totalRange > 0 && lowerShadow > body * 2 && upperShadow < body * 0.3m)
            return "Hammer (Bullish Reversal)";
        if (totalRange > 0 && upperShadow > body * 2 && lowerShadow < body * 0.3m)
            return "Shooting Star (Bearish Reversal)";
        if (close > open && prevClose < prevOpen && close > prevOpen && open < prevClose)
            return "Bullish Engulfing";
        if (close < open && prevClose > prevOpen && close < prevOpen && open > prevClose)
            return "Bearish Engulfing";
        if (totalRange > 0 && body / totalRange > 0.9m)
            return close > open ? "Bullish Marubozu" : "Bearish Marubozu";

        return "Normal";
    }

    // ==================== Technical Score ====================

    public double CalculateTechnicalScore(List<TechnicalIndicator> indicators)
    {
        if (indicators == null || indicators.Count < 20) return 50;

        double score = 50;
        var latest = indicators.Last();

        if (latest.RSI.HasValue)
        {
            if (latest.RSI.Value >= 30 && latest.RSI.Value <= 70)
                score += 10;
            else if (latest.RSI.Value < 30)
                score += 15;
            else if (latest.RSI.Value > 70)
                score -= 10;
        }

        if (latest.MACD.HasValue && latest.MACDSignal.HasValue)
        {
            if (latest.MACD > latest.MACDSignal)
                score += 10;
            else
                score -= 5;
        }

        if (latest.MA20.HasValue && latest.ClosePrice > latest.MA20)
            score += 5;
        else if (latest.MA20.HasValue)
            score -= 5;

        if (latest.MA50.HasValue && latest.ClosePrice > latest.MA50)
            score += 5;
        else if (latest.MA50.HasValue)
            score -= 5;

        // Fix: Volume Average returns double, use double literal
        var avgVolume = indicators.TakeLast(20).Average(i => (double)i.Volume);
        if ((double)latest.Volume > avgVolume * 1.5)
            score += 5;

        if (latest.BollingerLower.HasValue && latest.BollingerUpper.HasValue)
        {
            if (latest.ClosePrice <= latest.BollingerLower)
                score += 10;
            else if (latest.ClosePrice >= latest.BollingerUpper)
                score -= 5;
        }

        return Math.Clamp(score, 0, 100);
    }
}
