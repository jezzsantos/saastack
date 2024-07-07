extern alias PlatformAnalyzers;
using Xunit;
using MissingDocsAnalyzer = PlatformAnalyzers::Tools.Analyzers.Platform.MissingDocsAnalyzer;
using UsedImplicitly = PlatformAnalyzers::JetBrains.Annotations.UsedImplicitlyAttribute;

namespace Tools.Analyzers.Platform.UnitTests;

[UsedImplicitly]
public class MissingDocsAnalyzerSpec
{
    [Trait("Category", "Unit.Tooling")]
    public class GivenRule001
    {
        [Fact]
        public async Task WhenInJetbrainsAnnotationsNamespace_ThenNoAlert()
        {
            const string input = @"
namespace JetBrains.Annotations;
public class AClass
{
}";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }

        [Fact]
        public async Task WhenInApiHost1Namespace_ThenNoAlert()
        {
            const string input = @"
namespace ApiHost1;
public class AClass
{
}";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }

        [Fact]
        public async Task WhenPublicDelegate_ThenAlerts()
        {
            const string input = @"public delegate void ADelegate();";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 1,
                22, "ADelegate");
        }

        [Fact]
        public async Task WhenInternalDelegate_ThenAlerts()
        {
            const string input = @"internal delegate void ADelegate();";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 1,
                24, "ADelegate");
        }

        [Fact]
        public async Task WhenPublicInterface_ThenAlerts()
        {
            const string input = @"
public interface AnInterface
{
}";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 2,
                18, "AnInterface");
        }

