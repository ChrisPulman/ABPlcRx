// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using ABPlcRx.SourceGeneration;
using ABPlcRx.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Disposables;
using TUnit.Assertions;
using TUnit.Core;
using ReactiveAsyncContext = ReactiveUI.Primitives.Async.Reactive.AsyncContext;
using ReactiveIABPlcRx = ABPlcRx.Reactive.IABPlcRx;
using ReactiveLinqExtensions = ReactiveUI.Primitives.Reactive.LinqExtensions;
using ReactivePlcModelAttribute = ABPlcRx.Reactive.SourceGeneration.PlcModelAttribute;

namespace ABPlcRx.Tests;

/// <summary>Tests the PLC model source generator.</summary>
public sealed class SourceGeneratorTests
{
    /// <summary>Verifies generated models expose properties and observable streams.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task PlcModelGeneratorCreatesPropertiesAndObservableStreamsAsync()
    {
        const string source = """
            using ABPlcRx.SourceGeneration;

            namespace GeneratedSample;

            [PlcModel]
            [PlcTag(typeof(int), "Counter", "MyDINT")]
            [PlcTag(typeof(bool), "LightOn", "B3:3", Bit = 0)]
            [PlcTag(typeof(bool), "Ready", "MachineReady")]
            public partial class MachineTags
            {
            }
            """;

        var run = RunGenerator(source);
        await AssertNoErrorsAsync(run);
        var generatedSource = await GetGeneratedSourceAsync(run);

        await Assert.That(generatedSource).Contains("CounterObservable");
        await Assert.That(generatedSource).Contains("LightOnObservable");
        await Assert.That(generatedSource).Contains("LightOnObservableAsync");
        await Assert.That(generatedSource).Contains("ReadyObservable");
        await Assert.That(generatedSource).Contains("ReadyObservableAsync");
        await Assert.That(generatedSource).Contains("global::ReactiveUI.Primitives.Disposables.MultipleDisposable");
        await Assert.That(generatedSource).Contains("global::ReactiveUI.Primitives.Async.IObservableAsync<bool>");
        await Assert.That(generatedSource).Contains("global::ABPlcRx.ObservableAsyncBridgeExtensions.ToAsyncObservable");
        await Assert.That(generatedSource).Contains("controller.AddUpdateTagItem<short>(@\"LightOn\", @\"B3:3\", @\"Default\")");
        await Assert.That(generatedSource).Contains("controller.AddUpdateTagItem<bool>(@\"Ready\", @\"MachineReady\", @\"Default\")");
        await Assert.That(generatedSource).Contains("controller.Observe<bool>(@\"Ready\", -1)");
    }

    /// <summary>Verifies generated models bind to the reactive package namespace when reactive attributes are used.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task PlcModelGeneratorCreatesReactiveNamespaceModelsAsync()
    {
        const string source = """
            using ABPlcRx.Reactive.SourceGeneration;

            namespace GeneratedReactiveSample;

            [PlcModel]
            [PlcTag(typeof(int), "Counter", "MyDINT")]
            public partial class ReactiveMachineTags
            {
            }
            """;

        var run = RunGenerator(source, reactive: true);
        await AssertNoErrorsAsync(run);
        var generatedSource = await GetGeneratedSourceAsync(run);

        await Assert.That(generatedSource).Contains("using ReactiveUI.Primitives.Reactive;");
        await Assert.That(generatedSource).Contains("global::ABPlcRx.Reactive.IABPlcRx");
        await Assert.That(generatedSource).Contains("global::ABPlcRx.Reactive.ObservableAsyncBridgeExtensions.ToAsyncObservable");
        await Assert.That(generatedSource).Contains("controller.AddUpdateTagItem<int>(@\"Counter\", @\"MyDINT\", @\"Default\")");
    }

    /// <summary>Verifies non-partial PLC models produce the expected diagnostic.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task PlcModelGeneratorReportsDiagnosticForNonPartialModelsAsync()
    {
        const string source = """
            using ABPlcRx.SourceGeneration;

            namespace GeneratedSample;

            [PlcModel]
            [PlcTag(typeof(int), "Counter", "MyDINT")]
            public class MachineTags
            {
            }
            """;

        var run = RunGenerator(source);
        var diagnostic = run.Diagnostics.Single(diagnostic => diagnostic.Id == "ABPLCRXSG001");

        await Assert.That(diagnostic.Severity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostic.GetMessage()).Contains("MachineTags");
        await Assert.That(run.Driver.GetRunResult().GeneratedTrees.Length).IsEqualTo(0);
    }

