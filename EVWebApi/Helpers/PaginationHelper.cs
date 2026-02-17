using EVWebApi.DTOs.Pagination;
using Microsoft.EntityFrameworkCore;

namespace EVWebApi.Helpers
{
    public static class PaginationHelper
    {
        public static async Task<PagedResponse<T>> GetPagedResponseAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize)
        {
            if (pageSize <= 0)
                pageSize = 10;

            if (pageNumber <= 0)
                pageNumber = 1;

            var totalRecords = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            if (pageNumber > totalPages && totalPages > 0)
                pageNumber = totalPages;

            var data = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<T>
            {
                Data = data,
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