        [Fact]
        public async Task WhenInternalInterface_ThenAlerts()
        {
            const string input = @"
internal interface AnInterface
{
}";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 2,
                20, "AnInterface");
        }

        [Fact]
        public async Task WhenPublicEnum_ThenAlerts()
        {
            const string input = @"
public enum AnEnum
{
}";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 2,
                13, "AnEnum");
        }

        [Fact]
        public async Task WhenInternalEnum_ThenAlerts()
        {
            const string input = @"
internal enum AnEnum
{
}";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 2,
                15, "AnEnum");
        }

        [Fact]
        public async Task WhenPublicStruct_ThenAlerts()
        {
            const string input = @"
public struct AStruct
{
}";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 2,
                15, "AStruct");
        }

        [Fact]
        public async Task WhenInternalStruct_ThenAlerts()
        {
            const string input = @"
internal struct AStruct
{
}";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 2,
                17, "AStruct");
        }

        [Fact]
        public async Task WhenPublicReadOnlyStruct_ThenAlerts()
        {
            const string input = @"
public readonly struct AStruct
{
}";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 2,
                24, "AStruct");
        }

        [Fact]
        public async Task WhenInternalReadOnlyStruct_ThenAlerts()
        {
            const string input = @"
internal readonly struct AStruct
{
}";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 2,
                26, "AStruct");
        }

        [Fact]
        public async Task WhenPublicRecord_ThenAlerts()
        {
            const string input = @"
public record ARecord()
{
}";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 2,
                15, "ARecord");
        }

        [Fact]
        public async Task WhenInternalRecord_ThenAlerts()
        {
            const string input = @"
internal record ARecord
{
}";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 2,
                17, "ARecord");
        }

        [Fact]
        public async Task WhenPublicStaticClass_ThenNoAlert()
        {
            const string input = @"
public static class AClass
{
}
";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }

        [Fact]
        public async Task WhenInternalStaticClass_ThenNoAlert()
        {
            const string input = @"
internal static class AClass
{
}
";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }

        [Fact]
        public async Task WhenNestedPublicStaticClass_ThenNoAlert()
        {
            const string input = @"
public static class AClass1
{
    public static class AClass2
    {
    }
}
";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }

        [Fact]
        public async Task WhenNestedPrivateStaticClass_ThenNoAlert()
        {
            const string input = @"
/// <summary>
/// avalue
/// </summary>
public static class AClass1
{
    private static class AClass2
    {
    }
}
";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }

        [Fact]
        public async Task WhenNestedPublicInstanceClass_ThenAlerts()
        {
            const string input = @"
/// <summary>
/// avalue
/// </summary>
public class AClass1
{
    public class AClass2
    {
    }
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 7,
                18, "AClass2");
        }

        [Fact]
        public async Task WhenNestedPrivateInstanceClass_ThenNoAlert()
        {
            const string input = @"
/// <summary>
/// avalue
/// </summary>
public class AClass1
{
    private class AClass2
    {
    }
}
";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }

        [Fact]
        public async Task WhenPublicClassNoSummary_ThenAlerts()
        {
            const string input = @"
public class AClass
{
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 2,
                14, "AClass");
        }

        [Fact]
        public async Task WhenPublicPartialClassAloneNoSummary_ThenAlerts()
        {
            const string input = @"
public partial class AClass
{
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 2,
                22, "AClass");
        }

        [Fact]
        public async Task WhenPublicPartialClassesNoSummary_ThenAlerts()
        {
            const string input = @"
public partial class AClass
{
}
public partial class AClass
{
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(input,
                (MissingDocsAnalyzer.Rule001, 2, 22, "AClass"),
                (MissingDocsAnalyzer.Rule001, 5, 22, "AClass"));
        }

        [Fact]
        public async Task WhenInternalClassNoSummary_ThenAlerts()
        {
            const string input = @"
internal class AClass
{
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 2,
                16, "AClass");
        }

        [Fact]
        public async Task WhenPublicClassHasEmptyLine_ThenAlerts()
        {
            const string input = @"
public class AClass
{
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 2,
                14, "AClass");
        }

        [Fact]
        public async Task WhenPublicClassHasBlankLine_ThenAlerts()
        {
            const string input = @"

public class AClass
{
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 3,
                14, "AClass");
        }

        [Fact]
        public async Task WhenPublicClassHasEmptyOtherTag_ThenAlerts()
        {
            const string input = @"
/// <returns>
///
/// </returns>
public class AClass
{
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 5,
                14, "AClass");
        }

        [Fact]
        public async Task WhenPublicClassHasSomeOtherTag_ThenAlerts()
        {
            const string input = @"
/// <returns>
/// avalue
/// </returns>
public class AClass
{
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 5,
                14, "AClass");
        }

        [Fact]
        public async Task WhenPublicClassHasEmptySummary_ThenAlerts()
        {
            const string input = @"
/// <summary>
///
/// </summary>
public class AClass
{
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 5,
                14, "AClass");
        }

        [Fact]
        public async Task WhenPublicClassHasWhitespaceSummaryComment_ThenAlerts()
        {
            const string input = @"
/// <summary>
///   
/// </summary>
public class AClass
{
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule001, input, 5,
                14, "AClass");
        }

        [Fact]
        public async Task WhenPublicClassHasAnInheritDocComment_ThenNoAlert()
        {
            const string input = @"
/// <inheritdoc/>
public class AClass
{
}
";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }

        [Fact]
        public async Task WhenPublicClassHasASummaryComment_ThenNoAlert()
        {
            const string input = @"
/// <summary>
/// avalue
/// </summary>
public class AClass
{
}
";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }

        [Fact]
        public async Task WhenPublicPartialAloneClassHasASummaryComment_ThenNoAlert()
        {
            const string input = @"
/// <summary>
/// avalue
/// </summary>
public partial class AClass
{
}
";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }

        [Fact]
        public async Task WhenPublicPartialClassesAndAtLeastOneHasASummaryComment_ThenNoAlert()
        {
            const string input = @"
/// <summary>
/// avalue
/// </summary>
public partial class AClass
{
}
public partial class AClass
{
}
";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }
    }

    [Trait("Category", "Unit.Tooling")]
    public class GivenRule002
    {
        [Fact]
        public async Task WhenInJetbrainsAnnotationsNamespace_ThenNoAlert()
        {
            const string input = @"namespace JetBrains.Annotations;
public static class AClass
{
    public static void AMethod(){}
}";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }

        [Fact]
        public async Task WhenInApiHost1Namespace_ThenNoAlert()
        {
            const string input = @"namespace ApiHost1;
public static class AClass
{
    public static void AMethod(){}
}";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }

        [Fact]
        public async Task WhenInsideInternalStaticClass_ThenNoAlert()
        {
            const string input = @"internal static class AClass
{
    public static void AMethod1(){}
    public static void AMethod2(this string value){}
}";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }

        [Fact]
        public async Task WhenPublicStaticMethodOfStaticClass_ThenAlerts()
        {
            const string input = @"
public static class AClass
{
    public static void AMethod(){}
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule002, input, 4,
                24, "AMethod");
        }

        [Fact]
        public async Task WhenPublicStaticMethodOfInstanceClass_ThenNoAlert()
        {
            const string input = @"
/// <summary>
/// avalue
/// <summary>
public class AClass
{
    public static void AMethod(){}
}
";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }

        [Fact]
        public async Task WhenInternalStaticMethod_ThenAlerts()
        {
            const string input = @"
public static class AClass
{
    internal static void AMethod(){}
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule002, input, 4,
                26, "AMethod");
        }

        [Fact]
        public async Task WhenPublicStaticMethodWithParams_ThenAlerts()
        {
            const string input = @"
public static class AClass
{
    public static void AMethod(string value){}
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule002, input, 4,
                24, "AMethod");
        }

        [Fact]
        public async Task WhenInternalStaticMethodWithParams_ThenAlerts()
        {
            const string input = @"
public static class AClass
{
    internal static void AMethod(string value){}
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule002, input, 4,
                26, "AMethod");
        }

        [Fact]
        public async Task WhenInternalExtension_ThenAlerts()
        {
            const string input = @"
public static class AClass
{
    internal static void AMethod(this string value){}
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule002, input, 4,
                26, "AMethod");
        }

        [Fact]
        public async Task WhenPrivateExtension_ThenNoAlerts()
        {
            const string input = @"
public static class AClass
{
    private static void AMethod(this string value){}
}
";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }

        [Fact]
        public async Task WhenPublicExtensionHasNoSummary_ThenAlerts()
        {
            const string input = @"
public static class AClass
{
    public static void AMethod(this string value){}
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule002, input, 4,
                24, "AMethod");
        }

        [Fact]
        public async Task WhenPublicExtensionHasEmptySummary_ThenAlerts()
        {
            const string input = @"
public static class AClass
{
    /// <summary>
    /// 
    /// </summary>
    public static void AMethod(this string value){}
}
";

            await Verify.DiagnosticExists<MissingDocsAnalyzer>(MissingDocsAnalyzer.Rule002, input, 7,
                24, "AMethod");
        }

        [Fact]
        public async Task WhenPublicExtensionHasASummaryComment_ThenNoAlert()
        {
            const string input = @"
public static class AClass
{
    /// <summary>
    /// avalue
    /// </summary>
    public static void AMethod(this string value){}
}
";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }

        [Fact]
        public async Task WhenPublicExtensionHasAnInheritDocComment_ThenNoAlert()
        {
            const string input = @"
public static class AClass
{
    /// <inheritdoc/>
    public static void AMethod(this string value){}
}
";

            await Verify.NoDiagnosticExists<MissingDocsAnalyzer>(input);
        }
    }
}