    /// <summary>Verifies a model marker without tags does not emit source.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task PlcModelGeneratorSkipsModelsWithoutTagsAsync()
    {
        const string source = """
            using ABPlcRx.SourceGeneration;

            namespace GeneratedSample;

            [PlcModel]
            public partial class EmptyTags
            {
            }
            """;

        var run = RunGenerator(source);

        await AssertNoErrorsAsync(run);
        await Assert.That(run.Driver.GetRunResult().GeneratedTrees.Length).IsEqualTo(0);
    }

    /// <summary>Verifies unrelated attributed types are ignored.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task PlcModelGeneratorIgnoresUnrelatedAttributesAsync()
    {
        const string source = """
            namespace GeneratedSample;

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public sealed class OtherAttribute : System.Attribute
            {
            }

            [Other]
            public partial class IgnoredTags
            {
            }
            """;

        var run = RunGenerator(source);

        await AssertNoErrorsAsync(run);
        await Assert.That(run.Driver.GetRunResult().GeneratedTrees.Length).IsEqualTo(0);
    }

    /// <summary>Verifies invalid ABPlcRx tag attributes are ignored without source output.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task PlcModelGeneratorIgnoresInvalidTagAttributesAsync()
    {
        const string source = """
            using ABPlcRx.SourceGeneration;

            namespace GeneratedSample;

            [PlcModel]
            [PlcTag(typeof(int), "", "N7:0")]
            public partial class InvalidClassTags
            {
                [PlcTag("")]
                public int InvalidProperty { get; private set; }
            }
            """;

        var run = RunGenerator(source);

        await AssertNoErrorsAsync(run);
        await Assert.That(run.Driver.GetRunResult().GeneratedTrees.Length).IsEqualTo(0);
    }

    /// <summary>Verifies attributes with matching names but unsupported namespaces are ignored.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task PlcModelGeneratorIgnoresUnsupportedAttributeNamespacesAsync()
    {
        const string source = """
            namespace Other
            {
                [System.AttributeUsage(System.AttributeTargets.Class)]
                public sealed class PlcModelAttribute : System.Attribute
                {
                }
            }

            namespace External.SourceGeneration
            {
                [System.AttributeUsage(System.AttributeTargets.Class)]
                public sealed class PlcModelAttribute : System.Attribute
                {
                }
            }

            namespace GeneratedSample
            {
                [Other.PlcModel]
                [External.SourceGeneration.PlcModel]
                public partial class IgnoredTags
                {
                }
            }
            """;

        var run = RunGenerator(source);

        await AssertNoErrorsAsync(run);
        await Assert.That(run.Driver.GetRunResult().GeneratedTrees.Length).IsEqualTo(0);
    }

    /// <summary>Verifies property tags, custom settings, and identifier sanitization are emitted correctly.</summary>
    /// <returns><see cref="Task"/> representing the test.</returns>
    [Test]
    internal async Task PlcModelGeneratorEmitsPropertyTagsAndCustomSettingsAsync()
    {
        const string source = """
            using ABPlcRx.SourceGeneration;

            namespace GeneratedSample;

            [PlcModel]
            [PlcTag(typeof(int), "1 Bad-Name", "Quoted\"Tag", Variable = "PLC Counter", Group = "Fast")]
            public partial class MachineTags
            {
                [PlcTag("ExistingTag", Variable = "External Counter", Group = "Slow", RegisterTag = false)]
                public int? ExternalCounter { get; private set; }
            }
            """;

        var run = RunGenerator(source);
        await AssertNoErrorsAsync(run);
        var generatedSource = await GetGeneratedSourceAsync(run);

        await Assert.That(generatedSource).Contains("public int _1_Bad_Name");
        await Assert.That(generatedSource).Contains("controller.AddUpdateTagItem<int>(@\"PLC Counter\", @\"Quoted\"\"Tag\", @\"Fast\")");
        await Assert.That(generatedSource).DoesNotContain("controller.AddUpdateTagItem<int>(@\"External Counter\"");
        await Assert.That(generatedSource).Contains("controller.Observe<int>(@\"External Counter\", -1)");
        await Assert.That(generatedSource).Contains("_externalCounterObservable");
    }

