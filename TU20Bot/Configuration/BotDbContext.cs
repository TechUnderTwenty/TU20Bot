using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace TU20Bot.Configuration {
    public class BotDbContext : DbContext {

        public DbSet<UnverifiedUser> unverifiedUsers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            var server = Environment.GetEnvironmentVariable("ISCONTAINER") == "TRUE" ? "db" : "127.0.0.1";
            optionsBuilder.UseNpgsql($"Server={server};Port=5432;Database=discordbot;User Id=passportdev;Password=test;");
        }


    }
}
