using Microsoft.EntityFrameworkCore;
using VirtualDoctor.Data;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services.Analytics;

// ============================================================
// Filter & DTO
// ============================================================

public enum Granularity { Day, Week, Month }

public class DashboardFilter
{
    public DateTime From { get; set; } = DateTime.UtcNow.Date.AddDays(-29);
    public DateTime To { get; set; } = DateTime.UtcNow.Date;
    public Granularity Granularity { get; set; } = Granularity.Day;
    public string? Specialization { get; set; }
    public string? City { get; set; }
    public string? Channel { get; set; }   // Semua | Konsultasi | Appointment | Farmasi | Homecare

    public int DayCount => Math.Max(1, (int)(To.Date - From.Date).TotalDays + 1);
    public DateTime PrevFrom => From.AddDays(-DayCount);
    public DateTime PrevTo => From.AddDays(-1);

    public DashboardFilter Clone() => (DashboardFilter)MemberwiseClone();
}

/// <summary>KPI dengan tren periode sebelumnya dan deret untuk sparkline.</summary>
public record KpiCard(
    string Key,
    string Label,
    double Value,
    double Previous,
    string Format,
    IReadOnlyList<double> Series)
{
    public double DeltaPct => Previous <= 0 ? (Value > 0 ? 100 : 0) : (Value - Previous) / Previous * 100.0;
    public bool IsUp => Value >= Previous;
}

public record SeriesPoint(DateTime Date, double Value);
public record NamedSeries(string Name, IReadOnlyList<SeriesPoint> Points);
public record Slice(string Label, double Value);
public record RankedItem(string Label, string? Sub, double Value, double Secondary);
public record HeatCell(int Day, int Hour, double Value);

public record DoctorPerformance(
    string DoctorId, string Name, string Specialization,
    int Consultations, int Appointments, double Rating, int Reviews, decimal Revenue);

public record TransactionRow(
    DateTime Date, string Type, string Reference, string Patient,
    string Detail, string Status, decimal Amount);

public record FinanceRow(
    DateTime Date, string PaymentId, string Invoice, string Patient,
    string Reference, string Channel, string Provider, string Status, decimal Total, DateTime? PaidAt);

/// <summary>
/// Laporan keuangan berbasis tabel Payments. Terpisah dari <see cref="DashboardData.Revenue"/>
/// yang menghitung nilai transaksi; di sini yang dihitung adalah uang yang benar-benar tertagih.
/// </summary>
public class FinanceData
{
    public List<KpiCard> Kpis { get; set; } = new();
    /// <summary>Dua deret: tagihan diterbitkan dan kas masuk.</summary>
    public List<NamedSeries> CashFlow { get; set; } = new();
    public List<Slice> ChannelMix { get; set; } = new();
    public List<Slice> ServiceMix { get; set; } = new();
    public List<Slice> StateMix { get; set; } = new();
    /// <summary>Umur piutang: label, jumlah tagihan (Value), nilai rupiah (Secondary).</summary>
    public List<RankedItem> Aging { get; set; } = new();
    public List<RankedItem> ProviderMix { get; set; } = new();
    public List<FinanceRow> Recent { get; set; } = new();

    public decimal Billed { get; set; }
    public decimal Collected { get; set; }
    public decimal Outstanding { get; set; }
    public decimal Uncollectible { get; set; }
    public decimal Refunded { get; set; }
    public decimal ServiceFeeEarned { get; set; }
    public decimal DiscountGiven { get; set; }
    public decimal InsuranceCovered { get; set; }
    public decimal AverageTicket { get; set; }

    public int PaidCount { get; set; }
    public int OutstandingCount { get; set; }
    public int AwaitingVerificationCount { get; set; }
    public int OverdueCount { get; set; }

    /// <summary>Persentase nilai tagihan periode ini yang sudah tertagih.</summary>
    public double CollectionRate { get; set; }
    public bool HasData { get; set; }
}

