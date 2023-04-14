using System;
using Discord;

namespace TipBot.EmbedMessages {
    public class HelpMessages {
        public static EmbedBuilder FillIntroMessage() {
            var embed = new EmbedBuilder();
            embed.WithColor(Color.Blue);
            embed.WithTitle("👋 TipBot");
            embed.WithDescription("TipBot is a community Discord Bot built to allow you to tip cryptocurrencies to other users.");
            embed.AddField("Virtually **free** service", "TipBot lets users tip each other with no extra fees.\nThe only fees will happen when a user decides to withdraw their tokens to the Ronin blockchain to cover network fees.");
            embed.AddField("Intuitive commands", "TipBot is simple to use:\n`$tip 12 DAI @user` You can tip any supported tokens to any users.\nTo tip many people at the same time simply add more users at the end of the command: `$tip 12 DAI @user1 @user2 @user3`");
            embed.WithFooter("👋 Introduction 💰 Wallet");
            return embed;

        }

        public static EmbedBuilder FillWalletMessage() {
            var embed = new EmbedBuilder();
            embed.WithColor(Color.Blue);
            embed.WithTitle("💰 **Wallet**");
            embed.WithDescription("TipBot allows you to tip other users and withdraw tokens from and to the Ronin blockchain");
            embed.AddField("Tip", "`$tip 13 DAI @user` You can can tip one or many people at the same time");
            //embed.AddField("📥 Deposit", "`$deposit 13 DAI` Replace DAI by the token you want to deposit in your TipBot account. A private message will explain the step to get your tokens on the TipBot service!");
            embed.AddField("📤 Withdraw", "`$withdraw 15 DAI` Replace DAI by the token you want to withdraw to your ethereum wallet. Make sure you have withdrawal address set up using `$setwithdrawal ronin:your_address_goes_here`");
            embed.AddField("💵 Balances", "`$bal` Will give you the balance of each token you own.");
            embed.AddField("📜 Token List", "`$token` Will give the list of currently supported tokens.");
            embed.WithFooter("👋 Introduction 💰 Wallet");
            return embed;
        }

        public static EmbedBuilder FillGamblingMessage() {
            var embed = new EmbedBuilder();
            embed.WithColor(Color.Blue);
            embed.WithTitle("🎲 **Gambling**");
            embed.WithDescription("You can gamble some supported tokens on TipBot. Here are some of the games you can play:");
            embed.AddField("Coinflip", "`$gamble coin 10 dai heads/tails` To do a coin flip. 2x bet");
            embed.AddField("Blackjack", "`$gamble blackjack/bj/21 10 dai` To play a game of Interactive Blackjack. React to the emojis to hit, stand, double up or rebet after the end of a game. Up to 2.5x bet");
            embed.AddField("Etheroll", "`$gamble etheroll 10 dai 34` Means you are trying to roll 34 or under. Up to 100x bet");
            embed.AddField("Additional information", "`$bank` Can be used to see the current bank rolls and what is the largest gain on a bet you can do without without the TipBot refusing your bet!");
            embed.AddField("Convert Tokens to mTokens (milliTokens)", "`$convert 15 DAI` Means you will convert 15 DAI to 15000 mDAI used to gamble smaller amounts.\n`$convert 15000 mDAI` Will convert back the mDAI into 15 DAI.\nNot all tokens are supported");
            embed.WithFooter("👋 Introduction 💰 Wallet 🎲 Gambling");
            return embed;
        }

        public static EmbedBuilder FillAdminHelpMessage() {
            var embed = new EmbedBuilder();
            embed.WithColor(Color.Blue);
            embed.WithTitle("👨‍✈️ **Admin Commands**");
            embed.WithDescription("Some commands for admins:");
            embed.AddField("Setting Params", "`admin setFee/setMaxBet/setGambling int/int/bool` Don't forget value for fee and bet will be divivded by 10000 to give %. Example 50 => 0.5%\n`admin SetTokenFeeRed symbol value` to change ganmbled token fee reduction");
            embed.AddField("Bankroll", "`admin supplyBankRoll/withdrawBankRoll value` Add/remove funds to the gambling bankroll.");
            embed.AddField("Add Token", "`admin addtoken address symbol decimal emote` To support a new token");
            embed.AddField("Add NFT token", "`admin addnft address symbol emote` To suppor a new NFT");
            embed.AddField("Check token approval of address", "`admin CheckApproval address symbol` To check if addressed has approved the token");
            return embed;
        }

        public static EmbedBuilder FillGuildAdminHelpMessage() {
            var embed = new EmbedBuilder();
            embed.WithColor(Color.Blue);
            embed.WithTitle("👨‍✈️ **Guild Admin Commands**");
            embed.WithDescription("Some commands for guild admins. Admins will be to interact with commands below.");
            embed.AddField("Check guild params", "`$admin guildParam` to get the current's guild params.");
            embed.AddField("Add admins", "`$admin addGuildAdmin @user1 @user2` To add one or more admins at once.");
            embed.AddField("Remove admins", "`$admin removeGuildAdmin @user1 @user2` To remove one or more admins at once.");
            embed.AddField("Add gaming channels", "`$admin addGamingChannel #channel1 #channel2` To add one or more gaming channels at once.");
            embed.AddField("Remove gaming channels", "`$admin removeGamingChannel #channel1 #channel2` To remove one or more gaming channels at once.");
            return embed;
        }
    }
}
