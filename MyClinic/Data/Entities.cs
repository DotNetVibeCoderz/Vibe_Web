#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace MyClinic.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
    }

    public class Patient
    {
        public int Id { get; set; }
        public string MedicalRecordNumber { get; set; }
        public string FullName { get; set; }
        public string Nik { get; set; }
        public DateTime DoB { get; set; }
        public string Gender { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }

        public ICollection<Appointment> Appointments { get; set; }
        public ICollection<MedicalRecord> MedicalRecords { get; set; }
        public ICollection<Bill> Bills { get; set; }
    }

    public class Employee
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; } // Doctor, Nurse, Admin, Pharmacist
        public string Specialization { get; set; }
        public string Phone { get; set; }
    }

    public class Appointment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; }
        public int DoctorId { get; set; }
        public Employee Doctor { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } // Scheduled, InProgress, Completed, Cancelled
        public int QueueNumber { get; set; }
        public bool IsTelemedicine { get; set; }
    }

    public class MedicalRecord
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; }
        public int DoctorId { get; set; }
        public Employee Doctor { get; set; }
        public DateTime Date { get; set; }
        public string Diagnosis { get; set; }
        public string Notes { get; set; }
        public string Treatment { get; set; }

        public ICollection<Prescription> Prescriptions { get; set; }
        public ICollection<LabResult> LabResults { get; set; }
    }

    public class Medicine
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public int Stock { get; set; }
        public decimal Price { get; set; }
    }

    public class Prescription
    {
        public int Id { get; set; }
        public int MedicalRecordId { get; set; }
        public MedicalRecord MedicalRecord { get; set; }
        public int MedicineId { get; set; }
        public Medicine Medicine { get; set; }
        public int Quantity { get; set; }
        public string Dosage { get; set; }
        public string Status { get; set; } // Pending, Dispensed
    }

    public class LabResult
    {
        public int Id { get; set; }
        public int MedicalRecordId { get; set; }
        public MedicalRecord MedicalRecord { get; set; }
        public string TestName { get; set; }
        public string Result { get; set; }
        public DateTime Date { get; set; }
    }

    public class Bill
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsPaid { get; set; }
        public DateTime Date { get; set; }
        public string PaymentMethod { get; set; } // Cash, BPJS, Transfer
        public string BpjsClaimStatus { get; set; } // None, Pending, Claimed
    }
}