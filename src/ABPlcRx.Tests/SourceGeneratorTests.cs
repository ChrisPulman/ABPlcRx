// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ABPlcRx.SourceGeneration;
using ABPlcRx.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ReactiveUI.Extensions.Async;
using TUnit.Core;

namespace ABPlcRx.Tests;

public sealed class SourceGeneratorTests
{
    [Test]
    public async Task PlcModelGeneratorCreatesPropertiesAndObservableStreams()
    {
        const string source = """
            using ABPlcRx.SourceGeneration;

            namespace GeneratedSample;

            [PlcModel]
            [PlcTag(typeof(int), "Counter", "MyDINT")]
            [PlcTag(typeof(bool), "LightOn", "B3:3", Bit = 0)]
            public partial class MachineTags
            {
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new PlcModelGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        NoErrors(diagnostics);
        NoErrors(outputCompilation.GetDiagnostics());

        var generatedSource = driver
            .GetRunResult()
            .GeneratedTrees
            .Single(tree => tree.FilePath.EndsWith(".ABPlcRx.g.cs", StringComparison.Ordinal))
            .GetText()
            .ToString();

        Contains("CounterObservable", generatedSource);
        Contains("LightOnObservable", generatedSource);
        Contains("LightOnObservableAsync", generatedSource);
        Contains("controller.AddUpdateTagItem<short>(@\"LightOn\", @\"B3:3\", @\"Default\")", generatedSource);

        await Task.CompletedTask;
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var references = GetFrameworkReferences()
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(PlcModelAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IABPlcRx).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Reactive.Linq.Observable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IObservableAsync<>).Assembly.Location),
            });

        return CSharpCompilation.Create(
            "GeneratedSample",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static IEnumerable<MetadataReference> GetFrameworkReferences()
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
        {
            return [];
        }

        return trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Where(File.Exists)
            .Select(path => MetadataReference.CreateFromFile(path));
    }

    private static void NoErrors(IEnumerable<Diagnostic> diagnostics)
    {
        var errors = diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length != 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));
        }
    }

    private static void Contains(string expected, string actual)
    {
        if (!actual.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected generated source to contain '{expected}'.");
        }
    }
}
