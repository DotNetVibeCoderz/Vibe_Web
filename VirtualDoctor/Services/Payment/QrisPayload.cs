using System.Globalization;
using System.Text;

namespace VirtualDoctor.Services.Payment;

/// <summary>
/// Membaca dan menyusun payload QR standar EMVCo yang dipakai QRIS.
///
/// Kegunaan utamanya: mengubah QR statis milik merchant (yang tidak memuat nominal)
/// menjadi QR dinamis berisi nominal tagihan. Caranya mengganti tag 01 menjadi "12",
/// menyisipkan tag 54 berisi nominal, lalu menghitung ulang checksum CRC pada tag 63.
///
/// Payload statis tetap milik merchant — aplikasi tidak membuat identitas merchant baru.
/// </summary>
public static class QrisPayload
{
    private const string TagInitiationMethod = "01";
    private const string TagAmount = "54";
    private const string TagCrc = "63";
    private const string DynamicIndicator = "12";

    public record Field(string Tag, string Value);

    /// <summary>Pecah payload menjadi daftar tag-panjang-nilai. Melempar bila format rusak.</summary>
    public static List<Field> Parse(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new FormatException("Payload QRIS kosong.");

        var fields = new List<Field>();
        var i = 0;

        while (i < payload.Length)
        {
            if (i + 4 > payload.Length)
                throw new FormatException($"Payload QRIS terpotong pada posisi {i}.");

            var tag = payload.Substring(i, 2);
            var lengthText = payload.Substring(i + 2, 2);

            if (!int.TryParse(lengthText, NumberStyles.None, CultureInfo.InvariantCulture, out var length))
                throw new FormatException($"Panjang tidak valid pada tag {tag}.");

            if (i + 4 + length > payload.Length)
                throw new FormatException($"Nilai tag {tag} melebihi panjang payload.");

            fields.Add(new Field(tag, payload.Substring(i + 4, length)));
            i += 4 + length;
        }

        return fields;
    }

    /// <summary>Periksa apakah payload merchant terbaca dan checksum-nya cocok.</summary>
    public static (bool Ok, string Message) Validate(string payload)
    {
        try
        {
            var fields = Parse(payload);
            if (fields.Count == 0) return (false, "Payload tidak berisi data.");

            var crcField = fields.FirstOrDefault(f => f.Tag == TagCrc);
            if (crcField == null) return (false, "Tag checksum (63) tidak ditemukan.");

            var withoutCrc = payload[..payload.LastIndexOf("6304", StringComparison.Ordinal)];
            var expected = Crc16(withoutCrc + "6304");

            if (!string.Equals(expected, crcField.Value, StringComparison.OrdinalIgnoreCase))
                return (false, $"Checksum tidak cocok. Tertulis {crcField.Value}, seharusnya {expected}.");

            var merchant = fields.FirstOrDefault(f => f.Tag == "59")?.Value;
            return (true, string.IsNullOrEmpty(merchant) ? "Payload valid." : $"Payload valid untuk merchant {merchant}.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Hasilkan payload dinamis berisi nominal. Bila payload sudah dinamis dan sudah
    /// memuat nominal, nilainya diganti.
    /// </summary>
    public static string WithAmount(string staticPayload, decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Nominal harus lebih besar dari nol.");

        var fields = Parse(staticPayload)
            .Where(f => f.Tag != TagCrc)   // checksum lama dibuang, dihitung ulang di akhir
            .ToList();

        // Tandai sebagai QR dinamis
        var initiationIndex = fields.FindIndex(f => f.Tag == TagInitiationMethod);
        if (initiationIndex >= 0) fields[initiationIndex] = new Field(TagInitiationMethod, DynamicIndicator);
        else fields.Insert(Math.Min(1, fields.Count), new Field(TagInitiationMethod, DynamicIndicator));

        // Nominal: tanpa pemisah ribuan, titik sebagai pemisah desimal, tanpa desimal bila bulat
        var amountText = decimal.Truncate(amount) == amount
            ? decimal.Truncate(amount).ToString(CultureInfo.InvariantCulture)
            : amount.ToString("0.##", CultureInfo.InvariantCulture);

        fields.RemoveAll(f => f.Tag == TagAmount);

        // Tag 54 harus berada setelah 53 (mata uang) dan sebelum tag bernomor lebih besar
        var insertAt = fields.FindIndex(f => string.CompareOrdinal(f.Tag, TagAmount) > 0);
        if (insertAt < 0) insertAt = fields.Count;
        fields.Insert(insertAt, new Field(TagAmount, amountText));

        var builder = new StringBuilder();
        foreach (var field in fields)
            builder.Append(field.Tag).Append(field.Value.Length.ToString("D2")).Append(field.Value);

        builder.Append("6304");
        builder.Append(Crc16(builder.ToString()));
        return builder.ToString();
    }

    /// <summary>Ambil nama merchant dari payload untuk ditampilkan di layar bayar.</summary>
    public static string? ReadMerchantName(string payload)
    {
        try { return Parse(payload).FirstOrDefault(f => f.Tag == "59")?.Value; }
        catch { return null; }
    }

    /// <summary>CRC-16/CCITT-FALSE sesuai spesifikasi EMVCo: polinomial 0x1021, nilai awal 0xFFFF.</summary>
    public static string Crc16(string input)
    {
        const ushort polynomial = 0x1021;
        ushort crc = 0xFFFF;

        foreach (var b in Encoding.UTF8.GetBytes(input))
        {
            crc ^= (ushort)(b << 8);
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ polynomial) : (ushort)(crc << 1);
        }

        return crc.ToString("X4");
    }
}
