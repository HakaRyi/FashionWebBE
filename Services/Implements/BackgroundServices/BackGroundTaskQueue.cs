using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Services.Implements.BackgroundServices
{
    public class ModelProcessingJob
    {
        public int ModelId { get; set; }
        public int AccountId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    public interface IBackgroundTaskQueue
    {
        ValueTask QueueBackgroundWorkItemAsync(ModelProcessingJob workItem);
        ValueTask<ModelProcessingJob> DequeueAsync(CancellationToken cancellationToken);
    }
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<ModelProcessingJob> _queue;

        public BackgroundTaskQueue(int capacity = 100)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<ModelProcessingJob>(options);
        }

        public async ValueTask QueueBackgroundWorkItemAsync(ModelProcessingJob workItem)
        {
            await _queue.Writer.WriteAsync(workItem);
        }

        public async ValueTask<ModelProcessingJob> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
