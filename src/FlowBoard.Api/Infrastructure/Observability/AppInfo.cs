using System.Reflection;

namespace FlowBoard.Api.Infrastructure.Observability;

/// <summary>Versão da aplicação gravada no binário (dotnet publish -p:Version=X.Y.Z no pipeline).</summary>
public static class AppInfo
{
    public static string Version { get; } =
        typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "unknown";
}
