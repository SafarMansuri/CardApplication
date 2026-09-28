namespace CardApplicationAPI.Models.ResponseModel
{
    public enum ApplicationActionStatusCodes
    {
        Success = 200,
        Error = 500,
        NotFound = 404,
        ValidationFailed = 400,
        Unauthorized = 401,
        BadRequest = 400,
        Forbidden = 403,
        Conflict = 409
    }

    public class ApplicationActionResult<T>

    {
        public ApplicationActionStatusCodes StatusCode { get; set; }
        public string? StatusText { get; set; }
        public string? StatusMessage { get; set; }
        public T? ResultObject { get; set; }
    }
    public class PaginationParams
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int Skip => (PageNumber - 1) * PageSize;
    }
    public class PaginatedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
        public PaginationDetail Pagination { get; set; } = new();
    }

    public static class PaginationExtensions
    {
        public const int MaxPageSize = 100;

        public static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(
            this IQueryable<T> query,
            PaginationParams pagination,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(pagination);
            if (pagination.PageNumber < 1)
                throw new ArgumentException("Page number must be at least 1.", nameof(pagination));
            if (pagination.PageSize < 1 || pagination.PageSize > MaxPageSize)
                throw new ArgumentException($"Page size must be between 1 and {MaxPageSize}.", nameof(pagination));

            var totalCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(query, cancellationToken);
            var items = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                query.Skip(pagination.Skip).Take(pagination.PageSize), cancellationToken);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize);

            return new PaginatedResult<T>
            {
                Items = items,
                Pagination = new PaginationDetail
                {
                    TotalCount = totalCount,
                    PageSize = pagination.PageSize,
                    CurrentPage = pagination.PageNumber,
                    TotalPages = totalPages,
                    HasNext = pagination.PageNumber < totalPages,
                    HasPrevious = pagination.PageNumber > 1
                }
            };
        }
    }

    public class PaginationDetail
    {
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious { get; set; }
    }

    public class InternalActionResult<T> : ApplicationActionResult<T>
    {
        public bool StopProccess { get; set; }

        public static InternalActionResult<T> CreateSuccessResponse(T response)
        {
            return new InternalActionResult<T>
            {
                StatusCode = ApplicationActionStatusCodes.Success,
                StopProccess = true,
                ResultObject = response,
                StatusText = "Success",
                StatusMessage = "Operation completed successfully"
            };
        }
    }

    public static class FactoryResponse<T>
    {
        public static InternalActionResult<T> CreateSuccess(T response, string? message = null)
        {
            return new InternalActionResult<T>
            {
                StatusCode = ApplicationActionStatusCodes.Success,
                StopProccess = true,
                ResultObject = response,
                StatusText = "Success",
                StatusMessage = message ?? "Request completed successfully."
            };
        }

        public static InternalActionResult<T> CreateFail(Exception ex)
        {
            return new InternalActionResult<T>
            {
                StatusCode = ApplicationActionStatusCodes.Error,
                StopProccess = true,
                StatusText = ex.GetType().Name,
                StatusMessage = ex.Message
            };
        }

        public static InternalActionResult<T> CreateFail(string message, ApplicationActionStatusCodes code = ApplicationActionStatusCodes.Error, string? exceptionType = null)
        {
            return new InternalActionResult<T>
            {
                StatusCode = code,
                StopProccess = true,
                StatusText = exceptionType ?? "Failed",
                StatusMessage = message
            };
        }
    }
}
