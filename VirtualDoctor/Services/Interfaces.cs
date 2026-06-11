using System.Security.Claims;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(string email, string password, string fullName);
        Task<bool> LoginAsync(string email, string password);
        Task LogoutAsync();
        Task<bool> ResetPasswordAsync(string email);
        Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword);
        ClaimsPrincipal? GetCurrentUser();
        string? GetCurrentUserId();
        Task<ApplicationUser?> GetCurrentUserAsync();
        bool IsAdmin();
        bool IsDoctor();
        string? GetDoctorId();
    }

    public interface IUserService
    {
        Task<ApplicationUser?> GetByIdAsync(string id);
        Task<ApplicationUser?> GetByEmailAsync(string email);
        Task<bool> UpdateProfileAsync(ApplicationUser user);
        Task<bool> DeleteAsync(string id);
        Task<List<ApplicationUser>> GetAllAsync();
        Task<ApplicationUser?> CreateAsync(ApplicationUser user, string password);
        Task<bool> UpdateAsync(ApplicationUser user);
    }

    public interface IDoctorService
    {
        Task<List<Doctor>> GetAllAsync();
        Task<List<Doctor>> GetBySpecializationAsync(string s);
        Task<Doctor?> GetByIdAsync(string id);
        Task<Doctor?> GetByIdWithScheduleAsync(string id);
        Task<List<Doctor>> SearchAsync(string q);
        Task<Doctor> CreateAsync(Doctor doctor);
        Task<Doctor?> UpdateAsync(Doctor doctor);
        Task<bool> DeleteAsync(string id);
    }

    public interface IMedicineService
    {
        Task<List<Medicine>> GetAllAsync();
        Task<List<Medicine>> GetByCategoryAsync(string c);
        Task<Medicine?> GetByIdAsync(string id);
        Task<List<Medicine>> SearchAsync(string q);
        Task<List<Medicine>> GetRecommendationsAsync(string uid);
        Task<Medicine> CreateAsync(Medicine m);
        Task<Medicine?> UpdateAsync(Medicine m);
        Task<bool> DeleteAsync(string id);
    }

    public interface IHospitalService
    {
        Task<List<Hospital>> GetAllAsync();
        Task<List<Hospital>> GetByTypeAsync(HospitalType t);
        Task<Hospital?> GetByIdAsync(string id);
        Task<List<Hospital>> FindNearestAsync(double lat, double lng, double r = 10);
        Task<List<Hospital>> SearchAsync(string q);
        Task<Hospital> CreateAsync(Hospital h);
        Task<Hospital?> UpdateAsync(Hospital h);
        Task<bool> DeleteAsync(string id);
    }

    public interface IAppointmentService
    {
        Task<Appointment?> BookAsync(Appointment a);
        Task<List<Appointment>> GetUserAppointmentsAsync(string uid);
        Task<List<Appointment>> GetDoctorAppointmentsAsync(string did);
        Task<bool> UpdateStatusAsync(string id, AppointmentStatus s);
        Task<bool> CancelAsync(string id);
        Task<List<DoctorSchedule>> GetDoctorScheduleAsync(string did);
        Task<List<Appointment>> GetAllAsync();
        Task<Appointment?> GetByIdAsync(string id);
        Task<Appointment?> CreateAsync(Appointment a);
        Task<Appointment?> UpdateAsync(Appointment a);
        Task<bool> DeleteAsync(string id);
    }

    public interface IDoctorScheduleService
    {
        Task<List<DoctorSchedule>> GetAllAsync();
        Task<DoctorSchedule?> GetByIdAsync(string id);
        Task<List<DoctorSchedule>> GetByDoctorAsync(string doctorId);
        Task<DoctorSchedule> CreateAsync(DoctorSchedule s);
        Task<DoctorSchedule?> UpdateAsync(DoctorSchedule s);
        Task<bool> DeleteAsync(string id);
    }

    public interface IConsultationService
    {
        Task<Consultation?> StartAsync(string uid, string did, ConsultationType t);
        Task<bool> SendMessageAsync(string cid, string sid, string sn, string msg);
        Task<List<ConsultationMessage>> GetMessagesAsync(string cid);
        Task<List<Consultation>> GetUserConsultationsAsync(string uid);
        Task<List<Consultation>> GetDoctorConsultationsAsync(string did);
        Task<Consultation?> GetByIdAsync(string id);
        Task<bool> EndAsync(string cid);
    }

    public interface IOrderService
    {
        Task<Order?> CreateOrderAsync(Order o);
        Task<List<Order>> GetUserOrdersAsync(string uid);
        Task<Order?> GetByIdAsync(string id);
        Task<bool> UpdateStatusAsync(string id, OrderStatus s);
        Task<bool> CancelAsync(string id);
    }

    public interface IHomecareService
    {
        Task<HomecareService?> BookAsync(HomecareService s);
        Task<List<HomecareService>> GetUserServicesAsync(string uid);
        Task<bool> UpdateStatusAsync(string id, HomecareServiceStatus s);
        Task<List<HomecareService>> GetAllAsync();
        Task<HomecareService> CreateAsync(HomecareService s);
        Task<HomecareService?> UpdateAsync(HomecareService s);
        Task<bool> DeleteAsync(string id);
    }

    public interface IArticleService
    {
        Task<List<HealthArticle>> GetAllAsync();
        Task<(List<HealthArticle> Items, int TotalCount)> GetPagedAsync(int page, int size, string? search = null, string? category = null, bool? isIndexed = null);
        Task<HealthArticle?> GetByIdAsync(string id);
        Task<List<HealthArticle>> SearchAsync(string q);
        Task<List<HealthArticle>> GetByCategoryAsync(string c);
        Task<HealthArticle> CreateAsync(HealthArticle a);
        Task<HealthArticle> UpdateAsync(HealthArticle a);
        Task<bool> DeleteAsync(string id);
        Task<int> GetTotalCountAsync();
        Task<List<string>> GetCategoriesAsync();
    }

    public interface IRecommendationService
    {
        Task<List<Medicine>> RecommendMedicinesAsync(string uid);
        Task<List<string>> RecommendServicesAsync(string uid);
        Task<string> GetHealthTipAsync(string uid);
    }

    public interface IInsuranceService
    {
        Task<bool> VerifyInsuranceAsync(string p, string n);
        Task<decimal> CalculateCoverageAsync(string p, string n, decimal c);
        Task<List<string>> GetProvidersAsync();
    }

    public interface IReviewService
    {
        Task<DoctorStats> GetDoctorStatsAsync(string doctorId);
        Task<List<DoctorReview>> GetDoctorReviewsAsync(string doctorId, int take = 10);
        Task<List<DoctorReview>> GetReviewsByUserAsync(string userId);
        Task<DoctorReviewTarget?> GetPendingReviewForDoctorAsync(string doctorId, string userId);
        Task<bool> HasReviewForConsultationAsync(string consultationId, string userId);
        Task<bool> HasReviewForAppointmentAsync(string appointmentId, string userId);
        Task<DoctorReview?> CreateReviewForConsultationAsync(string consultationId, string userId, int rating, string comment);
        Task<DoctorReview?> CreateReviewForAppointmentAsync(string appointmentId, string userId, int rating, string comment);
    }
}

