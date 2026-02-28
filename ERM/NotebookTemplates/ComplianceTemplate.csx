// Notebook: Compliance & Regulatory Risk
// Category: Compliance
// Description: Menghitung risiko dan denda dari pelanggaran perlindungan data seperti GDPR.

Print("=== Compliance Risk Simulation: GDPR Penalty ===");

// 1. Parameter Input Perusahaan
double annualGlobalTurnover = 50000000.00; // $50 Juta USD
int totalCustomerRecords = 500000;
int affectedRecords = 12000; // Asumsi ada kebocoran 12.000 data

// Kategori Pelanggaran (Tingkat keparahan 1-3)
// 1 = Ringan (Kelalaian administratif)
// 2 = Sedang (Gagal melindungi data)
// 3 = Berat (Pelanggaran privasi serius dan tidak koperatif)
int severityLevel = 2; 

// 2. Regulasi Denda GDPR (Mock Rules)
// Denda maksimum adalah 4% dari Annual Global Turnover atau €20 Juta (mana yang lebih tinggi).
double maxPenaltyRate = 0.04;
double fixedPenaltyCap = 22000000.00; // Konversi ke USD kasarnya
double absoluteMax = Math.Max(annualGlobalTurnover * maxPenaltyRate, fixedPenaltyCap);

double estimatedFine = 0;
double legalFees = 150000.00; // Biaya Pengacara

if(severityLevel == 1) {
    estimatedFine = 0.005 * annualGlobalTurnover; 
} else if (severityLevel == 2) {
    estimatedFine = 0.02 * annualGlobalTurnover;
} else if (severityLevel == 3) {
    estimatedFine = absoluteMax;
}

double totalComplianceLoss = estimatedFine + legalFees;

Print($"Total Data Pelanggan: {totalCustomerRecords}");
Print($"Data Terdampak: {affectedRecords}");
Print($"Tingkat Keparahan Pelanggaran: {severityLevel}");
Print("--------------------------------------------------");

Print($"Estimasi Denda Regulator: {estimatedFine:C}");
Print($"Estimasi Biaya Legal: {legalFees:C}");
Print($"Total Potensi Kerugian Finansial Kepatuhan: {totalComplianceLoss:C}");

if (totalComplianceLoss > 1000000)
{
    Print("Warning: Biaya denda sangat kritikal! Segera aktivasi Compliance Incident Response Plan!");
}