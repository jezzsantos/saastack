using Common.Extensions;

namespace UnitTesting.Common;

public static class Solution
{
    private const string SolutionDirectoryName = "src";

    /// <summary>
    ///     Returns the relative path to the current solution directory, starting from
    ///     <see cref="Environment.CurrentDirectory" />
    /// </summary>
    public static string NavigateUpToSolutionDirectoryPath()
    {
        return NavigateUpToSolutionDirectoryPath(SolutionDirectoryName);
    }

    /// <summary>
    ///     Returns the relative path to the specified <see cref="SolutionDirectoryName" />, starting from
    ///     <see cref="Environment.CurrentDirectory" />
    /// </summary>
    private static string NavigateUpToSolutionDirectoryPath(string solutionDirectoryName)
    {
        var startingPath = Environment.CurrentDirectory;

        var starting = new DirectoryInfo(startingPath);
        if (starting.Name == solutionDirectoryName)
        {
            return starting.FullName;
        }

        var current = starting.FullName;
        var maxIterations = 20;
        while (maxIterations > 0)
        {
            maxIterations--;
            var parent = Directory.GetParent(current ?? string.Empty);
            if (parent?.Name == solutionDirectoryName)
            {
                return parent.FullName;
            }

            current = parent?.FullName;
        }

        throw new InvalidOperationException(
            Resources.Solution_NavigateUpToSolutionDirectoryPath_NoSolution.Format(solutionDirectoryName,
                startingPath));
    }
}