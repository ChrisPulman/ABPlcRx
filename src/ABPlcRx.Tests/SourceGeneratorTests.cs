// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ABPlcRx.SourceGeneration;
using ABPlcRx.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Disposables;
using TUnit.Assertions;
using TUnit.Core;

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

        var compilation = CreateCompilation(source);
        var generator = new PlcModelGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsFalse();
        await Assert.That(outputCompilation.GetDiagnostics().Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsFalse();

        var generatedTree = driver
            .GetRunResult()
            .GeneratedTrees
            .Single(tree => tree.FilePath.EndsWith(".ABPlcRx.g.cs", StringComparison.Ordinal));
        var generatedSource = (await generatedTree.GetTextAsync()).ToString();

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

    /// <summary>Creates a compilation for source generator tests.</summary>
    /// <param name="source">The source text.</param>
    /// <returns>The created compilation.</returns>
    private static CSharpCompilation CreateCompilation(string source)
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
            ]);

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
        return string.IsNullOrWhiteSpace(trustedPlatformAssemblies)
            ? []
            : trustedPlatformAssemblies
                .Split(Path.PathSeparator)
                .Where(File.Exists)
                .Select(CreateMetadataReference);
    }

    /// <summary>Creates a metadata reference from an assembly path.</summary>
    /// <param name="path">The assembly path.</param>
    /// <returns>The metadata reference.</returns>
    private static MetadataReference CreateMetadataReference(string path) => MetadataReference.CreateFromFile(path);
}
