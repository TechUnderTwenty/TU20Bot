using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace TU20Bot.Configuration {
    class BotDbContext : DbContext {

        public DbSet<UnverifiedUser> unverifiedUsers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            optionsBuilder.UseNpgsql("Server=127.0.0.1;Port=5432;Database=discordbot;User Id=passportdev;Password=strawhatluffy;");
        }


    }
}
