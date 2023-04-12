using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TipBot.EmbedMessages;
using Discord;
using System.Text;
using TipBot.Games;
using TipBot.Log;
namespace TipBot.TransferHelper.PendingObjects {
    public class PendingQueue {
        public static bool Executing = false;
        public static Queue<PendingTransaction> TransactionQueue = new Queue<PendingTransaction>();
        public static readonly object obj = new object();
        public static bool Running = false;

        public static async Task RunTaskLoop() {
            if (!Running) {
                Running = true;
                _ = TaskLoop();
            }
        }

        public static async Task TaskLoop() {
            while (true) {
                try {
                    while (TransactionQueue.Count > 0)
                        await ExecuteTask();
                }
                catch (Exception e) {
                    Logger.LogInternal("Task loop error : " + e.Message);
                }
                await Task.Delay(250);
            }
        }

        public static async Task ExecuteTask() {
            PendingTransaction tx = null;

            lock (obj) {
                tx = TransactionQueue.Dequeue();
            }
            await tx.ExecuteTransaction();
        }
    }
}
