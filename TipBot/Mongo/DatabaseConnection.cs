using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
namespace TipBot.Mongo {
    class DatabaseConnection {
        private static MongoClient Client;
        private static IMongoDatabase Database;
        public static string MongoUrl;
        public static string DatabaseName;


        private static void SetupConnection() {
            Client = new MongoClient(MongoUrl);
            Database = Client.GetDatabase(DatabaseName);
        }

        public static MongoClient GetClient() => Client;

        public static IMongoDatabase GetDb() {
            if (Client == null) {
                SetupConnection();
            }
            return Database;
        }

        public static async Task MigrateDb(string newDb) {
            SetupConnection();
            var newDatabase = Client.GetDatabase(newDb);
            var list = await (await Database.ListCollectionNamesAsync()).ToListAsync();
            foreach (var name in list) {
                var collec = Database.GetCollection<BsonDocument>(name);
                var newCollec = newDatabase.GetCollection<BsonDocument>(name);
                var docs = await (await collec.FindAsync(x => true)).ToListAsync();
                if (docs.Count > 0)
                    await newCollec.InsertManyAsync(docs);
            }
        }
    }
}