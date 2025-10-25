# Email Queue Message Examples

This document provides examples of properly formatted JSON messages for the email queue.

## Basic Email Message

```json
{
  "email": "user@example.com",
  "subject": "Welcome to Laurel Library",
  "message": "Thank you for joining our library system. Your account has been created successfully.",
  "timestamp": "2025-10-20T15:30:00Z"
}
```

## Email with HTML Content

```json
{
  "email": "member@example.com",
  "subject": "Book Return Reminder",
  "message": "Dear Library Member,\n\nThis is a friendly reminder that the following book is due tomorrow:\n\n- \"The Great Gatsby\" by F. Scott Fitzgerald\n- Due Date: October 21, 2025\n\nPlease return the book to avoid late fees.\n\nBest regards,\nLaurel Library Staff",
  "timestamp": "2025-10-20T16:45:00Z"
}
```

## Notification Email

```json
{
  "email": "patron@example.com",
  "subject": "Hold Available for Pickup",
  "message": "Good news! The book you requested is now available for pickup:\n\n- \"Dune\" by Frank Herbert\n- Hold expires: October 27, 2025\n\nPlease visit the library during operating hours to collect your book.\n\nThank you,\nLaurel Library Team",
  "timestamp": "2025-10-20T14:20:00Z"
}
```

## Event Notification

```json
{
  "email": "community@example.com",
  "subject": "Upcoming Library Event: Author Reading",
  "message": "Join us for an exciting author reading event!\n\nEvent: Meet Local Author Jane Smith\nDate: October 25, 2025\nTime: 7:00 PM - 8:30 PM\nLocation: Laurel Library Main Hall\n\nRefreshments will be provided. No registration required.\n\nSee you there!\nLaurel Library Events Team",
  "timestamp": "2025-10-20T11:00:00Z"
}
```

## Testing with Azure Storage Explorer

You can manually add these messages to your Azure Storage Queue using Azure Storage Explorer:

1. Open Azure Storage Explorer
2. Connect to your storage account
3. Navigate to Queues → emails
4. Click "Add Message"
5. Paste one of the JSON examples above
6. Click "OK" to add the message

The Azure Function will automatically process the message and send the email via Mailgun.

## Testing with Code

Use the `QueueTestHelper` class included in the project:

```csharp
var connectionString = "your-storage-connection-string";
var testHelper = new QueueTestHelper(connectionString);

// Add a welcome email
await testHelper.AddEmailToQueueAsync(
    email: "newuser@example.com",
    subject: "Welcome to Laurel Library",
    message: "Welcome! Your library account has been created successfully."
);

// Add a reminder email
await testHelper.AddEmailToQueueAsync(
    email: "member@example.com",
    subject: "Book Due Tomorrow",
    message: "Reminder: 'The Great Gatsby' is due tomorrow. Please return it to avoid late fees."
);
```

## Message Validation

The function validates that all required fields are present:

- ✅ `email`: Must not be null or empty
- ✅ `subject`: Must not be null or empty  
- ✅ `message`: Must not be null or empty
- ⚠️ `timestamp`: Optional, used for logging and tracking

If any required field is missing, the function will log a warning and skip processing the message.