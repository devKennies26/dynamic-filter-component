# DynamicFilter.Component

DynamicFilter is a lightweight and extensible .NET library for dynamically filtering and sorting entities in an Entity Framework Core DbContext based on string-based filter expressions. It's ideal for building flexible APIs where filter logic needs to be dynamic and runtime-driven.

## Features

- Filter any EF Core entity by name and a simple filter string
- Supports nested property sorting (e.g., `sortBy=Author.Name`)
- Automatically resolves entities via `DbContext` metadata
- Easily extensible for additional filter logic
- Minimal setup via `IServiceCollection` extension

## Usage

Install via NuGet Package Manager:

```bash
dotnet add package DynamicFilter.Component
```


1. Register the Service

Add the following in your `Program.cs` or wherever you configure services:

```csharp
builder.Services.AddDynamicFilter();
```

 2. Inject and Use the Filter Service

```csharp
public class MyController : ControllerBase
{
    private readonly IDynamicFilterService<MyDbContext> _filterService;

    public MyController(IDynamicFilterService<MyDbContext> filterService)
    {
        _filterService = filterService;
    }

    [HttpGet("filter")]
    public async Task<IActionResult> Filter(string filter, string entityName)
    {
        var results = await _filterService.FilterEntitiesAsync(filter, entityName);
        return Ok(results);
    }
}
```


## Filter Syntax

The filter string is a comma-separated list of key-value pairs. Supported keys:

- `[propertyName]=[value]` — Basic filtering by property
- `sortby=[PropertyName]` — Sort by the specified property (supports nested properties)

### Example

```plaintext
Example 1: name=John,age=30,sortby=CreatedDate,sortdescending=true
Example 2: category=Electronics, isinstock=true, sortby=price for Product entity
Example 3: category=Electronics, isinstock=true, sortby=price, maxprice=550 for Product 
Example 4: sortby=price, maxprice=550, minprice=222 for Product
Example 5: sortby=price, maxprice=550, minprice=222, price=333 (hard equality is  ignoring) for Product
Example 6: sortby=price, maxprice=550, price=220 (hard equality is  ignoring) for Product
Example 7: Roles.Name=Admin, sortby=CreatedDate
```