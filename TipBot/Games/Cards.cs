using System;
using System.Collections.Generic;
using System.Text;

namespace TipBot.Games {
    public enum Suits {
        Hearts,
        Clubs,
        Spades,
        Diamonds
    }

    public enum CardValue {
        Void,
        A,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        J,
        Q,
        K
    }

    public class Card {
        public Suits Suit;
        public CardValue Value;
        public Card(Suits s, CardValue v) {
            Suit = s;
            Value = v;
        }
    }
}
