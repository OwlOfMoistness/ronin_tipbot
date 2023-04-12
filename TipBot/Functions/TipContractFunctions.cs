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

    [Function("transfer")]
    public class TransferFunction : FunctionMessage {
        [Parameter("address", "_to", 1)]
        public string To { get; set; }

        [Parameter("uint256", "_amount", 2)]
        public BigInteger Amount { get; set; }
    }
}
