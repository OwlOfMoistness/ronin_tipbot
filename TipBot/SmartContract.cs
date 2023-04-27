using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MongoDB.Driver;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3.Accounts;
using Nethereum.Util;
using System.Threading.Tasks;
using TipBot.Events;
using TipBot.Log;
using TipBot.TransferHelper;
using Discord;
using TipBot.Mongo;
using TipBot.Functions;
using Newtonsoft.Json.Linq;
using System.Net;

namespace TipBot {
    public class SmartContract {
        //public static string OLD_TIP_ADDRESS = "0xA646Ac048c316de2479c6A1Ab81317303296f45C";
        public static string RON = "0xeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";
        public static string TIP_ADDRESS = "0x4c7EFee4315E8Ed23B02fDcC8148B19D8f624D32";
        public static string FACTORY_ADDRESS = "0x70469Dc24C12f97087661fB6471569265D8E781C";
        //public static string RONIN_ENDPOINT = "http://localhost:8845";
        public static string RONIN_ENDPOINT = "http://10.0.0.10:8845";
        public static bool withdrawalPossible = true;
        public static bool depositPossible = true;
        public static string DiscoKey;
        public static string DepositKey;
        public static int CHAIN_ID = 2020;


        //public static async Task<BigInteger> GetSlpPriceSell() {
        //    var web3 = new Web3("https://mainnet.infura.io/v3/b4e2781f02a94a5a96dcf8ce8cab9449");
        //    var slpLiquidityAddress = "0xf4158e282f2317597e31c028978c7fb7275d6fb4";
        //    var funcParams = new GetTokenToEthInputPrice() {
        //        TokensSold = 1
        //    };
        //    var handler = web3.Eth.GetContractQueryHandler<GetTokenToEthInputPrice>();
        //    var res = await handler.QueryDeserializingToObjectAsync<SlpPrice>(funcParams, slpLiquidityAddress);
        //    return res.Out;
        //}

        //public static async Task<BigInteger> GetV1TokenPrice(string add) {
        //    var web3 = new Web3("https://mainnet.infura.io/v3/b4e2781f02a94a5a96dcf8ce8cab9449");
        //    var funcParams = new GetTokenToEthInputPrice() {
        //        TokensSold = 1
        //    };
        //    var handler = web3.Eth.GetContractQueryHandler<GetTokenToEthInputPrice>();
        //    var res = await handler.QueryDeserializingToObjectAsync<SlpPrice>(funcParams, add);
        //    return res.Out;
        //}

        //public static async Task<BigInteger> GetV2TokenPriceToEth(string add, BigInteger amount) {
        //    var web3 = new Web3("https://mainnet.infura.io/v3/941d6f5ccab944728642af2ab67aea3a");
        //    var path = new List<string>();
        //    path.Add(add);
        //    path.Add("0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2");
        //    var funcParams = new GetAmountsOut() {
        //        AmountIn = amount,
        //        Path = path
        //    };
        //    var handler = web3.Eth.GetContractQueryHandler<GetAmountsOut>();
        //    var res = await handler.QueryDeserializingToObjectAsync<AmountsOutReturn>(funcParams, "0x7a250d5630B4cF539739dF2C5dAcb4c659F2488D");
        //    return res.Amounts[1];
        //}

        //public static async Task<BigInteger> GetSlpPriceBuy() {
        //    var web3 = new Web3("https://mainnet.infura.io/v3/b4e2781f02a94a5a96dcf8ce8cab9449");
        //    var slpLiquidityAddress = "0xf4158e282f2317597e31c028978c7fb7275d6fb4";
        //    var funcParams = new GetEthToTokenOutputPrice() {
        //        TokensBought = 1
        //    };
        //    var handler = web3.Eth.GetContractQueryHandler<GetEthToTokenOutputPrice>();
        //    var res = await handler.QueryDeserializingToObjectAsync<SlpPrice>(funcParams, slpLiquidityAddress);
        //    return res.Out;
        //}

