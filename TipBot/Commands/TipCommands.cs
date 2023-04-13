using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json.Linq;
using Discord.Commands;
using Discord.WebSocket;
using TipBot.TransferHelper;
using TipBot.EmbedMessages;
using TipBot.TransferHelper.PendingObjects;
namespace TipBot {
    public class TipCommands : ModuleBase {
        private bool IsDev(ICommandContext context) => context.Message.Author.Id == 195567858133106697;

        private bool IsMod(ICommandContext context) {
            if (IsDev(context))
                return true;
            if (context.Guild.Id != 704949262802485348)
                return false;
            var roles = context.Guild.Roles;
            foreach (var role in roles)
                if (role.Name.StartsWith("Mod") || role.Name.StartsWith("Core") || role.Name.StartsWith("Jihoz") || role.Name.StartsWith("Staff"))
                    return true;
            return false;
        }

        public static Dictionary<ulong, ReactionFocus> focusHelpDictionary;
        public static Dictionary<ulong, WithdrawalReactionFocus> focusWithdrawalDictionary;

        static TipCommands() {
            focusHelpDictionary = new Dictionary<ulong, ReactionFocus>();
            focusWithdrawalDictionary = new Dictionary<ulong, WithdrawalReactionFocus>();
        }

        //[Command("Faucet")]
        //public async Task ReceiveTokens(string symbol, string value, string add) {
        //    var token = await ServiceData.GetTokenSymbol(symbol);
        //    if (token == null && token.ContractAddress.Length > 10)
        //        await ReplyAsync("Token not supported");
        //    await SmartContract.TransferFrom(token.ContractAddress, BigInteger.Parse(value), add);
        //    await ReplyAsync("tokens sent");
        //}

        [Command("Tokens", RunMode = RunMode.Async)]
        [Alias("token")]
        public async Task CheckTokens() {
            if (Context.Guild != null && !await GuildParametres.CanPlay(Context.Guild.Id, Context.Channel.Id))
                return;
            var list = await ServiceData.GetList();
            await ReplyAsync(embed: Embeds.TokenListEmbed(list));
        }

        [Command("Balances", RunMode = RunMode.Async)]
        [Alias("bal")]
        public async Task GetBalances() {
            if (Context.Guild != null && !await GuildParametres.CanPlay(Context.Guild.Id, Context.Channel.Id))
                return;
            var user = await TipUser.GetUser(Context.Message.Author.Id);
            if (user == null) {
                user = new TipUser(Context.Message.Author.Id);
                await user.SaveToDatabase();
            }
            await ReplyAsync(embed: await Embeds.BalanceEmbed(user));
        }

        //[Command("tip", RunMode = RunMode.Async)]
        //[Alias("send", "transfer")]
        //public async Task SendNFT(string symbol, string tokenId, IUser user) {
        //    if (user.IsBot) {
        //        await ReplyAsync(embed: Embeds.BasicEmbed("🚫 NFT Transfer Error", "You can't tip a bot", Color.Red));
        //        return;
        //    }
        //    if (user.Id == Context.Message.Author.Id) {
        //        await ReplyAsync(embed: Embeds.BasicEmbed("🚫 NFT Transfer Error", "You can't tip yourself", Color.Red));
        //        return;
        //    }
        //    lock (PendingQueue.obj) {
        //        PendingQueue.TransactionQueue.Enqueue(new PendingTransaction(Log.TransactionType.NFTTransfer,
        //            new PendingNFT(Context.Message.Author.Id, tokenId, symbol, user.Id, Context.Channel)));
        //    }
        //}

