using System;
using System.Threading.Tasks;
using System.Linq;
using System.Numerics;
using Nethereum.Util;
using TipBot.TransferHelper;
using Discord;
using MongoDB.Driver;
using TipBot.Mongo;
using TipBot.EmbedMessages;

namespace TipBot.Games {
    public enum GameEnum {
        Coinflip,
        Etheroll,
        Blackjack
    }
    public class CasinoGames {
        public static async Task HandleModuloGame(string amount, string symbol,
            string betCondition, int modulo, int betUnder, GameEnum game, IUser user,
            ulong seed, IMessageChannel channel, IUserMessage reactMsg = null) {
            var gain = "";

            var gamblingObject = await GamblingObject.GetGamblingApproval(user.Id, amount, symbol, modulo, betUnder);
            if (!gamblingObject.Approved) {
                await channel.SendMessageAsync("🚫 " + gamblingObject.ErrorMessage);
                return;
            }
            if (!(await CheckGameParams(game, betCondition, channel)))
                return;
            gamblingObject.PotentialGain -= await GetGameFee(gamblingObject, symbol);
            gain = TransferFunctions.FormatUint(gamblingObject.PotentialGain, gamblingObject.SenderWallet.Decimal);
            reactMsg = await PostAwaitingEmbed(game, betCondition, gain, user, gamblingObject, reactMsg, channel);
            GamblingCommands.UpdateGambleFocus(user.Id, reactMsg, game, betCondition, amount, symbol);
            var (result, outcome) = GetGameOutcome(game, betCondition, seed);
            gain = amount;
            UpdateWalletValues(result, ref gain, gamblingObject);
            await ExecuteDatabaseTransaction(gamblingObject, amount);
            await TipPlayerStats.LogBetToDatabase(user.Id, symbol, gamblingObject.NumericValue, result ? gamblingObject.PotentialGain : BigInteger.Zero);
            await Task.Delay(2500);
            await reactMsg.ModifyAsync(m => m.Embed = Embeds.GetFinalRollEmbed(game.ToString(),
                betCondition, gain, user, gamblingObject.SenderWallet.Symbol, result, outcome).Build());
            await reactMsg.AddReactionAsync(new Emoji("🔄"));
            GamblingCommands.focusBet[user.Id].InGame = false;
        }

        public static void UpdateWalletValues(bool result, ref string gain, GamblingObject gamblingObject) {
            if (result) {
                gain = TransferFunctions.FormatUint(gamblingObject.PotentialGain, gamblingObject.SenderWallet.Decimal);
                gamblingObject.SenderWallet.Add(gamblingObject.PotentialGain);
                gamblingObject.BankRoll.Sub(gamblingObject.PotentialGain);
            }
        }

        public static (bool, string) GetGameOutcome(GameEnum game, string betCondition, ulong seed) {
            bool result = false;
            string outcome = "";
            if (game == GameEnum.Etheroll)
                (result, outcome) = Etheroll(Convert.ToInt32(betCondition), seed);
            else if (game == GameEnum.Coinflip)
                (result, outcome) = Coinflip(betCondition.ToLower() == "heads" ? false : true, seed);
            return (result, outcome);
        }

        public static async Task<IUserMessage> PostAwaitingEmbed(GameEnum game, string betCondition, string gain, IUser user, GamblingObject gamblingObject, IUserMessage reactMsg, IMessageChannel channel) {
            var embed = Embeds.GetAwaitingRollEmbed(game.ToString(), betCondition, gain, user, gamblingObject.SenderWallet.Symbol);
            if (reactMsg == null)
                reactMsg = await channel.SendMessageAsync(embed: embed.Build());
            else
                await reactMsg.ModifyAsync(m => m.Embed = embed.Build());
            return reactMsg;
        }

