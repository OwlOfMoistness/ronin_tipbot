using System;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace TipBot.Events {
    [Event("DiscordDeposit")]
    public class DiscordDepositEvent : IEventDTO {
        [Parameter("address", "tokenContract", 1, true)]
        public string tokenContract { get; set; }

        [Parameter("uint256", "amount", 2)]
        public BigInteger amount { get; set; }

        [Parameter("uint64", "discordId", 3, true)]
        public ulong discordId { get; set; }
    }

    [Event("DiscordWithdrawal")]
    public class DiscordWithdrawalEvent : IEventDTO {
        [Parameter("address", "tokenContract", 1)]
        public string tokenContract { get; set; }

        [Parameter("uint256", "amount", 2)]
        public BigInteger amount { get; set; }

        [Parameter("address", "recipient", 3)]
        public string recipient { get; set; }

        [Parameter("uint64", "discordId", 4)]
        public ulong discordId { get; set; }
    }

}
