using System.Reflection;

using NetArchTest.Rules;

namespace Architecture.Tests;

/// <summary>
/// Enforces the Clean Architecture rules from CLAUDE.md for every module.
/// Do not change these tests to make a violation pass; change the code.
/// </summary>
public class ModuleLayerTests
{
    private static readonly string[] Modules = ["Conversations", "GnomeCatalog"];

    private static readonly string[] FrameworkNamespaces =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Wolverine",
        "Npgsql",

        // LLM provider SDKs belong in Infrastructure, behind our own ports.
        "Microsoft.Extensions.AI",
        "OpenAI",
        "OllamaSharp",
    ];

    public static TheoryData<string> ModuleNames() => new(Modules);

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Domain_does_not_depend_on_other_layers_or_frameworks(string module)
    {
        var forbidden = new[]
        {
            $"{module}.Application",
            $"{module}.Infrastructure",
            $"{module}.Contracts",
        }.Concat(FrameworkNamespaces).ToArray();

        AssertNoDependencies(Load($"{module}.Domain"), forbidden);
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Application_does_not_depend_on_infrastructure_or_frameworks(string module)
    {
        var forbidden = new[] { $"{module}.Infrastructure" }
            .Concat(FrameworkNamespaces)
            .ToArray();

        AssertNoDependencies(Load($"{module}.Application"), forbidden);
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Modules_only_reference_other_modules_through_contracts(string module)
    {
        var otherModuleInternals = Modules
            .Where(other => other != module)
            .SelectMany(other => new[]
            {
                $"{other}.Domain",
                $"{other}.Application",
                $"{other}.Infrastructure",
            })
            .ToArray();

        if (otherModuleInternals.Length == 0)
        {
            return;
        }

        foreach (var layer in new[] { "Domain", "Application", "Infrastructure" })
        {
            AssertNoDependencies(Load($"{module}.{layer}"), otherModuleInternals);
        }
    }

    [Fact]
    public void BuildingBlocks_domain_has_no_framework_dependencies()
    {
        AssertNoDependencies(Load("BuildingBlocks.Domain"), FrameworkNamespaces);
    }

    private static Assembly Load(string name) => Assembly.Load(name);

    private static void AssertNoDependencies(Assembly assembly, string[] forbidden)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        var offenders = result.FailingTypeNames ?? [];
        result.IsSuccessful.ShouldBeTrue(
            $"{assembly.GetName().Name} depends on a forbidden layer: {string.Join(", ", offenders)}");
    }
}
