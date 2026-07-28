using Microsoft.EntityFrameworkCore;
using VirtualDoctor.Models;

namespace VirtualDoctor.Data;

/// <summary>
/// Mengisi transaksi contoh (konsultasi, pesanan, homecare, ulasan) yang tersebar
/// pada 90 hari terakhir supaya dashboard punya sesuatu untuk ditampilkan.
///
/// Hanya berjalan saat: environment Development, flag Seed:DemoTransactions = true,
/// dan database memang masih kosong dari transaksi. Tidak pernah menimpa data nyata.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        var existingConsultations = await db.Consultations.CountAsync();
        var existingOrders = await db.Orders.CountAsync();

        if (existingConsultations > 20 || existingOrders > 10)
        {
            // Database contoh dari versi sebelum fitur pembayaran ada: terbitkan tagihannya
            // saja supaya laporan keuangan tidak kosong. Transaksinya tidak disentuh.
            await BackfillPaymentsAsync(db, logger);
            logger.LogInformation("[DemoData] Database sudah berisi transaksi, pengisian dilewati");
            return;
        }

        var users = await db.Users.Where(u => !u.IsDoctor).Select(u => u.Id).ToListAsync();
        var doctors = await db.Doctors.Select(d => new { d.Id, d.ConsultationFee }).ToListAsync();
        var medicines = await db.Medicines.Select(m => new { m.Id, m.Name, m.Price }).ToListAsync();
        var hospitals = await db.Hospitals.Select(h => h.Id).ToListAsync();

        if (users.Count == 0 || doctors.Count == 0)
        {
            logger.LogWarning("[DemoData] Tidak ada pengguna/dokter, pengisian dibatalkan");
            return;
        }

        var rnd = new Random(20250728);
        var now = DateTime.UtcNow;
        var consultations = new List<Consultation>();
        var messages = new List<ConsultationMessage>();
        var appointments = new List<Appointment>();
        var orders = new List<Order>();
        var homecare = new List<HomecareService>();
        var reviews = new List<DoctorReview>();

        string PickUser() => users[rnd.Next(users.Count)];

        // Volume harian naik perlahan supaya grafik tren terlihat wajar
        for (var dayOffset = 89; dayOffset >= 0; dayOffset--)
        {
            var date = now.Date.AddDays(-dayOffset);
            var weekday = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var growth = 1.0 + (89 - dayOffset) / 120.0;
            var baseVolume = (weekday ? 2 : 5) * growth;

            var consultationCount = Poisson(rnd, baseVolume);
            for (var i = 0; i < consultationCount; i++)
            {
                var doctor = doctors[rnd.Next(doctors.Count)];
                var started = date.AddHours(rnd.Next(7, 21)).AddMinutes(rnd.Next(0, 60));
                if (started > now) continue;

                var completed = rnd.NextDouble() < 0.88;
                var userId = PickUser();
                var type = rnd.NextDouble() switch { < 0.7 => ConsultationType.Chat, < 0.9 => ConsultationType.Video, _ => ConsultationType.Phone };

                var c = new Consultation
                {
                    UserId = userId,
                    DoctorId = doctor.Id,
                    Type = type,
                    Status = completed ? ConsultationStatus.Completed : ConsultationStatus.Cancelled,
                    Fee = doctor.ConsultationFee,
                    ChiefComplaint = Complaints[rnd.Next(Complaints.Length)],
                    StartedAt = started,
                    EndedAt = completed ? started.AddMinutes(rnd.Next(8, 35)) : null
                };
                consultations.Add(c);

                messages.Add(new ConsultationMessage
                {
                    ConsultationId = c.Id,
                    SenderId = userId,
                    SenderName = "Pasien",
                    Message = c.ChiefComplaint ?? "Halo dokter",
                    SentAt = started.AddMinutes(1)
                });

                if (completed && rnd.NextDouble() < 0.45)
                {
                    reviews.Add(new DoctorReview
                    {
                        DoctorId = doctor.Id,
                        UserId = userId,
                        ConsultationId = c.Id,
                        Rating = rnd.NextDouble() < 0.75 ? rnd.Next(4, 6) : rnd.Next(2, 4),
                        Comment = ReviewComments[rnd.Next(ReviewComments.Length)],
                        CreatedAt = c.EndedAt ?? started
                    });
                }
            }

            var orderCount = Poisson(rnd, baseVolume * 0.7);
            for (var i = 0; i < orderCount; i++)
            {
                var created = date.AddHours(rnd.Next(7, 22));
                if (created > now) continue;

                var itemCount = rnd.Next(1, 4);
                var items = new List<OrderItem>();
                for (var j = 0; j < itemCount && medicines.Count > 0; j++)
                {
                    var med = medicines[rnd.Next(medicines.Count)];
                    var qty = rnd.Next(1, 4);
                    items.Add(new OrderItem
                    {
                        MedicineId = med.Id,
                        MedicineName = med.Name,
                        Quantity = qty,
                        Price = med.Price,
                        Subtotal = med.Price * qty
                    });
                }
                if (items.Count == 0) continue;

                var subtotal = items.Sum(x => x.Subtotal);
                var age = (now - created).TotalDays;
                var status = age > 5
                    ? (rnd.NextDouble() < 0.9 ? OrderStatus.Delivered : OrderStatus.Cancelled)
                    : age > 2 ? OrderStatus.Shipped
                    : age > 1 ? OrderStatus.Processing
                    : OrderStatus.Pending;

                orders.Add(new Order
                {
                    UserId = PickUser(),
                    PharmacyId = hospitals.Count > 0 ? hospitals[rnd.Next(hospitals.Count)] : null,
                    Status = status,
                    Subtotal = subtotal,
                    DeliveryFee = 15000,
                    Total = subtotal + 15000,
                    DeliveryAddress = Addresses[rnd.Next(Addresses.Length)],
                    PaymentMethod = (PaymentMethod)rnd.Next(0, 4),
                    PaymentStatus = status is OrderStatus.Delivered or OrderStatus.Shipped ? PaymentStatus.Paid : PaymentStatus.Unpaid,
                    CreatedAt = created,
                    DeliveredAt = status == OrderStatus.Delivered ? created.AddDays(rnd.Next(1, 4)) : null,
                    Items = items
                });
            }

            if (rnd.NextDouble() < 0.55)
            {
                var doctor = doctors[rnd.Next(doctors.Count)];
                var created = date.AddHours(rnd.Next(8, 18));
                if (created <= now)
                {
                    var apptDate = created.AddDays(rnd.Next(1, 10));
                    var hour = rnd.Next(8, 17);
                    appointments.Add(new Appointment
                    {
                        UserId = PickUser(),
                        DoctorId = doctor.Id,
                        HospitalId = hospitals.Count > 0 ? hospitals[rnd.Next(hospitals.Count)] : null,
                        AppointmentDate = apptDate,
                        StartTime = new TimeSpan(hour, 0, 0),
                        EndTime = new TimeSpan(hour, 30, 0),
                        Type = rnd.NextDouble() < 0.65 ? AppointmentType.InPerson : AppointmentType.Online,
                        Status = apptDate < now
                            ? (rnd.NextDouble() < 0.85 ? AppointmentStatus.Completed : AppointmentStatus.Cancelled)
                            : (rnd.NextDouble() < 0.6 ? AppointmentStatus.Confirmed : AppointmentStatus.Scheduled),
                        EstimatedCost = doctor.ConsultationFee,
                        CreatedAt = created
                    });
                }
            }

            if (rnd.NextDouble() < 0.3)
            {
                var created = date.AddHours(rnd.Next(8, 18));
                if (created <= now)
                {
                    var type = (HomecareServiceType)rnd.Next(0, 5);
                    homecare.Add(new HomecareService
                    {
                        UserId = PickUser(),
                        ServiceType = type,
                        ScheduledDate = created.AddDays(rnd.Next(1, 7)),
                        Address = Addresses[rnd.Next(Addresses.Length)],
                        Fee = HomecareFees[(int)type],
                        Status = created < now.AddDays(-7) ? HomecareServiceStatus.Completed : HomecareServiceStatus.Confirmed,
                        CreatedAt = created
                    });
                }
            }
        }

        db.Consultations.AddRange(consultations);
        db.ConsultationMessages.AddRange(messages);
        db.Appointments.AddRange(appointments);
        db.Orders.AddRange(orders);
        db.HomecareServices.AddRange(homecare);
        db.DoctorReviews.AddRange(reviews);
        await db.SaveChangesAsync();

        var payments = BuildPayments(rnd, now, consultations, appointments, orders, homecare);
        db.Payments.AddRange(payments);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "[DemoData] Ditambahkan {C} konsultasi, {A} janji temu, {O} pesanan, {H} homecare, {R} ulasan, {P} tagihan",
            consultations.Count, appointments.Count, orders.Count, homecare.Count, reviews.Count, payments.Count);
    }

    /// <summary>
    /// Menerbitkan tagihan untuk transaksi contoh yang sudah ada di database.
    /// Idempoten: berhenti bila tabel Payments sudah terisi.
    /// </summary>
    private static async Task BackfillPaymentsAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Payments.AnyAsync()) return;

        var consultations = await db.Consultations
            .Where(c => c.Status != ConsultationStatus.Cancelled).ToListAsync();
        var appointments = await db.Appointments
            .Where(a => a.Status != AppointmentStatus.Cancelled).ToListAsync();
        var orders = await db.Orders.ToListAsync();
        var homecare = await db.HomecareServices.ToListAsync();

        if (consultations.Count + appointments.Count + orders.Count + homecare.Count == 0) return;

        var payments = BuildPayments(new Random(20250728), DateTime.UtcNow,
            consultations, appointments, orders, homecare);

        db.Payments.AddRange(payments);
        await db.SaveChangesAsync();
        logger.LogInformation("[DemoData] {P} tagihan diterbitkan untuk transaksi contoh yang sudah ada", payments.Count);
    }

    /// <summary>
    /// Menerbitkan tagihan untuk transaksi contoh supaya laporan keuangan punya isi.
    /// Sebagian sengaja dibiarkan menggantung agar piutang dan antrean verifikasi terlihat.
    /// </summary>
    private static List<Payment> BuildPayments(
        Random rnd, DateTime now,
        List<Consultation> consultations, List<Appointment> appointments,
        List<Order> orders, List<HomecareService> homecare)
    {
        var payments = new List<Payment>();
        var sequence = 0;

        void Issue(PaymentReferenceType type, string referenceId, string userId,
                   string description, decimal amount, DateTime createdAt)
        {
            if (amount <= 0) return;

            // Cara bayar mengikuti komposisi yang lazim di klinik: QRIS mendominasi.
            var roll = rnd.NextDouble();
            var channel = roll switch
            {
                < 0.52 => PaymentChannel.Qris,
                < 0.74 => PaymentChannel.BankTransfer,
                < 0.88 => PaymentChannel.EWallet,
                < 0.95 => PaymentChannel.VirtualAccount,
                _ => PaymentChannel.Cash
            };

            var age = (now - createdAt).TotalDays;
            // Tagihan lama hampir selalu sudah selesai; yang baru masih berjalan.
            var settle = rnd.NextDouble();
            var state = age > 3
                ? (settle < 0.88 ? PaymentState.Paid : settle < 0.96 ? PaymentState.Expired : PaymentState.Failed)
                : (settle < 0.55 ? PaymentState.Paid
                    : settle < 0.75 ? PaymentState.AwaitingVerification
                    : PaymentState.Pending);

            DateTime? paidAt = state == PaymentState.Paid
                ? createdAt.AddMinutes(rnd.Next(2, 400))
                : null;
            if (paidAt > now) paidAt = now;

            sequence++;
            payments.Add(new Payment
            {
                InvoiceNumber = $"INV/{createdAt:yyyy}/{createdAt:MM}/{sequence:D4}",
                ReferenceType = type,
                ReferenceId = referenceId,
                UserId = userId,
                Description = description,
                Amount = amount,
                ServiceFee = 0,
                Total = amount,
                Channel = channel,
                Provider = channel == PaymentChannel.Qris ? "Qris" : "Manual",
                State = state,
                CreatedAt = createdAt,
                ExpiresAt = createdAt.AddHours(2),
                PaidAt = paidAt,
                ProofUrl = state == PaymentState.AwaitingVerification ? "/uploads/demo-bukti-transfer.png" : null,
                VerifiedBy = state == PaymentState.Paid && channel == PaymentChannel.BankTransfer ? "admin@virtualdoctor.com" : null,
                VerifiedAt = state == PaymentState.Paid && channel == PaymentChannel.BankTransfer ? paidAt : null
            });
        }

        foreach (var c in consultations.Where(x => x.Status != ConsultationStatus.Cancelled))
            Issue(PaymentReferenceType.Consultation, c.Id, c.UserId, "Biaya konsultasi dokter", c.Fee, c.StartedAt);

        foreach (var a in appointments.Where(x => x.Status != AppointmentStatus.Cancelled))
            Issue(PaymentReferenceType.Appointment, a.Id, a.UserId, "Biaya janji temu", a.EstimatedCost, a.CreatedAt);

        foreach (var o in orders)
            Issue(PaymentReferenceType.Order, o.Id, o.UserId, "Pesanan obat + ongkir", o.Total, o.CreatedAt);

        foreach (var h in homecare)
            Issue(PaymentReferenceType.Homecare, h.Id, h.UserId, "Layanan homecare", h.Fee, h.CreatedAt);

        // Penomoran invoice harus urut menurut waktu terbit dan reset tiap bulan,
        // sama seperti PaymentService.NextInvoiceNumberAsync agar nomor tidak bentrok.
        var ordered = payments.OrderBy(p => p.CreatedAt).ToList();
        var perMonth = new Dictionary<string, int>();
        foreach (var p in ordered)
        {
            var prefix = $"INV/{p.CreatedAt:yyyy}/{p.CreatedAt:MM}/";
            var next = perMonth.GetValueOrDefault(prefix) + 1;
            perMonth[prefix] = next;
            p.InvoiceNumber = prefix + next.ToString("D4");
        }

        return ordered;
    }

    private static int Poisson(Random rnd, double lambda)
    {
        var l = Math.Exp(-lambda);
        var k = 0;
        var p = 1.0;
        do { k++; p *= rnd.NextDouble(); } while (p > l);
        return k - 1;
    }

    private static readonly decimal[] HomecareFees = { 250000, 150000, 200000, 300000, 175000 };

    private static readonly string[] Complaints =
    {
        "Demam sudah dua hari disertai menggigil",
        "Batuk kering di malam hari",
        "Nyeri ulu hati setelah makan",
        "Sakit kepala berdenyut sebelah kanan",
        "Ruam gatal di lengan",
        "Sulit tidur dan mudah cemas",
        "Tekanan darah naik saat kontrol mandiri",
        "Nyeri sendi lutut saat naik tangga"
    };

    private static readonly string[] ReviewComments =
    {
        "Penjelasannya mudah dipahami, terima kasih dokter.",
        "Responsnya cepat dan sabar menjawab pertanyaan.",
        "Cukup membantu, tapi antre agak lama.",
        "Sangat teliti menanyakan riwayat penyakit.",
        "Saran obatnya jelas beserta aturan pakainya."
    };

    private static readonly string[] Addresses =
    {
        "Jl. Merdeka No.10, Jakarta Pusat",
        "Jl. Sudirman No.25, Jakarta Selatan",
        "Jl. Thamrin No.5, Jakarta Pusat",
        "Jl. Gatot Subroto No.8, Jakarta Selatan",
        "Jl. Diponegoro No.30, Jakarta Pusat"
    };
}
