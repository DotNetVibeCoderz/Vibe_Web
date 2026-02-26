using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMSNet.Models
{
    public class Customer
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }

    public class Vehicle
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [Required]
        public string LicensePlate { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string VIN { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;

        public virtual ICollection<ServiceJob> ServiceJobs { get; set; } = new List<ServiceJob>();
    }

    public class Part
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; }
        
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; } = 5;
        public string SupplierName { get; set; } = string.Empty;
    }

    public enum JobStatus
    {
        Pending,
        InProgress,
        WaitingForParts,
        Completed,
        Cancelled
    }

    public class ServiceJob
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        public string? TechnicianId { get; set; } // ApplicationUser Id
        
        [Required]
        public string Description { get; set; } = string.Empty;
        public string MechanicNotes { get; set; } = string.Empty;
        
        public JobStatus Status { get; set; } = JobStatus.Pending;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? EstimatedCompletion { get; set; }

        public virtual ICollection<JobItem> Items { get; set; } = new List<JobItem>();
        public virtual Invoice? Invoice { get; set; }
    }

    public class JobItem
    {
        public int Id { get; set; }
        public int ServiceJobId { get; set; }
        public ServiceJob? ServiceJob { get; set; }

        public int? PartId { get; set; }
        public Part? Part { get; set; }

        public string ServiceName { get; set; } = string.Empty; // If not a part
        public int Quantity { get; set; } = 1;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice => Quantity * UnitPrice;
    }

    public enum InvoiceStatus
    {
        Draft,
        Issued,
        Paid,
        Overdue
    }

    public class Invoice
    {
        public int Id { get; set; }
        public int ServiceJobId { get; set; }
        public ServiceJob? ServiceJob { get; set; }
        
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
        public string PaymentMethod { get; set; } = "Cash";
    }

    public class Appointment
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        
        public string CustomerName { get; set; } = string.Empty; // For guests
        public string Phone { get; set; } = string.Empty;
        
        public DateTime AppointmentDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Scheduled"; // Scheduled, Confirmed, Completed, Cancelled
    }
}