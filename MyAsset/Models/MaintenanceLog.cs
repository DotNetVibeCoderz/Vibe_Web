using System;

namespace MyAsset.Models
{
    public class MaintenanceLog
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public Asset? Asset { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
    }
}