        public static async Task<BigInteger> GetGameFee(GamblingObject gamblingObject, string symbol) {
            var houseFee = gamblingObject.PotentialGain *
                (await AdminParametres.GetParams()).HouseFee / 10000 *
                (await AdminParametres.GetTokenFee(symbol)) / 10000;
            if (houseFee == 0)
                houseFee = 1;
            return houseFee;
        }

        public static async Task ExecuteDatabaseTransaction(GamblingObject gamblingObject, string amount) {
            using (var session = DatabaseConnection.GetClient().StartSession()) {
                try {
                    session.StartTransaction();
                    await TransferToBankroll(gamblingObject.SenderWallet, gamblingObject.BankRoll, amount, session);
                    await gamblingObject.SenderWallet.SaveToDatabase(session);
                    await gamblingObject.BankRoll.SaveToDatabase(session);
                    await session.CommitTransactionAsync();
                }
                catch (Exception e) {
                    Logger.LogInternal(e.Message);
                    await session.AbortTransactionAsync();
                }
            }
        }

        public static async Task<bool> CheckGameParams(GameEnum game, string betCondition, IMessageChannel channel) {
            if (game == GameEnum.Etheroll)
                if (Convert.ToInt32(betCondition) < 2 || Convert.ToInt32(betCondition) > 99) {
                    await channel.SendMessageAsync("🚫 Roll_under value must be between [2;99]");
                    return false;
                }
            if (game == GameEnum.Coinflip)
                if (betCondition.ToLower() != "heads" && betCondition.ToLower() != "tails") {
                    await channel.SendMessageAsync("🚫 Possible outcomes are `heads` or `tails`");
                    return false;
                }
            return true;
        }

        public static async Task TransferToBankroll(TipWallet wallet, TipWallet bankRoll, string amount, IClientSessionHandle session) {
            wallet.Sub(BigInteger.Parse(wallet.ParseValue(amount)));
            bankRoll.Add(BigInteger.Parse(wallet.ParseValue(amount)));
            await wallet.SaveToDatabase(session);
            await bankRoll.SaveToDatabase(session);
        }


        public static string Keccak256(string value) {
            var timeStamp = Convert.ToInt32(((DateTimeOffset)(DateTime.UtcNow)).ToUnixTimeSeconds());
            var randomHex = GenerateRandomHex(12);
            var kec = Sha3Keccack.Current.CalculateHash(value + timeStamp.ToString() + randomHex);
            return kec;
        }

        public static BigInteger Keccak256BN(string value) {
            var timeStamp = Convert.ToInt32(((DateTimeOffset)(DateTime.UtcNow)).ToUnixTimeSeconds());
            var randomHex = GenerateRandomHex(12);
            var kec = Sha3Keccack.Current.CalculateHash(value + timeStamp.ToString() + randomHex);
            return BigInteger.Parse("0" + kec, System.Globalization.NumberStyles.AllowHexSpecifier);
        }

        public static string GenerateRandomHex(int length) {
            var random = new Random();
            byte[] buffer = new byte[length / 2];
            random.NextBytes(buffer);
            string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (length % 2 == 0)
                return result;
            return result + random.Next(16).ToString("X");
        }

        public static (bool, string) Etheroll(int betUnder, ulong msgId) {
            var randomHash = Keccak256BN(msgId.ToString());
            return ((randomHash % 100) < (betUnder - 1),
                (Convert.ToInt32((randomHash % 100).ToString()) + 1).ToString());
        }

        // false == heads and true == tails
        public static (bool, string) Coinflip(bool side, ulong msgId) {
            var randomHash = Keccak256BN(msgId.ToString());
            var res = (randomHash % 2) == 0;
            var outcome = res ? "tails" : "heads";
            return (res == side, outcome);
        }

        public static (bool, string) RollADice(int[] outcomes, ulong msgId) {
            var randomHash = Keccak256BN(msgId.ToString());
            return (outcomes.Contains((int)(randomHash % 6)),
                (Convert.ToInt32(randomHash % 6) + 1).ToString());
        }

    }
}
