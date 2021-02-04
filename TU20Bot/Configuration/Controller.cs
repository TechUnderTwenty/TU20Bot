using System;

namespace TU20Bot.Configuration {
    // Name conflicts incoming :D
    [AttributeUsage(AttributeTargets.Class)]
    public class Controller : Attribute {
        public readonly bool authorization;
        
        public Controller(bool authorization = true) {
            this.authorization = authorization;
        }
    }
}