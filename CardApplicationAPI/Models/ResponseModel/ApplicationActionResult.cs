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
        Forbidden = 403
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
        public string SearchTerm { get; set; } = string.Empty;
        public string SortBy { get; set; } = "Name";
        public bool IsDescending { get; set; } = false;
        public int Skip => (PageNumber - 1) * PageSize;

        // ✅ New optional filters
        public Guid? AppId { get; set; }      // To filter unassigned reports
        public bool MetadataOnly { get; set; } // To return trimmed metadata onl
    }
    public class PaginatedResult<T>
    {
        public IEnumerable<T> Items { get; set; }
        public PaginationDetail Pagination { get; set; }
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
