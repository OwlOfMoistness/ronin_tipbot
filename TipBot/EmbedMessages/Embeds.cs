using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Text;
using TipBot.TransferHelper;
using System.Threading.Tasks;
using TipBot.Games;
using MongoDB.Bson.Serialization.Serializers;

namespace TipBot.EmbedMessages {
    public class Embeds {
        public static Embed BasicEmbed(string title, string msg, Color color) {
            var embed = new EmbedBuilder();
            embed.WithColor(color);
            embed.WithTitle(title);
            embed.WithDescription(msg);
            return embed.Build();
        }

        public static Embed WithdrawalQueryEmbed(Token token, WithdrawalObject withdrawalObject) {
            var embed = new EmbedBuilder();
            embed.WithTitle($"{withdrawalObject.CurrentToken.Symbol} Withdrawal Approval Query").WithColor(Color.DarkBlue);
            embed.WithDescription("You need to approve this query for the TipBot to send your tokens. Bare in mind a **fee** will be deducted to cover gas fees.");
            embed.AddField("Transferred Value", $"{token.Emote} {TransferFunctions.FormatUint((withdrawalObject.NumericValue - withdrawalObject.Fee), token.Decimal)} {token.Symbol}");
            embed.AddField("Fee Taken to pay for gas", $"{token.Emote} {TransferFunctions.FormatUint(withdrawalObject.Fee, token.Decimal)} {token.Symbol}");
            embed.WithFooter("React with ✅ to approve or 🚫 to cancel. You have 30 seconds before this query expires.");
            return embed.Build();
        }

        public static Embed TransferEmbed(ulong sender, ulong[] users, Token token, string msg) {
            var embed = new EmbedBuilder();
            embed.WithTitle("Transfer");
            embed.WithColor(Color.Teal);
            var sb = new StringBuilder();
            foreach (var user in users)
                sb.Append($"<@{user}> ");
            sb.Remove(sb.Length - 1, 1);
            embed.WithDescription($"<@{sender}> ➡️ {sb}");
            embed.AddField($"Value", $"**{token.Emote} {msg} {token.Symbol}**");
            return embed.Build();
        }

        public static Embed TransferNFTEmbed(ulong sender, ulong to, NFT token, string msg) {
            var embed = new EmbedBuilder();
            if (token.Symbol == "AXIE")
                embed.WithThumbnailUrl("https://storage.googleapis.com/assets.axieinfinity.com/axies/"+ msg +"/axie/axie-full-transparent.png");
            embed.WithTitle("NFT Transfer");
            embed.WithColor(Color.Green);
            embed.WithDescription($"<@{sender}> ➡️ <@{to}>");
            embed.AddField($"NFT", $"**{token.Emote} {token.Symbol} #{msg}**");
            return embed.Build();
        }

        public static Embed AirDropEmbed(ulong user, ulong[] reactUsers, string value, string symbol) {
            var embed = new EmbedBuilder();
            embed.WithTitle("Air drop Time! 🎊");
            embed.WithColor(Color.Red);
            var sb = new StringBuilder();
            for (int i = 0; i < reactUsers.Count(); i++) {
                if (i > 39)
                    break;
                sb.Append($"<@!{reactUsers[i]}> ");
            }
            var remaining = reactUsers.Count() - 40;
            sb.Remove(sb.Length - 1, 1);
            var strToAttach = "";
            if (remaining > 0)
                strToAttach = " and " + remaining.ToString() + " more";
            embed.WithDescription($"<@!{user}> has sent **{value} {symbol}** to {sb}{strToAttach}!");
            return embed.Build();
        }

        public static Embed ConvertEmbed(ulong id, string value, string fromSymbol, string toSymbol, string msg, string emote) {
            var embed = new EmbedBuilder();
            embed.WithTitle("Conversion");
            embed.WithDescription($"<@!{id}> here is your conversion.");
            embed.WithColor(Color.Teal);
            embed.AddField($"{emote} {fromSymbol} swapped to {emote} {toSymbol}", $"**{value} ➡️ {msg}**");
            return embed.Build();
        }

        public static Embed TokenListEmbed(ServiceData list) {
            var embed = new EmbedBuilder();
            embed.WithTitle("📜 List of supported tokens 📜");
            embed.WithColor(Color.DarkOrange);
            foreach (var token in list.tokenList) {
                var url = "https://explorer.roninchain.com/token/ronin:" + token.ContractAddress.Substring(2);
                if (token.ContractAddress.Length > 10)
                    embed.AddField($"{token.Emote} **{token.Symbol}**", $"[Check it on Ronin Explorer]({url})");
                else if (token.Symbol == "RON")
                    embed.AddField($"{token.Emote} **{token.Symbol}**", $"Native Ronin token");
                else
                    embed.AddField($"{token.Emote} **{token.Symbol}**", $"Conversion token used in the TipBot for playing games");
            }
            return embed.Build();
        }

        public static Embed DepositEmbed(string baseUrl, string value, string symbol) {
            var embed = new EmbedBuilder();
            embed.WithColor(Color.Green);
            embed.WithTitle("Click here to deposit Tokens");
            embed.WithUrl(baseUrl);
            embed.WithDescription($"Depositing {value} {symbol.ToUpper()} to the TipBot smart contract.");
            return embed.Build();
        }

