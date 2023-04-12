using System;
using Discord;
using System.Numerics;
using System.Linq;
using System.Threading.Tasks;
using TipBot.Log;
using TipBot.Mongo;
namespace TipBot.TransferHelper {
    public class TransferFunctions {
        public static string FormatUint(string str, int dec, bool noSpace = false) {
            int periodIndex;
            if (str.Length <= dec) {
                str = str.PadLeft(dec, '0');
                str = "0." + str;
            }
            else
                str = str.Insert(str.Length - dec, ".");
            if (str.Last() == '.')
                str = str.Substring(0, str.Length - 1);
            else
                str = str.TrimEnd('0');
            if (noSpace)
                return str;
            periodIndex = str.IndexOf('.');
            if (periodIndex == -1)
                periodIndex = str.Length;
            var spaceCount = periodIndex / 3;
            var offset = 0;
            for (int i = 0; i < spaceCount; i++) {
                var index = i - offset + periodIndex % 3 + i * 3;
                if (index != 0)
                    str = str.Insert(index, " ");
                else
                    offset++;
            }
            if (str.Last() == '.')
                str = str.Substring(0, str.Length - 1);
            return str;
        }

        public static string FormatUint(BigInteger num, int dec, bool noSpace = false) {
            var str = num.ToString();
            return FormatUint(str, dec, noSpace);
        }

        public static async Task<(bool, string)> TransferNFT(ulong from, ulong to, string symbol, string tokenId) {
            var nftObject = await NFTObject.GetNFTApproval(from, symbol, tokenId, to);
            if (!nftObject.Approved)
                return (false, nftObject.ErrorMessage);
            symbol = nftObject.NFTContract.Symbol;

            using (var session = DatabaseConnection.GetClient().StartSession()) {
                try {
                    session.StartTransaction();
                    var recipientWallet = await TipUser.GetUser(to);
                    if (recipientWallet == null)
                        recipientWallet = new TipUser(to);
                    nftObject.NFT.Owner = to;
                    await recipientWallet.SaveToDatabase(session);
                    await nftObject.NFT.SaveToDatabase(session);
                    await session.CommitTransactionAsync();
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                    await session.AbortTransactionAsync();
                    return (false, "Error trying to log transaction to Database. Transaction rollbacked.");
                }
            }
            return (true, tokenId);
        }

        public static async Task<(bool, string)> TransferSplit(ulong from, ulong[] IDs, string symbol, string value) {
            var transferObject = await TransferObject.GetTransferApproval(from, symbol, value, IDs.Length);
            if (!transferObject.Approved)
                return (false, transferObject.ErrorMessage);
            symbol = transferObject.SenderWallet.Symbol;
            transferObject.SenderWallet.Sub(transferObject.NumericValue);
            transferObject.Recipients = new TipUser[IDs.Length];
            transferObject.RecipientWallets = new TipWallet[IDs.Length];
            using (var session = DatabaseConnection.GetClient().StartSession()) {
                try {
                    session.StartTransaction();
                    for (int i = 0; i < IDs.Length; i++) {
                        transferObject.Recipients[i] = await TipUser.GetUser(IDs[i]);
                        if (transferObject.Recipients[i] == null)
                            transferObject.Recipients[i] = new TipUser(IDs[i]);
                        if (!transferObject.Recipients[i].walletList.ContainsKey(symbol)) {
                            var t = transferObject.CurrentToken;
                            transferObject.RecipientWallets[i] = new TipWallet(t.ContractAddress, t.Symbol, t.Decimal);
                            transferObject.Recipients[i].walletList.Add(symbol, transferObject.RecipientWallets[i].id.ToString());
                            await transferObject.Recipients[i].SaveToDatabase(session);
                        }
                        else
                            transferObject.RecipientWallets[i] = await TipWallet.GetWallet(transferObject.Recipients[i].walletList[symbol]);
                        transferObject.RecipientWallets[i].Add(transferObject.Share);
                        await transferObject.RecipientWallets[i].SaveToDatabase(session);
                    }
                    await transferObject.SenderWallet.SaveToDatabase(session);
                    await TransactionLog.LogTransation(from, IDs, transferObject.CurrentToken,
                        transferObject.NumericValue.ToString(), transferObject.Share.ToString(),
                        TransactionType.Transfer);
                    await session.CommitTransactionAsync();
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                    await session.AbortTransactionAsync();
                    return (false, "Error trying to log transaction to Database. Transaction rollbacked.");
                }
            }
            return (true, FormatUint(transferObject.Share, transferObject.CurrentToken.Decimal));
        }

