using System;
using System.Threading.Tasks;
using TipBot.Mongo;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Discord;
namespace TipBot.TransferHelper.PendingObjects {
    public class PendingConvert {
        public ulong id;
        public string Value;
        public string FromSymbol;
        public string ToSymbol;
        public string Emote;
        public IMessageChannel Channel;

        public PendingConvert(ulong _id, string v, string fS, string tS, string emote, IMessageChannel channel) {
            id = _id;
            Value = v;
            FromSymbol = fS;
            ToSymbol = tS;
            Emote = emote;
            Channel = channel;
        }
    }
}
