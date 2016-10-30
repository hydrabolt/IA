﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IA.Events
{
    public class Module
    {
        public ModuleInformation defaultInfo;

        Dictionary<ulong, bool> enabled = new Dictionary<ulong, bool>();

        public Module(string name, bool enabled = true)
        {
            defaultInfo = new ModuleInformation();
            defaultInfo.name = name;
            defaultInfo.enabled = enabled;
        }       
        public Module(Action<ModuleInformation> info)
        {
            info.Invoke(defaultInfo);
        }
        public Module(SDK.Module addon)
        {
            defaultInfo = new ModuleInformation();
            defaultInfo.name = addon.defaultInfo.name;
            defaultInfo.enabled = addon.defaultInfo.enabled;
        }

        public string GetState()
        {
            return defaultInfo.name + ": " + "ACTIVE";
        }

        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        public Task Install()
        {
            return Task.CompletedTask;
        }

        public Task Uninstall()
        {
            return Task.CompletedTask;
        }
    }
}