        [Command("tip", RunMode = RunMode.Async)]
        [Alias("send", "transfer")]
        public async Task SendToken(string value, string symbol, params IUser[] users) {
            if (users.Count() > 10) {
                await ReplyAsync(embed: Embeds.BasicEmbed("🚫 Transfer Error", "You can't tip more than 10 users.", Color.Red));
                return;
            }
            foreach (var user in users) {
                if (user.IsBot) {
                    await ReplyAsync(embed: Embeds.BasicEmbed("🚫 Transfer Error", "You can't tip a bot", Color.Red));
                    return;
                }
                if (user.Id == Context.Message.Author.Id) {
                    await ReplyAsync(embed: Embeds.BasicEmbed("🚫 Transfer Error", "You can't tip yourself", Color.Red));
                    return;
                }
            }
            var userIDs = users.Select(u => u.Id).ToArray();
            if (userIDs.Length != userIDs.Distinct().Count()) {
                await ReplyAsync(embed: Embeds.BasicEmbed("🚫 Transfer Error", "You can't tip a user multiple times", Color.Red));
                return;
            }
            lock (PendingQueue.obj) {
                PendingQueue.TransactionQueue.Enqueue(new PendingTransaction(Log.TransactionType.Transfer,
                    new PendingTransfer(Context.Message.Author.Id, value, symbol, users.Select(u => u.Id).ToArray(), Context.Channel)));
            }
        }

        [Command("tip", RunMode = RunMode.Async)]
        [Alias("send", "transfer")]
        public async Task SendToken(string value, string symbol, SocketRole role) {
            if (role.IsEveryone)
                return;
            var users = role.Members;
            var userIDs = users.Select(u => u.Id).ToArray();
            var temp = userIDs.ToList();
            temp.Remove(Context.Message.Author.Id);
            userIDs = temp.ToArray();
            if (userIDs.Length != userIDs.Distinct().Count()) {
                await ReplyAsync(embed: Embeds.BasicEmbed("🚫 Transfer Error", "You can't tip a user multiple times", Color.Red));
                return;
            }
            lock (PendingQueue.obj) {
                PendingQueue.TransactionQueue.Enqueue(new PendingTransaction(Log.TransactionType.Transfer,
                    new PendingTransfer(Context.Message.Author.Id, value, symbol, userIDs, Context.Channel)));
            }
        }

        [Command("convert")]
        public async Task ConvertToken(string value, string symbol) {
            if (Context.Guild != null && !await GuildParametres.CanPlay(Context.Guild.Id, Context.Channel.Id))
                return;
            //TODO change nut to SLP
            if (symbol.ToUpper() == "SLP") {
                lock (PendingQueue.obj) {
                    PendingQueue.TransactionQueue.Enqueue(new PendingTransaction(Log.TransactionType.ConvertToMili,
                        new PendingConvert(Context.Message.Author.Id, value, "SLP", "mSLP", "<:slp:713374805730000926>", Context.Channel)));
                }
            }
            else if (symbol.ToUpper() == "MSLP") {
                lock (PendingQueue.obj) {
                    PendingQueue.TransactionQueue.Enqueue(new PendingTransaction(Log.TransactionType.ConvertFromMili,
                        new PendingConvert(Context.Message.Author.Id, value, "mSLP", "SLP", "<:slp:713374805730000926>", Context.Channel)));
                }
            }
            else {
                await ReplyAsync($"🚫 " + "You cannot convert this token");
                return;
            }
        }

        [Command("rain", RunMode = RunMode.Async)]
        [Alias("drop", "airdrop")]
        public async Task Airdrop(string value, string symbol, int minutes) {
            // TODO simplify
            var transferObject = await TransferObject.GetTransferApproval(Context.Message.Author.Id, symbol, value, 1);
            if (!transferObject.Approved) {
                await ReplyAsync($"🚫 " + transferObject.ErrorMessage);
                return;
            }
            var embed = new EmbedBuilder();
            embed.WithTitle("Air drop Time! 🎊");
            embed.WithColor(Color.Teal);
            embed.WithDescription($"React to the 🎊 emoji to have a share of that juicy {value} {transferObject.CurrentToken.Symbol} airdrop");
            embed.WithFooter($"You have {minutes} minutes to react before the airdrop!");
            var msg = await ReplyAsync(embed: embed.Build());
            await msg.AddReactionAsync(new Emoji("🎊"));
            while (minutes > 0) {
                await Task.Delay(60000);
                minutes--;
            }
            var reactUsers = (await msg.GetReactionUsersAsync(new Emoji("🎊"), 500).FlattenAsync()).ToList();
            reactUsers.RemoveAll(u => u.Id == Context.Message.Author.Id || u.Id == Context.Client.CurrentUser.Id);
            if (reactUsers.Count() <= 0) {
                await msg.DeleteAsync();
                return;
            }
            var IDs = reactUsers.Select(u => u.Id).ToArray();
            lock (PendingQueue.obj) {
                PendingQueue.TransactionQueue.Enqueue(new PendingTransaction(Log.TransactionType.Airdrop,
                    new PendingAirdrop(Context.Message.Author.Id, value, symbol, IDs, msg, Context.Channel)));
            }
        }

