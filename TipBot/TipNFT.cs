using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Numerics;
using MongoDB.Bson;
using MongoDB.Driver;
using TipBot.Mongo;
using TipBot.TransferHelper;
namespace TipBot {
    public class TipNFT {
        public ObjectId id;
        public string ContractAddress;
        public string TokenId;
        public ulong Owner;

        public TipNFT(string add, string token, ulong owner) {
            id = ObjectId.GenerateNewId();
            ContractAddress = add.ToLower();
            TokenId = token;
            Owner = owner;
        }

        public static async Task<TipNFT> GetNFT(string address, string tokenId) {
            var nftCollec = DatabaseConnection.GetDb().GetCollection<TipNFT>("NFTs");
            var nft = (await nftCollec.FindAsync(n => n.ContractAddress == address.ToLower() && n.TokenId == tokenId)).FirstOrDefault();
            return nft;
        }

        public static async Task<ulong> OwnerOf(string address, string tokenId) {
            var nftCollec = DatabaseConnection.GetDb().GetCollection<TipNFT>("NFTs");
            var nft = (await nftCollec.FindAsync(n => n.ContractAddress == address.ToLower() && n.TokenId == tokenId)).FirstOrDefault();
            if (nft == null)
                return 0;
            return nft.Owner;
        }

        public async Task TransferOwnership(ulong newOwner, IClientSessionHandle session) {
            Owner = newOwner;
            await SaveToDatabase(session);
        }

        public async Task SaveToDatabase() {
            var nftCollec = DatabaseConnection.GetDb().GetCollection<TipNFT>("NFTs");
            var data = (await nftCollec.FindAsync(w => w.id == id)).FirstOrDefault();
            if (data != null) {
                var update = Builders<TipNFT>.Update.Set(t => t.Owner, Owner);
                await nftCollec.FindOneAndUpdateAsync(w => w.id == id, update);
            }
            else
                await nftCollec.InsertOneAsync(this);
        }

        public async Task SaveToDatabase(IClientSessionHandle session) {
            var nftCollec = DatabaseConnection.GetDb().GetCollection<TipNFT>("NFTs");
            var data = (await nftCollec.FindAsync(w => w.id == id)).FirstOrDefault();
            if (data != null) {
                var update = Builders<TipNFT>.Update.Set(t => t.Owner, Owner);
                await nftCollec.FindOneAndUpdateAsync(session, w => w.id == id, update);
            }
            else
                await nftCollec.InsertOneAsync(session, this);
        }

        public async Task DeleteFromDatabase(IClientSessionHandle session) {
            var nftCollec = DatabaseConnection.GetDb().GetCollection<TipNFT>("NFTs");
            await nftCollec.DeleteOneAsync(session, w => w.id == id);
        }

        public async Task DeleteFromDatabase() {
            var nftCollec = DatabaseConnection.GetDb().GetCollection<TipNFT>("NFTs");
            await nftCollec.DeleteOneAsync(w => w.id == id);
        }
    }
}
