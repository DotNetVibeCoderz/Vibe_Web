namespace MyClinic.Data
{
    public class MonthlyRevenue
    {
        public string Month { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PaidBillsCount { get; set; }
    }

    public class PatientVisitStats
    {
        public string Month { get; set; }
        public int VisitCount { get; set; }
    }

    public class DiagnosisStats
    {
        public string Diagnosis { get; set; }
        public int Count { get; set; }
    }

    public class PaymentMethodStats
    {
        public string Method { get; set; }
        public decimal Amount { get; set; }
    }

    public class DoctorPerformanceStats
    {
        public string DoctorName { get; set; }
        public int PatientCount { get; set; }
    }
}
