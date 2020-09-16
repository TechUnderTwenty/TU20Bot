using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TU20Bot.Configuration {
    class UnverifiedUser {
        
        public int Id { get; set; }

        public ulong UserId { get; set; }

        public string Email { get; set; }

    }
}