        public static async Task<(bool, string)> ConvertMilliToUnitTokens(ulong from, string symbol, string value, string newSymbol) {
            // TODO add convert log by crating mint and burn simple logs
            var convertObject = await ConvertObject.GetWithdrawalApproval(from, symbol, value);
            if (!convertObject.Approved)
                return (false, convertObject.ErrorMessage);
            var wallet = convertObject.SenderWallet;
            var _value = convertObject.NumericValue - (convertObject.NumericValue % 1000);
            var newToken = await ServiceData.GetTokenSymbol(newSymbol);
            if (newToken == null)
                return (false, $"{newSymbol} token does not exist");
            using (var session = DatabaseConnection.GetClient().StartSession()) {
                try {
                    session.StartTransaction();
                    wallet.Sub(_value);
                    TipWallet otherWallet;
                    if (convertObject.Sender.walletList.ContainsKey(newToken.Symbol))
                        otherWallet = await TipWallet.GetWallet(convertObject.Sender.walletList[newToken.Symbol]);
                    else {
                        otherWallet = new TipWallet(newToken.ContractAddress, newToken.Symbol, newToken.Decimal);
                        convertObject.Sender.walletList.Add(newToken.Symbol, otherWallet.id.ToString());
                        await convertObject.Sender.SaveToDatabase(session);
                    }
                    otherWallet.Add(_value / 1000);
                    await wallet.SaveToDatabase(session);
                    await otherWallet.SaveToDatabase(session);
                    await session.CommitTransactionAsync();
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                    await session.AbortTransactionAsync();
                    return (false, "Error trying to log transaction to Database. Transaction rollbacked.");
                }
            }
            return (true, FormatUint((_value / 1000), 0));
        }

        public static async Task<(bool, string)> ConvertMilliTokens(ulong from, string symbol, string value, string newSymbol) {
            // TODO add convert log
            var convertObject = await ConvertObject.GetWithdrawalApproval(from, symbol, value);
            if (!convertObject.Approved)
                return (false, convertObject.ErrorMessage);
            var wallet = convertObject.SenderWallet;
            var _value = convertObject.NumericValue;
            var newToken = await ServiceData.GetTokenSymbol(newSymbol);
            if (newToken == null)
                return (false, $"{newSymbol} token does not exist");
            wallet.Sub(_value);
            using (var session = DatabaseConnection.GetClient().StartSession()) {
                try {
                    session.StartTransaction();
                    TipWallet otherWallet;
                    if (convertObject.Sender.walletList.ContainsKey(newToken.Symbol))
                        otherWallet = await TipWallet.GetWallet(convertObject.Sender.walletList[newToken.Symbol]);
                    else {
                        otherWallet = new TipWallet(newToken.ContractAddress, newToken.Symbol, newToken.Decimal);
                        convertObject.Sender.walletList.Add(newToken.Symbol, otherWallet.id.ToString());
                        await convertObject.Sender.SaveToDatabase(session);
                    }
                    otherWallet.Add(_value * 1000);
                    await wallet.SaveToDatabase(session);
                    await otherWallet.SaveToDatabase(session);
                    await session.CommitTransactionAsync();
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                    await session.AbortTransactionAsync();
                    return (false, "Error trying to log transaction to Database. Transaction rollbacked.");
                }
            }
            return (true, FormatUint((_value * 1000), 0));
        }
        // <:Axie_a2Reptile:438142822856523800>
        public static async Task DepositNFT(ulong discordId, string tokenContract, BigInteger tokenId, string transactionHash) {
            // TODO add NFT log
            var user = await TipUser.GetUser(discordId);
            var token = await ServiceData.GetNFTToken(tokenContract);
            if (token == null)
                return;
            using (var session = DatabaseConnection.GetClient().StartSession()) {
                try {
                    session.StartTransaction();
                    if (user == null) {
                        user = new TipUser(discordId);
                        await user.SaveToDatabase(session);
                    }
                    else {
                        var newNft = new TipNFT(tokenContract, tokenId.ToString(), discordId);
                        await newNft.SaveToDatabase(session);
                    }
                    await session.CommitTransactionAsync();
                }
                catch (Exception e) {
                    Logger.LogInternal("Deposit token error : " + e.Message);
                    await session.AbortTransactionAsync();
                }
            }
        }

        public static async Task DepositTokens(ulong discordId, string tokenContract, BigInteger amount, string transactionHash) {
            var user = await TipUser.GetUser(discordId);
            var token = await ServiceData.GetToken(tokenContract);
            TipWallet wallet;

            if (token == null)
                return;
            using (var session = DatabaseConnection.GetClient().StartSession()) {
                try {
                    session.StartTransaction();
                    if (user == null) {
                        user = new TipUser(discordId);
                        wallet = new TipWallet(token.ContractAddress, token.Symbol, token.Decimal);
                        user.walletList.Add(token.Symbol, wallet.id.ToString());
                        await user.SaveToDatabase(session);
                    }
                    else {
                        if (user.walletList.ContainsKey(token.Symbol))
                            wallet = await TipWallet.GetWallet(user.walletList[token.Symbol]);
                        else {
                            wallet = new TipWallet(token.ContractAddress, token.Symbol, token.Decimal);
                            user.walletList.Add(token.Symbol, wallet.id.ToString());
                            await user.SaveToDatabase(session);
                        }
                    }
                    wallet.Add(amount);
                    await wallet.SaveToDatabase(session);
                    await session.CommitTransactionAsync();
                }
                catch (Exception e) {
                    Logger.LogInternal("Deposit token error : " + e.Message);
                    await session.AbortTransactionAsync();
                }
            }
            await TransactionLog.LogTransation(
                                                0,
                                                new ulong[1] { discordId },
                                                token,
                                                amount.ToString(),
                                                amount.ToString(),
                                                TransactionType.Deposit,
                                                transactionHash);
        }
    }
}