        //public static async Task GetLogs() {
        //    try {
        //        var web3 = new Web3("https://mainnet.infura.io/v3/b4e2781f02a94a5a96dcf8ce8cab9449");
        //        var transferEventHandler = web3.Eth.GetEvent<erc20TransferEvents>("0xF5D669627376EBd411E34b98F19C868c8ABA5ADA");
        //        var filterEventForSpecificAddress = transferEventHandler.CreateFilterInput<string, string>(null, "0xa6dc4ebf7b56e2f9c0e701939f24e2122af3f681");
        //        var approvalEventsForSpecificAddress = await transferEventHandler.GetAllChanges(filterEventForSpecificAddress);
        //        Console.WriteLine("boo");
        //    }
        //    catch (Exception e) {

        //        Console.WriteLine(e.Message);

        //    }
        //}

        //public static async Task TransferFrom(string tokenContract, BigInteger amount, string to) {
        //    var myPassword = DiscoKey;
        //    var account = new Account(myPassword);
        //    Web3 web3;
        //    if (Program.IsRelease)
        //        web3 = new Web3(account, "https://mainnet.infura.io/v3/b4e2781f02a94a5a96dcf8ce8cab9449");
        //    else
        //        web3 = new Web3(account, "https://rinkeby.infura.io/v3/b4e2781f02a94a5a96dcf8ce8cab9449");

        //    var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();
        //    var param = new TransferFunction() {
        //        To = to,
        //        Amount = amount
        //    };
        //    param.GasPrice = Web3.Convert.ToWei(50, UnitConversion.EthUnit.Gwei);

        //    var estimateGas = await transferHandler.EstimateGasAsync(tokenContract, param);
        //    param.Gas = estimateGas;
        //    var receipt = await transferHandler.SendRequestAndWaitForReceiptAsync(tokenContract, param);
        //}

        //public static async Task<BigInteger> EstimateGasWithdrawTokens(BigInteger amount, BigInteger fee, string tokenAddress, string recipient, ulong discordId) {
        //    var myPassword = DiscoKey;
        //    var account = new Account(myPassword);
        //    Web3 web3;
        //    if (Program.IsRelease)
        //        web3 = new Web3(account, "https://mainnet.infura.io/v3/b4e2781f02a94a5a96dcf8ce8cab9449");
        //    else
        //        web3 = new Web3(account, "https://rinkeby.infura.io/v3/b4e2781f02a94a5a96dcf8ce8cab9449");

        //    var withdrawHandler = web3.Eth.GetContractTransactionHandler<WithdrawTokenFunction>();

        //    var functionParams = new WithdrawTokenFunction() {
        //        Amount = amount,
        //        Fee = fee,
        //        TokenContract = tokenAddress,
        //        Recipient = recipient,
        //        DiscordId = discordId
        //    };
        //    var gasFee = (int)(await GetGas("fastest"));
        //    functionParams.GasPrice = Web3.Convert.ToWei(gasFee, UnitConversion.EthUnit.Gwei);

        //    var estimateGas = await withdrawHandler.EstimateGasAsync(TIP_ADDRESS, functionParams);
        //    return estimateGas.Value;
        //}
        public static async Task<(bool, string)> ValidateTransaction(string hash) {
            var web3 = new Web3(RONIN_ENDPOINT);

            try {
                var tx = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(hash);

                //var tx = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(hash);
                if (tx == null)
                    return (false, "!find");
                else
                    return (tx.Succeeded(), hash);

            }
            catch (Exception e) {
                return (false, e.Message);
            }
            
        }

        public static async Task<BigInteger> BalanceOf(string token, string user) {
            var web3 = new Web3(RONIN_ENDPOINT);

            if (token == RON) {
                var ret = await web3.Eth.GetBalance.SendRequestAsync(user);
                return ret;
            }

            var funcParams = new BalanceOf() {
                User = user,
            };
            var handler = web3.Eth.GetContractQueryHandler<BalanceOf>();
            var res = await handler.QueryAsync<BigInteger>(token, funcParams);
            return res;
        }

