using MyPoS.Services;

namespace MyPoS.Api
{
    public static class ApiRoutes
    {
        /// <summary>
        /// Mendaftarkan seluruh endpoint REST di bawah satu grup. Filter kunci API dipasang
        /// pada grupnya, bukan pada tiap endpoint, sehingga endpoint baru tidak mungkin
        /// terlupa dilindungi.
        /// </summary>
        public static IEndpointRouteBuilder MapMyPosApi(this IEndpointRouteBuilder app, string prefix)
        {
            var group = app.MapGroup(prefix)
                .AddEndpointFilter<ApiKeyEndpointFilter>();

            group.MapCatalogEndpoints();
            group.MapSalesEndpoints();
            group.MapReportEndpoints();

            return app;
        }

        /// <summary>Membaca parameter halaman dengan batas yang wajar.</summary>
        internal static (int Page, int PageSize) ReadPaging(int? page, int? pageSize)
            => (Math.Max(1, page ?? 1), Math.Clamp(pageSize ?? 50, 1, 200));

        internal static PagedResult<T> ToPaged<T>(IReadOnlyList<T> items, int total, int page, int pageSize)
            => new(page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize), items);
    }
}
