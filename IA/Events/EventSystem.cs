﻿using Discord;
using IA.SQL;
using IA.SDK;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace IA.Events
{
    public class EventSystem
    {
        public List<ulong> developers = new List<ulong>();

        Dictionary<ulong, string> identifier = new Dictionary<ulong, string>();
        Dictionary<string, string> aliases = new Dictionary<string, string>();

        List<ulong> ignore = new List<ulong>();

        /// <summary>
        /// Variable to check if eventSystem has been defined already.
        /// </summary>
        static BotInformation bot;

        EventContainer events;
        static MySQL sql;

        public string DefaultIdentifier { private set; get; }
        public string OverrideIdentifier { private set; get; }

        /// <summary>
        /// Constructor for EventSystem.
        /// </summary>
        /// <param name="botInfo">Optional information for the event system about the bot.</param>
        public EventSystem(Action<BotInformation> botInfo)
        {
            if (bot != null)
            {
                Log.Warning("EventSystem already Defined, terminating...");
                return;
            }

            bot = new BotInformation(botInfo);
            events = new EventContainer();
            sql = new MySQL(bot.SqlInformation, bot.Identifier);

            MySQL.TryCreateTable("identifier(id BIGINT, i varchar(255))");

            OverrideIdentifier = bot.Name.ToLower() + ".";
            DefaultIdentifier = bot.Identifier;
        }    

        public async Task OnPrivateMessage(IMessage arg)
        {
            await Task.CompletedTask;
        }

        public void AddMentionEvent(Action<CommandEvent> info)
        {
            CommandEvent newEvent = new CommandEvent();
            info.Invoke(newEvent);
            newEvent.eventSystem = this;
            if (newEvent.aliases.Length > 0)
            {
                foreach (string s in newEvent.aliases)
                {
                    aliases.Add(s, newEvent.name.ToLower());
                }
            }
            events.MentionEvents.Add(newEvent.name.ToLower(), newEvent);

            MySQL.TryCreateTable("event(name VARCHAR(255), id BIGINT, enabled BOOLEAN)");
        }

        public void AddCommandEvent(Action<CommandEvent> info)
        {
            CommandEvent newEvent = new CommandEvent();
            info.Invoke(newEvent);
            newEvent.eventSystem = this;
            if(newEvent.usage[0] == "usage not set!")
            {
                newEvent.usage[0] = newEvent.name;
            }
            if (newEvent.aliases.Length > 0)
            {
                foreach (string s in newEvent.aliases)
                {
                    aliases.Add(s, newEvent.name.ToLower());
                }
            }
            events.CommandEvents.Add(newEvent.name.ToLower(), newEvent);

            MySQL.TryCreateTable("event(name VARCHAR(255), id BIGINT, enabled BOOLEAN)");
        }

        public void AddCommandDoneEvent(Action<CommandDoneEvent> info)
        {
            CommandDoneEvent newEvent = new CommandDoneEvent();
            info.Invoke(newEvent);
            newEvent.eventSystem = this;
            if (newEvent.aliases.Length > 0)
            {
                foreach (string s in newEvent.aliases)
                {
                    aliases.Add(s, newEvent.name.ToLower());
                }
            }
            events.CommandDoneEvents.Add(newEvent.name.ToLower(), newEvent);

            MySQL.TryCreateTable("event(name VARCHAR(255), id BIGINT, enabled BOOLEAN)");
        }

        public void AddJoinEvent(Action<GuildEvent> info)
        {
            GuildEvent newEvent = new GuildEvent();
            info.Invoke(newEvent);
            newEvent.eventSystem = this;
            if (newEvent.aliases.Length > 0)
            {
                foreach (string s in newEvent.aliases)
                {
                    aliases.Add(s, newEvent.name.ToLower());
                }
            }
            events.JoinServerEvents.Add(newEvent.name.ToLower(), newEvent);


            MySQL.TryCreateTable("event(name VARCHAR(255), id BIGINT, enabled BOOLEAN)");
        }

        public void AddLeaveEvent(Action<GuildEvent> info)
        {
            GuildEvent newEvent = new GuildEvent();
            info.Invoke(newEvent);
            newEvent.eventSystem = this;
            if (newEvent.aliases.Length > 0)
            {
                foreach (string s in newEvent.aliases)
                {
                    aliases.Add(s, newEvent.name.ToLower());
                }
            }
            events.LeaveServerEvents.Add(newEvent.name.ToLower(), newEvent);

            MySQL.TryCreateTable("event(name VARCHAR(255), id BIGINT, enabled BOOLEAN)");
        }

        public void AddContinuousEvent(Action<ContinuousEvent> info)
        {
            ContinuousEvent newEvent = new ContinuousEvent();
            info.Invoke(newEvent);
            newEvent.eventSystem = this;
            events.ContinuousEvents.Add(newEvent.name.ToLower(), newEvent);

            MySQL.TryCreateTable("event(name VARCHAR(255), id BIGINT, enabled BOOLEAN)");
        }

        /// <summary>
        /// Gets event and returns as base value.
        /// </summary>
        /// <param name="id">event id</param>
        /// <returns>Event from local database</returns>
        public Event GetEvent(string id)
        {
            return events.GetEvent(id);
        }

        /// <summary>
        /// Gets only command events as commandevent value
        /// </summary>
        /// <param name="id">event id</param>
        /// <returns>CommandEvent from local database</returns>
        public CommandEvent GetCommandEvent(string id)
        {
            if (events.CommandEvents.ContainsKey(id))
            {
                return events.CommandEvents[id];
            }
            return null;
        }

        public async Task SetIdentifierAsync(IGuild e, string prefix)
        {
            if (identifier.ContainsKey(e.Id))
            {
                identifier[e.Id] = prefix;
            }
            else
            {
                identifier.Add(e.Id, prefix);
            }

            await Task.Run(() => MySQL.Query("UPDATE identifier SET i=?i WHERE id=?id;", null, prefix, e.Id));
        }

        public async Task OnGuildLeave(IGuild e)
        {
            foreach (GuildEvent ev in events.LeaveServerEvents.Values)
            {
                if (await IsEnabled(ev, e.Id))
                {
                    await ev.Check(e);
                }
            }
        }

        public async Task OnGuildJoin(IGuild e)
        {
            foreach (GuildEvent ev in events.JoinServerEvents.Values)
            {
                if (await IsEnabled(ev, e.Id))
                {
                    await ev.Check(e);
                }
            }
        }

        public async Task<bool> SetEnabled(string eventName, ulong channelId, bool enabled)
        {
           
            Event setEvent = GetEvent(eventName);

            if(!setEvent.canBeDisabled && !enabled|| setEvent == null)
            {
                return false;
            }

            if (setEvent != null)
            {
                if (bot.SqlInformation != null)
                {
                    await MySQL.QueryAsync($"UPDATE event SET enabled=?enabled WHERE id=?id AND name=?name;", null, enabled, channelId, setEvent.name);
                }
                setEvent.enabled[channelId] = enabled;
                return true;
            }
            return false;
        }

        public async Task<string> ListCommands(IMessage e)
        {
            Dictionary<string, List<string>> moduleEvents = new Dictionary<string, List<string>>();
            moduleEvents.Add("Misc", new List<string>());
            EventAccessibility userEventAccessibility = GetUserAccessibility(e);

            foreach (Event ev in events.CommandEvents.Values)
            {
                if (await IsEnabled(ev, e.Channel.Id) && userEventAccessibility >= ev.accessibility)
                {
                    if (ev.module != null)
                    {
                        if (!moduleEvents.ContainsKey(ev.module.defaultInfo.name))
                        {
                            moduleEvents.Add(ev.module.defaultInfo.name, new List<string>());
                        }
                        if (GetUserAccessibility(e) >= ev.accessibility)
                        {
                            moduleEvents[ev.module.defaultInfo.name].Add(ev.name);
                        }
                    }
                    else
                    {
                        moduleEvents["Misc"].Add(ev.name);
                    }
                }
            }

            if (moduleEvents["Misc"].Count == 0)
            {
                moduleEvents.Remove("Misc");
            }

            moduleEvents.OrderBy(i => { return i.Key; });
            foreach (List<string> list in moduleEvents.Values)
            {
                list.OrderBy(x =>
                {
                    return x;
                });
            }

            string output = "";
            foreach (KeyValuePair<string, List<string>> items in moduleEvents)
            {
                output += "**" + items.Key + "**\n";
                for (int i = 0; i < items.Value.Count; i++)
                {
                    output += items.Value[i] + ", ";
                }
                output.Remove(output.Length - 2);
                output += "\n\n";
            }
            return output;
        }

        public async Task OnMessageRecieved(IMessage e, IGuild g)
        {
            if (e.Author.IsBot || ignore.Contains(g.Id)) return;

            if (!identifier.ContainsKey(g.Id)) LoadIdentifier(g.Id);

            string message = e.Content.ToLower();

            if (!message.StartsWith(identifier[g.Id])) return;

            if (await CheckIdentifier(message, identifier[g.Id], e))
            {
                return;
            }
            else if (await CheckIdentifier(message, OverrideIdentifier, e))
            {
                return;
            }
        }

        public async Task OnCommandDone(IMessage e, CommandEvent commandEvent)
        {
            foreach (CommandDoneEvent ev in events.CommandDoneEvents.Values)
            {
                await ev.processEvent(e, commandEvent);
            }
        }

        public async Task OnMention(IMessage e, IGuild g)
        {
            foreach (CommandEvent ev in events.MentionEvents.Values)
            {
                await ev.Check(e);
            }
        }

        public EventAccessibility GetUserAccessibility(IMessage e)
        {
            IGuildChannel channel = (e.Channel as IGuildChannel);
            if (channel == null) return EventAccessibility.PUBLIC;

            if (developers.Contains(e.Author.Id)) return EventAccessibility.DEVELOPERONLY;
            if ((e.Author as IGuildUser).GetPermissions(channel).Has(ChannelPermission.ManagePermissions)) return EventAccessibility.ADMINONLY;
            return EventAccessibility.PUBLIC;
        }

        public int CommandsUsed()
        {
            int output = 0;
            foreach (Event e in events.CommandEvents.Values)
            {
                output += e.CommandUsed;
            }
            return output;
        }

        public int CommandsUsed(string eventName)
        {
            return events.GetEvent(eventName).CommandUsed;
        }

        public void LoadIdentifier(ulong server)
        {
            if (bot.SqlInformation != null)
            {
                string instanceIdentifier = sql.GetIdentifier(server);
                if (instanceIdentifier == "ERROR")
                {
                    sql.SetIdentifier(bot.Identifier, server);
                    identifier.Add(server, bot.Identifier);
                }
                else
                {
                    identifier.Add(server, instanceIdentifier);
                }
            }
            else
            {
                identifier.Add(server, bot.Identifier);
            }
        }
        public string GetIdentifier(ulong server_id)
        {
            if (identifier.ContainsKey(server_id))
            {
                return identifier[server_id];
            }
            else
            {
                return sql.GetIdentifier(server_id);
            }
        }

        async Task<bool> CheckIdentifier(string message, string identifier, IMessage e, bool doRunCommand = true)
        {
            if (message.StartsWith(identifier))
            {
                string command = message.Substring(identifier.Length).Split(' ')[0];

                if (events.CommandEvents.ContainsKey(command))
                {
                    if (await IsEnabled(events.CommandEvents[command], e.Channel.Id))
                    {
                        if (doRunCommand)
                        {
                            if (GetUserAccessibility(e) >= events.CommandEvents[command].accessibility)
                            {
                                await Task.Run(() => events.CommandEvents[command].Check(e, identifier));
                                return true;
                            }
                        }
                    }
                }
                else if (aliases.ContainsKey(command))
                {
                    if (await IsEnabled(events.CommandEvents[aliases[command]], e.Channel.Id))
                    {
                        if (GetUserAccessibility(e) >= events.CommandEvents[aliases[command]].accessibility)
                        {
                            await Task.Run(() => events.CommandEvents[aliases[command]].Check(e, identifier));
                            return true;
                        }
                    }
                }
                return false;
            }
            return false;
        }

        async Task<bool> IsEnabled(Event e, ulong id)
        {
            if (bot.SqlInformation == null) return e.defaultEnabled;

            if (e.enabled.ContainsKey(id))
            {
                return events.CommandEvents[e.name].enabled[id];
            }

            int state = await Task.Run(() => sql.IsEventEnabled(e.name, id));
            if(state == -1)
            {
                await Task.Run(() => MySQL.Query("INSERT INTO event(name, id, enabled) VALUES(?name, ?id, ?enabled);", null, e.name, id, e.defaultEnabled));
                e.enabled.Add(id, e.defaultEnabled);
                return e.defaultEnabled;
            }
            e.enabled.Add(id, e.defaultEnabled);
            return (state == 1) ? true : false;
        }
    }
}
