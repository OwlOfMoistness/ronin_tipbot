using System;
using System.Threading.Tasks;
using TipBot.Mongo;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using TipBot.Games;
using Discord;
namespace TipBot.TransferHelper.PendingObjects {

    public class PendingGame {
        public IUser User;
        public string Value;
        public string Symbol;
        public string BetCondition;
        public int Modulo;
        public int BetUnder;
        public GameEnum GameType;
        public ulong Seed;
        public IMessageChannel Channel;
        public IUserMessage Message;

        public PendingGame(IUser user, string v, string s, string b, int m, int bU, GameEnum type, ulong seed, IMessageChannel chan, IUserMessage msg = null) {
            User = user;
            Value = v;
            Symbol = s;
            BetCondition = b;
            Modulo = m;
            BetUnder = bU;
            GameType = type;
            Seed = seed;
            Channel = chan;
            Message = msg;
        }
    }
}
