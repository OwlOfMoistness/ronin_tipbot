using System;
using System.Numerics;
using System.Threading.Tasks;
namespace TipBot.TransferHelper {
    public class ConvertObject : TransactionObject {
        public ConvertObject() {
        }

        public static async Task<WithdrawalObject> GetWithdrawalApproval(ulong sender, string symbol, string value) {
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
            if (!t.ApproveFunds())
                return t;
            t.Approved = true;
            return t;
        }
    }
}
