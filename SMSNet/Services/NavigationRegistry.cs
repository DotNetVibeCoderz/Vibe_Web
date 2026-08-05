using SMSNet.Models;

namespace SMSNet.Services;

/// <summary>
/// The single definition of the application's navigable surface.
/// <para>
/// Sidebar rendering, the role-access report, and the documentation all read
/// from here. Keeping one list means a page can never appear in the menu for a
/// role that the page's own <c>[Authorize]</c> attribute will then reject.
/// </para>
/// </summary>
public static class NavigationRegistry
{
    public sealed record NavItem(string Label, string Href, string Icon, string[] Roles)
    {
        public bool AllowedFor(IEnumerable<string> userRoles) =>
            Roles.Length == 0 || userRoles.Any(r => Roles.Contains(r, StringComparer.OrdinalIgnoreCase));
    }

    public sealed record NavGroup(string Label, NavItem[] Items);

    private static readonly string[] Everyone = AppRoles.All;
    private static readonly string[] AdminOnly = { AppRoles.Admin };
    private static readonly string[] Staff = { AppRoles.Admin, AppRoles.Guru };
    private static readonly string[] Family = { AppRoles.Admin, AppRoles.Siswa, AppRoles.OrangTua };
    private static readonly string[] Billing = { AppRoles.Admin, AppRoles.OrangTua };
    private static readonly string[] Learners = { AppRoles.Admin, AppRoles.Guru, AppRoles.Siswa };

    public static readonly NavGroup[] Groups =
    {
        new("Ringkasan", new NavItem[]
        {
            new("Dashboard", "/", "home", Everyone),
            new("Pak Dedi (Asisten)", "/asisten", "chat", Everyone)
        }),

        new("Akademik", new NavItem[]
        {
            new("Kurikulum & Jadwal", "/academic/curriculum", "calendar", Staff),
            new("Penjadwalan Otomatis", "/academic/schedule-generator", "sparkle", Staff),
            new("Absensi QR", "/academic/attendance-qr", "grid", Staff),
            new("Absensi Manual", "/academic/attendance", "check", Staff),
            new("Penilaian & Rapor", "/academic/grades", "chart", Staff),
            new("E-Learning", "/academic/elearning", "book", Learners)
        }),

        new("Guru & Staff", new NavItem[]
        {
            new("Dashboard Guru", "/teacher/dashboard", "board", Staff),
            new("Tugas & Ujian", "/teacher/tasks", "clipboard", Staff),
            new("Komunikasi Internal", "/teacher/communication", "message", Staff),
            new("Evaluasi Kinerja", "/teacher/performance", "gauge", Staff)
        }),

        new("Orang Tua & Siswa", new NavItem[]
        {
            new("Portal", "/parent/portal", "users", Family),
            new("Notifikasi", "/parent/notifications", "bell", Everyone),
            new("E-Payment", "/parent/epayment", "wallet", Billing),
            new("Dokumen Digital", "/parent/documents", "file", Family)
        }),

        new("Administrasi & Keuangan", new NavItem[]
        {
            new("Manajemen Keuangan", "/admin/finance", "coins", AdminOnly),
            new("Metode Pembayaran", "/admin/payment-gateways", "credit-card", AdminOnly),
            new("Inventory", "/admin/inventory", "box", AdminOnly),
            new("Payroll", "/admin/payroll", "receipt", AdminOnly),
            new("Laporan Keuangan", "/admin/financial-report", "trend", AdminOnly)
        }),

        new("Analitik & Laporan", new NavItem[]
        {
            new("Dashboard Analitik", "/analytics/overview", "pie", AdminOnly),
            new("Data Analytics", "/analytics/data", "activity", AdminOnly),
            new("Custom Reports", "/analytics/reports", "layers", AdminOnly),
            new("Laporan Akademik", "/reports/academic", "chart", Staff),
            new("Laporan Guru & Staff", "/reports/teacher", "board", Staff),
            new("Laporan Orang Tua", "/reports/parent", "users", Staff),
            new("Laporan Keuangan", "/reports/finance", "coins", AdminOnly),
            new("Laporan Master Data", "/reports/master", "database", AdminOnly)
        }),

        new("Master Data", new NavItem[]
        {
            new("Siswa", "/master/students", "user", Staff),
            new("Guru", "/master/teachers", "teacher", Staff),
            new("Kartu Ber-QR", "/master/cards", "grid", Staff),
            new("Mata Pelajaran", "/master/subjects", "book", Staff),
            new("Kelas", "/master/classes", "grid", Staff)
        }),

        new("Kegiatan", new NavItem[]
        {
            new("Event & Ekstrakurikuler", "/events", "flag", Everyone)
        }),

        new("Keamanan & Integrasi", new NavItem[]
        {
            new("Role Access", "/security/roles", "shield", AdminOnly),
            new("Audit Trail", "/security/audit", "history", AdminOnly),
            new("REST API", "/integration/api", "plug", AdminOnly)
        })
    };

    /// <summary>Groups with their items filtered to what the given roles may open.</summary>
    public static IEnumerable<NavGroup> VisibleTo(IEnumerable<string> userRoles)
    {
        var roles = userRoles as string[] ?? userRoles.ToArray();

        foreach (var group in Groups)
        {
            var items = group.Items.Where(i => i.AllowedFor(roles)).ToArray();
            if (items.Length > 0)
            {
                yield return group with { Items = items };
            }
        }
    }

    /// <summary>Every route paired with the roles allowed to reach it — used by the RBAC matrix page.</summary>
    public static IEnumerable<(string Group, NavItem Item)> AllRoutes() =>
        Groups.SelectMany(g => g.Items.Select(i => (g.Label, i)));
}
