using System;
using System.Collections.Generic;
using System.Linq;

namespace MyPoS.Services
{
    /// <summary>Satu baris keranjang, cukup untuk menghitung total.</summary>
    public interface ICartLine
    {
        int Quantity { get; }
        decimal UnitPrice { get; }
        decimal DiscountAmount { get; }
    }

    /// <summary>Hasil perhitungan satu transaksi. Semua nilai sudah dibulatkan ke presisi mata uang.</summary>
    public record OrderTotals(
        decimal LineSubTotal,
        decimal LineDiscount,
        decimal OrderDiscount,
        decimal TotalDiscount,
        decimal ServiceCharge,
        decimal TaxableAmount,
        decimal TaxAmount,
        decimal Rounding,
        decimal Total)
    {
        public static readonly OrderTotals Empty =
            new(0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    /// <summary>
    /// Perhitungan diskon, service charge, pajak, dan pembulatan.
    ///
    /// Versi lama memakai <c>TaxAmount = SubTotal * 0.11m</c>, yang keliru dalam tiga hal:
    /// tarifnya tidak bisa diubah, diskon tidak pernah mengurangi Dasar Pengenaan Pajak,
    /// dan harga yang sudah termasuk PPN akan dikenai pajak dua kali. Kelas ini memperbaiki
    /// ketiganya dan membulatkan setiap komponen ke presisi mata uang supaya penjumlahan
    /// yang tercetak di struk selalu sama dengan total yang ditagih.
    /// </summary>
    public static class TaxCalculator
    {
        public static OrderTotals Compute(
            IEnumerable<ICartLine> lines,
            PosSettings settings,
            decimal orderDiscountAmount = 0m,
            decimal orderDiscountPercent = 0m)
        {
            var decimals = Math.Clamp(settings.CurrencyDecimals, 0, 4);
            decimal R(decimal v) => Math.Round(v, decimals, MidpointRounding.AwayFromZero);

            var list = lines?.ToList() ?? new List<ICartLine>();
            if (list.Count == 0) return OrderTotals.Empty;

            var gross = R(list.Sum(l => l.UnitPrice * l.Quantity));
            var lineDiscount = R(list.Sum(l => l.DiscountAmount));
            var lineSubTotal = gross - lineDiscount;

            // Diskon tingkat transaksi: persen dihitung dari subtotal setelah diskon baris.
            var percentPart = orderDiscountPercent > 0
                ? R(lineSubTotal * (orderDiscountPercent / 100m))
                : 0m;
            var orderDiscount = Math.Clamp(percentPart + R(orderDiscountAmount), 0m, lineSubTotal);

            var netSales = lineSubTotal - orderDiscount;

            var serviceCharge = settings.ServiceChargeEnabled && settings.ServiceChargePercent > 0
                ? R(netSales * (settings.ServiceChargePercent / 100m))
                : 0m;

            // Dasar Pengenaan Pajak.
            var taxBase = settings.TaxAppliedAfterDiscount ? netSales : lineSubTotal;
            if (settings.ServiceChargeTaxable) taxBase += serviceCharge;

            decimal tax = 0m;
            decimal total;

            if (!settings.TaxEnabled || settings.TaxRatePercent <= 0)
            {
                taxBase = 0m;
                total = netSales + serviceCharge;
            }
            else
            {
                var rate = settings.TaxRatePercent / 100m;

                if (settings.TaxInclusive)
                {
                    // Harga jual sudah mengandung pajak: pajak diurai, bukan ditambahkan.
                    tax = R(taxBase - (taxBase / (1m + rate)));
                    total = netSales + serviceCharge;
                }
                else
                {
                    tax = R(taxBase * rate);
                    total = netSales + serviceCharge + tax;
                }
            }

            var rounded = ApplyRounding(total, settings.RoundingMode);
            var rounding = R(rounded - total);

            return new OrderTotals(
                LineSubTotal: gross,
                LineDiscount: lineDiscount,
                OrderDiscount: orderDiscount,
                TotalDiscount: lineDiscount + orderDiscount,
                ServiceCharge: serviceCharge,
                TaxableAmount: R(taxBase),
                TaxAmount: tax,
                Rounding: rounding,
                Total: R(rounded));
        }

        /// <summary>Pembulatan total akhir, lazim dipakai toko yang tidak menyimpan pecahan kecil.</summary>
        public static decimal ApplyRounding(decimal total, string mode)
        {
            var step = mode switch
            {
                "Nearest100" => 100m,
                "Nearest500" => 500m,
                "Nearest1000" => 1000m,
                _ => 0m
            };
            if (step <= 0) return total;
            return Math.Round(total / step, 0, MidpointRounding.AwayFromZero) * step;
        }
    }
}
