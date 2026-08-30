namespace DayNex.Domain.Common.Entity
{
    public class Result
    {
        public bool Succeeded { get; }
        public string? Error { get; }
        public string? ErrorCode { get; }

        protected Result(bool succeeded, string? error, string? errorCode)
        {
            Succeeded = succeeded;
            Error = error;
            ErrorCode = errorCode;
        }

        public static Result Success() => new(true, null, null);
        public static Result Failure(string error, string errorCode = "BAD_REQUEST") =>
            new(false, error, errorCode);
    }

    public class Result<T> : Result
    {
        public T? Data { get; }

        private Result(bool succeeded, T? data, string? error, string? errorCode)
            : base(succeeded, error, errorCode)
        {
            Data = data;
        }

        public static Result<T> Success(T data) => new(true, data, null, null);

        public static new Result<T> Failure(string error, string errorCode = "BAD_REQUEST") =>
            new(false, default, error, errorCode);
    }

}
