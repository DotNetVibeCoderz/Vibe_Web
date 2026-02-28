// Notebook: Financial Risk Simulation (Value at Risk / VaR)
// Category: Financial
// Description: Simulasi Monte Carlo sederhana untuk menghitung Value at Risk (VaR) portofolio investasi.

Print("=== Financial Risk Simulation: Value at Risk (VaR) ===");

// 1. Parameter Input (Dataset Dummy)
double initialInvestment = 1000000.00; // Investasi Awal: $1,000,000
int simulationDays = 30;
int numberOfSimulations = 10000;
double meanDailyReturn = 0.0005; // 0.05%
double dailyVolatility = 0.015; // 1.5%

var random = new Random();

// Helper untuk Normal Distribution (Box-Muller transform)
double GetNormalDistribution(double mean, double stdDev)
{
    double u1 = 1.0 - random.NextDouble();
    double u2 = 1.0 - random.NextDouble();
    double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    return mean + stdDev * randStdNormal;
}

// 2. Simulasi Monte Carlo
List<double> finalPortfolioValues = new List<double>();

Print($"Menjalankan {numberOfSimulations} simulasi untuk {simulationDays} hari ke depan...");

for (int i = 0; i < numberOfSimulations; i++)
{
    double portfolioValue = initialInvestment;
    for (int day = 0; day < simulationDays; day++)
    {
        double simulatedReturn = GetNormalDistribution(meanDailyReturn, dailyVolatility);
        portfolioValue = portfolioValue * (1 + simulatedReturn);
    }
    finalPortfolioValues.Add(portfolioValue);
}

// 3. Menghitung VaR (95% Confidence Interval)
finalPortfolioValues.Sort();
int percentileIndex = (int)(numberOfSimulations * 0.05); // 5% terburuk
double varValue = initialInvestment - finalPortfolioValues[percentileIndex];

// 4. Kesimpulan
Print("--------------------------------------------------");
Print($"Nilai Investasi Awal: {initialInvestment:C}");
Print($"Value at Risk (VaR 95%) 30-Hari: {varValue:C}");
Print($"Artinya, ada probabilitas 5% bahwa kerugian akan melebihi {varValue:C} dalam 30 hari ke depan.");