        public static async Task<string> GetDepositAddress(BigInteger discordId) {
            var web3 = new Web3(RONIN_ENDPOINT);

            var funcParams = new GetDeterministicDepositAddress() {
                DiscordId = discordId,
            };
            var handler = web3.Eth.GetContractQueryHandler<GetDeterministicDepositAddress>();
            var res = await handler.QueryAsync<string>(FACTORY_ADDRESS, funcParams);
            return res;
        }

        public static async Task<bool> IsDeployed(BigInteger discordId) {
            var web3 = new Web3(RONIN_ENDPOINT);

            var address = await GetDepositAddress(discordId);
            return await IsDeployed(address);
        }

        public static async Task<bool> IsDeployed(string address) {
            var web3 = new Web3(RONIN_ENDPOINT);

            var funcParams = new Deployed() {
                Address = address,
            };
            var handler = web3.Eth.GetContractQueryHandler<Deployed>();
            var res = await handler.QueryAsync<bool>(FACTORY_ADDRESS, funcParams);
            return res;
        }

        public static async Task<bool> GenerateDepositAddress(BigInteger discordId) {
            try {

                var myPassword = DepositKey;
                var account = new Account(myPassword, CHAIN_ID);
                var web3 = new Web3(account, RONIN_ENDPOINT);

                var funcParams = new GenerateDepositAddress() {
                    DiscordId = discordId,
                };
                var handler = web3.Eth.GetContractTransactionHandler<GenerateDepositAddress>();
                var gasFee = 20;
                funcParams.GasPrice = Web3.Convert.ToWei(gasFee, UnitConversion.EthUnit.Gwei);
                var estimateGas = await handler.EstimateGasAsync(FACTORY_ADDRESS, funcParams);
                funcParams.Gas = estimateGas;
                var receipt = await handler.SendRequestAndWaitForReceiptAsync(FACTORY_ADDRESS, funcParams);
                return receipt.Succeeded();
            }
            catch (Exception e) {
                return false;
            }
        }

        public static async Task<(bool, string)> DepositTokens(BigInteger discordId, string tokenAddress, BigInteger value) {
            var txReceipt = "";
            try {
                depositPossible = false;
                var myPassword = DepositKey;
                var account = new Account(myPassword, CHAIN_ID);
                var web3 = new Web3(account, RONIN_ENDPOINT);

                var depositAddress = await GetDepositAddress(discordId);

                if (tokenAddress == RON) {
                    var funcParams = new TransferNativeToken() {
                        Amount = value
                    };
                    var depositHandler = web3.Eth.GetContractTransactionHandler<TransferNativeToken>();
                    var gasFee = 20;
                    funcParams.GasPrice = Web3.Convert.ToWei(gasFee, UnitConversion.EthUnit.Gwei);
                    var estimateGas = await depositHandler.EstimateGasAsync(depositAddress, funcParams);
                    funcParams.Gas = estimateGas;
                    txReceipt = await depositHandler.SendRequestAsync(depositAddress, funcParams);
                }
                else {
                    var funcParams = new TransferERC20Token() {
                        Token = tokenAddress,
                        Amount = value
                    };
                    var depositHandler = web3.Eth.GetContractTransactionHandler<TransferERC20Token>();
                    var gasFee = 20;
                    funcParams.GasPrice = Web3.Convert.ToWei(gasFee, UnitConversion.EthUnit.Gwei);
                    var estimateGas = await depositHandler.EstimateGasAsync(depositAddress, funcParams);
                    funcParams.Gas = estimateGas;
                    txReceipt = await depositHandler.SendRequestAsync(depositAddress, funcParams);
                }
            }
            catch (Exception e) {
                depositPossible = true;
                Console.WriteLine("Error : " + e.Message);
                return (false, "Error occured, transaction could not be broadcasted");
            }
            depositPossible = true;
            return (true, txReceipt);
        }

