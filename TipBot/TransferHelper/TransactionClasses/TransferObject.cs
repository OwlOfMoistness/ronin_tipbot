using System;
using System.Numerics;
using System.Threading.Tasks;
namespace TipBot.TransferHelper {
    public class TransferObject : TransactionObject {
        public TipUser[] Recipients;
        public TipWallet[] RecipientWallets;
        public BigInteger Share;

        public static async Task<TransferObject> GetTransferApproval(ulong sender, string symbol, string value, int length) {
            var t = new TransferObject();
            t.InitNull();
            if (!await t.ApproveToken(symbol))
                return t;
            if (!await t.ApproveSender(sender))
                return t;
            if (!await t.ApproveWallet())
                return t;
            if (!t.ApproveValue(value, length))
                return t;
            if (!t.ApproveFunds())
                return t;
            t.Approved = true;
            return t;
        }

        public bool ApproveValue(string value, int length) {
            if (value == "all")
                NumericValue = BigInteger.Parse(SenderWallet.Value);
            else if (!SenderWallet.IsValidValue(value)) {
                ErrorMessage = "Wrong value format.";
                return false;
            }
            else 
                NumericValue = BigInteger.Parse(SenderWallet.ParseValue(value));
            NumericValue -= NumericValue % new BigInteger(length);
            Share = NumericValue / new BigInteger(length);
            if (Share == 0) {
                ErrorMessage = "Wrong value format.";
                return false;
            }
            return true;
        }

        public override void InitNull() {
            Sender = null;
            Recipients = null;
            SenderWallet = null;
            RecipientWallets = null;
            CurrentToken = null;
            NumericValue = 0;
            Share = 0;
            ErrorMessage = "";
            Approved = false;
        }
    }

}