public class DashboardData
{
    public List<KpiCard> Kpis { get; set; } = new();
    public List<NamedSeries> Activity { get; set; } = new();
    public List<SeriesPoint> Revenue { get; set; } = new();
    public List<Slice> RevenueMix { get; set; } = new();
    public List<Slice> AppointmentStatus { get; set; } = new();
    public List<Slice> OrderStatus { get; set; } = new();
    public List<RankedItem> TopDoctors { get; set; } = new();
    public List<RankedItem> TopMedicines { get; set; } = new();
    public List<HeatCell> Heatmap { get; set; } = new();
    public List<DoctorPerformance> DoctorTable { get; set; } = new();
    public List<TransactionRow> Transactions { get; set; } = new();
    public FinanceData Finance { get; set; } = new();
    public List<string> Specializations { get; set; } = new();
    public List<string> Cities { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}

public interface IDashboardService
{
    Task<DashboardData> GetAsync(DashboardFilter filter, CancellationToken ct = default);
}

// ============================================================
// Implementasi
// ============================================================

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardData> GetAsync(DashboardFilter f, CancellationToken ct = default)
    {
        var from = f.From.Date;
        var to = f.To.Date.AddDays(1).AddTicks(-1);
        var prevFrom = f.PrevFrom.Date;
        var prevTo = f.PrevTo.Date.AddDays(1).AddTicks(-1);
        var windowStart = prevFrom; // ambil sekaligus dua periode

        // --- proyeksi minimal agar aman di SQLite/SqlServer/MySql/PostgreSQL ---
        var doctors = await _db.Doctors.AsNoTracking()
            .Select(d => new { d.Id, d.FullName, d.Specialization, d.Rating, d.ConsultationFee })
            .ToListAsync(ct);
        var doctorById = doctors.ToDictionary(d => d.Id);

        var specFilter = string.IsNullOrWhiteSpace(f.Specialization) ? null : f.Specialization;
        var allowedDoctorIds = specFilter == null
            ? null
            : doctors.Where(d => d.Specialization == specFilter).Select(d => d.Id).ToHashSet();

        var hospitals = await _db.Hospitals.AsNoTracking()
            .Select(h => new { h.Id, h.Name, h.City }).ToListAsync(ct);
        var cityByHospital = hospitals.ToDictionary(h => h.Id, h => h.City ?? "-");

        var consultations = await _db.Consultations.AsNoTracking()
            .Where(c => c.StartedAt >= windowStart && c.StartedAt <= to)
            .Select(c => new { c.Id, c.DoctorId, c.UserId, c.StartedAt, c.Status, c.Fee, c.Type })
            .ToListAsync(ct);

        var appointments = await _db.Appointments.AsNoTracking()
            .Where(a => a.CreatedAt >= windowStart && a.CreatedAt <= to)
            .Select(a => new { a.Id, a.DoctorId, a.UserId, a.HospitalId, a.CreatedAt, a.AppointmentDate, a.StartTime, a.Status, a.EstimatedCost, a.Type })
            .ToListAsync(ct);

        var orders = await _db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= windowStart && o.CreatedAt <= to)
            .Select(o => new { o.Id, o.UserId, o.CreatedAt, o.Status, o.Total, o.PaymentStatus })
            .ToListAsync(ct);

        var orderItems = await _db.OrderItems.AsNoTracking()
            .Where(i => _db.Orders.Any(o => o.Id == i.OrderId && o.CreatedAt >= from && o.CreatedAt <= to))
            .Select(i => new { i.MedicineName, i.Quantity, i.Subtotal })
            .ToListAsync(ct);

        var homecare = await _db.HomecareServices.AsNoTracking()
            .Where(h => h.CreatedAt >= windowStart && h.CreatedAt <= to)
            .Select(h => new { h.Id, h.UserId, h.CreatedAt, h.Status, h.Fee, h.ServiceType })
            .ToListAsync(ct);

        var reviews = await _db.DoctorReviews.AsNoTracking()
            .Select(r => new { r.DoctorId, r.Rating, r.CreatedAt })
            .ToListAsync(ct);

