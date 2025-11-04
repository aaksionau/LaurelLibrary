using Microsoft.AspNetCore.Mvc;

namespace LaurelLibrary.UI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("autocomplete")]
    public IActionResult AutocompleteTest()
    {
        var html =
            @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Autocomplete Test</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            max-width: 800px;
            margin: 50px auto;
            padding: 20px;
        }
        .form-group {
            margin-bottom: 20px;
        }
        label {
            display: block;
            margin-bottom: 5px;
            font-weight: bold;
        }
        input[type=""text""] {
            width: 100%;
            padding: 10px;
            border: 1px solid #ddd;
            border-radius: 4px;
        }
        .autocomplete-results {
            border: 1px solid #ddd;
            border-top: none;
            max-height: 200px;
            overflow-y: auto;
            background: white;
            position: absolute;
            width: 100%;
            z-index: 1000;
        }
        .autocomplete-item {
            padding: 10px;
            cursor: pointer;
            border-bottom: 1px solid #eee;
        }
        .autocomplete-item:hover {
            background-color: #f0f0f0;
        }
        .container {
            position: relative;
        }
        .test-section {
            margin-bottom: 40px;
            padding: 20px;
            border: 1px solid #eee;
            border-radius: 8px;
        }
    </style>
</head>
<body>
    <h1>Author and Category Autocomplete Test</h1>
    <p>This is a test page for the autocomplete API endpoints. Make sure you are logged in to the application.</p>
    
    <div class=""test-section"">
        <h2>Authors Autocomplete</h2>
        <div class=""form-group"">
            <label for=""authorSearch"">Search Authors:</label>
            <div class=""container"">
                <input type=""text"" id=""authorSearch"" placeholder=""Type to search authors..."">
                <div id=""authorResults"" class=""autocomplete-results"" style=""display: none;""></div>
            </div>
        </div>
        <p><strong>Selected Author:</strong> <span id=""selectedAuthor"">None</span></p>
    </div>

    <div class=""test-section"">
        <h2>Categories Autocomplete</h2>
        <div class=""form-group"">
            <label for=""categorySearch"">Search Categories:</label>
            <div class=""container"">
                <input type=""text"" id=""categorySearch"" placeholder=""Type to search categories..."">
                <div id=""categoryResults"" class=""autocomplete-results"" style=""display: none;""></div>
            </div>
        </div>
        <p><strong>Selected Category:</strong> <span id=""selectedCategory"">None</span></p>
    </div>

    <script>
        // Generic autocomplete function
        function setupAutocomplete(inputId, resultsId, apiEndpoint, displayProperty, valueProperty, selectedElementId) {
            const input = document.getElementById(inputId);
            const results = document.getElementById(resultsId);
            const selectedElement = document.getElementById(selectedElementId);
            let debounceTimer;

            input.addEventListener('input', function() {
                const query = this.value.trim();
                
                clearTimeout(debounceTimer);
                
                if (query.length < 2) {
                    results.style.display = 'none';
                    return;
                }

                debounceTimer = setTimeout(() => {
                    search(query);
                }, 300);
            });

            input.addEventListener('blur', function() {
                // Delay hiding to allow clicking on results
                setTimeout(() => {
                    results.style.display = 'none';
                }, 200);
            });

            function search(query) {
                fetch(`${apiEndpoint}?q=${encodeURIComponent(query)}&limit=10`)
                    .then(response => {
                        if (!response.ok) {
                            throw new Error(`HTTP error! status: ${response.status}`);
                        }
                        return response.json();
                    })
                    .then(data => {
                        displayResults(data);
                    })
                    .catch(error => {
                        console.error('Error:', error);
                        results.innerHTML = `<div class=""autocomplete-item"">Error: ${error.message}</div>`;
                        results.style.display = 'block';
                    });
            }

            function displayResults(items) {
                if (items.length === 0) {
                    results.innerHTML = '<div class=""autocomplete-item"">No results found</div>';
                    results.style.display = 'block';
                    return;
                }

                const html = items.map(item => 
                    `<div class=""autocomplete-item"" data-value=""${item[valueProperty]}"" data-display=""${item[displayProperty]}"">
                        ${item[displayProperty]}
                    </div>`
                ).join('');

                results.innerHTML = html;
                results.style.display = 'block';

                // Add click handlers
                results.querySelectorAll('.autocomplete-item').forEach(item => {
                    item.addEventListener('click', function() {
                        const value = this.getAttribute('data-value');
                        const display = this.getAttribute('data-display');
                        
                        input.value = display;
                        selectedElement.textContent = `${display} (ID: ${value})`;
                        results.style.display = 'none';
                    });
                });
            }
        }

        // Setup autocomplete for authors
        setupAutocomplete(
            'authorSearch', 
            'authorResults', 
            '/api/authors/search', 
            'fullName', 
            'authorId', 
            'selectedAuthor'
        );

        // Setup autocomplete for categories
        setupAutocomplete(
            'categorySearch', 
            'categoryResults', 
            '/api/categories/search', 
            'name', 
            'categoryId', 
            'selectedCategory'
        );
    </script>
</body>
</html>";

        return Content(html, "text/html");
    }
}
