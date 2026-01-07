namespace Application.Common.Interfaces;

public interface IBackgroundJobSignal
{
    Task WaitAsync(TimeSpan timeSpan, CancellationToken cancellationToken);
    void Signal();
}