        var users = await _db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.FullName, u.CreatedAt }).ToListAsync(ct);
        var userName = users.ToDictionary(u => u.Id, u => u.FullName);

        // --- terapkan filter spesialisasi / kota ---
        bool DoctorOk(string? id) => allowedDoctorIds == null || (id != null && allowedDoctorIds.Contains(id));
        bool CityOk(string? hospitalId) =>
            string.IsNullOrWhiteSpace(f.City) ||
            (hospitalId != null && cityByHospital.TryGetValue(hospitalId, out var c) && c == f.City);

        var cons = consultations.Where(c => DoctorOk(c.DoctorId)).ToList();
        var appts = appointments.Where(a => DoctorOk(a.DoctorId) && CityOk(a.HospitalId)).ToList();
        var ords = orders.ToList();
        var homes = homecare.ToList();

        // channel filter hanya membatasi kartu tabel/aktivitas, bukan KPI global
        var channel = f.Channel;

        var data = new DashboardData
        {
            Specializations = doctors.Select(d => d.Specialization).Where(s => !string.IsNullOrWhiteSpace(s))
                                     .Distinct().OrderBy(s => s).ToList(),
            Cities = hospitals.Select(h => h.City ?? "").Where(c => !string.IsNullOrWhiteSpace(c))
                              .Distinct().OrderBy(c => c).ToList()
        };

        // --- KPI ---
        var buckets = BuildBuckets(from, f.To.Date, f.Granularity);

        double ConsCur() => cons.Count(c => c.StartedAt >= from && c.StartedAt <= to);
        double ConsPrev() => cons.Count(c => c.StartedAt >= prevFrom && c.StartedAt <= prevTo);
        double ApptCur() => appts.Count(a => a.CreatedAt >= from && a.CreatedAt <= to);
        double ApptPrev() => appts.Count(a => a.CreatedAt >= prevFrom && a.CreatedAt <= prevTo);
        double OrdCur() => ords.Count(o => o.CreatedAt >= from && o.CreatedAt <= to);
        double OrdPrev() => ords.Count(o => o.CreatedAt >= prevFrom && o.CreatedAt <= prevTo);

        decimal RevenueIn(DateTime a, DateTime b) =>
            cons.Where(c => c.StartedAt >= a && c.StartedAt <= b).Sum(c => c.Fee)
            + ords.Where(o => o.CreatedAt >= a && o.CreatedAt <= b).Sum(o => o.Total)
            + homes.Where(h => h.CreatedAt >= a && h.CreatedAt <= b).Sum(h => h.Fee)
            + appts.Where(x => x.CreatedAt >= a && x.CreatedAt <= b).Sum(x => x.EstimatedCost);

        var activePatients = cons.Where(c => c.StartedAt >= from && c.StartedAt <= to).Select(c => c.UserId)
            .Concat(appts.Where(a => a.CreatedAt >= from && a.CreatedAt <= to).Select(a => a.UserId))
            .Concat(ords.Where(o => o.CreatedAt >= from && o.CreatedAt <= to).Select(o => o.UserId))
            .Distinct().Count();
        var activePatientsPrev = cons.Where(c => c.StartedAt >= prevFrom && c.StartedAt <= prevTo).Select(c => c.UserId)
            .Concat(appts.Where(a => a.CreatedAt >= prevFrom && a.CreatedAt <= prevTo).Select(a => a.UserId))
            .Concat(ords.Where(o => o.CreatedAt >= prevFrom && o.CreatedAt <= prevTo).Select(o => o.UserId))
            .Distinct().Count();

        data.Kpis.Add(new KpiCard("consultations", "Konsultasi", ConsCur(), ConsPrev(), "int",
            Bucketize(buckets, cons.Where(c => c.StartedAt >= from).Select(c => (c.StartedAt, 1d)), f.Granularity)));
        data.Kpis.Add(new KpiCard("appointments", "Janji Temu", ApptCur(), ApptPrev(), "int",
            Bucketize(buckets, appts.Where(a => a.CreatedAt >= from).Select(a => (a.CreatedAt, 1d)), f.Granularity)));
        data.Kpis.Add(new KpiCard("orders", "Pesanan Obat", OrdCur(), OrdPrev(), "int",
            Bucketize(buckets, ords.Where(o => o.CreatedAt >= from).Select(o => (o.CreatedAt, 1d)), f.Granularity)));
        data.Kpis.Add(new KpiCard("revenue", "Pendapatan", (double)RevenueIn(from, to), (double)RevenueIn(prevFrom, prevTo), "money",
            Bucketize(buckets,
                cons.Where(c => c.StartedAt >= from).Select(c => (c.StartedAt, (double)c.Fee))
                    .Concat(ords.Where(o => o.CreatedAt >= from).Select(o => (o.CreatedAt, (double)o.Total)))
                    .Concat(homes.Where(h => h.CreatedAt >= from).Select(h => (h.CreatedAt, (double)h.Fee))),
                f.Granularity)));
        data.Kpis.Add(new KpiCard("patients", "Pasien Aktif", activePatients, activePatientsPrev, "int",
            Bucketize(buckets, cons.Where(c => c.StartedAt >= from).Select(c => (c.StartedAt, 1d)), f.Granularity)));

        // --- deret aktivitas ---
        var actSeries = new List<NamedSeries>();
        if (channel is null or "Semua" or "Konsultasi")
            actSeries.Add(new NamedSeries("Konsultasi", ToPoints(buckets, cons.Where(c => c.StartedAt >= from).Select(c => (c.StartedAt, 1d)), f.Granularity)));
        if (channel is null or "Semua" or "Appointment")
            actSeries.Add(new NamedSeries("Janji Temu", ToPoints(buckets, appts.Where(a => a.CreatedAt >= from).Select(a => (a.CreatedAt, 1d)), f.Granularity)));
        if (channel is null or "Semua" or "Farmasi")
            actSeries.Add(new NamedSeries("Pesanan", ToPoints(buckets, ords.Where(o => o.CreatedAt >= from).Select(o => (o.CreatedAt, 1d)), f.Granularity)));
        if (channel is null or "Semua" or "Homecare")
            actSeries.Add(new NamedSeries("Homecare", ToPoints(buckets, homes.Where(h => h.CreatedAt >= from).Select(h => (h.CreatedAt, 1d)), f.Granularity)));
        data.Activity = actSeries;

        data.Revenue = ToPoints(buckets,
            cons.Where(c => c.StartedAt >= from).Select(c => (c.StartedAt, (double)c.Fee))
                .Concat(ords.Where(o => o.CreatedAt >= from).Select(o => (o.CreatedAt, (double)o.Total)))
                .Concat(homes.Where(h => h.CreatedAt >= from).Select(h => (h.CreatedAt, (double)h.Fee)))
                .Concat(appts.Where(a => a.CreatedAt >= from).Select(a => (a.CreatedAt, (double)a.EstimatedCost))),
            f.Granularity);

        // --- komposisi ---
        data.RevenueMix = new()
        {
            new("Konsultasi", (double)cons.Where(c => c.StartedAt >= from).Sum(c => c.Fee)),
            new("Janji Temu", (double)appts.Where(a => a.CreatedAt >= from).Sum(a => a.EstimatedCost)),
            new("Farmasi", (double)ords.Where(o => o.CreatedAt >= from).Sum(o => o.Total)),
            new("Homecare", (double)homes.Where(h => h.CreatedAt >= from).Sum(h => h.Fee))
        };
        data.RevenueMix.RemoveAll(s => s.Value <= 0);

        data.AppointmentStatus = appts.Where(a => a.CreatedAt >= from)
            .GroupBy(a => a.Status).Select(g => new Slice(StatusLabel(g.Key), g.Count()))
            .OrderByDescending(s => s.Value).ToList();

        data.OrderStatus = ords.Where(o => o.CreatedAt >= from)
            .GroupBy(o => o.Status).Select(g => new Slice(StatusLabel(g.Key), g.Count()))
            .OrderByDescending(s => s.Value).ToList();

        // --- peringkat ---
        data.TopDoctors = cons.Where(c => c.StartedAt >= from)
            .GroupBy(c => c.DoctorId)
            .Select(g => new RankedItem(
                doctorById.TryGetValue(g.Key, out var d) ? d.FullName : g.Key,
                doctorById.TryGetValue(g.Key, out var d2) ? d2.Specialization : null,
                g.Count(),
                (double)g.Sum(x => x.Fee)))
            .OrderByDescending(r => r.Value).Take(8).ToList();

        data.TopMedicines = orderItems
            .GroupBy(i => i.MedicineName)
            .Select(g => new RankedItem(g.Key, null, g.Sum(x => x.Quantity), (double)g.Sum(x => x.Subtotal)))
            .OrderByDescending(r => r.Value).Take(8).ToList();

        // --- heatmap hari x jam (dari jam praktik janji temu + jam konsultasi) ---
        var heat = new Dictionary<(int, int), double>();
        foreach (var a in appts.Where(x => x.CreatedAt >= from))
        {
            var key = ((int)a.AppointmentDate.DayOfWeek, a.StartTime.Hours);
            heat[key] = heat.GetValueOrDefault(key) + 1;
        }
        foreach (var c in cons.Where(x => x.StartedAt >= from))
        {
            var local = c.StartedAt.ToLocalTime();
            var key = ((int)local.DayOfWeek, local.Hour);
            heat[key] = heat.GetValueOrDefault(key) + 1;
        }
        data.Heatmap = heat.Select(kv => new HeatCell(kv.Key.Item1, kv.Key.Item2, kv.Value)).ToList();

        // --- tabel performa dokter ---
        data.DoctorTable = doctors
            .Where(d => allowedDoctorIds == null || allowedDoctorIds.Contains(d.Id))
            .Select(d =>
            {
                var dc = cons.Where(c => c.DoctorId == d.Id && c.StartedAt >= from).ToList();
                var da = appts.Where(a => a.DoctorId == d.Id && a.CreatedAt >= from).ToList();
                var dr = reviews.Where(r => r.DoctorId == d.Id).ToList();
                return new DoctorPerformance(
                    d.Id, d.FullName, d.Specialization,
                    dc.Count, da.Count,
                    dr.Count > 0 ? Math.Round(dr.Average(r => r.Rating), 2) : d.Rating,
                    dr.Count,
                    dc.Sum(x => x.Fee) + da.Sum(x => x.EstimatedCost));
            })
            .OrderByDescending(d => d.Consultations + d.Appointments)
            .ToList();

        // --- tabel transaksi terbaru ---
        var tx = new List<TransactionRow>();
        tx.AddRange(cons.Where(c => c.StartedAt >= from).Select(c => new TransactionRow(
            c.StartedAt, "Konsultasi", ShortId(c.Id),
            userName.GetValueOrDefault(c.UserId, "-"),
            doctorById.TryGetValue(c.DoctorId, out var d) ? d.FullName : "-",
            StatusLabel(c.Status), c.Fee)));
        tx.AddRange(appts.Where(a => a.CreatedAt >= from).Select(a => new TransactionRow(
            a.CreatedAt, "Janji Temu", ShortId(a.Id),
            userName.GetValueOrDefault(a.UserId, "-"),
            (doctorById.TryGetValue(a.DoctorId, out var d) ? d.FullName : "-") + " · " + a.AppointmentDate.ToString("dd MMM"),
            StatusLabel(a.Status), a.EstimatedCost)));
        tx.AddRange(ords.Where(o => o.CreatedAt >= from).Select(o => new TransactionRow(
            o.CreatedAt, "Farmasi", ShortId(o.Id),
            userName.GetValueOrDefault(o.UserId, "-"),
            PaymentLabel(o.PaymentStatus), StatusLabel(o.Status), o.Total)));
        tx.AddRange(homes.Where(h => h.CreatedAt >= from).Select(h => new TransactionRow(
            h.CreatedAt, "Homecare", ShortId(h.Id),
            userName.GetValueOrDefault(h.UserId, "-"),
            HomecareLabel(h.ServiceType), StatusLabel(h.Status), h.Fee)));

        if (!string.IsNullOrWhiteSpace(channel) && channel != "Semua")
            tx = tx.Where(t => t.Type == channel || (channel == "Farmasi" && t.Type == "Farmasi")).ToList();

        data.Transactions = tx.OrderByDescending(t => t.Date).Take(100).ToList();

        data.Finance = await BuildFinanceAsync(f, buckets, from, to, prevFrom, prevTo, windowStart, userName, ct);

        return data;
    }

    // ============================================================
    // Laporan keuangan
    // ============================================================

    private async Task<FinanceData> BuildFinanceAsync(
        DashboardFilter f, List<DateTime> buckets,
        DateTime from, DateTime to, DateTime prevFrom, DateTime prevTo, DateTime windowStart,
        Dictionary<string, string> userName, CancellationToken ct)
    {
        var fin = new FinanceData();

        // Tagihan yang lahir di jendela dua periode, ditambah semua tagihan yang masih
        // menggantung berapa pun umurnya — piutang lama justru yang paling perlu terlihat.
        var payments = await _db.Payments.AsNoTracking()
            .Where(p => (p.CreatedAt >= windowStart && p.CreatedAt <= to)
                     || (p.PaidAt != null && p.PaidAt >= windowStart && p.PaidAt <= to)
                     || p.State == PaymentState.Pending
                     || p.State == PaymentState.AwaitingVerification)
            .Select(p => new
            {
                p.Id, p.InvoiceNumber, p.UserId, p.ReferenceType, p.Channel, p.Provider, p.State,
                p.Total, p.ServiceFee, p.Discount, p.InsuranceCoverage,
                p.CreatedAt, p.PaidAt, p.ExpiresAt
            })
            .ToListAsync(ct);

        fin.HasData = payments.Count > 0;
        if (!fin.HasData) return fin;

        bool BilledIn(DateTime a, DateTime b, DateTime created) => created >= a && created <= b;
        bool PaidIn(DateTime a, DateTime b, DateTime? paidAt) => paidAt != null && paidAt >= a && paidAt <= b;

        var billedNow = payments.Where(p => BilledIn(from, to, p.CreatedAt)).ToList();
        var billedPrev = payments.Where(p => BilledIn(prevFrom, prevTo, p.CreatedAt)).ToList();
        var paidNow = payments.Where(p => p.State == PaymentState.Paid && PaidIn(from, to, p.PaidAt)).ToList();
        var paidPrev = payments.Where(p => p.State == PaymentState.Paid && PaidIn(prevFrom, prevTo, p.PaidAt)).ToList();
        var outstanding = payments.Where(p => p.State is PaymentState.Pending or PaymentState.AwaitingVerification).ToList();

        fin.Billed = billedNow.Sum(p => p.Total);
        fin.Collected = paidNow.Sum(p => p.Total);
        fin.Outstanding = outstanding.Sum(p => p.Total);
        fin.Uncollectible = billedNow.Where(p => p.State is PaymentState.Failed or PaymentState.Expired).Sum(p => p.Total);
        fin.Refunded = billedNow.Where(p => p.State == PaymentState.Refunded).Sum(p => p.Total);
        fin.ServiceFeeEarned = paidNow.Sum(p => p.ServiceFee);
        fin.DiscountGiven = billedNow.Sum(p => p.Discount);
        fin.InsuranceCovered = billedNow.Sum(p => p.InsuranceCoverage);

        fin.PaidCount = paidNow.Count;
        fin.OutstandingCount = outstanding.Count;
        fin.AwaitingVerificationCount = outstanding.Count(p => p.State == PaymentState.AwaitingVerification);
        fin.OverdueCount = outstanding.Count(p => p.ExpiresAt != null && p.ExpiresAt < DateTime.UtcNow);

        fin.AverageTicket = fin.PaidCount > 0 ? Math.Round(fin.Collected / fin.PaidCount) : 0;
        fin.CollectionRate = fin.Billed > 0 ? (double)(fin.Collected / fin.Billed) * 100.0 : 0;

        var prevCollected = paidPrev.Sum(p => p.Total);
        var prevBilled = billedPrev.Sum(p => p.Total);
        var prevRate = prevBilled > 0 ? (double)(prevCollected / prevBilled) * 100.0 : 0;
        var prevTicket = paidPrev.Count > 0 ? (double)(prevCollected / paidPrev.Count) : 0;

        fin.Kpis.Add(new KpiCard("cash_in", "Kas Masuk", (double)fin.Collected, (double)prevCollected, "money",
            Bucketize(buckets, paidNow.Select(p => (p.PaidAt!.Value, (double)p.Total)), f.Granularity)));
        fin.Kpis.Add(new KpiCard("billed", "Tagihan Terbit", (double)fin.Billed, (double)prevBilled, "money",
            Bucketize(buckets, billedNow.Select(p => (p.CreatedAt, (double)p.Total)), f.Granularity)));
        fin.Kpis.Add(new KpiCard("collection_rate", "Tingkat Penagihan", Math.Round(fin.CollectionRate, 1), Math.Round(prevRate, 1), "percent",
            Bucketize(buckets, paidNow.Select(p => (p.PaidAt!.Value, (double)p.Total)), f.Granularity)));
        fin.Kpis.Add(new KpiCard("avg_ticket", "Nilai Rata-rata", (double)fin.AverageTicket, prevTicket, "money",
            Bucketize(buckets, paidNow.Select(p => (p.PaidAt!.Value, (double)p.Total)), f.Granularity)));
        fin.Kpis.Add(new KpiCard("receivable", "Piutang Berjalan", (double)fin.Outstanding, (double)fin.Outstanding, "money",
            Bucketize(buckets, outstanding.Where(p => p.CreatedAt >= from).Select(p => (p.CreatedAt, (double)p.Total)), f.Granularity)));

        fin.CashFlow = new()
        {
            new NamedSeries("Tagihan terbit", ToPoints(buckets, billedNow.Select(p => (p.CreatedAt, (double)p.Total)), f.Granularity)),
            new NamedSeries("Kas masuk", ToPoints(buckets, paidNow.Select(p => (p.PaidAt!.Value, (double)p.Total)), f.Granularity))
        };

        fin.ChannelMix = paidNow.GroupBy(p => p.Channel)
            .Select(g => new Slice(PaymentLabels.Channel(g.Key), (double)g.Sum(x => x.Total)))
            .OrderByDescending(s => s.Value).ToList();

        fin.ServiceMix = paidNow.GroupBy(p => p.ReferenceType)
            .Select(g => new Slice(PaymentLabels.Reference(g.Key), (double)g.Sum(x => x.Total)))
            .OrderByDescending(s => s.Value).ToList();

        fin.StateMix = billedNow.GroupBy(p => p.State)
            .Select(g => new Slice(PaymentLabels.State(g.Key), g.Count()))
            .OrderByDescending(s => s.Value).ToList();

        fin.ProviderMix = paidNow.GroupBy(p => p.Provider)
            .Select(g => new RankedItem(g.Key, $"{g.Count()} transaksi", (double)g.Sum(x => x.Total), g.Count()))
            .OrderByDescending(r => r.Value).ToList();

        // Umur piutang dihitung dari tanggal tagihan terbit sampai hari ini.
        var now = DateTime.UtcNow;
        var agingBands = new (string Label, int MinDays, int MaxDays)[]
        {
            ("0–1 hari", 0, 1), ("2–3 hari", 2, 3), ("4–7 hari", 4, 7),
            ("8–30 hari", 8, 30), ("> 30 hari", 31, int.MaxValue)
        };
        fin.Aging = agingBands.Select(b =>
        {
            var rows = outstanding.Where(p =>
            {
                var age = (int)(now - p.CreatedAt).TotalDays;
                return age >= b.MinDays && age <= b.MaxDays;
            }).ToList();
            return new RankedItem(b.Label, rows.Count > 0 ? $"{rows.Count} tagihan" : null, (double)rows.Sum(p => p.Total), rows.Count);
        }).Where(r => r.Value > 0 || r.Secondary > 0).ToList();

        fin.Recent = payments
            .Where(p => BilledIn(from, to, p.CreatedAt) || PaidIn(from, to, p.PaidAt) || p.State is PaymentState.Pending or PaymentState.AwaitingVerification)
            .OrderByDescending(p => p.PaidAt ?? p.CreatedAt)
            .Take(100)
            .Select(p => new FinanceRow(
                p.CreatedAt, p.Id, p.InvoiceNumber,
                userName.GetValueOrDefault(p.UserId, "-"),
                PaymentLabels.Reference(p.ReferenceType),
                PaymentLabels.Channel(p.Channel),
                p.Provider,
                PaymentLabels.State(p.State),
                p.Total, p.PaidAt))
            .ToList();

        return fin;
    }

    // ---- helpers ----

    private static List<DateTime> BuildBuckets(DateTime from, DateTime to, Granularity g)
    {
        var list = new List<DateTime>();
        var cur = Floor(from, g);
        var end = Floor(to, g);
        while (cur <= end)
        {
            list.Add(cur);
            cur = g switch
            {
                Granularity.Day => cur.AddDays(1),
                Granularity.Week => cur.AddDays(7),
                _ => cur.AddMonths(1)
            };
        }
        return list;
    }

    private static DateTime Floor(DateTime d, Granularity g) => g switch
    {
        Granularity.Day => d.Date,
        Granularity.Week => d.Date.AddDays(-(int)d.DayOfWeek),
        _ => new DateTime(d.Year, d.Month, 1)
    };

    private static List<SeriesPoint> ToPoints(List<DateTime> buckets, IEnumerable<(DateTime, double)> rows, Granularity g)
    {
        var map = buckets.ToDictionary(b => b, _ => 0d);
        foreach (var (dt, v) in rows)
        {
            var k = Floor(dt, g);
            if (map.ContainsKey(k)) map[k] += v;
        }
        return buckets.Select(b => new SeriesPoint(b, map[b])).ToList();
    }

    private static List<double> Bucketize(List<DateTime> buckets, IEnumerable<(DateTime, double)> rows, Granularity g)
        => ToPoints(buckets, rows, g).Select(p => p.Value).ToList();

    private static string ShortId(string id) => id.Length > 8 ? id[..8].ToUpperInvariant() : id.ToUpperInvariant();

    private static string PaymentLabel(PaymentStatus s) => s switch
    {
        PaymentStatus.Paid => "Lunas",
        PaymentStatus.Refunded => "Dikembalikan",
        _ => "Belum dibayar"
    };

    private static string HomecareLabel(HomecareServiceType t) => t switch
    {
        HomecareServiceType.LabTest => "Tes laboratorium",
        HomecareServiceType.Vaccination => "Vaksinasi",
        HomecareServiceType.VitaminBooster => "Vitamin booster",
        HomecareServiceType.DoctorVisit => "Kunjungan dokter",
        _ => "Kunjungan perawat"
    };

    private static string StatusLabel(object status) => status.ToString() switch
    {
        "Waiting" => "Menunggu",
        "InProgress" => "Berlangsung",
        "Completed" => "Selesai",
        "Cancelled" => "Dibatalkan",
        "Scheduled" => "Terjadwal",
        "Confirmed" => "Dikonfirmasi",
        "Pending" => "Menunggu",
        "Processing" => "Diproses",
        "Shipped" => "Dikirim",
        "Delivered" => "Diterima",
        "Requested" => "Diminta",
        var s => s ?? "-"
    };
}