        [Command("withdraw", RunMode = RunMode.Async)]
        public async Task WithdrawTokens(string value, string symbol) {
            WithdrawalObject withdrawalObject;
            Token token = null;
            var strFee = "0";
            // TODO change to SLP on mainnet
            if (symbol.ToUpper() == "SLP" || symbol.ToUpper() == "HAUT") {
                token = await ServiceData.GetTokenSymbol(symbol);
                var fee = 0;
                strFee = fee.ToString();
            }
            else if (symbol.ToUpper() == "AXS" || symbol.ToUpper() == "RON") {
                token = await ServiceData.GetTokenSymbol(symbol);
                //if (TipWallet.IsValidValue(value, token.Decimal)) {
                //    //var val = TipWallet.ParseValueToTokenDecimal(value, token.Decimal);
                //    //var gas = await SmartContract.EstimateGasWithdrawTokens(BigInteger.Parse(val), 1, token.ContractAddress, "0xEeeeeEeeeEeEeeEeEeEeeEEEeeeeEeeeeeeeEEeE", 0);
                //    //gas = SmartContract.GetTotalGas(await SmartContract.GetGas("fastest"), gas);
                //    //strFee = await GetTokenFee(token.ContractAddress, token.Decimal, gas);
                //}
                //else {
                //    await Context.Message.Author.SendMessageAsync(embed: Embeds.BasicEmbed("🚫 Withdrawal Error", "Wrong value format", Color.Red));
                //    return;
                //}
            }
            withdrawalObject = await WithdrawalObject.GetWithdrawalApproval(Context.Message.Author.Id, symbol, value, strFee);
            if (!withdrawalObject.Approved) {
                await Context.Message.Author.SendMessageAsync(embed: Embeds.BasicEmbed("🚫 Withdrawal Error", withdrawalObject.ErrorMessage, Color.Red));
                return;
            }
            var msg = await Context.Message.Author.SendMessageAsync(embed: Embeds.WithdrawalQueryEmbed(token, withdrawalObject));
            await msg.AddReactionsAsync(new IEmote[2] { new Emoji("✅"), new Emoji("🚫") });
            if (focusWithdrawalDictionary.ContainsKey(Context.Message.Author.Id))
                focusWithdrawalDictionary[Context.Message.Author.Id].UpdateWithdrawalFocus(value, symbol, strFee, msg);
            else
                focusWithdrawalDictionary.Add(Context.Message.Author.Id, new WithdrawalReactionFocus(value, symbol, strFee, msg));
        }

        [Command("deposit", RunMode = RunMode.Async)]
        public async Task DepositTokens(string value, string symbol) {
            if (!IsDev(Context))
                return;
            var baseUrl = "https://cesarsld.github.io/TipBotDepositWebsite/";
            var urlParams = "";
            var token = await ServiceData.GetTokenSymbol(symbol);
            if (token == null) {
                await ReplyAsync(embed: Embeds.BasicEmbed("🚫 Deposit Error", "Token not supported", Color.Red));
                return;
            }
            var testWallet = new TipWallet(token.ContractAddress, token.Symbol, token.Decimal);
            if (!testWallet.IsValidValue(value)) {
                await ReplyAsync(embed: Embeds.BasicEmbed("🚫 Deposit Error", "Wrong value format", Color.Red));
                return;
            }
            urlParams = $"?token={token.ContractAddress}&amount={testWallet.ParseValue(value)}&decimal={token.Decimal}&discordId={Context.Message.Author.Id}&symbol={token.Symbol}";
            baseUrl += urlParams;

            await Context.Message.Author.SendMessageAsync("Contact Owl");
            //await Context.Message.Author.SendMessageAsync(embed: Embeds.DepositEmbed(baseUrl, value, symbol));
        }

