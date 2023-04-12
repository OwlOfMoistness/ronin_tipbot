using System;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace TipBot.Events {
    [Event("Approval")]
    public class erc20ApprovalEvents : IEventDTO {
        [Parameter("address", "owner", 1, true)]
        public string owner { get; set; }

        [Parameter("address", "spender", 2, true)]
        public string spender { get; set; }

        [Parameter("uint256", "value", 3)]
        public BigInteger value { get; set; }
    }

    [Event("Transfer")]
    public class erc20TransferEvents : IEventDTO {
        [Parameter("address", "_from", 1, true)]
        public string From { get; set; }

        [Parameter("address", "_to", 2, true)]
        public string To { get; set; }

        [Parameter("uint256", "_value", 3)]
        public BigInteger value { get; set; }
    }
}
