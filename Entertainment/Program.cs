using System;
using System.Threading.Tasks;
using Entertainment.Models;
using Microsoft.EntityFrameworkCore;

namespace Entertainment
{
    class Program
    {
        static async Task Main(string [] args)
        {
            var database = new EntertainmentDbContext();
            
            // migrate to the latest
            await database.Database.MigrateAsync();


        }
    }
}