        [Command("SetWithdrawal", RunMode = RunMode.Async)]
        public async Task SetRoninWithdrawalAddress(string address) {
            if (!TipUser.IsValidAddress(address)) {
                await ReplyAsync("🚫 Address is invalid. Please use format \"ronin:1b2c5f7a...\"");
                return;
            }
            var user = await TipUser.GetUser(Context.Message.Author.Id);
            if (user == null)
                user = new TipUser(Context.Message.Author.Id);
            user.WithdrawalAddress = address;
            await user.SaveWithdrawalAddress();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("help")]
        public async Task Help() {
            if (Context.Guild != null && !await GuildParametres.CanPlay(Context.Guild.Id, Context.Channel.Id))
                return;
            var msg = await ReplyAsync(embed: HelpMessages.FillIntroMessage().Build());
            if (focusHelpDictionary.ContainsKey(Context.Message.Author.Id))
                focusHelpDictionary[Context.Message.Author.Id].UpdateFocus(msg);
            else
                focusHelpDictionary.Add(Context.Message.Author.Id, new ReactionFocus(msg));
            await msg.AddReactionsAsync(new IEmote[2] { new Emoji("👋"), new Emoji("💰")});
        }

        [Command("bankroll", RunMode = RunMode.Async)]
        [Alias("bank")]
        public async Task ShowBankRoll() {
            if (Context.Guild != null && !await GuildParametres.CanPlay(Context.Guild.Id, Context.Channel.Id))
                return;
            var user = await TipUser.GetUser(712976781438615572);
            if (user == null) {
                user = new TipUser(712976781438615572);
                await user.SaveToDatabase();
            }
            await ReplyAsync(embed: await Embeds.BankrollEmbed(user));
        }

        [Command("ping")]
        public async Task Ping() {
            await ReplyAsync("pong");
        }

        public static async Task<double> GetEthPrice() {
            var data = "";
            using (System.Net.WebClient wc = new System.Net.WebClient()) {
                data = await wc.DownloadStringTaskAsync("https://api.coingecko.com/api/v3/simple/price?ids=ethereum&vs_currencies=USD");
            }
            var json = JObject.Parse(data);
            double ethPrice = (double)json["ethereum"]["usd"];
            return ethPrice;
        }

        //public static async Task<string> GetTokenFee(string address, int dec, BigInteger gas) {
        //    var tokenSellPrice = await SmartContract.GetV2TokenPriceToEth(address, BigInteger.Parse("1".PadRight(dec + 1, '0')));
        //    var tokenFee = gas * BigInteger.Parse("1000000000000000000") / tokenSellPrice;
        //    var str = TransferFunctions.FormatUint(tokenFee, 18, true);
        //    return str;
        //}

        //public static async Task<string> GetLunaFee() {
        //    var tokenSellPrice = await SmartContract.GetV2TokenPriceToEth("0xf5d669627376ebd411e34b98f19c868c8aba5ada", BigInteger.Parse("1000000000000000000"));
        //    var lunaPrice = 0.1;
        //    var ethPrice = await GetEthPrice();
        //    var lunaEth = lunaPrice / ethPrice;
        //    var total = SmartContract.GetTotalGas(await SmartContract.GetGas("fastest"), 75000);
        //    var strVal = lunaEth.ToString().Substring(0, 20);
        //    var intValue = BigInteger.Parse(new TipWallet("", "", 18).ParseValue(strVal));
        //    var doubleFee = (double.Parse(total.ToString()) / double.Parse(intValue.ToString())).ToString();
        //    int periodIndex = doubleFee.IndexOf('.');
        //    if (doubleFee.Length - 1 - periodIndex > 18)
        //        doubleFee = doubleFee.Substring(0, periodIndex + 18);
        //    return doubleFee;
        //}

    }
}