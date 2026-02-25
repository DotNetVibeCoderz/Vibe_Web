using System;
using System.ComponentModel.DataAnnotations;

namespace MyAsset.Models
{
    public enum AssetStatus
    {
        InUse,
        InStorage,
        UnderMaintenance,
        Disposed,
        Lost
    }

    public class Asset
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string SerialNumber { get; set; } = string.Empty; // For Barcode/QR
        
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        
        public string Location { get; set; } = string.Empty;

        // Geolocation
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        
        [DataType(DataType.Currency)]
        public decimal PurchasePrice { get; set; }
        
        public AssetStatus Status { get; set; } = AssetStatus.InUse;
        
        public string ImageUrl { get; set; } = "images/default-asset.png";
        
        public string AssignedTo { get; set; } = string.Empty; // Employee Name or ID

        // For Predictive Maintenance (Simulated)
        public int ExpectedLifeSpanMonths { get; set; } = 36;
        public int MaintenanceCount { get; set; } = 0;

        public DateTime LastMaintenanceDate { get; set; }

        public decimal CurrentValue()
        {
            // Simple straight-line depreciation
            var ageInMonths = (DateTime.Now - PurchaseDate).TotalDays / 30;
            if (ageInMonths >= ExpectedLifeSpanMonths) return 0;
            var depreciationPerMonth = PurchasePrice / ExpectedLifeSpanMonths;
            return PurchasePrice - (decimal)(depreciationPerMonth * (decimal)ageInMonths);
        }

        public double HealthScore()
        {
            // 0-100 score. Starts at 100. Reduced by age and boosted by maintenance.
            var ageInMonths = (DateTime.Now - PurchaseDate).TotalDays / 30;
            var score = 100.0 - (ageInMonths * 2); 
            score += MaintenanceCount * 5; 
            if (score > 100) score = 100;
            if (score < 0) score = 0;
            return score;
        }
    }
}