using System;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace TipBot.Functions {
    [Function("withdrawToken")]
    public class WithdrawTokenFunction : FunctionMessage {
        [Parameter("uint256", "amount", 1)]
        public BigInteger Amount { get; set; }

        [Parameter("uint256", "fee", 2)]
        public BigInteger Fee { get; set; }

        [Parameter("address", "tokenContract", 3)]
        public string TokenContract { get; set; }

        [Parameter("address", "recipient", 4)]
        public string Recipient { get; set; }

        [Parameter("uint64", "discordId", 5)]
        public ulong DiscordId { get; set; }
    }

    [Function("withdrawEther")]
    public class WithdrawEtherFunction : FunctionMessage {
        [Parameter("uint256", "amount", 1)]
        public BigInteger Amount { get; set; }

        [Parameter("uint256", "fee", 2)]
        public BigInteger Fee { get; set; }

        [Parameter("address", "recipient", 3)]
        public string Recipient { get; set; }

        [Parameter("uint64", "discordId", 4)]
        public ulong DiscordId { get; set; }
    }

    [Function("transfer")]
    public class TransferFunction : FunctionMessage {
        [Parameter("address", "_to", 1)]
        public string To { get; set; }

        [Parameter("uint256", "_amount", 2)]
        public BigInteger Amount { get; set; }
    }

    [Function("getDeterministicDepositAddress", "address")]
    public class GetDeterministicDepositAddress : FunctionMessage {
        [Parameter("uint256", "_discordId", 1)]
        public BigInteger DiscordId { get; set; }
    }

    [Function("generateDepositAddress")]
    public class GenerateDepositAddress : FunctionMessage {
        [Parameter("uint256", "_discordId", 1)]
        public BigInteger DiscordId { get; set; }
    }

    [Function("deployed", "bool")]
    public class Deployed : FunctionMessage {
        [Parameter("address", "_address", 1)]
        public string Address { get; set; }
    }

    [Function("transferERC20Token")]
    public class TransferERC20Token : FunctionMessage {
        [Parameter("address", "_token", 1)]
        public string Token { get; set; }
        [Parameter("uint256", "_amount", 2)]
        public BigInteger Amount { get; set; }
    }

    [Function("transferNativeToken")]
    public class TransferNativeToken : FunctionMessage {
        [Parameter("uint256", "_amount", 1)]
        public BigInteger Amount { get; set; }
    }

    [Function("balanceOf", "uint256")]
    public class BalanceOf : FunctionMessage {
        [Parameter("address", "_user", 1)]
        public string User { get; set; }
    }
}
