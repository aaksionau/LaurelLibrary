namespace LaurelLibrary.UI.ViewModels
{
    public class PaginationViewModel
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public string Area { get; set; } = "Administration";
        public string Page { get; set; } = "/Books/List";
        public string? Tab { get; set; }
    }
}
