using Microsoft.EntityFrameworkCore;
using VirtualDoctor.Data;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services.Payment;

/// <summary>
/// Pemberi nomor invoice berurutan per bulan.
///
/// Cara lama membaca nomor terakhir dari tabel Payments lalu menambah satu. Dua
/// checkout yang bersamaan membaca nomor yang sama, dan karena kolom InvoiceNumber
/// berindeks unik, salah satunya gagal tersimpan — pasien melihat checkout error.
///
/// Sekarang nomor diambil dari tabel penghitung tersendiri:
/// satu pernyataan UPDATE menaikkan nilainya sekaligus mengunci barisnya sampai
/// commit, sehingga permintaan lain menunggu alih-alih membaca nilai basi. Pola ini
/// berlaku sama di keempat provider database yang didukung (SQLite, SQL Server,
/// PostgreSQL, MySQL) karena semuanya menahan kunci tulis sampai transaksi selesai.
/// </summary>
public static class InvoiceNumbering
{
    private const int Attempts = 6;

    /// <summary>
    /// Penjaga di dalam proses. Instansi tunggal adalah kasus yang lazim, dan
    /// menyerialkan di sini membuat transaksi database tidak pernah saling
    /// menunggu — kunci basis data tetap dipakai untuk kasus banyak instansi.
    /// </summary>
    private static readonly SemaphoreSlim Gate = new(1, 1);

    /// <summary>Ambil satu nomor urut baru untuk awalan tertentu.</summary>
    public static async Task<int> NextAsync(AppDbContext db, string prefix, CancellationToken ct = default)
    {
        await Gate.WaitAsync(ct);
        try
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await AllocateAsync(db, prefix, ct);
                }
                catch (Exception ex) when (attempt < Attempts && IsContention(ex))
                {
                    // Baris penghitung sedang dikunci instansi lain, atau dua proses
                    // membuat baris awalan yang sama. Keduanya sembuh dengan mencoba lagi.
                    Forget(db);
                    await Task.Delay(20 * attempt, ct);
                }
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task<int> AllocateAsync(AppDbContext db, string prefix, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var updated = await db.InvoiceCounters
            .Where(c => c.Prefix == prefix)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.LastNumber, c => c.LastNumber + 1)
                .SetProperty(c => c.UpdatedAt, _ => DateTime.UtcNow), ct);

        int number;
        if (updated == 0)
        {
            // Awalan belum pernah dipakai. Mulai dari nomor tertinggi yang sudah
            // terbit supaya database lama — yang penomorannya dibuat sebelum tabel
            // ini ada — tidak mengulang nomor yang sudah tercetak di invoice.
            number = await HighestIssuedAsync(db, prefix, ct) + 1;
            db.InvoiceCounters.Add(new InvoiceCounter { Prefix = prefix, LastNumber = number, UpdatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync(ct);
        }
        else
        {
            number = await db.InvoiceCounters.AsNoTracking()
                .Where(c => c.Prefix == prefix)
                .Select(c => c.LastNumber)
                .FirstAsync(ct);
        }

        await tx.CommitAsync(ct);
        return number;
    }

    /// <summary>Nomor terbesar yang sudah dipakai pada awalan ini, 0 bila belum ada.</summary>
    private static async Task<int> HighestIssuedAsync(AppDbContext db, string prefix, CancellationToken ct)
    {
        var issued = await db.Payments.AsNoTracking()
            .Where(p => p.InvoiceNumber.StartsWith(prefix))
            .Select(p => p.InvoiceNumber)
            .ToListAsync(ct);

        var highest = 0;
        foreach (var number in issued)
            if (int.TryParse(number[prefix.Length..], out var value) && value > highest) highest = value;

        return highest;
    }

    /// <summary>Buang baris penghitung yang gagal disimpan agar percobaan berikutnya bersih.</summary>
    private static void Forget(AppDbContext db)
    {
        foreach (var entry in db.ChangeTracker.Entries<InvoiceCounter>().ToList())
            entry.State = EntityState.Detached;
    }

    /// <summary>Kegagalan yang disebabkan perebutan, bukan kesalahan pemrograman.</summary>
    private static bool IsContention(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is DbUpdateException) return true;

            var message = e.Message;
            if (message.Contains("locked", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("busy", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("deadlock", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
