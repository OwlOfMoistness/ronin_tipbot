using System;
using System.Numerics;
using System.Threading.Tasks;
using TipBot.TransferHelper;
namespace TipBot.Games {
    public class GamblingObject : TransactionObject {
        public TipUser BankRollUser;
        public TipWallet BankRoll;
        public BigInteger PotentialGain;

        public static async Task<GamblingObject> GetGamblingApproval(ulong sender,
            string value, string symbol, int modulo, int rollUnder) {
            var t = new GamblingObject();
            t.InitNull();
            if (!await t.IsGamblingEnabled())
                return t;
            if (!await t.ApproveToken(symbol))
                return t;
            if (!await t.ApproveBankRollUser())
                return t;
            if (!await t.ApproveBankRoll())
                return t;
            if (!await t.ApproveSender(sender))
                return t;
            if (!await t.ApproveWallet())
                return t;
            if (!t.ApproveValue(value))
                return t;
            if (!t.ApproveFunds())
                return t;
            if (!await t.ApprovePotentialGain(modulo, rollUnder))
                return t;
            t.Approved = true;
            return t;
        }
        public async Task<bool> IsGamblingEnabled() {
            var value = (await AdminParametres.GetParams()).IsGamblingEnabled;
            if (!value) {
                ErrorMessage = "Gambling is currently not enabled";
                return value;
            }
            return value;
        }

        public async Task<bool> ApproveBankRollUser() {
            BankRollUser = await TipUser.GetUser(712976781438615572);
            if (BankRollUser == null) {
                ErrorMessage = "Gambling token not supported";
                return false;
            }
            return true;
        }

        public async Task<bool> ApproveBankRoll() {
            if (!BankRollUser.walletList.ContainsKey(CurrentToken.Symbol)) {
                ErrorMessage = "Gambling token not supported";
                return false;
            }
            BankRoll = await TipWallet.GetWallet(BankRollUser.walletList[CurrentToken.Symbol]);
            if (BankRoll == null) {
                ErrorMessage = "Gambling token not supported";
                return false;
            }
            return true;
        }

        public async Task<bool> ApprovePotentialGain(int modulo, int rollUnder) {
            PotentialGain = NumericValue * modulo / rollUnder;
            var maxBetValue = BigInteger.Parse(BankRoll.Value);
            maxBetValue /= (10000 / (await AdminParametres.GetParams()).MaxBetPercentage);
            if (!BankRoll.HasFunds(PotentialGain) || maxBetValue < PotentialGain) {
                ErrorMessage = "Cannot afford to lose this bet, sorry.";
                return false;
            }
            return true;
        }
    }
}