namespace VirtualDoctor.Services.AI
{
    public interface ILlmProviderFactory
    {
        Kernel GetKernel(string? provider = null, double? temperature = null);
        IChatCompletionService GetChatService(string? provider = null);
        OpenAIPromptExecutionSettings GetExecutionSettings(string? provider = null, double? temperature = null, bool enableFunctions = false);
        List<string> GetAvailableProviders();
        string SystemPrompt { get; }
        Kernel GetBaseKernel();
    }
    public interface IAiChatService
    {
        Task<string> SendMessageAsync(string userId, string chatId, string message, string? provider = null, string? imageUrl = null, string? documentUrl = null);
        IAsyncEnumerable<string> SendStreamingMessageAsync(string userId, string chatId, string message, string? provider = null, string? imageUrl = null, string? documentUrl = null);
        Task<List<Models.ChatHistory>> GetUserChatsAsync(string userId);
        Task<Models.ChatHistory?> GetChatAsync(string chatId);
        Task<Models.ChatHistory> CreateChatAsync(string userId, string title = "Konsultasi Baru");
        Task<bool> DeleteChatAsync(string chatId);
        Task ClearChatAsync(string chatId);
        Task<string> GetCurrentProviderAsync();
        Task SetProviderAsync(string provider);
        Task<string> GetBotNameAsync();
        Task SetBotNameAsync(string name);
        Task UpdateSystemPromptAsync(string prompt);
        Task UpdateTemperatureAsync(double temperature);
    }
    public interface IKernelFunctionService
    {
        void RegisterAllPlugins(Kernel kernel);
        Task<string> SearchInternetAsync(string query);
        Task<string> CheckDateAsync();
        Task<string> MathCalcAsync(string expression);
        Task<string> ReadFileFromUrlAsync(string url);
        Task<string> DescribeImageAsync(string imageUrl);
        Task<string> ScrapWebPageAsync(string url);
        Task<string> AskDoctorAsync(string question);
        Task<string> OrderMedicineAsync(string medicineName, int quantity);
        Task<string> ScheduleDoctorAsync(string request);
        Task<string> FindHospitalAsync(string location);
        Task<string> QueryHealthDocsAsync(string question);
    }
}

namespace VirtualDoctor.Services.RAG
{
    public interface IVectorStoreService
    {
        Task InitializeAsync();
        Task IndexDocumentAsync(string documentId, string content, Dictionary<string, string>? metadata = null);
        Task IndexChunksAsync(string documentId, List<string> chunks, Dictionary<string, string>? metadata = null);
        Task<List<(string DocumentId, string Content, float Score)>> SearchAsync(string query, int topK = 5);
        Task DeleteDocumentAsync(string documentId);
        Task<bool> IsDocumentIndexedAsync(string documentId);
        Task<int> GetDocumentCountAsync();
    }
    public interface IDocumentIndexingService { Task IndexPdfFileAsync(string path); Task IndexPdfFolderAsync(string folder); Task<string> ExtractTextFromPdfAsync(string path); List<string> ChunkText(string text, int chunkSize = 1000, int overlap = 200); Task ReindexAllAsync(); }
    public interface IRagQueryService { Task<string> QueryAsync(string question, string? llmProvider = null); Task<List<(Models.HealthArticle Article, float Score)>> FindRelevantArticlesAsync(string query, int topK = 5); }
}

namespace VirtualDoctor.Services.Storage
{
    public interface IFileStorageService { Task<string> UploadAsync(Stream stream, string fileName, string contentType); Task<Stream?> DownloadAsync(string filePath); Task<bool> DeleteAsync(string filePath); Task<string> GetPublicUrlAsync(string filePath); Task<bool> ExistsAsync(string filePath); Task<List<string>> ListFilesAsync(string prefix = ""); }
    public interface ILocationService { Task<List<(string Name, double Lat, double Lng, string Address)>> FindNearbyHospitalsAsync(double lat, double lng, double r = 10); Task<double> CalculateDistanceAsync(double lat1, double lng1, double lat2, double lng2); Task<string?> GeocodeAsync(string a); Task<(double Lat, double Lng)?> ReverseGeocodeAsync(string a); }
    public interface ISearchService { Task<string> SearchAsync(string query); Task<string> SearchHealthAsync(string query); }
}
