namespace LaurelLibrary.EmailSenderServices.Dtos;

public class BookDueDateReminderEmailDto
{
    public string ReaderName { get; set; } = string.Empty;
    public string ReaderEmail { get; set; } = string.Empty;
    public string LibraryName { get; set; } = string.Empty;
    public string? LibraryAddress { get; set; }
    public string? LibraryDescription { get; set; }
    public ReminderType ReminderType { get; set; }
    public List<OverdueBookDto> Books { get; set; } = new List<OverdueBookDto>();
}

public enum ReminderType
{
    UpcomingDueDate, // 3 days before due date
    DueToday, // books due today
    Overdue, // books overdue (5+ days after due date)
}

public class OverdueBookDto
{
    public string Title { get; set; } = string.Empty;
    public string Authors { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public string? Publisher { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysOverdue { get; set; } // Negative for upcoming, 0 for due today, positive for overdue
}
