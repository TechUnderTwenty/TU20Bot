using System;

using Microsoft.EntityFrameworkCore;

using TU20Bot.Models;

namespace TU20Bot.Database {
    public class BotDbContext : DbContext {
        public DbSet<UnverifiedUser> unverifiedUsers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            var server = Environment.GetEnvironmentVariable("ISCONTAINER") == "TRUE" ? "db" : "127.0.0.1";
            optionsBuilder.UseNpgsql($"Server={server};Port=5432;Database=discordbot;User Id=passportdev;Password=test;");
        }
    }
}