        public static async Task<Embed> BankrollEmbed(TipUser user) {
            var embed = new EmbedBuilder();
            embed.WithTitle("💰 Bankroll 💰");
            embed.WithDescription("Max bet gain is 5% of current bankroll.");
            embed.WithColor(Color.DarkBlue);
            foreach (var keyPair in user.walletList) {
                var emote = await ServiceData.GetTokenSymbol(keyPair.Key);
                var val = await TipWallet.GetWalletValue(keyPair.Value, true);
                var valFormat = await TipWallet.GetWalletValue(keyPair.Value);
                if (val != "0")
                    embed.AddField($"{emote.Emote} {keyPair.Key}",
                        $"**{valFormat}** - Max gain = {double.Parse(val) / (10000.0 / (await AdminParametres.GetParams()).MaxBetPercentage)}");
            }
            return embed.Build();
        }

        public static async Task<Embed> BalanceEmbed(TipUser user) {
            var list = await ServiceData.GetList();
            var embed = new EmbedBuilder();
            embed.WithTitle("💵 Balances 💵");
            embed.WithColor(Color.DarkBlue);
            foreach (var keyPair in user.walletList) {
                var emote = list.tokenList.FirstOrDefault(t => t.Symbol == keyPair.Key).Emote;
                var val = await TipWallet.GetWalletValue(keyPair.Value);
                if (val != "0")
                    embed.AddField($"{emote} {keyPair.Key}", val);
            }
            return embed.Build();
        }

        public static async Task<Embed> GetTopHolders(List<TipWallet> list, Token token) {
            var embed = new EmbedBuilder().WithColor(Color.Blue).WithTitle($"Top {token.Symbol} token holders");
            var sb = new StringBuilder();
            var counter = 10;
            var off = 0;
            if (list.Count < counter)
                counter = list.Count;
            try {
                for (int i = 0; i < counter; i++) {
                    var user = await TipUser.GetUserFromWalletId(list[i].id.ToString(), token.Symbol);
                    if (!(await Bot.GetUser(user.id)).IsBot)
                        sb.Append($"#{i + 1 + off}. <@!{user.id}> - {list[i].GetWallerValue()}\n");
                    else {
                        off--;
                        if (counter < list.Count)
                            counter++;
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
            embed.WithDescription(sb.ToString());
            return embed.Build();
        }

        public static Embed GetPlayerStats(TipPlayerStats stats, Token token, string username) {
            var embed = new EmbedBuilder();
            embed.WithTitle($"Game stats for {username}");
            embed.WithColor(Color.DarkOrange);
            if (stats.GambledAmounts.ContainsKey(token.Symbol)) {
                embed.AddField($"Total games played with {token.Emote} {token.Symbol}", stats.GamesPlayed[token.Symbol]);
                embed.AddField($"Total {token.Emote} {token.Symbol} gambled", TransferFunctions.FormatUint(stats.GambledAmounts[token.Symbol], token.Decimal));
                embed.AddField($"Total {token.Emote} {token.Symbol} earned", TransferFunctions.FormatUint(stats.WonAmounts[token.Symbol], token.Decimal));
                embed.AddField($"Highest {token.Emote} {token.Symbol} earned from game", TransferFunctions.FormatUint(stats.LargestGainAmounts[token.Symbol], token.Decimal));
            }
            return embed.Build();
        }

        public static EmbedBuilder GetAwaitingRollEmbed(string game, string bet, string potentialGain, IUser user, string sym) {
            var embed = new EmbedBuilder();
            embed.WithTitle($"{game} game for {user.Username}");
            embed.WithColor(new Color(95, 211, 232));
            if (game.ToLower() == "coinflip")
                embed.AddField("Bet", bet);
            else if (game.ToLower() == "etheroll")
                embed.AddField("Bet", $"Roll under {bet}");
            embed.AddField("Result", "?", true);
            embed.AddField("Potential gain", $"{potentialGain} {sym}", true);
            return embed;
        }
        public static EmbedBuilder GetFinalRollEmbed(string game, string bet, string potentialGain, IUser user, string sym, bool won, string result) {
            var embed = new EmbedBuilder();
            embed.WithTitle($"{game} game result for {user.Username}");
            if (won) {
                embed.WithDescription("You have won your bet");
                embed.WithColor(Color.Green);
            }
            else {
                embed.WithDescription("You have lost your bet");
                embed.WithColor(Color.Red);
            }
            if (game.ToLower() == "coinflip")
                embed.AddField("Bet", bet);
            else if (game.ToLower() == "etheroll")
                embed.AddField("Bet", $"Roll under {bet}");
            embed.AddField("Result", result, true);
            if (won)
                embed.AddField("Won amount", $"{potentialGain} {sym}", true);
            else
                embed.AddField("Lost amount", $"{potentialGain} {sym}", true);
            embed.WithFooter("React 🔄 to rebet");
            return embed;
        }

    }
}
