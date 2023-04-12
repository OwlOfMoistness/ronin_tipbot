using System;
using Discord;
using System.Threading;
using System.Numerics;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;

namespace TipBot {
    public class Program {
        public static bool IsRelease = false;
        static void Main(string[] args) {
            Mongo.DatabaseConnection.MongoUrl = args[0];
            SmartContract.DiscoKey = args[4];
            if (args[1] == "prod") {
                Mongo.DatabaseConnection.DatabaseName = "TipDataTest";
                IsRelease = true;
            }
            else {
                Mongo.DatabaseConnection.DatabaseName = "TipDataTest";
                SmartContract.RONIN_ENDPOINT = args[5];
            }
            RunBot(token: args[2], prefix: args[3]);
        }
        static void RunBot(string token, string prefix) {
            while (true) {
                try {
                    new Bot().RunAsync(token, prefix).GetAwaiter().GetResult();
                }
                catch (Exception ex) {
                    Logger.Log(new LogMessage(LogSeverity.Error, ex.ToString(), "Unexpected Exception", ex));
                    Console.WriteLine(ex.ToString());
                }
                Thread.Sleep(1000);
                break;
            }
        }
    }
}
