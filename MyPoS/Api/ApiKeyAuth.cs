using System;
using System.Threading.Tasks;
using MyPoS.Models;
using MyPoS.Services;

namespace MyPoS.Api
{
    /// <summary>
    /// Filter otentikasi untuk seluruh endpoint REST.
    ///
    /// Kunci dikirim lewat header <c>X-Api-Key</c>. Kunci yang hanya boleh membaca akan
    /// ditolak pada metode yang mengubah data, sehingga integrasi pelaporan cukup diberi
    /// kunci baca saja tanpa risiko mengubah stok atau transaksi.
    /// </summary>
    public class ApiKeyEndpointFilter : IEndpointFilter
    {
        public const string HeaderName = "X-Api-Key";
        public const string ContextItemKey = "MyPoS.ApiKey";

        private static readonly string[] WriteMethods = ["POST", "PUT", "PATCH", "DELETE"];

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;
            var service = http.RequestServices.GetRequiredService<ApiKeyService>();

            if (!http.Request.Headers.TryGetValue(HeaderName, out var presented) || presented.Count == 0)
            {
                return Results.Json(
                    new ApiError($"Header {HeaderName} wajib disertakan."),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var key = await service.ValidateAsync(presented.ToString(), http.RequestAborted);

            if (key is null)
            {
                return Results.Json(
                    new ApiError("Kunci API tidak dikenal, dinonaktifkan, atau sudah kedaluwarsa."),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var isWrite = Array.Exists(WriteMethods, m =>
                string.Equals(m, http.Request.Method, StringComparison.OrdinalIgnoreCase));

            if (isWrite && !key.CanWrite)
            {
                return Results.Json(
                    new ApiError("Kunci API ini hanya memiliki izin baca."),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            http.Items[ContextItemKey] = key;
            return await next(context);
        }
    }

    public static class ApiKeyContextExtensions
    {
        /// <summary>Kunci yang dipakai pada permintaan ini, tersedia setelah filter lolos.</summary>
        public static ApiKey? GetApiKey(this HttpContext context)
            => context.Items.TryGetValue(ApiKeyEndpointFilter.ContextItemKey, out var value) ? value as ApiKey : null;
    }
}
