using SMSNet.Models;

namespace SMSNet.Services.Assistant;

/// <summary>
/// Who the assistant is answering for.
/// <para>
/// The kernel functions run server-side with full database access, so the model
/// alone must not decide what a caller may see. Every sensitive function checks
/// this first — a signed-in <c>siswa</c> asking for the payroll gets a refusal
/// from the function, not from the prompt.
/// </para>
/// </summary>
public sealed class AssistantUserContext
{
    public string UserId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = "Pengguna";

    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    public bool IsAdmin => HasRole(AppRoles.Admin);

    public bool IsStaff => HasRole(AppRoles.Admin) || HasRole(AppRoles.Guru);

    /// <summary>Admin or a parent — the two roles with a legitimate interest in billing.</summary>
    public bool CanSeeFinance => HasRole(AppRoles.Admin) || HasRole(AppRoles.OrangTua);

    public bool HasRole(string role) =>
        Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));

    public string RoleLabel => Roles.Count == 0 ? "tamu" : string.Join(", ", Roles);
}
