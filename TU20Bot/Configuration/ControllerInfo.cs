using System;

namespace TU20Bot.Configuration {
    [AttributeUsage(AttributeTargets.Class)]
    public class ControllerInfo : Attribute {
        public readonly bool authorization;
        
        public ControllerInfo(bool authorization = true) {
            this.authorization = authorization;
        }
    }
}