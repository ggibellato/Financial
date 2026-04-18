using System;
using System.IO;

namespace Financial.Api;

internal static class PathResolver
{
    public static string? ResolveContentRootPath(string contentRootPath, string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
    }
}
