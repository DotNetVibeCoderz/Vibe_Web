using System;
using System.Collections.Generic;

namespace MyPoS.Api
{
    /// <summary>Amplop standar untuk daftar bernomor halaman.</summary>
    /// <typeparam name="T">Jenis item.</typeparam>
    public record PagedResult<T>(int Page, int PageSize, int Total, int TotalPages, IReadOnlyList<T> Items);

    /// <summary>Bentuk galat yang seragam untuk seluruh endpoint.</summary>
    public record ApiError(string Message, string? Detail = null);

    // ------------------------------------------------------------------ Produk

    public record ProductDto(
        int Id,
        string Name,
        string? Barcode,
        int CategoryId,
        string? CategoryName,
        decimal Price,
        decimal Cost,
        int Stock,
        int MinStock,
        bool IsActive,
        string? Description,
        string? ImageUrl);

    /// <summary>Isi permintaan untuk membuat atau mengubah produk.</summary>
    public record ProductWriteDto(
        string Name,
        string? Barcode,
        int CategoryId,
        decimal Price,
        decimal Cost,
        int Stock,
        int MinStock,
        bool IsActive = true,
        string? Description = null,
        string? ImageUrl = null);

    /// <summary>Penyesuaian stok. Nilai negatif mengurangi, positif menambah.</summary>
    public record StockAdjustmentDto(int Delta, string? Reason);

    // --------------------------------------------------------------- Kategori

    public record CategoryDto(int Id, string Name, int ProductCount);

    public record CategoryWriteDto(string Name);

    // -------------------------------------------------------------- Pelanggan

    public record CustomerDto(int Id, string Name, string? Phone, string? Email, int LoyaltyPoints);

    public record CustomerWriteDto(string Name, string? Phone, string? Email, int LoyaltyPoints = 0);

    // -------------------------------------------------------------- Transaksi

    public record TransactionLineDto(
        int ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal DiscountAmount,
        decimal SubTotal);

    public record TransactionDto(
        int Id,
        string InvoiceNumber,
        DateTime Date,
        string Status,
        string CashierName,
        int? CustomerId,
        string? CustomerName,
        string PaymentMethod,
        string PaymentProvider,
        decimal SubTotal,
        decimal DiscountAmount,
        decimal TaxableAmount,
        decimal TaxAmount,
        decimal ServiceChargeAmount,
        decimal RoundingAmount,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal ChangeAmount,
        decimal TaxRate,
        bool TaxInclusive,
        IReadOnlyList<TransactionLineDto> Lines);

    /// <summary>Satu baris keranjang pada permintaan pembuatan transaksi.</summary>
    public record CheckoutLineDto(int ProductId, int Quantity, decimal? UnitPrice, decimal DiscountAmount = 0);

    /// <summary>Isi permintaan untuk membuat transaksi baru dari aplikasi luar.</summary>
    public record CheckoutRequestDto(
        IReadOnlyList<CheckoutLineDto> Lines,
        string? PaymentProvider = "Cash",
        int? CustomerId = null,
        decimal OrderDiscountAmount = 0,
        decimal OrderDiscountPercent = 0,
        decimal PaidAmount = 0,
        string? CashierName = null,
        string? Notes = null);

    public record CheckoutResponseDto(
        bool Success,
        TransactionDto? Transaction,
        string? PaymentUrl,
        string? Error);

    public record VoidRequestDto(string Reason);

    // ---------------------------------------------------------------- Laporan

    public record SalesSummaryDto(
        DateTime From,
        DateTime To,
        int TransactionCount,
        int ItemCount,
        decimal Revenue,
        decimal Cost,
        decimal GrossProfit,
        decimal MarginPercent,
        decimal TaxCollected,
        decimal DiscountGiven);

    public record DailySalesDto(DateTime Date, int TransactionCount, decimal Revenue);

    public record ProductSalesDto(
        int ProductId,
        string ProductName,
        string CategoryName,
        int Quantity,
        decimal Revenue,
        decimal Cost,
        decimal GrossProfit);

    // ------------------------------------------------------------------ Stok

    public record LowStockDto(int Id, string Name, string? CategoryName, int Stock, int Threshold);

    // ------------------------------------------------------------ Info aplikasi

    public record StoreInfoDto(
        string StoreName,
        string CurrencyCode,
        string CurrencySymbol,
        int CurrencyDecimals,
        bool TaxEnabled,
        string TaxName,
        decimal TaxRatePercent,
        bool TaxInclusive,
        IReadOnlyList<string> PaymentProviders);
}
