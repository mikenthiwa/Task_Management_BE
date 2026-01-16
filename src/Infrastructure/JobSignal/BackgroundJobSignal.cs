using Application.Common.Interfaces;

namespace Infrastructure.JobSignal;

public class BackgroundJobSignal : IBackgroundJobSignal
{
    private readonly SemaphoreSlim _signal = new(0);
    public Task WaitAsync(TimeSpan timeSpan, CancellationToken cancellationToken) => _signal.WaitAsync(cancellationToken);
    public void Signal() => _signal.Release();
}
