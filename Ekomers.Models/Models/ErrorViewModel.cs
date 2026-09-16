namespace Ekomers.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public string? RequestPath { get; set; }
        public string? ExceptionType { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? TechnicalDetails { get; set; }
        public bool ShowTechnicalDetails { get; set; }

        // Eski hata görünümüyle geriye dönük uyumluluk için korunuyor.
        public string? exception { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public static ErrorViewModel FromException(
            Exception? error,
            string? requestPath,
            string? requestId,
            bool showTechnicalDetails) => new()
        {
            RequestId = requestId,
            RequestPath = requestPath,
            ExceptionType = error?.GetType().FullName,
            ExceptionMessage = error?.Message,
            TechnicalDetails = showTechnicalDetails ? error?.ToString() : null,
            ShowTechnicalDetails = showTechnicalDetails,
            exception = showTechnicalDetails ? error?.Message : null
        };
    }
}
