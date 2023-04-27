using System;
using Discord;
using TipBot.Log;
using System.Threading.Tasks;
using TipBot.EmbedMessages;
using TipBot.Games;
namespace TipBot.TransferHelper.PendingObjects {
    public class PendingTransaction {
        public TransactionType Type;
        public object TypeObj;
        public PendingTransaction(TransactionType t, object a) {
            Type = t;
            TypeObj = a;
        }

        public async Task ExecuteTransaction() {
            bool result;
            string msg;
            Token token;
            switch (Type) {
                case TransactionType.Transfer:
                    var pT = (PendingTransfer)TypeObj;
                    (result, msg) = await TransferFunctions.TransferSplit(pT.id, pT.Receivers, pT.Symbol, pT.Value);
                    if (!result)
                        await pT.Channel.SendMessageAsync(embed: Embeds.BasicEmbed("🚫 Transfer Error", msg, Color.Red));
                    else {
                        token = await ServiceData.GetTokenSymbol(pT.Symbol);
                        await pT.Channel.SendMessageAsync(embed: Embeds.TransferEmbed(pT.id, pT.Receivers, token, msg));
                    }
                    break;
                case TransactionType.NFTTransfer:
                    var nT = (PendingNFT)TypeObj;
                    (result, msg) = await TransferFunctions.TransferNFT(nT.id, nT.To, nT.Symbol, nT.TokenId);
                    if (!result)
                        await nT.Channel.SendMessageAsync(embed: Embeds.BasicEmbed("🚫 NFT Transfer Error", msg, Color.Red));
                    else {
                        var nft = await ServiceData.GetNFTTokenSymbol(nT.Symbol);
                        await nT.Channel.SendMessageAsync(embed: Embeds.TransferNFTEmbed(nT.id, nT.To, nft, msg));
                    }
                    break;
                case TransactionType.Airdrop:
                    var pA = (PendingAirdrop)TypeObj;
                    (result, msg) = await TransferFunctions.TransferSplit(pA.id, pA.Receivers, pA.Symbol, pA.Value);
                    if (!result)
                        await pA.Message.ModifyAsync(m=> m.Embed = Embeds.BasicEmbed("🚫 Airdrop Error", msg, Color.Red));
                    else {
                        token = await ServiceData.GetTokenSymbol(pA.Symbol);
                        await pA.Message.ModifyAsync(msg => msg.Embed = Embeds.AirDropEmbed(pA.id, pA.Receivers, pA.Value, token.Symbol));
                    }
                    break;
                case TransactionType.ConvertFromMili:
                case TransactionType.ConvertToMili:
                    var pC = (PendingConvert)TypeObj;
                    token = await ServiceData.GetTokenSymbol(pC.FromSymbol);
                    if (Type == TransactionType.ConvertToMili)
                        (result, msg) = await TransferFunctions.ConvertMilliTokens(pC.id, pC.FromSymbol, pC.Value, pC.ToSymbol);
                    else
                        (result, msg) = await TransferFunctions.ConvertMilliToUnitTokens(pC.id, pC.FromSymbol, pC.Value, pC.ToSymbol);
                    if (!result) {
                        await pC.Channel.SendMessageAsync(embed: Embeds.BasicEmbed("🚫 Conversion Error", msg, Color.Red));
                        return;
                    }
                    await pC.Channel.SendMessageAsync(embed: Embeds.ConvertEmbed(pC.id, pC.Value, pC.FromSymbol, pC.ToSymbol, msg, pC.Emote));
                    break;
                case TransactionType.Withdrawal:
                    var pW = (PendingWithdrawal)TypeObj;
                    await pW.LogPendingWithdrawal();
                    break;
                case TransactionType.Game:
                    var pG = (PendingGame)TypeObj;
                    await CasinoGames.HandleModuloGame(pG.Value, pG.Symbol, pG.BetCondition, pG.Modulo, pG.BetUnder, pG.GameType, pG.User, pG.Seed, pG.Channel, pG.Message);
                    break;
                case TransactionType.Blackjack:
                    var pBJ = (PendingBlackjack)TypeObj;
                    await pBJ.HandleState();
                    break;
                case TransactionType.Deposit:
                    var pD = (PendingDeposit)TypeObj;
                    await pD.LogPendingDeposit();
                    break;
            }
        }
    }
}
