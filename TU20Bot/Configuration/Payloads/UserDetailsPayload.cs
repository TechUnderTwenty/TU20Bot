using System.Collections.Generic;

namespace TU20Bot.Configuration.Payloads {
    public class UserDetailsPayload {
        public string firstName;
        public string lastName;
        public string email;

        public string fullName => firstName + " " + lastName;
        public string fullNameNoSpace => firstName + lastName;
    }

    public class UserMatchPayload {
        public List<UserDetailsPayload> details = new List<UserDetailsPayload>();
        public ulong role;
    }
}
