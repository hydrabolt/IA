﻿using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IA.SDK
{
    public class Event
    {
        public string name = "name not set";
        public string[] aliases = new string[0];

        public string description = "description not set for this command!";
        public string[] usage = new string[] { "usage not set!" };
        public string errorMessage = "Something went wrong!";

        public bool canBeOverridenByDefaultPrefix = false;
        public bool canBeDisabled = true;
        public bool defaultEnabled = true;

        public Module module;

        public EventAccessibility accessibility = EventAccessibility.PUBLIC;

        public Event(Action<Event> info)
        {
            info.Invoke(this);
        }
    }
}
