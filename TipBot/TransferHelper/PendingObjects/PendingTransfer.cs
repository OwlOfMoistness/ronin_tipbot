using System;
using System.Threading.Tasks;
using TipBot.Mongo;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Discord;
namespace TipBot.TransferHelper.PendingObjects {
    public class PendingTransfer {
        public ulong id;
        public string Value;
        public string Symbol;
        public ulong[] Receivers;
        public IMessageChannel Channel;

        public PendingTransfer(ulong _id, string v, string s, ulong[] rs, IMessageChannel channel) {
            id = _id;
            Value = v;
            Symbol = s;
            Receivers = rs;
            Channel = channel;
        }
    }
}
