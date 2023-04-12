using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using TipBot.TransferHelper;
using TipBot.EmbedMessages;
using TipBot.Mongo;

namespace TipBot {
    [Group("admin")]
    public class AdminCommands : ModuleBase {
        private bool IsDev(ICommandContext context) => context.Message.Author.Id == 195567858133106697;

        [Command("help")]
        public async Task GetAdminHelpMessage() {
            if (IsDev(Context))
                await ReplyAsync(embed: HelpMessages.FillAdminHelpMessage().Build());
            else if (await GuildParametres.IsAllowed(Context.Guild.Id, Context.Message.Author.Id))
                await ReplyAsync(embed: HelpMessages.FillGuildAdminHelpMessage().Build());
        }

        [Command("ping")]
        public async Task Pong() {
            await ReplyAsync("Pong");
        }

        [Command("guildhelp")]
        public async Task GetAdminGuildHelpMessage() {
            if (IsDev(Context))
                await ReplyAsync(embed: HelpMessages.FillGuildAdminHelpMessage().Build());
        }

        [Command("stats")]
        [Alias("stat")]
        public async Task GetPlayerStats(string symbol, ulong user) {
            var stats = await TipBot.Games.TipPlayerStats.GetStats(user);
            var token = await ServiceData.GetTokenSymbol(symbol);
            if (stats == null || token == null)
                return;
            await ReplyAsync(embed: Embeds.GetPlayerStats(stats, token, Context.Message.Author.Username));
        }

        [Command("CheckApproval", RunMode = RunMode.Async)]
        public async Task CheckApproval(string address, string sym) {
            if (!IsDev(Context))
                return;
            var token = await ServiceData.GetTokenSymbol(sym);
            if (await SmartContract.CheckforApprovalEvent(address, token.ContractAddress))
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            else
                await Context.Message.AddReactionAsync(new Emoji("🚫"));
        }

        [Command("supplyBankRoll", RunMode = RunMode.Async)]
        public async Task supplyBankRoll(string value, string symbol) {
            if (!IsDev(Context))
                return;
            var (result, msg) = await TransferFunctions.TransferSplit(Context.Message.Author.Id, new ulong[1] { Context.Client.CurrentUser.Id }, symbol, value);
            if (!result)
                await ReplyAsync($"🚫 {msg}");
            else
                await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("withDrawBankRoll", RunMode = RunMode.Async)]
        public async Task withdrawBankRoll(string value, string symbol) {
            if (!IsDev(Context))
                return;
            var (result, msg) = await TransferFunctions.TransferSplit(Context.Client.CurrentUser.Id, new ulong[1] { Context.Message.Author.Id }, symbol, value);
            if (!result)
                await ReplyAsync($"🚫 {msg}");
            else
                await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("AddToken", RunMode = RunMode.Async)]
        public async Task AddToken(string add, string sym, int dec, string emote) {
            if (!IsDev(Context))
                return;
            var list = await ServiceData.GetList();
            if (list.tokenList.Exists(t => t.ContractAddress == add || t.Symbol == sym))
                return;
            list.tokenList.Add(new Token(add, sym, dec, emote));
            await list.SaveData();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("AddNFT", RunMode = RunMode.Async)]
        public async Task AddToken(string add, string sym, string emote) {
            if (!IsDev(Context))
                return;
            var list = await ServiceData.GetList();
            if (list.NFTList.Exists(t => t.ContractAddress == add || t.Symbol == sym))
                return;
            list.NFTList.Add(new NFT(add, sym, emote));
            await list.SaveData();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("testDeposit")]
        public async Task TestDeposi(string sym, string amount) {
            var list = await ServiceData.GetList();
            var nft = list.tokenList.Where(n => n.Symbol == sym.ToUpper()).FirstOrDefault();
            await TransferFunctions.DepositTokens(Context.Message.Author.Id, nft.ContractAddress, BigInteger.Parse(amount), "");
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("bal", RunMode = RunMode.Async)]
        public async Task GetBalancesOfUser(IUser u) {
            if (!IsDev(Context))
                return;
            var user = await TipUser.GetUser(u.Id);
            if (user == null) {
                user = new TipUser(Context.Message.Author.Id);
                await user.SaveToDatabase();
            }
            var embed = new EmbedBuilder();
            embed.WithTitle("Balances");
            embed.WithColor(Color.DarkBlue);
            foreach (var keyPair in user.walletList) {
                var val = await TipWallet.GetWalletValue(keyPair.Value);
                embed.AddField(keyPair.Key, val);
            }
            await ReplyAsync(embed: embed.Build());
        }

