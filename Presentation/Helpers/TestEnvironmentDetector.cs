using System;
using System.Linq;

namespace InventoryERP.Presentation.Helpers
{
    internal static class TestEnvironmentDetector
    {
        private static bool? _isRunningUnderTest;

        public static bool IsRunningUnderTest
        {
            get
            {
                if (_isRunningUnderTest.HasValue)
                {
                    return _isRunningUnderTest.Value;
                }

                // Primary hint provided by dotnet test host
                var envFlag = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_TESTHOST");
                if (!string.IsNullOrWhiteSpace(envFlag) && envFlag.Equals("1", StringComparison.OrdinalIgnoreCase))
                {
                    _isRunningUnderTest = true;
                    return true;
                }

                // Fallback: scan loaded assemblies for well-known test frameworks
                var isTestAssemblyLoaded = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Select(a => a.GetName().Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Any(name =>
                        name!.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ||
                        name!.StartsWith("Microsoft.TestPlatform", StringComparison.OrdinalIgnoreCase) ||
                        name!.Equals("Microsoft.VisualStudio.TestPlatform.TestFramework", StringComparison.OrdinalIgnoreCase));

                _isRunningUnderTest = isTestAssemblyLoaded;
                return _isRunningUnderTest.Value;
            }
        }
    }
}

