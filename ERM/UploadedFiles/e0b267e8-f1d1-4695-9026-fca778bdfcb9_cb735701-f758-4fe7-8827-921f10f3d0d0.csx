// Notebook: Cyber Risk & Ransomware Simulation
// Category: Cyber
// Description: Simulasi Probabilitas Serangan Ransomware dan Biaya Pemulihannya (FAIR Framework approach).

Print("=== Cyber Risk Simulation: Ransomware Incident ===");

// 1. Data Historis / Asumsi (Mock Dataset)
double threatEventFrequency = 5.0; // Rata-rata diserang 5 kali setahun
double vulnerabilityLevel = 0.2; // 20% kemungkinan serangan berhasil menembus pertahanan
double lossEventFrequency = threatEventFrequency * vulnerabilityLevel;

Print($"Estimasi Serangan Berhasil Per Tahun: {lossEventFrequency} Kali");

// 2. Simulasi Biaya Jika Terjadi Serangan (Ransomware)
double incidentResponseCost = 50000.00; // Biaya IT Forensic
double downtimeCostPerDay = 100000.00; // Kerugian operasional
int expectedDowntimeDays = 5; // Rata-rata sistem mati 5 hari
double ransomDemand = 250000.00; // Tuntutan tebusan (jika terpaksa bayar)

// Pilihan Strategi: Bayar vs Pemulihan dari Backup
bool hasGoodBackup = true; 
double totalImpact = 0;

Print("\nSkenario Kejadian:");
if (hasGoodBackup)
{
    Print("Perusahaan memiliki Backup Data yang baik. Tidak perlu membayar tebusan.");
    totalImpact = incidentResponseCost + (downtimeCostPerDay * expectedDowntimeDays);
}
else
{
    Print("Perusahaan TIDAK memiliki Backup. Harus membayar tebusan.");
    // downtime biasanya lebih cepat jika decryptor bekerja, tapi biaya tebusan mahal
    totalImpact = incidentResponseCost + (downtimeCostPerDay * 2) + ransomDemand;
}

// 3. Perhitungan Risiko Tahunan (Annualized Loss Expectancy)
double annualizedLossExpectancy = lossEventFrequency * totalImpact;

Print("--------------------------------------------------");
Print($"Biaya Pemulihan Sekali Serangan: {totalImpact:C}");
Print($"Annualized Loss Expectancy (ALE): {annualizedLossExpectancy:C} per tahun");

if (annualizedLossExpectancy > 150000)
{
    Print("Rekomendasi: Upgrade Cyber Insurance dan perketat MFA/Endpoint Protection.");
}