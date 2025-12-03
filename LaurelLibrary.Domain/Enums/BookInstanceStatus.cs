using System;

namespace LaurelLibrary.Domain.Enums;

public enum BookInstanceStatus
{
    Available = 1,
    Borrowed = 2,
    Reserved = 3,
    Lost = 4,
    Damaged = 5,
    PendingReturn = 6
}
