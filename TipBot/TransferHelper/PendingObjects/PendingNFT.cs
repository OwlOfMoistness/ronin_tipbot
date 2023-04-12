using System;
using System.Threading.Tasks;
using TipBot.Mongo;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Discord;
namespace TipBot.TransferHelper.PendingObjects {
    public class PendingNFT {
        public ulong id;
        public string TokenId;
        public string Symbol;
        public ulong To;
        public IMessageChannel Channel;

        public PendingNFT(ulong _id, string t, string s, ulong to, IMessageChannel channel) {
            id = _id;
            TokenId = t;
            Symbol = s;
            To = to;
            Channel = channel;
        }
    }
}