        [Command("renameToken", RunMode = RunMode.Async)]
        public async Task RenameToken(string symbol, string newSymbol, string emote) {
            if (!IsDev(Context))
                return;
            var token = await ServiceData.GetTokenSymbol(symbol);
            if (token == null) {
                await ReplyAsync(embed: Embeds.BasicEmbed("🚫 Admin Error", "Token not supported", Color.Red));
                return;
            }
            var serviceData = await ServiceData.GetList();
            var updatedToken = serviceData.tokenList.Where(t => t.Symbol == token.Symbol).FirstOrDefault();
            updatedToken.Emote = emote;
            updatedToken.Symbol = newSymbol.ToUpper();
            var wallets = await TipWallet.GetListOfWalletsOfOneToken(token);
            int i = 0;
            using (var session = DatabaseConnection.GetClient().StartSession()) {
                try {
                    //foreach (var wallet in wallets) {
                    //    Console.WriteLine($"Wallet #{i} being updated out of {wallets.Count}");
                    //    var user = await TipUser.GetUserFromWalletId(wallet.id.ToString(), token.Symbol);
                    //    wallet.Symbol = newSymbol.ToUpper();
                    //    await wallet.SaveNameToDatabase();
                    //    if (user != null) {
                    //        user.walletList.Add(newSymbol.ToUpper(), user.walletList[token.Symbol]);
                    //        user.walletList.Remove(token.Symbol);

                    //        await user.SaveToDatabase();
                    //    }
                    //    i++;
                    //}
                    //await session.CommitTransactionAsync();
                    await serviceData.SaveData();
                    await Context.Message.AddReactionAsync(new Emoji("✅"));
                }
                catch (Exception e) {
                    Logger.LogInternal("Delete token error : " + e.Message + $" at index {i}");
                }
            }
        }

        // for beta testing, will remove later on
        [Command("DeleteToken", RunMode = RunMode.Async)]
        public async Task DeletToken(string symbol) {
            if(!IsDev(Context))
                return;
            var token = await ServiceData.GetTokenSymbol(symbol);
            if (token == null) {
                await ReplyAsync(embed: Embeds.BasicEmbed("🚫 Admin Error", "Token not supported", Color.Red));
                return;
            }
            var serviceData = await ServiceData.GetList();
            serviceData.tokenList.RemoveAll(t => t.ContractAddress == token.ContractAddress);
            var wallets = await TipWallet.GetListOfWalletsOfOneToken(token);
            using (var session = DatabaseConnection.GetClient().StartSession()) {
                try {
                    session.StartTransaction();
                    foreach (var wallet in wallets) {
                        var user = await TipUser.GetUserFromWalletId(wallet.id.ToString(), token.Symbol);
                        await wallet.DeleteFromDatabase(session);
                        if (user != null) {
                            user.walletList.Remove(token.Symbol);
                            await user.SaveToDatabase(session);
                        }
                    }
                    await session.CommitTransactionAsync();
                    await serviceData.SaveData();
                    await Context.Message.AddReactionAsync(new Emoji("✅"));
                }
                catch (Exception e) {
                    Logger.LogInternal("Delete token error : " + e.Message);
                    await session.AbortTransactionAsync();
                }
            }
        }

        [Command("TopHolders", RunMode = RunMode.Async)]
        public async Task GetTopTokenHodlers(string sym) {
            if (!IsDev(Context))
                return;
            var token = await ServiceData.GetTokenSymbol(sym);
            if (token == null) {
                await ReplyAsync(embed: Embeds.BasicEmbed("🚫 Admin Error", "Token not supported", Color.Red));
                return;
            }
            var wallets = await TipWallet.GetListOfWalletsOfOneToken(token);
            wallets = wallets.OrderByDescending(w => BigInteger.Parse(w.Value)).ToList();
            await ReplyAsync(embed: await Embeds.GetTopHolders(wallets, token));
        }

        [Command("totalSupply", RunMode = RunMode.Async)]
        public async Task GetTotalSupply(string sym) {
            if (!IsDev(Context))
                return;
            var token = await ServiceData.GetTokenSymbol(sym);
            if (token == null) {
                await ReplyAsync(embed: Embeds.BasicEmbed("🚫 Admin Error", "Token not supported", Color.Red));
                return;
            }
            var wallets = await TipWallet.GetListOfWalletsOfOneToken(token);
            var total = BigInteger.Zero;
            foreach (var wallet in wallets)
                total += BigInteger.Parse(wallet.Value);
            await ReplyAsync(total.ToString());
        }


