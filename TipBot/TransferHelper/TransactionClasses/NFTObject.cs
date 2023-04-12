using System.Numerics;
using System.Threading.Tasks;
namespace TipBot.TransferHelper {
    public class NFTObject : TransactionObject {
        public NFT NFTContract;
        public TipNFT NFT;

        public static async Task<NFTObject> GetNFTApproval(ulong sender, string symbol, string tokenId, ulong recipient) {
            var t = new NFTObject();
            t.InitNull();
            if (!await t.ApproveToken(symbol))
                return t;
            if (!await t.ApproveSender(sender))
                return t;
            if (!await t.CheckIfTokenIdExists(tokenId))
                return t;
            if (!t.ApproveOwner(sender))
                return t;
            t.Approved = true;
            return t;
        }

        public override async Task<bool> ApproveToken(string symbol) {
            NFTContract = await ServiceData.GetNFTTokenSymbol(symbol);
            if (NFTContract == null) {
                ErrorMessage = "NFT is not supported";
                return false;
            }
            return true;
        }

        public async Task<bool> CheckIfTokenIdExists(string tokenId) {
            NFT = await TipNFT.GetNFT(NFTContract.ContractAddress, tokenId);
            if (NFT == null) {
                ErrorMessage = "Sender does not own token ID.";
                return false;
            }
            return true;
        }

        public bool ApproveOwner(ulong sender) {
            if (NFT.Owner != sender) {
                ErrorMessage = "Sender does not own token ID.";
                return false;
            }
            return true;
        }
    }
}
