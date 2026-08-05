using System.ComponentModel;
using System.Globalization;
using System.Text;
using Microsoft.SemanticKernel;

namespace SMSNet.Services.Assistant.Plugins;

/// <summary>
/// Arithmetic the model must not do in its head.
/// <para>
/// The evaluator is hand-written rather than something like
/// <c>DataTable.Compute</c> so that the accepted grammar is a closed, auditable
/// set — no expression string can reach a general-purpose interpreter.
/// </para>
/// </summary>
public sealed class MatematikaPlugin
{
    [KernelFunction("hitung")]
    [Description("Menghitung ekspresi matematika dan mengembalikan hasilnya. " +
                 "Mendukung + - * / % ^, tanda kurung, dan fungsi: sqrt, abs, round, floor, ceil, " +
                 "min, max, pow, log, ln, exp, sin, cos, tan. Konstanta: pi, e. " +
                 "Contoh: '(1250000 * 12) * 0.05' atau 'round(87.6666, 2)'.")]
    public string Evaluate(
        [Description("Ekspresi matematika, contoh: (450 + 320) / 2")] string ekspresi)
    {
        if (string.IsNullOrWhiteSpace(ekspresi))
        {
            return "Ekspresi kosong.";
        }

        try
        {
            var value = new ExpressionParser(ekspresi).Parse();

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return "Hasil tidak terdefinisi (pembagian nol atau akar bilangan negatif).";
            }

            var rounded = Math.Round(value, 10);
            return $"{ekspresi.Trim()} = {rounded.ToString("0.##########", CultureInfo.InvariantCulture)}";
        }
        catch (FormatException ex)
        {
            return $"Ekspresi tidak valid: {ex.Message}";
        }
    }

    [KernelFunction("persentase")]
    [Description("Menghitung persentase sebuah bagian terhadap total, misalnya persentase kehadiran.")]
    public string Percentage(
        [Description("Nilai bagian, misalnya jumlah siswa hadir")] double bagian,
        [Description("Nilai total, misalnya jumlah seluruh siswa")] double total)
    {
        if (total == 0)
        {
            return "Total tidak boleh nol.";
        }

        var pct = bagian / total * 100;
        return $"{bagian:0.##} dari {total:0.##} = {pct:0.##}%";
    }

    [KernelFunction("statistik")]
    [Description("Menghitung ringkasan statistik (jumlah data, total, rata-rata, minimum, maksimum, median) " +
                 "dari sederet angka yang dipisahkan koma.")]
    public string Summary(
        [Description("Angka dipisahkan koma, contoh: 80, 75, 92, 68")] string angka)
    {
        var values = (angka ?? string.Empty)
            .Split(new[] { ',', ';', ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => double.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
                ? (double?)d
                : null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToArray();

        if (values.Length == 0)
        {
            return "Tidak ada angka yang bisa dibaca.";
        }

        var sorted = values.OrderBy(v => v).ToArray();
        var median = sorted.Length % 2 == 1
            ? sorted[sorted.Length / 2]
            : (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2;

        return $"""
                Jumlah data : {values.Length}
                Total       : {values.Sum():0.##}
                Rata-rata   : {values.Average():0.##}
                Median      : {median:0.##}
                Minimum     : {sorted[0]:0.##}
                Maksimum    : {sorted[^1]:0.##}
                """;
    }

    /// <summary>
    /// Recursive-descent parser over a fixed grammar:
    /// expression → term (('+'|'-') term)*
    /// term       → factor (('*'|'/'|'%') factor)*
    /// factor     → unary ('^' factor)?          [right-associative]
    /// unary      → ('-'|'+')? primary
    /// primary    → number | constant | function '(' args ')' | '(' expression ')'
    /// </summary>
    private sealed class ExpressionParser
    {
        private readonly string _text;
        private int _pos;

        public ExpressionParser(string text) => _text = text;

        public double Parse()
        {
            var value = ParseExpression();
            SkipWhitespace();

            if (_pos < _text.Length)
            {
                throw new FormatException($"karakter tak terduga '{_text[_pos]}' pada posisi {_pos + 1}");
            }

            return value;
        }

        private double ParseExpression()
        {
            var left = ParseTerm();

            while (true)
            {
                SkipWhitespace();
                if (Match('+')) left += ParseTerm();
                else if (Match('-')) left -= ParseTerm();
                else return left;
            }
        }

        private double ParseTerm()
        {
            var left = ParseFactor();

            while (true)
            {
                SkipWhitespace();
                if (Match('*')) left *= ParseFactor();
                else if (Match('/')) left /= ParseFactor();
                else if (Match('%')) left %= ParseFactor();
                else return left;
            }
        }

        private double ParseFactor()
        {
            var left = ParseUnary();
            SkipWhitespace();
            return Match('^') ? Math.Pow(left, ParseFactor()) : left;
        }

        private double ParseUnary()
        {
            SkipWhitespace();
            if (Match('-')) return -ParseUnary();
            if (Match('+')) return ParseUnary();
            return ParsePrimary();
        }

        private double ParsePrimary()
        {
            SkipWhitespace();

            if (_pos >= _text.Length)
            {
                throw new FormatException("ekspresi berakhir lebih cepat dari yang diharapkan");
            }

            if (Match('('))
            {
                var inner = ParseExpression();
                SkipWhitespace();
                if (!Match(')'))
                {
                    throw new FormatException("kurung tutup ')' tidak ditemukan");
                }
                return inner;
            }

            if (char.IsLetter(_text[_pos]))
            {
                return ParseIdentifier();
            }

            return ParseNumber();
        }

        private double ParseIdentifier()
        {
            var start = _pos;
            while (_pos < _text.Length && (char.IsLetterOrDigit(_text[_pos]) || _text[_pos] == '_'))
            {
                _pos++;
            }

            var name = _text[start.._pos].ToLowerInvariant();
            SkipWhitespace();

            if (!Match('('))
            {
                return name switch
                {
                    "pi" => Math.PI,
                    "e" => Math.E,
                    _ => throw new FormatException($"konstanta '{name}' tidak dikenal")
                };
            }

            var args = new List<double>();
            SkipWhitespace();

            if (!Match(')'))
            {
                do
                {
                    args.Add(ParseExpression());
                    SkipWhitespace();
                } while (Match(','));

                if (!Match(')'))
                {
                    throw new FormatException($"kurung tutup ')' tidak ditemukan untuk fungsi '{name}'");
                }
            }

            return Apply(name, args);
        }

        private static double Apply(string name, List<double> a)
        {
            double Need(int n)
            {
                if (a.Count != n)
                {
                    throw new FormatException($"fungsi '{name}' membutuhkan {n} argumen, diberi {a.Count}");
                }
                return 0;
            }

            switch (name)
            {
                case "sqrt": Need(1); return Math.Sqrt(a[0]);
                case "abs": Need(1); return Math.Abs(a[0]);
                case "floor": Need(1); return Math.Floor(a[0]);
                case "ceil": Need(1); return Math.Ceiling(a[0]);
                case "ln": Need(1); return Math.Log(a[0]);
                case "exp": Need(1); return Math.Exp(a[0]);
                case "sin": Need(1); return Math.Sin(a[0]);
                case "cos": Need(1); return Math.Cos(a[0]);
                case "tan": Need(1); return Math.Tan(a[0]);
                case "round":
                    if (a.Count == 1) return Math.Round(a[0]);
                    if (a.Count == 2) return Math.Round(a[0], (int)a[1]);
                    throw new FormatException("fungsi 'round' membutuhkan 1 atau 2 argumen");
                case "log":
                    if (a.Count == 1) return Math.Log10(a[0]);
                    if (a.Count == 2) return Math.Log(a[0], a[1]);
                    throw new FormatException("fungsi 'log' membutuhkan 1 atau 2 argumen");
                case "pow": Need(2); return Math.Pow(a[0], a[1]);
                case "min": Need(2); return Math.Min(a[0], a[1]);
                case "max": Need(2); return Math.Max(a[0], a[1]);
                default: throw new FormatException($"fungsi '{name}' tidak dikenal");
            }
        }

        private double ParseNumber()
        {
            var sb = new StringBuilder();

            while (_pos < _text.Length && (char.IsDigit(_text[_pos]) || _text[_pos] == '.'))
            {
                sb.Append(_text[_pos++]);
            }

            if (sb.Length == 0)
            {
                throw new FormatException($"angka tidak ditemukan pada posisi {_pos + 1}");
            }

            return double.Parse(sb.ToString(), CultureInfo.InvariantCulture);
        }

        private void SkipWhitespace()
        {
            while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos]))
            {
                _pos++;
            }
        }

        private bool Match(char c)
        {
            if (_pos >= _text.Length || _text[_pos] != c)
            {
                return false;
            }

            _pos++;
            return true;
        }
    }
}
