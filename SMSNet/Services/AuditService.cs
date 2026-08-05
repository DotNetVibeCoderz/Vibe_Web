using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SMSNet.Data;
using SMSNet.Models;

namespace SMSNet.Services;

/// <summary>
/// Writes the audit trail.
/// <para>
/// The <c>AuditTrail</c> table existed but nothing ever wrote to it outside the
/// seeder, so the Audit page showed two demo rows forever. Every create, update,
/// and delete now records who did what.
/// </para>
/// </summary>
public sealed class AuditService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly AuthenticationStateProviderAccessor _accessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        AuthenticationStateProviderAccessor accessor,
        ILogger<AuditService> logger)
    {
        _dbFactory = dbFactory;
        _accessor = accessor;
        _logger = logger;
    }

    public Task RecordCreateAsync(string entity, string detail) =>
        WriteAsync($"Tambah {entity}", detail);

    public Task RecordUpdateAsync(string entity, string detail) =>
        WriteAsync($"Ubah {entity}", detail);

    public Task RecordDeleteAsync(string entity, string detail) =>
        WriteAsync($"Hapus {entity}", detail);

    public async Task WriteAsync(string action, string? detail = null)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            db.AuditTrails.Add(new AuditTrail
            {
                Action = action,
                Actor = await _accessor.GetUserNameAsync(),
                Timestamp = SchoolClock.LocalNow,
                Detail = detail
            });

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // An audit failure must never take down the operation being audited.
            _logger.LogWarning(ex, "Failed to write audit entry for {Action}", action);
        }
    }
}

/// <summary>
/// Reads the current user's name without every caller having to thread a
/// cascading <c>AuthenticationState</c> through its constructor.
/// </summary>
public sealed class AuthenticationStateProviderAccessor
{
    private readonly Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider _provider;

    public AuthenticationStateProviderAccessor(
        Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider provider)
    {
        _provider = provider;
    }

    public async Task<string> GetUserNameAsync()
    {
        try
        {
            var state = await _provider.GetAuthenticationStateAsync();
            var user = state.User;

            return user.FindFirst(AppUserClaimsPrincipalFactory.FullNameClaim)?.Value
                   ?? user.Identity?.Name
                   ?? "sistem";
        }
        catch (InvalidOperationException)
        {
            // Outside a circuit (background work, seeding).
            return "sistem";
        }
    }

    public async Task<ClaimsPrincipal?> GetUserAsync()
    {
        try
        {
            return (await _provider.GetAuthenticationStateAsync()).User;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
