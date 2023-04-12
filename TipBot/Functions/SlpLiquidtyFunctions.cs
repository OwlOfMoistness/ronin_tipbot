using System.Collections.Generic;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace TipBot.Functions {
    [Function("getTokenToEthInputPrice")]
    public class GetTokenToEthInputPrice : FunctionMessage {
        [Parameter("uint256", "tokens_sold", 1)]
        public BigInteger TokensSold { get; set; }
    }

    [Function("getEthToTokenOutputPrice")]
    public class GetEthToTokenOutputPrice : FunctionMessage {
        [Parameter("uint256", "tokens_bought", 1)]
        public BigInteger TokensBought { get; set; }
    }

    [FunctionOutput]
    public class SlpPrice : IFunctionOutputDTO {
        [Parameter("uint256", "out")]
        public BigInteger Out { get; set; }
    }

    [Function("getAmountsOut")]
    public class GetAmountsOut : FunctionMessage {
        [Parameter("uint", "amountIn", 1)]
        public BigInteger AmountIn { get; set; }

        [Parameter("address[]", "path", 2)]
        public List<string> Path { get; set; }

    }

    [FunctionOutput]
    public class AmountsOutReturn : IFunctionOutputDTO {
        [Parameter("uint[]", "amounts")]
        public List<BigInteger> Amounts { get; set; }
    }
}
