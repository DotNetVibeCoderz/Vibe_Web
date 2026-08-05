using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using SMSNet.Data;
using SMSNet.Services;

namespace SMSNet.Components.Shared;

/// <summary>
/// Shared mechanics for the master-data screens: load, search, sort, page,
/// confirm-then-delete, and CSV export.
/// <para>
/// Every CRUD page previously repeated ~120 lines of this by hand, which is how
/// they drifted — some paged, some didn't; none confirmed a delete. Pages now
/// supply only what is specific to their entity.
/// </para>
/// <para>
/// Data access goes through <see cref="IDbContextFactory{TContext}"/> rather
/// than an injected context: a Blazor circuit outlives many awaits, and sharing
/// one context across them throws "A second operation was started on this
/// context".
/// </para>
/// </summary>
public abstract class CrudPageBase<TEntity> : ComponentBase where TEntity : class, new()
{
    [Inject] protected IDbContextFactory<ApplicationDbContext> DbFactory { get; set; } = default!;
    [Inject] protected ToastService Toasts { get; set; } = default!;
    [Inject] protected AuditService Audit { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    protected const int PageSize = 10;

    protected List<TEntity> Items { get; set; } = new();
    protected TEntity Form { get; set; } = new();

    protected string Search { get; set; } = string.Empty;
    protected string SortColumn { get; set; } = string.Empty;
    protected bool SortAscending { get; set; } = true;
    protected int CurrentPage { get; set; } = 1;

    protected bool ShowForm { get; set; }
    protected bool Loading { get; set; } = true;
    protected bool ConfirmDeleteOpen { get; set; }
    protected TEntity? PendingDelete { get; set; }

    // --- Contract for the concrete page -----------------------------------

    /// <summary>Singular, lower-case, e.g. "siswa". Used in messages and the audit trail.</summary>
    protected abstract string EntityLabel { get; }

    /// <summary>The rows this page manages.</summary>
    protected abstract DbSet<TEntity> Table(ApplicationDbContext db);

    /// <summary>Text the free-text search box looks through.</summary>
    protected abstract IEnumerable<string?> SearchableText(TEntity item);

    /// <summary>How one row is named in a confirmation or toast.</summary>
    protected abstract string Describe(TEntity item);

    protected abstract int IdOf(TEntity item);

    /// <summary>A detached copy, so cancelling an edit doesn't mutate the row on screen.</summary>
    protected abstract TEntity CloneForEdit(TEntity item);

    /// <summary>Column header → sort key. Applied in memory after filtering.</summary>
    protected abstract IEnumerable<TEntity> ApplySort(IEnumerable<TEntity> items);

    /// <summary>Extra per-page filters (class, status, category…). Default: none.</summary>
    protected virtual IEnumerable<TEntity> ApplyFilters(IEnumerable<TEntity> items) => items;

    protected abstract string CsvHeader { get; }

    protected abstract string CsvRow(TEntity item);

    protected virtual string CsvFileName => $"{EntityLabel}-{SchoolClock.Today:yyyyMMdd}.csv";

    /// <summary>A blank row for the add form — override to seed sensible defaults.</summary>
    protected virtual TEntity NewEntity() => new();

    // --- Derived views -----------------------------------------------------

    protected List<TEntity> Filtered
    {
        get
        {
            IEnumerable<TEntity> query = Items;

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim();
                query = query.Where(i => SearchableText(i)
                    .Any(t => t is not null && t.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            query = ApplyFilters(query);
            return ApplySort(query).ToList();
        }
    }

    protected List<TEntity> Paged =>
        Filtered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();

    protected int TotalPages => Math.Max(1, (int)Math.Ceiling(Filtered.Count / (double)PageSize));

    // --- Lifecycle ---------------------------------------------------------

    protected override async Task OnInitializedAsync() => await LoadAsync();

    protected async Task LoadAsync()
    {
        Loading = true;

        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            Items = await Table(db).AsNoTracking().ToListAsync();
        }
        catch (Exception ex)
        {
            Toasts.Error($"Gagal memuat data {EntityLabel}: {ex.Message}");
            Items = new List<TEntity>();
        }
        finally
        {
            Loading = false;
        }
    }

    // --- Commands ----------------------------------------------------------

    protected virtual void StartAdd()
    {
        Form = NewEntity();
        OnFormOpened();
        ShowForm = true;
    }

    protected virtual void StartEdit(TEntity item)
    {
        Form = CloneForEdit(item);
        OnFormOpened();
        ShowForm = true;
    }

    protected virtual void CancelForm()
    {
        Form = NewEntity();
        ShowForm = false;
    }

    /// <summary>
    /// Called after <see cref="Form"/> is populated and before the dialog opens.
    /// <para>
    /// Pages whose editor state lives outside the entity — a rich-text buffer, a
    /// multi-select list — use this to sync it. Doing that in the page's own
    /// StartEdit would be missed by every other path that opens the form.
    /// </para>
    /// </summary>
    protected virtual void OnFormOpened() { }

    protected async Task SaveAsync()
    {
        var isNew = IdOf(Form) == 0;

        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();

            if (isNew)
            {
                Table(db).Add(Form);
            }
            else
            {
                Table(db).Update(Form);
            }

            await db.SaveChangesAsync();

            var description = Describe(Form);
            await (isNew
                ? Audit.RecordCreateAsync(EntityLabel, description)
                : Audit.RecordUpdateAsync(EntityLabel, description));

            Toasts.Success(isNew ? $"{description} ditambahkan." : $"{description} diperbarui.");
            CancelForm();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Toasts.Error($"Gagal menyimpan: {ex.Message}");
        }
    }

    protected void AskDelete(TEntity item)
    {
        PendingDelete = item;
        ConfirmDeleteOpen = true;
    }

    protected string PendingDeleteLabel => PendingDelete is null ? string.Empty : Describe(PendingDelete);

    protected virtual async Task ConfirmDeleteAsync()
    {
        if (PendingDelete is null)
        {
            return;
        }

        var description = Describe(PendingDelete);
        var id = IdOf(PendingDelete);

        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            var row = await Table(db).FindAsync(id);

            if (row is not null)
            {
                Table(db).Remove(row);
                await db.SaveChangesAsync();
                await Audit.RecordDeleteAsync(EntityLabel, description);
                Toasts.Success($"{description} dihapus.");
            }

            await LoadAsync();

            // Deleting the last row of the last page would otherwise strand the user
            // on an empty page with no way back.
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }
        }
        catch (Exception ex)
        {
            Toasts.Error($"Gagal menghapus: {ex.Message}");
        }
        finally
        {
            PendingDelete = null;
        }
    }

    protected void SortBy(string column)
    {
        if (SortColumn == column)
        {
            SortAscending = !SortAscending;
        }
        else
        {
            SortColumn = column;
            SortAscending = true;
        }

        CurrentPage = 1;
    }

    /// <summary>Caret shown in a sortable column header.</summary>
    protected string SortCaret(string column) =>
        SortColumn == column ? (SortAscending ? "▲" : "▼") : string.Empty;

    protected void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
        }
    }

    protected void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
        }
    }

    /// <summary>Resetting to page 1 on a new search keeps results visible.</summary>
    protected void OnSearchChanged(string value)
    {
        Search = value;
        CurrentPage = 1;
    }

    protected async Task ExportCsvAsync()
    {
        var rows = Filtered;

        if (rows.Count == 0)
        {
            Toasts.Warn("Tidak ada data untuk diekspor.");
            return;
        }

        var csv = string.Join("\r\n", new[] { CsvHeader }.Concat(rows.Select(CsvRow)));

        try
        {
            await JS.InvokeVoidAsync("smsnetDownload", CsvFileName, csv);
            Toasts.Success($"{rows.Count} baris diekspor.");
        }
        catch (JSException ex)
        {
            Toasts.Error($"Gagal mengekspor: {ex.Message}");
        }
    }

    /// <summary>
    /// Formats a number for CSV using invariant culture.
    /// <para>
    /// The application runs under id-ID, where the decimal separator is a comma — the
    /// same character that separates CSV columns. Writing 85.5 as "85,5" would split
    /// one value across two columns, so numbers are always written with a point here
    /// even though they are displayed with a comma on screen.
    /// </para>
    /// </summary>
    protected static string CsvNumber(decimal value, string format = "0.##") =>
        value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);

    /// <inheritdoc cref="CsvNumber(decimal, string)"/>
    protected static string CsvNumber(double value, string format = "0.##") =>
        value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Quotes a CSV field so commas, quotes, and newlines survive Excel.</summary>
    protected static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        var escaped = value.Replace("\"", "\"\"");

        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }
}
