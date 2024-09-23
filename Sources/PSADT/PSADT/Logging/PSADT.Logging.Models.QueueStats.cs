namespace PSADT.Logging.Models
{
    public class QueueStats
    {
        public int QueueDepth { get; }
        public uint DroppedMessages { get; }

        public QueueStats(int queueDepth, uint droppedMessages)
        {
            QueueDepth = queueDepth;
            DroppedMessages = droppedMessages;
        }
    }
}
