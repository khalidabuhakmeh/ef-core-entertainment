using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Entertainment;
using Entertainment.Models;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using Console = System.Console;

var database = new EntertainmentDbContext();

// migrate to the latest
await database.Database.MigrateAsync();

// query time
// 1. basic query for movies
var movies = database.Movies.OrderByDescending(x => x.WorldwideBoxOfficeGross);

// 1.1 Sum from same query
// Sum is used in table footnote
// Note, in production code, be cautious of multiple enumerations
var sum = await movies.SumAsync(x => x.WorldwideBoxOfficeGross);

#region Render Highest Grossing Movies

Output.Results(movies, "Highest Grossing Movies", 
    new Table()
    .RoundedBorder()
    .BorderColor(Color.Yellow)
    .AddColumns("Name", "Worldwide Box Office ($)")
    .AddRows(
        movies,
        m => $"[red]{m.Name}[/]",
        m => $"[green]{m.WorldwideBoxOfficeGross:C}[/]"
    )
    .Footnote($"[purple]Total: {sum:C}[/]"));

#endregion

// 2. Highest Rated productions
var highestRated = database
    .Productions
    .Select(x => new
    {
        id = x.Id,
        name = x.Name,
        avg = (double)x.Ratings.Average(x => x.Stars),
        type = x.GetType().Name
    })
    .OrderByDescending(x => x.avg);

#region Render productions by ratings

Output.Results(
    highestRated, 
    "Productions By Ratings", 
    new Table()
    .RoundedBorder()
    .BorderColor(Color.Yellow)
    .AddColumns("Name", "Type", "Rating", "Stars")
    .AddRows(
        highestRated,
        m => $"[red]{m.name}[/]",
        m => m.type,
        m => $"[green]{m.avg:0.00}[/]",
        m => $"{string.Join("", Enumerable.Repeat("⭐️", (int) Math.Floor(m.avg)))}"
    ));

#endregion

// 3. Actors Playing Multiple Characters
var multipleCharacters = database
    .Actors
    .Where(a => a.Characters.Count > 1)
    .Select(a => new {
        a.Name, 
        // some characters are both in TV and Movies
        Characters = a.Characters.Select(x => new {
            Name = x.Name,
            ProductionType = x.Production.GetType().Name
        })
        .OrderBy(x => x.Name)
        .ToList()
    });

#region Actors Playing Multiple Characters

Output.Results(
    multipleCharacters, 
    "Actors Playing Multiple Characters", 
    new Table()
        .RoundedBorder()
        .BorderColor(Color.Yellow)
        .AddColumns("Actor", "Characters")
        .AddRows(
            await multipleCharacters.ToListAsync(),
            m => $"[red]{m.Name}[/]",
            m => string.Join(", ", m.Characters.Select(c => $"[green]{c.Name}[/] [fuchsia]({c.ProductionType})[/]"))
        )
);

#endregion

// 4. Aggregate Ratings By Source
var sources = database
    .Ratings
    .GroupBy(x => x.Source)
    .Select(x => new { Source = x.Key, Count = x.Count() })
    .OrderByDescending(x => x.Count);
    
#region Ratings By Source

Output.Results(
    sources, 
    "Ratings By Source", 
    new Table()
        .RoundedBorder()
        .BorderColor(Color.Yellow)
        .AddColumns("Source", "Count")
        .AddRows(
            await sources.ToListAsync(),
            m => $"[red]{m.Source}[/]",
            m => m.Count 
        )
);

#endregion    
    