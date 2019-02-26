using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.InteropServices;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Example.Modules
{
    [Name("Fun")]
    public class FunModule : InteractiveBase
    {
        private Dictionary<string, int> diceDictionary = new Dictionary<string, int>();

        public FunModule()
        {
            diceDictionary.Add("d4", 4);
            diceDictionary.Add("d6", 6);
            diceDictionary.Add("d8", 8);
            diceDictionary.Add("d10", 10);
            diceDictionary.Add("d12", 20);
            diceDictionary.Add("d20", 20);
        }

        [Command("roll"), Alias("r")]
        [Summary("roll x number of dice of y number of sides")]
        [RequireUserPermission(ChannelPermission.SendMessages)]
        public async Task Roll(int numDice, string die)
        {
            await ReplyAsync($"Rolling d{diceDictionary[die]}, {numDice} times :game_die: ....");
            var rnd = new Random();

            var msg = $"{Context.User.Mention} Rolled: ";

            for (int i = 0; i < numDice; i++)
            {
                msg += $"{rnd.Next(1, diceDictionary[die] + 1)}, ";
            }

            msg = msg.Trim();
            msg = msg.Remove(msg.Length - 1, 1);
            msg += $" :game_die:";

            await ReplyAsync(msg);
        }

        [Command("coinFlip"), Alias("cf")]
        [Summary("Flips a coin")]
        [RequireUserPermission(ChannelPermission.SendMessages)]
        public async Task CoinFlip()
        {
            var msg = $"";

            var rnd = new Random();

            var num = rnd.Next(0, 2);

            Console.WriteLine(num);

            switch (num)
            {
                case 0:
                    msg += "Heads";
                    break;
                case 1:
                    msg += "Tailes";
                    break;
                default:
                    break;
            }

            await ReplyAsync(msg);
        }

        [Command("ping")]
        [Summary("pong")]
        [RequireUserPermission(GuildPermission.SendMessages)]
        public async Task Ping()
        {
            await ReplyAsync("pong");
        }

        [Command("BlackJack"), Alias("bj")]
        [Summary("Plays a round of black jack")]
        [RequireUserPermission(ChannelPermission.SendMessages),
         RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task Blackjack()
        {
            int playerPoints = 0;
            int dealerPoints = 0;
            var deck = LoadJson();
            var shuffledDeck = Shuffle(deck.ToList());
            string handMsg = "";
            IUserMessage msg = null;


            List<List<JObject>> hands = new List<List<JObject>>();
            List<JObject> dealersHand = new List<JObject>();
            List<JObject> playersHand = new List<JObject>();
            hands.Add(playersHand);
            hands.Add(dealersHand);

            //cards are dealt
            hands.ForEach(hand =>
            {
                for (int i = 0; i < 2; i++)
                {
                    hand.Add((JObject) shuffledDeck.Pop());
                }
            });

            handMsg = "";
            playersHand.ForEach(card => handMsg += $"{card["value"]}:{card["suit"]}: ");

            msg = await ReplyAsync(handMsg);
            //Player has opportunity to draw
            while (true)
            {
                handMsg = "";
                playerPoints = CalcPlayerPoints(playersHand);
                playersHand.ForEach(card => handMsg += $"{card["value"]}:{card["suit"]}: ");
                handMsg += $"\nFor at total of {playerPoints} points" +
                           $"\nTo draw another card type '+', to end type 'end'";

                await msg.ModifyAsync(x => x.Content = handMsg);

                var response = await NextMessageAsync(timeout: TimeSpan.FromSeconds(15));
                if (response != null)
                {
                    if (response.Content.Equals("+"))
                    {
                        playersHand.Add((JObject) shuffledDeck.Pop());
                        await response.DeleteAsync();
                    }
                    else if (response.Content.Equals("end"))
                    {
                        break;
                    }
                }
                else
                {
                    await ReplyAsync("You took to long, the game has ended");
                    break;
                }
            }

            //dealer count total value and draws
            dealerPoints = CalcDealerPoints(dealersHand);
            while (dealerPoints < 17)
            {
                dealersHand.Add((JObject) shuffledDeck.Pop());
                dealerPoints = CalcDealerPoints(dealersHand);
            }

            //calc final score for both players
            dealerPoints = CalcDealerPoints(dealersHand);
            playerPoints = CalcPlayerPoints(playersHand);

            /*
             player win conditions
             -player has 21 points
             -dealer has more then 21 points
             -player has more points then the dealer but not more then 21
             */

            /*
             dealer win conditions
             -dealer has 21 points
             -player has more then 21 points
             -dealer has more points then the dealer but not more then 21
             */

            if (playerPoints == 21)
            {
                //player win
                handMsg = $"The player won with {playerPoints} against the dealers {dealerPoints}";
            }
            else if (dealerPoints == 21)
            {
                //dealer win
                handMsg = $"The dealer won with {dealerPoints} against you {playerPoints}";
            }
            else if(playerPoints == 21 && dealerPoints == 21)
            {
                //dealer win
                handMsg = $"The dealer won with {dealerPoints} against you {playerPoints}";
            }
            else
            {
                if (dealerPoints > 21 && playerPoints < 22)
                {
                    //player win
                    handMsg = $"The player won with {playerPoints} against the dealers {dealerPoints}";
                }
                else if (playerPoints > 21 && dealerPoints < 22)
                {
                    //dealer win
                    handMsg = $"The dealer won with {dealerPoints} against you {playerPoints}";
                }
                else if (playerPoints > 21 && dealerPoints > 22)
                {
                    //dealer win
                    handMsg = $"The dealer won with {dealerPoints} against you {playerPoints}";
                }
                else if (playerPoints == dealerPoints)
                {
                    //dealer win
                    handMsg = $"The dealer won with {dealerPoints} against you {playerPoints}";
                }
                else
                {
                    if (playerPoints > dealerPoints && playerPoints < 22)
                    {
                        //player win
                        handMsg = $"The player won with {playerPoints} against the dealers {dealerPoints}";
                    }
                    else if (dealerPoints > playerPoints && dealerPoints < 22)
                    {
                        //dealer win
                        handMsg = $"The dealer won with {dealerPoints} against you {playerPoints}";
                    }
                }
            }
            
            await ReplyAsync(handMsg);
        }

        private static int CalcDealerPoints(List<JObject> dealersHand)
        {
            int dealerPoints = 0;
            dealersHand.ForEach(card =>
            {
                if (card["value"].ToString() == "A")
                {
                    if (dealerPoints < 11)
                    {
                        dealerPoints += 11;
                    }
                    else
                    {
                        dealerPoints += 1;
                    }
                }
                else
                {
                    dealerPoints += Int32.Parse(card["point"].ToString());
                }
            });
            return dealerPoints;
        }

        private int CalcPlayerPoints(List<JObject> playersHand)
        {
            int playerPoints = 0;
            playersHand.ForEach(card =>
            {
                if (card["value"].ToString() == "A")
                {
                    card["value"] = AceValue().Result.ToString();
                    playerPoints += Int32.Parse(card["value"].ToString());
                }
                else
                {
                    if (card["value"].ToString().Equals("11")
                        || card["value"].ToString().Equals("1"))
                    {
                        playerPoints += Int32.Parse(card["value"].ToString());
                    }
                    else
                    {
                        playerPoints += Int32.Parse(card["point"].ToString());
                    }
                }
            });
            return playerPoints;
        }

        private async Task<int> AceValue()
        {
            var msg = await ReplyAsync("You drew an Ace, what value is it?");
            var response = await NextMessageAsync(timeout: TimeSpan.FromSeconds(15));
            if (response != null)
            {
                string responseString = response.Content.ToString();

                await msg.DeleteAsync();
                await response.DeleteAsync();
                return AceValue(responseString);
            }

            await msg.DeleteAsync();
            return 1;
        }

        private int AceValue(string responseString)
        {
            if (responseString == "11")
            {
                return 11;
            }

            if (responseString == "1")
            {
                return 1;
            }

            return -1;
        }

        private static Random rng = new Random();

        public static Stack Shuffle<T>(IList<T> list)
        {
            Stack stack = new Stack();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            list.ToList().ForEach(val => stack.Push(val));
            return stack;
        }

        public JArray LoadJson()
        {
            using (StreamReader r = new StreamReader("Deck_of_cards.json"))
            {
                string json = r.ReadToEnd();
                JArray deck = JsonConvert.DeserializeObject<JArray>(json);
                return deck;
            }
        }
    }
}