    /// <summary>Runs the source generator for a source snippet.</summary>
    /// <param name="source">The source text.</param>
    /// <param name="reactive">True to reference the reactive runtime surface.</param>
    /// <returns>The generator run result.</returns>
    private static GeneratorRun RunGenerator(string source, bool reactive = false)
    {
        var compilation = CreateCompilation(source, reactive);
        var generator = new PlcModelGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        return new(driver, outputCompilation, diagnostics);
    }

    /// <summary>Asserts that a generator run produced no compile or generator errors.</summary>
    /// <param name="run">The generator run.</param>
    /// <returns><see cref="Task"/> representing the assertion.</returns>
    private static async Task AssertNoErrorsAsync(GeneratorRun run)
    {
        var generatorErrors = string.Join(Environment.NewLine, run.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var outputErrors = string.Join(Environment.NewLine, run.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        await Assert.That(generatorErrors).IsEqualTo(string.Empty);
        await Assert.That(outputErrors).IsEqualTo(string.Empty);
    }

    /// <summary>Gets the generated source from a successful run.</summary>
    /// <param name="run">The generator run.</param>
    /// <returns>The generated source.</returns>
    private static async Task<string> GetGeneratedSourceAsync(GeneratorRun run)
    {
        var generatedTree = run.Driver
            .GetRunResult()
            .GeneratedTrees
            .Single(tree => tree.FilePath.EndsWith(".ABPlcRx.g.cs", StringComparison.Ordinal));

        return (await generatedTree.GetTextAsync()).ToString();
    }

    /// <summary>Creates a compilation for source generator tests.</summary>
    /// <param name="source">The source text.</param>
    /// <param name="reactive">True to add explicit references to the reactive runtime surface.</param>
    /// <returns>The created compilation.</returns>
    private static CSharpCompilation CreateCompilation(string source, bool reactive)
    {
        var references = GetFrameworkReferences()
            .Concat(
            [
                MetadataReference.CreateFromFile(typeof(PlcModelAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IABPlcRx).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(RxVoid).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(LinqExtensions).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MultipleDisposable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IObservableAsync<>).Assembly.Location),
            ])
            .Concat(reactive
                ? [
                    MetadataReference.CreateFromFile(typeof(ReactivePlcModelAttribute).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(ReactiveIABPlcRx).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(ReactiveLinqExtensions).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(ReactiveAsyncContext).Assembly.Location),
                ]
                : []);

        return CSharpCompilation.Create(
            "GeneratedSample",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>Gets trusted platform assembly references for Roslyn compilation.</summary>
    /// <returns>The metadata references.</returns>
    private static IEnumerable<MetadataReference> GetFrameworkReferences()
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        var frameworkReferences = string.IsNullOrWhiteSpace(trustedPlatformAssemblies)
            ? []
            : trustedPlatformAssemblies
                .Split(Path.PathSeparator)
                .Where(File.Exists)
                .Select(CreateMetadataReference);

        return frameworkReferences.Concat(GetOutputAssemblyReferences());
    }

    /// <summary>Creates a metadata reference from an assembly path.</summary>
    /// <param name="path">The assembly path.</param>
    /// <returns>The metadata reference.</returns>
    private static MetadataReference CreateMetadataReference(string path) => MetadataReference.CreateFromFile(path);

    /// <summary>Gets copied package assemblies from the test output folder.</summary>
    /// <returns>The metadata references.</returns>
    private static IEnumerable<MetadataReference> GetOutputAssemblyReferences() =>
        Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll")
            .Where(File.Exists)
            .Select(CreateMetadataReference);

    /// <summary>Stores a generator execution result.</summary>
    /// <param name="Driver">The generator driver.</param>
    /// <param name="OutputCompilation">The output compilation.</param>
    /// <param name="Diagnostics">The generator diagnostics.</param>
    private sealed record GeneratorRun(GeneratorDriver Driver, Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics);
}
