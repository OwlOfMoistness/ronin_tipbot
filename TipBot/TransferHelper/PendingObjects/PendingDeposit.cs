using System;
using System.Numerics;
using System.Threading.Tasks;
using TipBot.Mongo;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using MongoDB.Driver;
using Discord;
using TipBot.EmbedMessages;
using TipBot.Log;
namespace TipBot.TransferHelper.PendingObjects {
    public class PendingDeposit {

        public string id;
        public ulong discordId;
        public string Value;
        public string Symbol;
        public ObjectId LockUpWalletId;

        public static bool Running = false;

        public PendingDeposit(ulong _id, string _symbol) {
            discordId = _id;
            Value = "0";
            Symbol = _symbol;
        }

        public async Task LogPendingDeposit() {
            var token = await ServiceData.GetTokenSymbol(Symbol);
            var lockUpWallet = new TipWallet(token.ContractAddress, token.Symbol, token.Decimal);
            LockUpWalletId = lockUpWallet.id;
            var depositAddress = await SmartContract.GetDepositAddress(discordId);
            Value = (await SmartContract.BalanceOf(token.ContractAddress, depositAddress)).ToString();
            var depositObject = await DepositObject.GetDepositApproval(discordId, Symbol, Value);
            if (!depositObject.Approved) {
                await (await Bot.GetUser(discordId)).SendMessageAsync(embed: Embeds.BasicEmbed("Deposit Error", depositObject.ErrorMessage, Color.Red));
                return;
            }
            var res = false;
            var msg = "";
            if (BigInteger.Parse(Value) == 0) {
                await (await Bot.GetUser(discordId)).SendMessageAsync(embed: Embeds.DepositEmbed(depositAddress, token));
                return;
            }
            else {
                var created = true;
                if (!(await SmartContract.IsDeployed(discordId)))
                    created = await SmartContract.GenerateDepositAddress(discordId);
                if (!created) {
                    await (await Bot.GetUser(discordId)).SendMessageAsync(embed: Embeds.BasicEmbed("Deposit Error", "Could not generate deposit address, please try again", Color.Red));
                    return;
                }
                (res, msg) = await SmartContract.DepositTokens(discordId, token.ContractAddress, BigInteger.Parse(Value));
            }
            if (!res) {
                await (await Bot.GetUser(discordId)).SendMessageAsync(embed: Embeds.BasicEmbed("Deposit Error", msg, Color.Red));
                return;
            }
            id = msg;
            lockUpWallet.Add(BigInteger.Parse(Value));
            await (await Bot.GetUser(discordId)).SendMessageAsync(embed: Embeds.DepositPendingEmbed(lockUpWallet.GetWallerValue(), token));
            await ExecuteLockUpTransaction(lockUpWallet);
        }

        public static async Task ExecuteDeposit() {
            var pD = await GetFirstInQueue();
            if (pD == null)
                return;
            var token = await ServiceData.GetTokenSymbol(pD.Symbol);
            var user = await TipUser.GetUser(pD.discordId);
            var lockedWallet = await TipWallet.GetWallet(pD.LockUpWalletId.ToString());
            var (res, msg) = await SmartContract.ValidateTransaction(pD.id);
            if (res) {
                await pD.ExecuteDepositTransaction(user, lockedWallet, token);
                await TransactionLog.LogTransation(0, new ulong[1] { pD.discordId }, token,
                pD.Value, pD.Value, TransactionType.Deposit, msg);
                await (await Bot.GetUser(pD.discordId)).SendMessageAsync(embed: Embeds.DepositConfirmationEmbed(lockedWallet.GetWallerValue(),token));
                await DeletePendingDeposit(pD.id);
            }
            else if (!res && msg.ToLower() == pD.id.ToLower()) {
                await pD.ExecuteFundReleaseTransaction(lockedWallet);
                await DeletePendingDeposit(pD.id);
            }
            else {
                var pendingCollec = DatabaseConnection.GetDb().GetCollection<PendingDeposit>("PendingDeposits");
                await DeletePendingDeposit(pD.id);
                await pendingCollec.InsertOneAsync(pD);
            }
        }

        private async Task ExecuteLockUpTransaction(TipWallet lockUpWallet) {
            var pendingCollec = DatabaseConnection.GetDb().GetCollection<PendingDeposit>("PendingDeposits");
            using (var session = DatabaseConnection.GetClient().StartSession()) {
                try {
                    session.StartTransaction();
                    await lockUpWallet.SaveToDatabase(session);
                    await pendingCollec.InsertOneAsync(session, this);
                    await session.CommitTransactionAsync();
                }
                catch (Exception e) {
                    Logger.LogInternal("Pending Withdrawal error : " + e.Message);
                    await session.AbortTransactionAsync();
                }
            }
        }

        private async Task ExecuteDepositTransaction(TipUser user, TipWallet lockedWallet, Token token) {
            TipWallet wallet;
            string walletId;
            if (!user.walletList.ContainsKey(token.Symbol)) {
                wallet = new TipWallet(token.ContractAddress, token.Symbol, token.Decimal);
                walletId = wallet.id.ToString();
                user.walletList.Add(token.Symbol, walletId);
            }
            else {
                walletId = user.walletList[token.Symbol];
                wallet = await TipWallet.GetWallet(walletId);
            }
            using (var session = DatabaseConnection.GetClient().StartSession()) {
                try {
                    session.StartTransaction();
                    await user.SaveToDatabase(session);
                    wallet.Add(BigInteger.Parse(lockedWallet.Value));
                    await wallet.SaveToDatabase(session);
                    await lockedWallet.DeleteFromDatabase(session);
                    await session.CommitTransactionAsync();
                }
                catch (Exception e) {
                    Logger.LogInternal("Execute deposit error : " + e.Message);
                    await session.AbortTransactionAsync();
                }
            }
        }

        private async Task ExecuteFundReleaseTransaction(TipWallet lockedWallet) {
            using (var session = DatabaseConnection.GetClient().StartSession()) {
                try {
                    session.StartTransaction();
                    await lockedWallet.DeleteFromDatabase(session);
                    await session.CommitTransactionAsync();
                }
                catch (Exception e) {
                    Logger.LogInternal("Execute deposit release error : " + e.Message);
                    await session.AbortTransactionAsync();
                }
            }
        }

        public static async Task<PendingDeposit> GetFirstInQueue() {
            var pendingCollec = DatabaseConnection.GetDb().GetCollection<PendingDeposit>("PendingDeposits");
            return (await pendingCollec.FindAsync(p => true)).FirstOrDefault();
        }

        public static async Task DeletePendingDeposit(string id) {
            var pendingCollec = DatabaseConnection.GetDb().GetCollection<PendingDeposit>("PendingDeposits");
            await pendingCollec.DeleteOneAsync(p => p.id == id);
        }

        public static async Task DepositLoop() {
            while (true) {
                try {
                    if (SmartContract.depositPossible)
                        await ExecuteDeposit();
                }
                catch (Exception e) {
                    Logger.LogInternal("Deposit loop error : " + e.Message);
                }
                await Task.Delay(3000);
            }
        }

        public static async Task RunDepositLoop() {
            if (!Running) {
                Running = true;
                _ = DepositLoop();
            }
        }
    }
}
