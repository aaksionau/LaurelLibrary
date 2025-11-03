using LaurelLibrary.Domain.Entities;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class DashboardStatisticsDto
{
    public int TotalBooks { get; set; }
    public int TotalBookInstances { get; set; }
    public int AvailableBooks { get; set; }
    public int BorrowedBooks { get; set; }
    public int OverdueBooks { get; set; }
    public int ReservedBooks { get; set; }
    public int LostOrDamagedBooks { get; set; }
    public int TotalReaders { get; set; }
    public int ActiveReaders { get; set; }
    public int BooksDueToday { get; set; }
    public int BooksDueTomorrow { get; set; }
    public int BooksDueThisWeek { get; set; }

    public List<BookInstance> OverdueBookInstances { get; set; } = new();
    public List<BookInstance> DueTodayBookInstances { get; set; } = new();
    public List<BookInstance> DueTomorrowBookInstances { get; set; } = new();
    public List<PopularBookDto> MostPopularBooks { get; set; } = new();
    public List<ReaderDto> MostActiveReaders { get; set; } = new();
}

public class PopularBookDto
{
    public Guid BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Authors { get; set; } = string.Empty;
    public int CheckoutCount { get; set; }
    public string? Image { get; set; }
}
