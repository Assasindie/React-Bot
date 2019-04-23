using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace React_Bot
{
    class Program
    {
        private readonly DiscordSocketClient _client;
        private Random random = new Random();
        private static string token = "";

        //dictionary for specific user(s) emoji(s)
        Dictionary<ulong, List<string>> reactDictionary = new Dictionary<ulong, List<string>>();

        static void Main(string[] args)
        {
            string dir = Environment.CurrentDirectory + @"\.env";
            //check for env file if doesnt exist return without starting the bot
            if (File.Exists(dir))
            {
                try
                {
                    DotNetEnv.Env.Load(dir);
                    token = Environment.GetEnvironmentVariable("BOT_TOKEN");
                }catch (Exception e)
                {
                    Console.WriteLine("Exception occured ensure that your env file is set up correctly " + e);
                    Console.ReadKey();
                    return;
                }
                if(token == null)
                {
                    Console.WriteLine("Could not get token! Ensure that your env file is set up correctly");
                    Console.ReadKey();
                    return;
                }
            }
            else
            {
                Console.WriteLine("Could not find env file at : " + Environment.CurrentDirectory);
                Console.ReadKey();
                return;
            }
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public Program()
        {
            //set up stuff
            DiscordSocketConfig config = new DiscordSocketConfig
            {
                MessageCacheSize = 20
            };
            _client = new DiscordSocketClient(config);
            _client.SetGameAsync("Reacting to 🤡s");
            _client.SetStatusAsync(UserStatus.Invisible);
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;
            _client.ReactionAdded += ReactionAddedAsync;
        }

        public async Task MainAsync()
        {
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            // Block the program until it is closed.
            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            _client.SetStatusAsync(UserStatus.Offline);
            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            Console.WriteLine($"{_client.CurrentUser} is connected!");
            return Task.CompletedTask;
        }

        private Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            IUserMessage message = arg1.Value;
            try
            {
                //checks if the reaction is to the bots message
                if (_client.CurrentUser.Id == message.Author.Id && message.Content.Contains("React the emoji you want"))
                {
                    List<ulong> targets = new List<ulong>(arg1.Value.MentionedUserIds);
                    foreach (ulong target in targets)
                    {
                        List<string> emoteList = new List<string>
                        {
                            arg3.Emote.ToString()
                        };
                        if (!reactDictionary.ContainsKey(target))
                        {
                            Console.WriteLine("Added user id: " + target);
                            reactDictionary.Add(target, emoteList);
                        }
                        else 
                        {
                            reactDictionary[target].Add(arg3.Emote.ToString());
                        }
                    }
                }
            }
            //execptions got me like -> 😭
            catch (Exception e)
            {
                Console.WriteLine("exception occured : " + e);
            }
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            //returns if message is null and sets mesasge to be the message content
            if (!(message is IUserMessage usermsg)) return;

            //causes 😭
            if (message.Content == "!nerd")
            {
                await message.Channel.SendMessageAsync("🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓🤓");
            }

            //set customer emoji(s) for user(s)
            if (message.Content.Contains("!setcustomemoji"))
            {
                if (message.MentionedUsers.Count > 0)
                {
                    string str = "";
                    foreach (SocketUser user in message.MentionedUsers)
                    {
                        str += "<@!" + user.Id + "> ";
                    }
                    await message.Channel.SendMessageAsync("React the emoji you want " + str + "to be reacted with!");
                }
                else 
                {
                    await message.Channel.SendMessageAsync("Please include a user");
                }
            }

            //lists current user(s) on the reactDictionary
            if (message.Content == "!currentcustomemojis")
            {
                string currentNerds = "";
                if (reactDictionary.Count == 0)
                {
                    currentNerds = "There are no custom emojis at the moment!";
                }
                else
                {
                    foreach (KeyValuePair<ulong, List<string>> nerd in reactDictionary)
                    {
                        string emojis = "";
                        foreach (string value in nerd.Value)
                        {
                            emojis += ParseEmote(value) + " ";
                        }
                        currentNerds += ("User = <@!" + nerd.Key + "> Emoji = " + emojis + "\n");
                    }
                }
                await message.Channel.SendMessageAsync(currentNerds);
            }

            //removes user(s) from the custom dictionary
            if (message.Content.Contains("!removecustomemoji"))
            {
                string removedMembers = "";
                if (message.MentionedUsers.Count > 0)
                {
                    foreach (SocketUser user in message.MentionedUsers)
                    {
                        if (reactDictionary.ContainsKey(user.Id))
                        {
                            reactDictionary.Remove(user.Id);
                            removedMembers += "Removed " + user.Username + " ";
                        }
                    }
                }
                await message.Channel.SendMessageAsync(removedMembers);
            }

            //for the custom emojis and ensures the bot isnt reacting to itself
            if (reactDictionary.ContainsKey(message.Author.Id) && message.Author.Id != _client.CurrentUser.Id)
            {
                if (random.Next(20) == 1)
                {
                    reactDictionary.TryGetValue(message.Author.Id, out List<string> emojiName);
                    List<IEmote> reactEmojis = new List<IEmote>();
                    foreach (string emoji in emojiName)
                    {
                        reactEmojis.Add(ParseEmote(emoji));
                    }
                    await usermsg.AddReactionsAsync(reactEmojis.ToArray());
                }
            }
        }

        private IEmote ParseEmote(string input)
        {
            IEmote emote; 
            //Try to do custom emote ( will not work in the bot is not in the server that the emote is from :( )
            if (Emote.TryParse(input, out Emote newEmote))
            {
                emote = newEmote;
            }
            else
            {
                Regex rgx = new Regex(@"\p{Cs}");
                //if the emoji matches the regex proceed normally however if it isnt an Emote or Emoji (possibly an Emote from another server) returns with X
                emote = rgx.IsMatch(input) ? new Emoji(input) : new Emoji("❌");
            }
            return emote;
        }
    }
}