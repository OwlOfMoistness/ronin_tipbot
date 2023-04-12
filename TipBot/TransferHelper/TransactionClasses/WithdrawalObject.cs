using System;
using System.Numerics;
using System.Threading.Tasks;
namespace TipBot.TransferHelper {
    public class WithdrawalObject : TransactionObject {
        public BigInteger Fee;
        public static async Task<WithdrawalObject> GetWithdrawalApproval(ulong sender, string symbol, string value, string fee) {
            var t = new WithdrawalObject();
            t.InitNull();
            if (!await t.ApproveToken(symbol))
                return t;
            if (!await t.ApproveSender(sender))
                return t;
            if (!await t.ApproveWallet())
                return t;
            if (!t.ApproveValue(value))
                return t;
            if (!t.ApproveEthAddress())
                return t;
            if (!t.ApproveFunds())
                return t;
            if (!t.ApproveFee(fee))
                return t;
            t.Approved = true;
            return t;
        }

        public bool ApproveFee(string fee) {
            if (!SenderWallet.IsValidValue(fee)) {
                ErrorMessage = "Wrong fee format.";
                return false;
            }
            Fee = BigInteger.Parse(SenderWallet.ParseValue(fee));
            if (Fee > NumericValue) {
                ErrorMessage = "Fee amount superior to withdrawn amount.";
                return false;
            }
            return true;
        }
    }
}