        public static async Task<(bool, string)> WithdrawTokens(BigInteger amount, BigInteger fee, string tokenAddress, string recipient, ulong discordId) {
            var embed = new EmbedBuilder();
            var res = "";
            try {
                recipient = SanitiseAddress(recipient);
                withdrawalPossible = false;
                var myPassword = DiscoKey;
                var account = new Account(myPassword, CHAIN_ID);
                Web3 web3;
                if (Program.IsRelease)
                    web3 = new Web3(account, RONIN_ENDPOINT);
                else
                    web3 = new Web3(account, RONIN_ENDPOINT);
                var txReceipt = "";
                if (tokenAddress == RON) {
                    var withdrawHandler = web3.Eth.GetContractTransactionHandler<WithdrawEtherFunction>();

                    var functionParams = new WithdrawEtherFunction() {
                        Amount = amount,
                        Fee = fee,
                        Recipient = recipient,
                        DiscordId = discordId
                    };
                    var gasFee = 20;
                    functionParams.GasPrice = Web3.Convert.ToWei(gasFee, UnitConversion.EthUnit.Gwei);
                    var estimateGas = await withdrawHandler.EstimateGasAsync(TIP_ADDRESS, functionParams);
                    functionParams.Gas = estimateGas;
                    txReceipt = await withdrawHandler.SendRequestAsync(TIP_ADDRESS, functionParams);
                }
                else {
                    var withdrawHandler = web3.Eth.GetContractTransactionHandler<WithdrawTokenFunction>();

                    var functionParams = new WithdrawTokenFunction() {
                        Amount = amount,
                        Fee = fee,
                        TokenContract = tokenAddress,
                        Recipient = recipient,
                        DiscordId = discordId
                    };
                    var gasFee = 20;
                    functionParams.GasPrice = Web3.Convert.ToWei(gasFee, UnitConversion.EthUnit.Gwei);

                    var estimateGas = await withdrawHandler.EstimateGasAsync(TIP_ADDRESS, functionParams);
                    functionParams.Gas = estimateGas;
                    txReceipt = await withdrawHandler.SendRequestAsync(TIP_ADDRESS, functionParams);
                }
                //if (txReceipt.Status.Value == BigInteger.One) {
                //    embed.WithColor(Color.Green);
                //    embed.WithTitle("Withdrawal Transaction Successful!");
                //}
                //else {
                //    embed.WithColor(Color.Red);
                //    embed.WithTitle("Transaction failed!");
                //}
                embed.WithColor(Color.Green);
                embed.WithTitle("Withdrawal Transaction sent!");
                embed.WithUrl("https://explorer.roninchain.com/tx/" + txReceipt);
                res = txReceipt;
            }
            catch (Exception e) {
                withdrawalPossible = true;
                Console.WriteLine("Error : " + e.Message);
                return (false, "Error occured, transaction could not be broadcasted");
            }
            withdrawalPossible = true;
            await (await Bot.GetUser(discordId)).SendMessageAsync(embed: embed.Build());
            return (true, res);
        }

        public static async Task<bool> CheckforApprovalEvent(string from, string tokenAddress) {
            //Web3 web3;
            //if (Program.IsRelease)
            //    web3 = new Web3(RONIN_ENDPOINT);
            //else
            //    web3 = new Web3(RONIN_ENDPOINT);
            //var approvalEventHandler = web3.Eth.GetEvent<erc20ApprovalEvents>(tokenAddress);
            //var filterEventForSpecificAddress = approvalEventHandler.CreateFilterInput(from);
            //var approvalEventsForSpecificAddress = await approvalEventHandler.GetAllChanges(filterEventForSpecificAddress);
            //if (approvalEventsForSpecificAddress.Count == 0)
            //    return false;
            //if (approvalEventsForSpecificAddress.Last().Event.value == 0)
            //    return false;
            return false;
        }

        //public static async Task WatchChainForEvents() {
        //    _ = Task.Run(ChainLoop);
        //}

