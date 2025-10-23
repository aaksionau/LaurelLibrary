using System.Collections.Generic;

namespace LaurelLibrary.Services.Abstractions.Dtos.Responses;

public class IsbnBulkSearchResult
{
    public List<IsbnBookDto>? Data { get; set; }
}
