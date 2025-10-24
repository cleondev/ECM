using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ocr.Messaging;

public interface IKafkaConsumer
{
    Task ConsumeAsync(string topic, Func<KafkaMessage, CancellationToken, Task> handler, CancellationToken cancellationToken = default);
}
