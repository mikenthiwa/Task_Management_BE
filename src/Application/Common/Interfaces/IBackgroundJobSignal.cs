namespace Application.Common.Interfaces;

public interface IBackgroundJobSignal
{
    Task WaitAsync(CancellationToken cancellationToken);
    void Signal();
}
