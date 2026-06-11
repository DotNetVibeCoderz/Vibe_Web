using Microsoft.EntityFrameworkCore;
using VirtualDoctor.Data;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _db;
    public ReviewService(AppDbContext db) => _db = db;

    public async Task<DoctorStats> GetDoctorStatsAsync(string doctorId)
    {
        var reviews = _db.DoctorReviews.Where(r => r.DoctorId == doctorId);
        var avgRating = await reviews.Select(r => (double?)r.Rating).AverageAsync() ?? 0;
        var reviewCount = await reviews.CountAsync();

        var consultationUsers = _db.Consultations
            .Where(c => c.DoctorId == doctorId && c.Status == ConsultationStatus.Completed)
            .Select(c => c.UserId);

        var appointmentUsers = _db.Appointments
            .Where(a => a.DoctorId == doctorId && a.Status == AppointmentStatus.Completed)
            .Select(a => a.UserId);

        var totalPatients = await consultationUsers.Union(appointmentUsers).Distinct().CountAsync();

        return new DoctorStats
        {
            AverageRating = avgRating,
            ReviewCount = reviewCount,
            TotalPatients = totalPatients
        };
    }

    public async Task<List<DoctorReview>> GetDoctorReviewsAsync(string doctorId, int take = 10)
    {
        return await _db.DoctorReviews
            .Include(r => r.User)
            .Where(r => r.DoctorId == doctorId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<DoctorReview>> GetReviewsByUserAsync(string userId)
    {
        return await _db.DoctorReviews
            .Where(r => r.UserId == userId)
            .ToListAsync();
    }

    public async Task<DoctorReviewTarget?> GetPendingReviewForDoctorAsync(string doctorId, string userId)
    {
        var reviewedConsultations = await _db.DoctorReviews
            .Where(r => r.UserId == userId && r.ConsultationId != null)
            .Select(r => r.ConsultationId!)
            .ToListAsync();

        var reviewedAppointments = await _db.DoctorReviews
            .Where(r => r.UserId == userId && r.AppointmentId != null)
            .Select(r => r.AppointmentId!)
            .ToListAsync();

        var lastConsult = await _db.Consultations
            .Where(c => c.DoctorId == doctorId && c.UserId == userId && c.Status == ConsultationStatus.Completed)
            .Where(c => !reviewedConsultations.Contains(c.Id))
            .OrderByDescending(c => c.EndedAt)
            .FirstOrDefaultAsync();

        if (lastConsult != null)
        {
            return new DoctorReviewTarget
            {
                SourceId = lastConsult.Id,
                SourceType = ReviewSourceType.Consultation,
                CompletedAt = lastConsult.EndedAt
            };
        }

        var lastAppointment = await _db.Appointments
            .Where(a => a.DoctorId == doctorId && a.UserId == userId && a.Status == AppointmentStatus.Completed)
            .Where(a => !reviewedAppointments.Contains(a.Id))
            .OrderByDescending(a => a.AppointmentDate)
            .FirstOrDefaultAsync();

        if (lastAppointment != null)
        {
            return new DoctorReviewTarget
            {
                SourceId = lastAppointment.Id,
                SourceType = ReviewSourceType.Appointment,
                CompletedAt = lastAppointment.AppointmentDate
            };
        }

        return null;
    }

    public async Task<bool> HasReviewForConsultationAsync(string consultationId, string userId)
    {
        return await _db.DoctorReviews.AnyAsync(r => r.ConsultationId == consultationId && r.UserId == userId);
    }

    public async Task<bool> HasReviewForAppointmentAsync(string appointmentId, string userId)
    {
        return await _db.DoctorReviews.AnyAsync(r => r.AppointmentId == appointmentId && r.UserId == userId);
    }

    public async Task<DoctorReview?> CreateReviewForConsultationAsync(string consultationId, string userId, int rating, string comment)
    {
        if (rating is < 1 or > 5) return null;
        var consultation = await _db.Consultations.Include(c => c.Doctor).FirstOrDefaultAsync(c => c.Id == consultationId);
        if (consultation == null || consultation.UserId != userId || consultation.Status != ConsultationStatus.Completed) return null;
        if (await HasReviewForConsultationAsync(consultationId, userId)) return null;

        var review = new DoctorReview
        {
            DoctorId = consultation.DoctorId,
            UserId = userId,
            ConsultationId = consultationId,
            Rating = rating,
            Comment = comment?.Trim() ?? string.Empty
        };

        _db.DoctorReviews.Add(review);
        await _db.SaveChangesAsync();
        return review;
    }

    public async Task<DoctorReview?> CreateReviewForAppointmentAsync(string appointmentId, string userId, int rating, string comment)
    {
        if (rating is < 1 or > 5) return null;
        var appointment = await _db.Appointments.Include(a => a.Doctor).FirstOrDefaultAsync(a => a.Id == appointmentId);
        if (appointment == null || appointment.UserId != userId || appointment.Status != AppointmentStatus.Completed) return null;
        if (await HasReviewForAppointmentAsync(appointmentId, userId)) return null;

        var review = new DoctorReview
        {
            DoctorId = appointment.DoctorId,
            UserId = userId,
            AppointmentId = appointmentId,
            Rating = rating,
            Comment = comment?.Trim() ?? string.Empty
        };

        _db.DoctorReviews.Add(review);
        await _db.SaveChangesAsync();
        return review;
    }
}
