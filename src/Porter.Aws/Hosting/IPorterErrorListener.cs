namespace Porter.Hosting;

public interface IPorterErrorListener
{
    public Task OnError(Exception ex);
}

sealed class ErrorListener : IPorterErrorListener
{
    readonly Func<Exception, Task> handler;

    public ErrorListener(Func<Exception, Task> handler) => this.handler = handler;
    public Task OnError(Exception ex) => handler?.Invoke(ex) ?? Task.CompletedTask;
}
