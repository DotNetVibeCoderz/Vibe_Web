using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyPoS.Services.Import
{
    /// <summary>Apa yang akan dilakukan terhadap satu baris berkas saat impor dijalankan.</summary>
    public enum ImportAction
    {
        /// <summary>Baris akan menjadi data baru.</summary>
        Create,
        /// <summary>Baris cocok dengan data yang sudah ada dan akan menimpanya.</summary>
        Update,
        /// <summary>Baris dilewati — entah karena bermasalah, atau karena pembaruan dimatikan.</summary>
        Skip
    }

    /// <summary>Satu kolom pada template Excel.</summary>
    /// <param name="Header">Judul kolom, dicocokkan tanpa memperhatikan besar kecil huruf dan spasi.</param>
    /// <param name="Required">Kolom yang wajib diisi pada setiap baris.</param>
    /// <param name="Description">Penjelasan yang muncul di lembar Petunjuk.</param>
    /// <param name="Example">Contoh nilai yang ditulis pada baris contoh.</param>
    public record ImportColumn(string Header, bool Required, string Description, string Example);

    /// <summary>Hasil pemeriksaan satu baris, sebelum apa pun ditulis ke basis data.</summary>
    public class ImportRowResult
    {
        public int RowNumber { get; init; }

        /// <summary>Ringkasan singkat isi baris, dipakai sebagai label di tabel pratinjau.</summary>
        public string Summary { get; set; } = "";

        public ImportAction Action { get; set; } = ImportAction.Create;

        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();

        /// <summary>
        /// Entitas hasil parse. Disimpan sebagai object supaya satu dialog pratinjau dapat
        /// melayani semua jenis data master tanpa perlu generik yang menular ke komponen.
        /// </summary>
        public object? Payload { get; set; }

        public bool IsValid => Errors.Count == 0;
    }

    /// <summary>Pilihan yang dapat diubah pengguna sebelum impor dijalankan.</summary>
    public class ImportOptions
    {
        /// <summary>true = baris yang cocok dengan data lama akan menimpanya; false = dilewati.</summary>
        public bool UpdateExisting { get; set; } = true;

        /// <summary>Khusus produk: membuat kategori yang belum ada alih-alih menolak barisnya.</summary>
        public bool CreateMissingCategories { get; set; }
    }

    /// <summary>Seluruh hasil pemeriksaan berkas, siap ditampilkan sebagai pratinjau.</summary>
    public class ImportPreview
    {
        public List<ImportRowResult> Rows { get; } = new();

        /// <summary>Masalah pada berkasnya sendiri: lembar kosong, kolom wajib hilang, dan sejenisnya.</summary>
        public List<string> FileErrors { get; } = new();

        public int CreateCount => Rows.Count(r => r.IsValid && r.Action == ImportAction.Create);
        public int UpdateCount => Rows.Count(r => r.IsValid && r.Action == ImportAction.Update);
        public int SkipCount => Rows.Count(r => r.IsValid && r.Action == ImportAction.Skip);
        public int ErrorCount => Rows.Count(r => !r.IsValid);

        /// <summary>true bila ada sesuatu yang benar-benar dapat ditulis.</summary>
        public bool CanCommit => FileErrors.Count == 0 && (CreateCount + UpdateCount) > 0;
    }

    /// <summary>Hasil akhir setelah impor dijalankan.</summary>
    public record ImportResult(int Created, int Updated, int Skipped, string? Error = null)
    {
        public bool Success => Error is null;
        public int Total => Created + Updated;
    }

    /// <summary>
    /// Kontrak impor untuk satu jenis data master.
    ///
    /// Pemeriksaan dan penulisan sengaja dipisah menjadi dua langkah: pengguna selalu
    /// melihat persis apa yang akan terjadi — berapa baris dibuat, berapa ditimpa, dan
    /// baris mana yang bermasalah — sebelum satu baris pun tersimpan.
    /// </summary>
    public interface IMasterDataImporter
    {
        /// <summary>Nama teknis, dipakai sebagai bagian nama berkas template.</summary>
        string Key { get; }

        /// <summary>Nama yang dilihat pengguna, mis. "Produk".</summary>
        string DisplayName { get; }

        IReadOnlyList<ImportColumn> Columns { get; }

        /// <summary>true bila jenis data ini punya pilihan "buat kategori yang belum ada".</summary>
        bool SupportsCategoryCreation => false;

        /// <summary>Membuat berkas template Excel yang siap diisi.</summary>
        Task<byte[]> BuildTemplateAsync(CancellationToken ct = default);

        /// <summary>Membaca dan memeriksa berkas, tanpa menulis apa pun.</summary>
        Task<ImportPreview> ParseAsync(Stream file, ImportOptions options, CancellationToken ct = default);

        /// <summary>Menulis baris yang lolos pemeriksaan.</summary>
        Task<ImportResult> CommitAsync(ImportPreview preview, ImportOptions options, CancellationToken ct = default);
    }
}
