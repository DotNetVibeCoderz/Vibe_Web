// Notebook: Operational Risk Simulation
// Category: Operational
// Description: Mengestimasi kerugian karena gangguan operasional seperti kerusakan server atau downtime.

Print("=== Operational Risk Simulation: Server Downtime ===");

// 1. Parameter Input
double hourlyRevenue = 15000.00; // Pendapatan per jam perusahaan (USD)
double repairCostPerHour = 2000.00; // Biaya perbaikan per jam (USD)
double compensationCost = 5000.00; // Kompensasi SLA ke klien (Flat USD)

// Distribusi probabilitas downtime (jam)
var downtimeProbabilities = new Dictionary<int, double>
{
    { 1, 0.50 }, // 50% kemungkinan downtime 1 jam
    { 3, 0.30 }, // 30% kemungkinan downtime 3 jam
    { 8, 0.15 }, // 15% kemungkinan downtime 8 jam
    { 24, 0.05 } // 5% kemungkinan downtime 24 jam (Bencana besar)
};

// 2. Simulasi Perhitungan
double expectedLoss = 0;

Print("Skenario dan Estimasi Kerugian:");
foreach (var scenario in downtimeProbabilities)
{
    int hours = scenario.Key;
    double probability = scenario.Value;
    
    double lossOfRevenue = hours * hourlyRevenue;
    double costOfRepair = hours * repairCostPerHour;
    double totalLoss = lossOfRevenue + costOfRepair + compensationCost;
    
    expectedLoss += totalLoss * probability;
    
    Print($"- Jika {hours} Jam Downtime (Prob: {probability*100}%): Potensi Kerugian = {totalLoss:C}");
}

// 3. Kesimpulan
Print("--------------------------------------------------");
Print($"Expected Annualized Operational Loss: {expectedLoss:C}");

if(expectedLoss > 100000)
{
    Print("Warning: Risiko tinggi! Diperlukan investasi segera pada sistem redudansi (Disaster Recovery).");
}
else
{
    Print("Status: Risiko dalam batas toleransi wajar.");
}