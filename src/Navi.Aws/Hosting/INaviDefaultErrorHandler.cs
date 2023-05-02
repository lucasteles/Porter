namespace Navi.Hosting;

public interface INaviErrorListener
{
    public Task OnError(Exception ex);
}

sealed class ErrorListener : INaviErrorListener
{
    readonly Func<Exception, Task> handler;

    public ErrorListener(Func<Exception, Task> handler) => this.handler = handler;
    public Task OnError(Exception ex) => handler?.Invoke(ex) ?? Task.CompletedTask;
}
