using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;

namespace LaurelLibrary.Services.Services;

public class DashboardService : IDashboardService
{
    private readonly IBooksRepository _booksRepository;
    private readonly IReadersRepository _readersRepository;
    private readonly IReaderActionRepository _readerActionRepository;

    public DashboardService(
        IBooksRepository booksRepository,
        IReadersRepository readersRepository,
        IReaderActionRepository readerActionRepository
    )
    {
        _booksRepository = booksRepository;
        _readersRepository = readersRepository;
        _readerActionRepository = readerActionRepository;
    }

    public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(Guid libraryId)
    {
        var now = DateTimeOffset.Now;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var nextWeek = today.AddDays(7);

        // Get all borrowed book instances for the library
        var borrowedBookInstances = await _booksRepository.GetBorrowedBooksByLibraryAsync(
            libraryId
        );

        // Get basic counts
        var totalBooks = await _booksRepository.GetBookCountByLibraryIdAsync(libraryId);
        var totalReaders = await _readersRepository.GetReaderCountByLibraryIdAsync(libraryId);

        // Calculate statistics from borrowed instances
        var borrowedBooks = borrowedBookInstances.Count;

        // Get borrowed book instances with due dates
        var borrowedInstancesWithDates = borrowedBookInstances
            .Where(bi => bi.DueDate.HasValue)
            .ToList();

        // Calculate overdue and due soon
        var overdueInstances = borrowedInstancesWithDates
            .Where(bi => bi.DueDate!.Value.Date < today)
            .ToList();

        var dueTodayInstances = borrowedInstancesWithDates
            .Where(bi => bi.DueDate!.Value.Date == today)
            .ToList();

        var dueTomorrowInstances = borrowedInstancesWithDates
            .Where(bi => bi.DueDate!.Value.Date == tomorrow)
            .ToList();

        var dueThisWeekInstances = borrowedInstancesWithDates
            .Where(bi => bi.DueDate!.Value.Date > today && bi.DueDate!.Value.Date <= nextWeek)
            .ToList();

        // Get reader statistics
        var readersWithActiveCheckouts = borrowedBookInstances
            .Where(bi => bi.ReaderId.HasValue)
            .Select(bi => bi.ReaderId!.Value)
            .Distinct()
            .Count();

        // Get most recent actions for analytics
        var recentActions = await _readerActionRepository.GetRecentActionsAsync(libraryId, 1000);

        // Get most popular books (books with most checkouts in last 30 days)
        var thirtyDaysAgo = now.AddDays(-30);
        var popularBooks = recentActions
            .Where(ra => ra.ActionType == "CHECKOUT" && ra.ActionDate >= thirtyDaysAgo)
            .GroupBy(ra => new { ra.BookInstance.BookId, ra.BookTitle })
            .Select(g => new PopularBookDto
            {
                BookId = g.Key.BookId,
                Title = g.Key.BookTitle ?? "Unknown",
                Authors = g.FirstOrDefault()?.BookAuthors ?? "",
                CheckoutCount = g.Count(),
            })
            .OrderByDescending(p => p.CheckoutCount)
            .Take(5)
            .ToList();

        // Get most active readers (readers with most actions in last 30 days)
        var activeReaderData = recentActions
            .Where(ra => ra.ActionDate >= thirtyDaysAgo)
            .GroupBy(ra => ra.ReaderId)
            .Select(g => new { ReaderId = g.Key, ActionCount = g.Count() })
            .OrderByDescending(r => r.ActionCount)
            .Take(5)
            .ToList();

        var mostActiveReaders = new List<ReaderDto>();
        foreach (var readerData in activeReaderData)
        {
            var reader = await _readersRepository.GetByIdAsync(readerData.ReaderId, libraryId);
            if (reader != null)
            {
                mostActiveReaders.Add(
                    new ReaderDto
                    {
                        ReaderId = reader.ReaderId,
                        FirstName = reader.FirstName,
                        LastName = reader.LastName,
                        Email = reader.Email,
                        DateOfBirth = reader.DateOfBirth,
                        Address = reader.Address,
                        City = reader.City,
                        State = reader.State,
                        Zip = reader.Zip,
                        Ean = reader.Ean,
                        BarcodeImageUrl = reader.BarcodeImageUrl,
                    }
                );
            }
        }

        return new DashboardStatisticsDto
        {
            TotalBooks = totalBooks,
            TotalBookInstances = 0, // We don't have an easy way to get this without additional queries
            AvailableBooks = 0, // We don't have an easy way to get this without additional queries
            BorrowedBooks = borrowedBooks,
            OverdueBooks = overdueInstances.Count,
            ReservedBooks = 0, // We don't have an easy way to get this without additional queries
            LostOrDamagedBooks = 0, // We don't have an easy way to get this without additional queries
            TotalReaders = totalReaders,
            ActiveReaders = readersWithActiveCheckouts,
            BooksDueToday = dueTodayInstances.Count,
            BooksDueTomorrow = dueTomorrowInstances.Count,
            BooksDueThisWeek = dueThisWeekInstances.Count,
            OverdueBookInstances = overdueInstances,
            DueTodayBookInstances = dueTodayInstances,
            DueTomorrowBookInstances = dueTomorrowInstances,
            MostPopularBooks = popularBooks,
            MostActiveReaders = mostActiveReaders,
        };
    }
}
