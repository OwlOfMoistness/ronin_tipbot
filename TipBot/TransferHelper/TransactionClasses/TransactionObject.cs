using System;
using System.Numerics;
using System.Threading.Tasks;
namespace TipBot.TransferHelper {
    public class TransactionObject {
        public TipUser Sender;
        public TipWallet SenderWallet;
        public Token CurrentToken;
        public BigInteger NumericValue;
        public bool Approved;
        public string ErrorMessage;

        public virtual async Task<bool> ApproveToken(string symbol) {
            CurrentToken = await ServiceData.GetTokenSymbol(symbol);
            if (CurrentToken == null) {
                ErrorMessage = "Token is not supported";
                return false;
            }
            return true;
        }

        public async Task<bool> ApproveSender(ulong sender) {
            Sender = await TipUser.GetUser(sender);
            if (Sender == null) {
                ErrorMessage = "Sender does not exist.";
                return false;
            }
            return true;
        }

        public virtual async Task<bool> ApproveWallet() {
            if (!Sender.walletList.ContainsKey(CurrentToken.Symbol)) {
                ErrorMessage = "Sender does not have token.";
                return false;
            }
            SenderWallet = await TipWallet.GetWallet(Sender.walletList[CurrentToken.Symbol]);
            return true;
        }

        public bool ApproveValue(string value) {
            if (!SenderWallet.IsValidValue(value)) {
                ErrorMessage = "Wrong value format.";
                return false;
            }
            NumericValue = BigInteger.Parse(SenderWallet.ParseValue(value));
            if (NumericValue.IsZero) {
                ErrorMessage = "Value cannot be zero.";
                return false;
            }
            return true;
        }

        public bool ApproveFunds() {
            if (!SenderWallet.HasFunds(NumericValue)) {
                ErrorMessage = "Sender does not have sufficient tokens.";
                return false;
            }
            return true;
        }

        public bool ApproveEthAddress() {
            if (!TipUser.IsValidAddress(Sender.WithdrawalAddress)) {
                ErrorMessage = "User's ethereum address is not valid or not set. Make sure you have the correct format \"ronin:1abde35...\"";
                return false;
            }
            return true;
        }

        public virtual void InitNull() {
            Sender = null;
            SenderWallet = null;
            CurrentToken = null;
            NumericValue = 0;
            ErrorMessage = "";
            Approved = false;
        }
    }

}
