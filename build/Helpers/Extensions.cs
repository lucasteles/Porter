using JetBrains.Annotations;

namespace Helpers;

public static class Extensions
{
    public static T LocalTool<T>(this T tool, string localtool = "")
        where T : ToolSettings =>
        tool
            .SetProcessToolPath(DotNetPath)
            .SetProcessArgumentConfigurator(args => new Arguments().Add(localtool).Concatenate(args));

    [CanBeNull]
    public static Project FindProjects(this Solution sln, string name) =>
        sln.AllProjects.SingleOrDefault(x => name.Equals(x.Name, StringComparison.Ordinal));

    public static ITargetDefinition TryExecutes(
        this ITargetDefinition @this,
        Action @try,
        Action @catch,
        Action @finally = null) =>
        @this.TryExecutes(@try, (_, _) =>
        {
            @catch();
            return false;
        }, @finally);

    public static ITargetDefinition TryExecutes(
        this ITargetDefinition @this,
        Action @try,
        Func<ProcessException, int, bool> @catch,
        Action @finally = null) =>
        @this.Executes(() =>
        {
            var (retry, retryCount) = (false, 0);
            do
            {
                try
                {
                    @try();
                }
                catch (ProcessException e)
                {
                    Log.Warning("Failure trying target: {Message}", e.Message);
                    retry = @catch(e, retryCount++);
                }
                finally
                {
                    @finally?.Invoke();
                }
            } while (retry);
        });
}