        //public static async Task ChainLoop() {
        //    // initiate web, contract and events
        //    Web3 web3;
        //    if (Program.IsRelease)
        //        web3 = new Web3(RONIN_ENDPOINT);
        //    else
        //        web3 = new Web3(RONIN_ENDPOINT);
        //    var discordDepositEvent = web3.Eth.GetEvent<DiscordDepositEvent>(TIP_ADDRESS);
        //    bool isOn = true;

        //    // get last block param from db
        //    var checkCollec = DatabaseConnection.GetDb().GetCollection<Checkpoint>("Checkpoints");
        //    var checkpoint = (await checkCollec.FindAsync(c => c.id == 1)).FirstOrDefault();

        //    BlockParameter lastBlock = await GetLastBlockCheckpoint(web3);
        //    BlockParameter firstBlock = new BlockParameter(new HexBigInteger(new BigInteger(checkpoint.lastBlockChecked)));
        //    while (isOn) {
        //        checkpoint = (await checkCollec.FindAsync(c => c.id == 1)).FirstOrDefault();
        //        firstBlock = new BlockParameter(new HexBigInteger(new BigInteger(checkpoint.lastBlockChecked)));
        //        lastBlock = await GetLastBlockCheckpoint(web3);
        //        lastBlock = GetLastSafeCheckPoint(firstBlock, lastBlock);
        //        try {
        //            // event filters
        //            var discordDepositFilter = discordDepositEvent.CreateFilterInput(firstBlock, lastBlock);

        //            // event logs from block range
        //            var depositLogs = await discordDepositEvent.GetAllChanges(discordDepositFilter);

        //            foreach (var deposit in depositLogs) {
        //                if (!(await TransactionLog.CheckifEventIsLogged(deposit.Log.TransactionHash))) {
        //                    var token = await ServiceData.GetToken(deposit.Event.tokenContract);
        //                    if (token != null) {
        //                        await TransferFunctions.DepositTokens(
        //                            deposit.Event.discordId,
        //                            deposit.Event.tokenContract,
        //                            deposit.Event.amount,
        //                            deposit.Log.TransactionHash);
        //                        await (await Bot.GetUser(deposit.Event.discordId)).SendMessageAsync($"Your deposit of {TransferFunctions.FormatUint(deposit.Event.amount, token.Decimal)} {token.Symbol} has been added to your account!");
        //                    }
        //                }
        //            }
        //            checkpoint.lastBlockChecked = Convert.ToInt32(lastBlock.BlockNumber.Value.ToString());
        //            await checkCollec.FindOneAndReplaceAsync(c => c.id == 1, checkpoint);
        //        }
        //        catch (Exception e) {
        //            Console.WriteLine("Error in chain loop : " + e.Message);
        //        }
        //        await Task.Delay(60000);
        //    }
        //}

        private static BlockParameter GetLastSafeCheckPoint(BlockParameter first, BlockParameter last) {
            var span = last.BlockNumber.Value - first.BlockNumber.Value;
            if (span > 10000)
                return new BlockParameter(new HexBigInteger(first.BlockNumber.Value + 10000));
            return last;
        }

        private static async Task<BlockParameter> GetLastBlockCheckpoint(Web3 web3) {
            var lastBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var blockNumber = lastBlock.Value - 6;
            return new BlockParameter(new HexBigInteger(blockNumber));
        }

        private static async Task PostToTestChannel(string msg) {
            var channel = Bot.GetChannelContext(582891241906241546) as IMessageChannel;
            await channel.SendMessageAsync(msg);
        }

        private static async Task PostToTestChannel(Embed msg) {
            var channel = Bot.GetChannelContext(582891241906241546) as IMessageChannel;
            await channel.SendMessageAsync(embed: msg);
        }

        private static string SanitiseAddress(string add) {
            if (add.StartsWith("ronin:"))
                return "0x" + add.Substring(6);
            else
                return add;
        }
    }
}

public class Checkpoint {
    public int id;
    public int lastBlockChecked;
    public Checkpoint(int _id, int _lastBlockChecked) {
        id = _id;
        lastBlockChecked = _lastBlockChecked;
    }
}
