namespace Ekomers.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public string? exception { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    }
}