        [Command("AddEmoji")]
        public async Task AddEMoji(int id) {
            if (!IsDev(Context))
                return;
            byte[] data = null;
            using (System.Net.WebClient wc = new System.Net.WebClient()) {
                try {
                    data = await wc.DownloadDataTaskAsync("https://storage.googleapis.com/assets.axieinfinity.com/axies/" + id.ToString() + "/axie/axie-full-transparent.png");
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
            var stream = new System.IO.MemoryStream(data);
            Image axieImage = new Image(stream);
            await Context.Guild.CreateEmoteAsync($"axie{id}", axieImage);
        }

        [Command("GuildParam")]
        public async Task GetGuildParam() {
            if (IsDev(Context) || await GuildParametres.IsAllowed(Context.Guild.Id, Context.Message.Author.Id)) {
                var guild = await GuildParametres.GetGuild(Context.Guild.Id);
                await ReplyAsync(embed: AdminEmbeds.GuildParamEmbed(guild));
            }
        }

        [Command("AddGamingChannel", RunMode = RunMode.Async)]
        public async Task AddGamingChannel(params IChannel[] channels) {
            if (!await GuildParametres.IsAllowed(Context.Guild.Id, Context.Message.Author.Id) || IsDev(Context)) {
                var guildParam = await GuildParametres.GetGuild(Context.Guild.Id);
                if (guildParam == null)
                    await new GuildParametres(Context.Guild.Id, new List<ulong>(), channels.Select(u => u.Id).ToList()).SaveToDatabase();
                else
                    await guildParam.AddGameChannel(channels.Select(u => u.Id).ToList());
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
        }

        [Command("RemoveGamingChannel", RunMode = RunMode.Async)]
        public async Task RemoveGamingChannels(params IChannel[] channels) {
            if (await GuildParametres.IsAllowed(Context.Guild.Id, Context.Message.Author.Id) || IsDev(Context)) {
                var guildParam = await GuildParametres.GetGuild(Context.Guild.Id);
                if (guildParam == null) {
                    await ReplyAsync(embed: Embeds.BasicEmbed("🚫 Admin Error", "Nothing to remove", Color.Red));
                    return;
                }
                else
                    await guildParam.RemoveGameChannel(channels.Select(u => u.Id).ToList());
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
        }

        [Command("AddGuildAdmin", RunMode = RunMode.Async)]
        public async Task AddGuildAdmins(params IUser[] users) {
            if (await GuildParametres.IsAllowed(Context.Guild.Id, Context.Message.Author.Id) || IsDev(Context)) {
                var guildParam = await GuildParametres.GetGuild(Context.Guild.Id);
                if (guildParam == null)
                    await new GuildParametres(Context.Guild.Id, users.Select(u => u.Id).ToList(), new List<ulong>()).SaveToDatabase();
                else
                    await guildParam.AddAdminRole(users.Select(u => u.Id).ToList());
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
        }

        [Command("RemoveGuildAdmin", RunMode = RunMode.Async)]
        public async Task RemoveGuildAdmins(params IUser[] users) {
            if (await GuildParametres.IsAllowed(Context.Guild.Id, Context.Message.Author.Id) || IsDev(Context)) {
                var guildParam = await GuildParametres.GetGuild(Context.Guild.Id);
                if (guildParam == null) {
                    await ReplyAsync(embed: Embeds.BasicEmbed("🚫 Admin Error", "Nothing to remove", Color.Red));
                    return;
                }
                else
                    await guildParam.AddAdminRole(users.Select(u => u.Id).ToList());
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
        }

        [Command("SetFee", RunMode = RunMode.Async)]
        public async Task SetHouseFee(int fee) {
            if (!IsDev(Context))
                return;
            await AdminParametres.SetHouseFee(fee);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("SetGambling", RunMode = RunMode.Async)]
        public async Task SetGambling(bool value) {
            if (!IsDev(Context))
                return;
            await AdminParametres.SetGambling(value);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("SetMaxBet", RunMode = RunMode.Async)]
        public async Task SetMaxBet(int maxBet) {
            if (!IsDev(Context))
                return;
            await AdminParametres.SetMaxBet(maxBet);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("SetTokenFeeRed", RunMode = RunMode.Async)]
        public async Task SetTokenFeeRed(string symbol, int value) {
            if (!IsDev(Context))
                return;
            await AdminParametres.SetTokenFeeValue(value, symbol);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }
    }
}
