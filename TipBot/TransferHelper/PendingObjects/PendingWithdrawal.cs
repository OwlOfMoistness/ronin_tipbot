using System;
using System.Numerics;
using System.Threading.Tasks;
using TipBot.Mongo;
using MongoDB.Driver;
using MongoDB.Bson;
using TipBot.EmbedMessages;
using MongoDB.Driver.Linq;
using Discord;
using TipBot.Log;
using MongoDB.Bson.Serialization.Conventions;

namespace TipBot.TransferHelper.PendingObjects {
    public enum WithdrawalStatus {
        Pending,
        Broadcasting
    }
    public class PendingWithdrawal {
        public ulong id;
        public string Value;
        public string Symbol;
        public string Fee;
        public ObjectId LockUpWalletId;

        public static bool Running = false;

        public PendingWithdrawal(ulong _id, string v, string s, string f) {
            id = _id;
            Value = v;
            Symbol = s;
            Fee = f;
        }

        public void Update(string v, string f) {
            Value = v;
            Fee = f;
        }

        public async Task LogPendingWithdrawal() {
            var token = await ServiceData.GetTokenSymbol(Symbol);
            var lockUpWallet = new TipWallet(token.ContractAddress, token.Symbol, token.Decimal);
            LockUpWalletId = lockUpWallet.id;
            var withdrawalObject = await WithdrawalObject.GetWithdrawalApproval(id, Symbol, Value, Fee);
            if (!withdrawalObject.Approved) {
                await (await Bot.GetUser(id)).SendMessageAsync(embed: Embeds.BasicEmbed("Withdrawal Error", withdrawalObject.ErrorMessage, Color.Red));
                return;
            }
            Update(withdrawalObject.NumericValue.ToString(), withdrawalObject.Fee.ToString());
            var userWallet = withdrawalObject.SenderWallet;
            var _value = withdrawalObject.NumericValue;
            userWallet.Sub(_value);
            lockUpWallet.Add(_value);
            await ExecuteLockUpTransaction(userWallet, lockUpWallet);
        }

        public static async Task ExecuteWithdrawal() {
            var pW = await GetFirstInQueue();
            if (pW == null)
                return;
            var token = await ServiceData.GetTokenSymbol(pW.Symbol);
            var user = await TipUser.GetUser(pW.id);
            var lockedWallet = await TipWallet.GetWallet(pW.LockUpWalletId.ToString());
            var (res, msg) = await SmartContract.WithdrawTokens(BigInteger.Parse(pW.Value), BigInteger.Parse(pW.Fee),
                token.ContractAddress, user.WithdrawalAddress, pW.id);
            if (res) {
                await lockedWallet.DeleteFromDatabase();
                await TransactionLog.LogTransation( 0, new ulong[1] { pW.id }, token,
                                        pW.Value, pW.Value, TransactionType.Withdrawal, msg);
            }
            else {
                await pW.ExecuteFundReleaseTransaction(user, lockedWallet, token);
                await (await Bot.GetUser(pW.id)).SendMessageAsync(embed: Embeds.BasicEmbed("Withdrawal Error, execution rollbacked", msg, Color.Red));
            }
            await DeletePendingWithdrawal(pW.id);
        }

        private async Task ExecuteLockUpTransaction(TipWallet userWallet, TipWallet lockUpWallet) {
            var pendingCollec = DatabaseConnection.GetDb().GetCollection<PendingWithdrawal>("PendingWithdrawals");
            using (var session = DatabaseConnection.GetClient().StartSession()) {
                try {
                    session.StartTransaction();
                    await userWallet.SaveToDatabase(session);
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

        private async Task ExecuteFundReleaseTransaction(TipUser user, TipWallet lockedWallet, Token token) {
            var wallet = await TipWallet.GetWallet(user.walletList[token.Symbol]);
            wallet.Add(BigInteger.Parse(Value));
            using (var session = DatabaseConnection.GetClient().StartSession()) {
                try {
                    session.StartTransaction();
                    await wallet.SaveToDatabase(session);
                    await lockedWallet.DeleteFromDatabase(session);
                    await session.CommitTransactionAsync();
                }
                catch (Exception e) {
                    Logger.LogInternal("Execute withdrawal error : " + e.Message);
                    await session.AbortTransactionAsync();
                }
            }
        }

        public static async Task<PendingWithdrawal> GetPendingWithdrawal(ulong id) {
            var pendingCollec = DatabaseConnection.GetDb().GetCollection<PendingWithdrawal>("PendingWithdrawals");
            return (await pendingCollec.FindAsync(p => p.id == id)).FirstOrDefault();
        }

        public static async Task<PendingWithdrawal> GetFirstInQueue() {
            var pendingCollec = DatabaseConnection.GetDb().GetCollection<PendingWithdrawal>("PendingWithdrawals");
            return (await pendingCollec.FindAsync(p => true)).FirstOrDefault();
        }

        public static async Task DeletePendingWithdrawal(ulong id) {
            var pendingCollec = DatabaseConnection.GetDb().GetCollection<PendingWithdrawal>("PendingWithdrawals");
            await pendingCollec.DeleteOneAsync(p => p.id == id);
        }
        public static async Task WithdrawalLoop() {
            while (true) {
                try {
                    if (SmartContract.withdrawalPossible)
                        await ExecuteWithdrawal();
                }
                catch (Exception e) {
                    Logger.LogInternal("Withdrawal loop error : " + e.Message);
                }
                await Task.Delay(1000);
            }
        }

        public static async Task RunWithdrawalLoop() {
            if (!Running) {
                Running = true;
                _ = WithdrawalLoop();
            }
        }
    }
}
