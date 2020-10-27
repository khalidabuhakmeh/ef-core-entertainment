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

Output.Results(movies, $"Highest Grossing Movies", 
    new Table()
    .RoundedBorder()
    .BorderColor(Color.Yellow)
    .AddColumn("Name")
    .AddColumn("Worldwide Box Office ($)", c => c.Footer($"[purple]{sum:C}[/]"))
    .AddRows(
        movies,
        m => $"[red]{m.Name}[/]",
        m => $"[green]{m.WorldwideBoxOfficeGross:C}[/]"
    )
);

#endregion

// 2. Highest Rated productions
var highestRated = database
    .Productions
    .Select(x => new
    {
        id = x.Id,
        name = x.Name,
        avg = x.Ratings.Average(r => r.Stars),
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

var leastEpisodes = database
    .Series
    .OrderBy(x => x.NumberOfEpisodes)
    .Select(x => new { x.Name, x.NumberOfEpisodes, x.Release })
    .Take(1);
    //.FirstAsync();

#region Series with the fewest episodes

Output.Results(
    leastEpisodes, 
    "Series with the fewest episodes", 
    new Table()
        .RoundedBorder()
        .BorderColor(Color.Yellow)
        .AddColumns("Name", "# of Episodes", "Release")
        .AddRows(
            await leastEpisodes.ToListAsync(),
            m => $"[red]{m.Name}[/]",
            m => m.NumberOfEpisodes,
            m => $"[fuchsia]({m.Release:d})[/]"
        )
);

#endregion

var filteringTableByDiscriminator = 
    database.Productions.OfType<Movie>();

#region filteringTableByDiscriminator

Output.Results(
    filteringTableByDiscriminator, 
    "Filtered production by discriminator", 
    new Table()
        .RoundedBorder()
        .BorderColor(Color.Yellow)
        .AddColumns("Name", "Release")
        .AddRows(
            await filteringTableByDiscriminator.ToListAsync(),
            m => $"[red]{m.Name}[/]",
            m => $"[fuchsia]({m.Release:d})[/]"
        )
);

#endregion

var actorsPlayingThemselves =
    database.Characters
        .Where(c => c.Name == c.Actor.Name)
        .Select(c => new
        {
            character = c.Name,
            actor = c.Actor.Name
        });

#region Actors playing themselves

Output.Results(
    actorsPlayingThemselves, 
    "Characters In Multiple Productions", 
    new Table()
        .RoundedBorder()
        .BorderColor(Color.Yellow)
        .AddColumns("Character", "Actor")
        .AddRows(
            await actorsPlayingThemselves.ToListAsync(),
            m => $"[red]{m.character}[/]",
            m => $"[green]{m.actor}[/]"
        )
);

#endregion

var productionsWithTheInName = database
    .Productions
    .Where(x => x.Name.Contains("The"));

#region Productions With "The" In Title

Output.Results(
    productionsWithTheInName, 
    "Productions With \"The\" In Title", 
    new Table()
        .RoundedBorder()
        .BorderColor(Color.Yellow)
        .AddColumn("Name", c => c.Footer($"{productionsWithTheInName.Count()}"))
        .AddRows(
            await productionsWithTheInName.ToListAsync(),
            m => $"[red]{m.Name}[/]"
        )
);

#endregion

var summerMovies = database
    .Movies
    .Where(x => x.Release.Month >= 4 && x.Release.Month < 8);

#region Summer Movies

Output.Results(
    summerMovies, 
    "Summer Movies", 
    new Table()
        .RoundedBorder()
        .BorderColor(Color.Yellow)
        .AddColumn("Name")
        .AddColumn("Release", c => c.Footer($"{summerMovies.Count()}"))
        .AddRows(
            await summerMovies.ToListAsync(),
            m => $"[red]{m.Name}[/]",
            m => $"[fuchsia]({m.Release:d})[/]"
        )
);

#endregion 

var highestRating = database
    .Ratings
    .OrderByDescending(x => x.Production.Release)
    .ThenByDescending(x => x.Stars)
    .Select(r => new
    {
        stars = r.Stars, 
        source = r.Source,
        production = r.Production.Name
    })
    .Take(1);
    
#region Highest Rating

Output.Results(
    highestRating, 
    "Highest Rating", 
    new Table()
        .RoundedBorder()
        .BorderColor(Color.Yellow)
        .AddColumns("Source", "Stars", "Production")
        .AddRows(
            await highestRating.ToListAsync(),
            m => $"[red]{m.source}[/]",
            m => $"{string.Join("", Enumerable.Repeat("⭐️", m.stars))}",
            m => m.production
        )
);

#endregion 