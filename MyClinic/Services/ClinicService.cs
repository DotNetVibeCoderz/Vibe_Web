using Microsoft.EntityFrameworkCore;
using MyClinic.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace MyClinic.Services
{
    public class ClinicService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _factory;

        public ClinicService(IDbContextFactory<ApplicationDbContext> factory)
        {
            _factory = factory;
        }

        // --- PATIENTS ---
        public async Task<List<Patient>> GetPatientsAsync()
        {
            using var context = _factory.CreateDbContext();
            return await context.Patients.OrderByDescending(p => p.Id).ToListAsync();
        }

        public async Task<Patient> AddPatientAsync(Patient patient)
        {
            using var context = _factory.CreateDbContext();
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
            return patient;
        }

        public async Task UpdatePatientAsync(Patient patient)
        {
            using var context = _factory.CreateDbContext();
            context.Patients.Update(patient);
            await context.SaveChangesAsync();
        }

        public async Task DeletePatientAsync(int id)
        {
            using var context = _factory.CreateDbContext();
            var patient = await context.Patients.FindAsync(id);
            if (patient != null)
            {
                context.Patients.Remove(patient);
                await context.SaveChangesAsync();
            }
        }

        // --- EMPLOYEES ---
        public async Task<List<Employee>> GetEmployeesAsync()
        {
            using var context = _factory.CreateDbContext();
            return await context.Employees.ToListAsync();
        }

        public async Task<Employee> AddEmployeeAsync(Employee employee)
        {
            using var context = _factory.CreateDbContext();
            context.Employees.Add(employee);
            await context.SaveChangesAsync();
            return employee;
        }

        public async Task UpdateEmployeeAsync(Employee employee)
        {
            using var context = _factory.CreateDbContext();
            context.Employees.Update(employee);
            await context.SaveChangesAsync();
        }

        public async Task DeleteEmployeeAsync(int id)
        {
            using var context = _factory.CreateDbContext();
            var employee = await context.Employees.FindAsync(id);
            if (employee != null)
            {
                context.Employees.Remove(employee);
                await context.SaveChangesAsync();
            }
        }

        // --- MEDICINES ---
        public async Task<List<Medicine>> GetMedicinesAsync()
        {
            using var context = _factory.CreateDbContext();
            return await context.Medicines.OrderByDescending(m => m.Stock).ToListAsync();
        }

        public async Task<Medicine> AddMedicineAsync(Medicine medicine)
        {
            using var context = _factory.CreateDbContext();
            context.Medicines.Add(medicine);
            await context.SaveChangesAsync();
            return medicine;
        }

        public async Task UpdateMedicineAsync(Medicine medicine)
        {
            using var context = _factory.CreateDbContext();
            context.Medicines.Update(medicine);
            await context.SaveChangesAsync();
        }

        public async Task DeleteMedicineAsync(int id)
        {
            using var context = _factory.CreateDbContext();
            var medicine = await context.Medicines.FindAsync(id);
            if (medicine != null)
            {
                context.Medicines.Remove(medicine);
                await context.SaveChangesAsync();
            }
        }

        // --- APPOINTMENTS ---
        public async Task<List<Appointment>> GetAppointmentsAsync()
        {
            using var context = _factory.CreateDbContext();
            return await context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        public async Task<Appointment> AddAppointmentAsync(Appointment appointment)
        {
            using var context = _factory.CreateDbContext();
            if (appointment.Patient != null) context.Entry(appointment.Patient).State = EntityState.Unchanged;
            if (appointment.Doctor != null) context.Entry(appointment.Doctor).State = EntityState.Unchanged;

            context.Appointments.Add(appointment);
            await context.SaveChangesAsync();
            return appointment;
        }

        public async Task UpdateAppointmentAsync(Appointment appointment)
        {
            using var context = _factory.CreateDbContext();
            if (appointment.Patient != null) context.Entry(appointment.Patient).State = EntityState.Unchanged;
            if (appointment.Doctor != null) context.Entry(appointment.Doctor).State = EntityState.Unchanged;
            
            context.Appointments.Update(appointment);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAppointmentAsync(int id)
        {
            using var context = _factory.CreateDbContext();
            var appt = await context.Appointments.FindAsync(id);
            if (appt != null)
            {
                context.Appointments.Remove(appt);
                await context.SaveChangesAsync();
            }
        }

        // --- MEDICAL RECORDS (EMR) ---
        public async Task<List<MedicalRecord>> GetMedicalRecordsAsync()
        {
            using var context = _factory.CreateDbContext();
            return await context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .OrderByDescending(m => m.Date)
                .ToListAsync();
        }

        public async Task<MedicalRecord> AddMedicalRecordAsync(MedicalRecord record)
        {
            using var context = _factory.CreateDbContext();
            if (record.Patient != null) context.Entry(record.Patient).State = EntityState.Unchanged;
            if (record.Doctor != null) context.Entry(record.Doctor).State = EntityState.Unchanged;

            context.MedicalRecords.Add(record);
            await context.SaveChangesAsync();
            return record;
        }

        public async Task UpdateMedicalRecordAsync(MedicalRecord record)
        {
            using var context = _factory.CreateDbContext();
            if (record.Patient != null) context.Entry(record.Patient).State = EntityState.Unchanged;
            if (record.Doctor != null) context.Entry(record.Doctor).State = EntityState.Unchanged;

            context.MedicalRecords.Update(record);
            await context.SaveChangesAsync();
        }

        public async Task DeleteMedicalRecordAsync(int id)
        {
            using var context = _factory.CreateDbContext();
            var record = await context.MedicalRecords.FindAsync(id);
            if (record != null)
            {
                context.MedicalRecords.Remove(record);
                await context.SaveChangesAsync();
            }
        }

        // --- BILLS ---
        public async Task<List<Bill>> GetBillsAsync()
        {
            using var context = _factory.CreateDbContext();
            return await context.Bills
                .Include(b => b.Patient)
                .OrderByDescending(b => b.Date)
                .ToListAsync();
        }

        public async Task<Bill> AddBillAsync(Bill bill)
        {
            using var context = _factory.CreateDbContext();
            if (bill.Patient != null) context.Entry(bill.Patient).State = EntityState.Unchanged;

            context.Bills.Add(bill);
            await context.SaveChangesAsync();
            return bill;
        }

        public async Task UpdateBillAsync(Bill bill)
        {
            using var context = _factory.CreateDbContext();
            if (bill.Patient != null) context.Entry(bill.Patient).State = EntityState.Unchanged;

            context.Bills.Update(bill);
            await context.SaveChangesAsync();
        }

        public async Task DeleteBillAsync(int id)
        {
            using var context = _factory.CreateDbContext();
            var bill = await context.Bills.FindAsync(id);
            if (bill != null)
            {
                context.Bills.Remove(bill);
                await context.SaveChangesAsync();
            }
        }

        // --- LAB RESULTS ---
        public async Task<List<LabResult>> GetLabResultsAsync()
        {
            using var context = _factory.CreateDbContext();
            return await context.LabResults
                .Include(l => l.MedicalRecord)
                .ThenInclude(m => m.Patient)
                .OrderByDescending(l => l.Date)
                .ToListAsync();
        }

        public async Task<LabResult> AddLabResultAsync(LabResult labResult)
        {
            using var context = _factory.CreateDbContext();
            if (labResult.MedicalRecord != null) context.Entry(labResult.MedicalRecord).State = EntityState.Unchanged;

            context.LabResults.Add(labResult);
            await context.SaveChangesAsync();
            return labResult;
        }

        public async Task UpdateLabResultAsync(LabResult labResult)
        {
            using var context = _factory.CreateDbContext();
            if (labResult.MedicalRecord != null) context.Entry(labResult.MedicalRecord).State = EntityState.Unchanged;

            context.LabResults.Update(labResult);
            await context.SaveChangesAsync();
        }

        public async Task DeleteLabResultAsync(int id)
        {
            using var context = _factory.CreateDbContext();
            var lab = await context.LabResults.FindAsync(id);
            if (lab != null)
            {
                context.LabResults.Remove(lab);
                await context.SaveChangesAsync();
            }
        }

        // --- PRESCRIPTIONS ---
        public async Task<List<Prescription>> GetPrescriptionsAsync()
        {
            using var context = _factory.CreateDbContext();
            return await context.Prescriptions
                .Include(p => p.Medicine)
                .Include(p => p.MedicalRecord)
                .ThenInclude(m => m.Patient)
                .ToListAsync();
        }

        public async Task<Prescription> AddPrescriptionAsync(Prescription prescription)
        {
            using var context = _factory.CreateDbContext();
            if (prescription.Medicine != null) context.Entry(prescription.Medicine).State = EntityState.Unchanged;
            if (prescription.MedicalRecord != null) context.Entry(prescription.MedicalRecord).State = EntityState.Unchanged;

            context.Prescriptions.Add(prescription);
            await context.SaveChangesAsync();
            return prescription;
        }

        public async Task UpdatePrescriptionAsync(Prescription prescription)
        {
            using var context = _factory.CreateDbContext();
            if (prescription.Medicine != null) context.Entry(prescription.Medicine).State = EntityState.Unchanged;
            if (prescription.MedicalRecord != null) context.Entry(prescription.MedicalRecord).State = EntityState.Unchanged;

            context.Prescriptions.Update(prescription);
            await context.SaveChangesAsync();
        }

        public async Task DeletePrescriptionAsync(int id)
        {
            using var context = _factory.CreateDbContext();
            var pres = await context.Prescriptions.FindAsync(id);
            if (pres != null)
            {
                context.Prescriptions.Remove(pres);
                await context.SaveChangesAsync();
            }
        }

        // --- REPORTING DASHBOARD ---

        public async Task<List<int>> GetAvailableYearsAsync()
        {
            using var context = _factory.CreateDbContext();
            // Get years from Bills and Appointments
            var billYears = await context.Bills.Select(b => b.Date.Year).Distinct().ToListAsync();
            var apptYears = await context.Appointments.Select(a => a.AppointmentDate.Year).Distinct().ToListAsync();
            
            return billYears.Concat(apptYears).Distinct().OrderByDescending(y => y).ToList();
        }

        public async Task<List<MonthlyRevenue>> GetMonthlyRevenueStatsAsync(int year)
        {
            using var context = _factory.CreateDbContext();
            var bills = await context.Bills
                .Where(b => b.IsPaid && b.Date.Year == year)
                .ToListAsync();
            
            var result = bills.GroupBy(b => b.Date.Month)
                .Select(g => new MonthlyRevenue
                {
                    Month = new DateTime(year, g.Key, 1).ToString("MMM"), // Use abbreviation
                    TotalRevenue = g.Sum(x => x.TotalAmount),
                    PaidBillsCount = g.Count()
                })
                .OrderBy(x => DateTime.ParseExact(x.Month, "MMM", System.Globalization.CultureInfo.InvariantCulture))
                .ToList();

            // Fill missing months for better chart visualization
            var allMonths = Enumerable.Range(1, 12).Select(i => new DateTime(year, i, 1).ToString("MMM")).ToList();
            var finalResult = new List<MonthlyRevenue>();
            
            foreach (var m in allMonths)
            {
                var existing = result.FirstOrDefault(r => r.Month == m);
                finalResult.Add(existing ?? new MonthlyRevenue { Month = m, TotalRevenue = 0, PaidBillsCount = 0 });
            }

            return finalResult;
        }

        public async Task<List<PatientVisitStats>> GetPatientVisitsStatsAsync(int year)
        {
            using var context = _factory.CreateDbContext();
            var appointments = await context.Appointments
                .Where(a => a.Status == "Completed" && a.AppointmentDate.Year == year)
                .ToListAsync();

            var result = appointments.GroupBy(a => a.AppointmentDate.Month)
                .Select(g => new PatientVisitStats
                {
                    Month = new DateTime(year, g.Key, 1).ToString("MMM"),
                    VisitCount = g.Count()
                })
                .OrderBy(x => DateTime.ParseExact(x.Month, "MMM", System.Globalization.CultureInfo.InvariantCulture))
                .ToList();

            // Fill missing months
            var allMonths = Enumerable.Range(1, 12).Select(i => new DateTime(year, i, 1).ToString("MMM")).ToList();
            var finalResult = new List<PatientVisitStats>();

            foreach (var m in allMonths)
            {
                var existing = result.FirstOrDefault(r => r.Month == m);
                finalResult.Add(existing ?? new PatientVisitStats { Month = m, VisitCount = 0 });
            }

            return finalResult;
        }

        public async Task<List<DiagnosisStats>> GetTopDiagnosesAsync(int year)
        {
            using var context = _factory.CreateDbContext();
            // Assuming MedicalRecords have a Date property
            var records = await context.MedicalRecords
                .Where(m => m.Date.Year == year)
                .ToListAsync();

            var result = records
                .Where(m => !string.IsNullOrEmpty(m.Diagnosis))
                .GroupBy(m => m.Diagnosis)
                .Select(g => new DiagnosisStats
                {
                    Diagnosis = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            return result;
        }

        public async Task<List<PaymentMethodStats>> GetRevenueByPaymentMethodAsync(int year)
        {
            using var context = _factory.CreateDbContext();
            var bills = await context.Bills
                .Where(b => b.IsPaid && b.Date.Year == year)
                .ToListAsync();

            var result = bills
                .GroupBy(b => b.PaymentMethod)
                .Select(g => new PaymentMethodStats
                {
                    Method = g.Key,
                    Amount = g.Sum(b => b.TotalAmount)
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            return result;
        }

        public async Task<List<DoctorPerformanceStats>> GetDoctorPerformanceAsync(int year)
        {
            using var context = _factory.CreateDbContext();
            var appointments = await context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.Status == "Completed" && a.AppointmentDate.Year == year)
                .ToListAsync();

            var result = appointments
                .Where(a => a.Doctor != null)
                .GroupBy(a => a.Doctor.FullName)
                .Select(g => new DoctorPerformanceStats
                {
                    DoctorName = g.Key,
                    PatientCount = g.Count()
                })
                .OrderByDescending(x => x.PatientCount)
                .Take(7) // Top 7 doctors
                .ToList();

            return result;
        }
    }
}
