using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using TipBot.Games;
namespace TipBot {
    public class GambleReactionFocus : ReactionFocus {
        public GameEnum Game;
        public string Condition;
        public string Value;
        public string Symbol;
        public bool InGame;

        public GambleReactionFocus(IUserMessage msg, GameEnum g, string c,  string v, string s) : base(msg) {
            Game = g;
            Condition = c;
            Value = v;
            Symbol = s;
            InGame = true;
        }

        public void UpdateGameFocus(IMessage msg, GameEnum game, string c, string v, string s) {
            Game = game;
            Condition = c;
            Value = v;
            Symbol = s;
            InGame = true;
            UpdateFocus(msg);
        }
    }
}
