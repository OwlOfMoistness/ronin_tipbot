using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Text;
using TipBot.TransferHelper;
using System.Threading.Tasks;
using TipBot.Games;
namespace TipBot.EmbedMessages {
    public class AdminEmbeds {
        public static Embed GuildParamEmbed(GuildParametres gParams) {
            var embed = new EmbedBuilder();
            embed.WithColor(Color.Green);
            embed.WithTitle("⚙️ Guild Parametres ⚙️");
            if (gParams != null) {
                if (gParams.AdminRoles.Count > 0) {
                    var admins = String.Join("\n", gParams.AdminRoles.Select(g => $"<@!{g}>").ToArray());
                    embed.AddField("Admins", admins);
                }
                if (gParams.GameApprovedChannels.Count > 0) {
                    var channels = String.Join("\n", gParams.GameApprovedChannels.Select(g => $"<#{g}>").ToArray());
                    embed.AddField("Game approved channels", channels);
                }
            }
            return embed.Build();
        }
    }
}
