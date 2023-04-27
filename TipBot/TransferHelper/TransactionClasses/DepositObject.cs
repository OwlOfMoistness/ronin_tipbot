using System;
using System.Numerics;
using System.Threading.Tasks;
namespace TipBot.TransferHelper {
    public class DepositObject : TransactionObject {
        public static async Task<DepositObject> GetDepositApproval(ulong sender, string symbol, string value) {
            var t = new DepositObject();
            t.InitNull();
            if (!await t.ApproveToken(symbol))
                return t;
            if (!await t.ApproveSender(sender))
                return t;
            if (!t.ApproveDepositValue(value))
                return t;
            t.Approved = true;
            return t;
        }

        public bool ApproveDepositValue(string value) {
            if (!TipWallet.IsValidValue(value, CurrentToken.Decimal)) {
                ErrorMessage = "Wrong value format.";
                return false;
            }
            NumericValue = BigInteger.Parse(TipWallet.ParseValueToTokenDecimal(value, CurrentToken.Decimal));
            return true;
        }
    }
}
