using System;
using TipBot.Mongo;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
namespace TipBot {
    public class GuildParametres {
        public ulong id;
        public List<ulong> AdminRoles;
        public List<ulong> GameApprovedChannels;

        public GuildParametres(ulong _id, List<ulong> adR, List<ulong> gameChnls) {
            id = _id;
            AdminRoles = adR;
            GameApprovedChannels = gameChnls;
        }

        public static async Task<GuildParametres> GetGuild(ulong _id) {
            var guildCollec = DatabaseConnection.GetDb().GetCollection<GuildParametres>("Guilds");
            return (await guildCollec.FindAsync(g => g.id == _id)).FirstOrDefault();
        }

        public static async Task<bool> IsAllowed(ulong guildId, ulong user) {
            var guildParam = await GetGuild(guildId);
            if (guildParam == null)
                return false;
            return guildParam.AdminRoles.Contains(user);
        }

        public static async Task<bool> CanPlay(ulong guildId, ulong channelId) {
            var guildParam = await GetGuild(guildId);
            if (guildParam == null)
                return true;
            return guildParam.GameApprovedChannels.Contains(channelId) || guildParam.GameApprovedChannels.Count == 0;
        }

        public async Task SaveToDatabase() {
            var guildCollec = DatabaseConnection.GetDb().GetCollection<GuildParametres>("Guilds");
            var data = (await guildCollec.FindAsync(w => w.id == id)).FirstOrDefault();
            if (data != null) {
                var update = Builders<GuildParametres>.Update.Set(t => t.AdminRoles, AdminRoles).Set(t => t.GameApprovedChannels, GameApprovedChannels);
                await guildCollec.FindOneAndUpdateAsync(w => w.id == id, update);
            }
            else
                await guildCollec.InsertOneAsync(this);
        }

        public async Task AddAdminRole(List<ulong> roles) {
            AdminRoles.AddRange(roles);
            await SaveToDatabase();
        }

        public async Task RemoveAdminRole(List<ulong> roles) {
            AdminRoles = AdminRoles.Except(roles).ToList();
            await SaveToDatabase();
        }

        public async Task AddGameChannel(List<ulong> channels) {
            GameApprovedChannels.AddRange(channels);
            await SaveToDatabase();
        }

        public async Task RemoveGameChannel(List<ulong> channels) {
            GameApprovedChannels = GameApprovedChannels.Except(channels).ToList();
            await SaveToDatabase();
        }
    }
}
