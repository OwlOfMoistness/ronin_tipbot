using System;
using Discord;
namespace TipBot.TransferHelper.PendingObjects {
    public class PendingAirdrop {
        public ulong id;
        public string Value;
        public string Symbol;
        public ulong[] Receivers;
        public IUserMessage Message;
        public IMessageChannel Channel;

        public PendingAirdrop(ulong _id, string v, string s, ulong[] rs, IUserMessage message, IMessageChannel channel) {
            id = _id;
            Value = v;
            Symbol = s;
            Receivers = rs;
            Message = message;
            Channel = channel;
        }
    }
}
