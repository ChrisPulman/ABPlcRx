// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ABPlcRx.SourceGenerators;

/// <summary>Generates reactive PLC stream models from ABPlcRx source generation attributes.</summary>
[Generator]
public sealed class PlcModelGenerator : IIncrementalGenerator
{
    /// <summary>The fully qualified PLC model attribute metadata name.</summary>
    private const string PlcModelAttributeName = "ABPlcRx.SourceGeneration.PlcModelAttribute";

    /// <summary>The fully qualified PLC tag attribute metadata name.</summary>
    private const string PlcTagAttributeName = "ABPlcRx.SourceGeneration.PlcTagAttribute";

    /// <summary>The diagnostic descriptor used when a model type is not partial.</summary>
    private static readonly DiagnosticDescriptor PartialRequiredDescriptor = new(
        "ABPLCRXSG001",
        "PLC stream model must be partial",
        "Type '{0}' must be partial to generate ABPlcRx stream members",
        "ABPlcRx.SourceGenerators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var generationResults = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => IsCandidate(node),
                CreateGenerationResult)
            .Where(static result => result is not null)
            .Select(static (result, _) => result.GetValueOrDefault());

        context.RegisterSourceOutput(generationResults, EmitGenerationResult);
    }

    /// <summary>Checks whether a syntax node can contain PLC source-generation attributes.</summary>
    /// <param name="node">The syntax node.</param>
    /// <returns>True when the node should receive semantic inspection.</returns>
    private static bool IsCandidate(SyntaxNode node) =>
        node is TypeDeclarationSyntax { AttributeLists.Count: > 0 };

    /// <summary>Creates a generated source result or diagnostic from one candidate declaration.</summary>
    /// <param name="context">The generator syntax context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generation result, or null when the declaration is not an ABPlcRx model.</returns>
    private static GenerationResult? CreateGenerationResult(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var declaration = (TypeDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        var tags = CollectTags(typeSymbol);
        if (tags.Length == 0 && !HasAttribute(typeSymbol.GetAttributes(), PlcModelAttributeName))
        {
            return null;
        }

        if (!IsPartial(declaration))
        {
            return GenerationResult.FromDiagnostic(CreatePartialRequiredDiagnostic(declaration, typeSymbol));
        }

        if (tags.Length == 0)
        {
            return null;
        }

        var hintName = $"{GetSafeHintName(typeSymbol)}.ABPlcRx.g.cs";
        return GenerationResult.FromSource(hintName, GenerateModel(typeSymbol, tags));
    }

    /// <summary>Emits generated source or reports a candidate diagnostic.</summary>
    /// <param name="context">The source production context.</param>
    /// <param name="result">The generation result.</param>
    private static void EmitGenerationResult(SourceProductionContext context, GenerationResult result)
    {
        if (result.Diagnostic is { } diagnostic)
        {
            context.ReportDiagnostic(diagnostic);
            return;
        }

        context.AddSource(result.HintName!, SourceText.From(result.Source!, Encoding.UTF8));
    }

    /// <summary>Collects PLC tag metadata from class and property attributes.</summary>
    /// <param name="typeSymbol">The candidate type symbol.</param>
    /// <returns>The collected tag models.</returns>
    private static ImmutableArray<TagModel> CollectTags(INamedTypeSymbol typeSymbol)
    {
        var builder = ImmutableArray.CreateBuilder<TagModel>();

        foreach (var attribute in typeSymbol.GetAttributes().Where(static x => IsAttribute(x, PlcTagAttributeName)))
        {
            if (TryCreateClassTag(attribute, out var tag))
            {
                builder.Add(tag);
            }
        }

        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            foreach (var attribute in property.GetAttributes().Where(static x => IsAttribute(x, PlcTagAttributeName)))
            {
                if (TryCreatePropertyTag(property, attribute, out var tag))
                {
                    builder.Add(tag);
                }
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>Creates a tag model from a class-level tag attribute.</summary>
    /// <param name="attribute">The attribute data.</param>
    /// <param name="tag">The created tag model.</param>
    /// <returns>True when the attribute contains a valid tag model.</returns>
    private static bool TryCreateClassTag(AttributeData attribute, out TagModel tag)
    {
        tag = default;
        if (attribute.ConstructorArguments.Length != 3 ||
            attribute.ConstructorArguments[0].Value is not ITypeSymbol valueType ||
            attribute.ConstructorArguments[1].Value is not string propertyName ||
            attribute.ConstructorArguments[2].Value is not string tagName ||
            string.IsNullOrWhiteSpace(propertyName) ||
            string.IsNullOrWhiteSpace(tagName))
        {
            return false;
        }

        var settings = ReadSettings(attribute, propertyName);
        tag = new(
            propertyName,
            settings.Variable,
            tagName,
            settings.Group,
            GetObserveType(valueType),
            GetObserveType(valueType),
            GetRegisterType(valueType, settings.Bit),
            settings.Bit,
            settings.RegisterTag,
            generateProperty: true,
            requiresValueGetOrDefault: false);
        return true;
    }

    /// <summary>Creates a tag model from a property-level tag attribute.</summary>
    /// <param name="property">The attributed property.</param>
    /// <param name="attribute">The attribute data.</param>
    /// <param name="tag">The created tag model.</param>
    /// <returns>True when the attribute contains a valid tag model.</returns>
    private static bool TryCreatePropertyTag(IPropertySymbol property, AttributeData attribute, out TagModel tag)
    {
        tag = default;
        if (attribute.ConstructorArguments.Length == 0 ||
            attribute.ConstructorArguments[0].Value is not string tagName ||
            string.IsNullOrWhiteSpace(tagName))
        {
            return false;
        }

        var settings = ReadSettings(attribute, property.Name);
        var valueType = GetNullableUnderlyingType(property.Type) ?? property.Type;
        tag = new(
            property.Name,
            settings.Variable,
            tagName,
            settings.Group,
            GetObserveType(valueType),
            property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            GetRegisterType(valueType, settings.Bit),
            settings.Bit,
            settings.RegisterTag,
            generateProperty: false,
            requiresValueGetOrDefault: false);
        return true;
    }

    /// <summary>Reads optional tag settings from named attribute arguments.</summary>
    /// <param name="attribute">The attribute data.</param>
    /// <param name="propertyName">The fallback property name.</param>
    /// <returns>The resolved tag settings.</returns>
    private static TagSettings ReadSettings(AttributeData attribute, string propertyName)
    {
        var variable = propertyName;
        var group = "Default";
        var bit = -1;
        var registerTag = true;

        foreach (var namedArgument in attribute.NamedArguments)
        {
            switch (namedArgument.Key)
            {
                case "Variable" when namedArgument.Value.Value is string value && !string.IsNullOrWhiteSpace(value):
                {
                    variable = value;
                    break;
                }

                case "Group" when namedArgument.Value.Value is string value && !string.IsNullOrWhiteSpace(value):
                {
                    group = value;
                    break;
                }

                case "Bit" when namedArgument.Value.Value is int value:
                {
                    bit = value;
                    break;
                }

                case "RegisterTag" when namedArgument.Value.Value is bool value:
                {
                    registerTag = value;
                    break;
                }
            }
        }

        return new(variable, group, bit, registerTag);
    }

    /// <summary>Generates the partial model source for collected tags.</summary>
    /// <param name="typeSymbol">The target type symbol.</param>
    /// <param name="tags">The tags to generate.</param>
    /// <returns>The generated source text.</returns>
    private static string GenerateModel(INamedTypeSymbol typeSymbol, ImmutableArray<TagModel> tags)
    {
        var builder = new StringBuilder();
        var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace ? null : typeSymbol.ContainingNamespace.ToDisplayString();

        _ = builder.AppendLine("// <auto-generated />");
        _ = builder.AppendLine("#nullable enable");
        _ = builder.AppendLine("using System;");
        _ = builder.AppendLine("using ReactiveUI.Primitives;");
        _ = builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            _ = builder.Append("namespace ").Append(namespaceName).AppendLine(";");
            _ = builder.AppendLine();
        }

        _ = builder.Append("partial class ").AppendLine(typeSymbol.Name);
        _ = builder.AppendLine("{");
        _ = builder.AppendLine("    private readonly global::ReactiveUI.Primitives.Disposables.MultipleDisposable _abPlcRxSubscriptions = new();");
        _ = builder.AppendLine("    private global::ABPlcRx.IABPlcRx? _abPlcRxController;");
        _ = builder.AppendLine();

        foreach (var tag in tags)
        {
            AppendTagMembers(builder, tag);
        }

        AppendAttachMethod(builder, tags);
        AppendDetachMethod(builder, tags);

        _ = builder.AppendLine("}");
        return builder.ToString();
    }

    /// <summary>Appends properties and observable accessors for one tag.</summary>
    /// <param name="builder">The target source builder.</param>
    /// <param name="tag">The tag model.</param>
    private static void AppendTagMembers(StringBuilder builder, TagModel tag)
    {
        var fieldName = "_" + ToCamelCase(SanitizeIdentifier(tag.PropertyName));
        var observableFieldName = fieldName + "Observable";

        _ = builder.Append("    private global::System.IObservable<").Append(tag.ObserveType).Append(">? ").Append(observableFieldName).AppendLine(";");

        if (tag.GenerateProperty)
        {
            _ = builder.Append("    private ").Append(tag.PropertyType).Append(' ').Append(fieldName).AppendLine(";");
            _ = builder.AppendLine();
            _ = builder.Append("    public ").Append(tag.PropertyType).Append(' ').AppendLine(tag.PropertyName);
            _ = builder.AppendLine("    {");
            _ = builder.Append("        get => ").Append(fieldName).AppendLine(";");
            _ = builder.Append("        private set => ").Append(fieldName).AppendLine(" = value;");
            _ = builder.AppendLine("    }");
        }

        _ = builder.AppendLine();
        _ = builder.Append("    public global::System.IObservable<").Append(tag.ObserveType).Append("> ").Append(tag.PropertyName).AppendLine("Observable =>");
        _ = builder.Append("        ").Append(observableFieldName).Append(" ?? throw new global::System.InvalidOperationException(\"Call ")
            .Append("AttachPlcStreams").AppendLine(" before reading generated PLC observables.\");");
        _ = builder.AppendLine();
        _ = builder.AppendLine("#if NET8_0_OR_GREATER");
        _ = builder.Append("    public global::ReactiveUI.Primitives.Async.IObservableAsync<").Append(tag.ObserveType).Append("> ")
            .Append(tag.PropertyName).AppendLine("ObservableAsync =>");
        _ = builder.Append("        global::ABPlcRx.ObservableAsyncBridgeExtensions.ToAsyncObservable(")
            .Append(tag.PropertyName).AppendLine("Observable);");
        _ = builder.AppendLine("#endif");
        _ = builder.AppendLine();
    }

    /// <summary>Appends the AttachPlcStreams method.</summary>
    /// <param name="builder">The target source builder.</param>
    /// <param name="tags">The generated tags.</param>
    private static void AppendAttachMethod(StringBuilder builder, ImmutableArray<TagModel> tags)
    {
        _ = builder.AppendLine("    public global::System.IDisposable AttachPlcStreams(global::ABPlcRx.IABPlcRx controller)");
        _ = builder.AppendLine("    {");
        _ = builder.AppendLine("        if (controller is null)");
        _ = builder.AppendLine("        {");
        _ = builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(controller));");
        _ = builder.AppendLine("        }");
        _ = builder.AppendLine();
        _ = builder.AppendLine("        DetachPlcStreams();");
        _ = builder.AppendLine("        _abPlcRxController = controller;");
        _ = builder.AppendLine();

        foreach (var tag in tags)
        {
            var observableFieldName = "_" + ToCamelCase(SanitizeIdentifier(tag.PropertyName)) + "Observable";
            if (tag.RegisterTag)
            {
                _ = builder.Append("        controller.AddUpdateTagItem<").Append(tag.RegisterType).Append(">(")
                    .Append(ToLiteral(tag.Variable)).Append(", ")
                    .Append(ToLiteral(tag.TagName)).Append(", ")
                    .Append(ToLiteral(tag.Group)).AppendLine(");");
            }

            _ = builder.Append("        ").Append(observableFieldName).Append(" = controller.Observe<").Append(tag.ObserveType).Append(">(")
                .Append(ToLiteral(tag.Variable)).Append(", ").Append(tag.Bit.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .AppendLine(").Publish().RefCount();");
            _ = builder.Append("        _abPlcRxSubscriptions.Add(").Append(observableFieldName).Append(".Subscribe(value => ");
            if (tag.RequiresValueGetOrDefault)
            {
                _ = builder.Append(tag.PropertyName).AppendLine(" = value.GetValueOrDefault()));");
            }
            else
            {
                _ = builder.Append(tag.PropertyName).AppendLine(" = value));");
            }
        }

        _ = builder.AppendLine();
        _ = builder.AppendLine("        return new global::ReactiveUI.Primitives.Disposables.ActionDisposable(DetachPlcStreams);");
        _ = builder.AppendLine("    }");
        _ = builder.AppendLine();
    }

    /// <summary>Appends the DetachPlcStreams method.</summary>
    /// <param name="builder">The target source builder.</param>
    /// <param name="tags">The generated tags.</param>
    private static void AppendDetachMethod(StringBuilder builder, ImmutableArray<TagModel> tags)
    {
        _ = builder.AppendLine("    public void DetachPlcStreams()");
        _ = builder.AppendLine("    {");
        _ = builder.AppendLine("        _abPlcRxSubscriptions.Clear();");
        _ = builder.AppendLine("        _abPlcRxController = null;");

        foreach (var tag in tags)
        {
            _ = builder.Append("        _").Append(ToCamelCase(SanitizeIdentifier(tag.PropertyName))).AppendLine("Observable = null;");
        }

        _ = builder.AppendLine("    }");
    }

    /// <summary>Checks whether an attribute collection contains a metadata name.</summary>
    /// <param name="attributes">The attribute collection.</param>
    /// <param name="metadataName">The metadata name.</param>
    /// <returns>True when the metadata name is present.</returns>
    private static bool HasAttribute(ImmutableArray<AttributeData> attributes, string metadataName) =>
        attributes.Any(attribute => IsAttribute(attribute, metadataName));

    /// <summary>Checks whether an attribute matches a metadata name.</summary>
    /// <param name="attribute">The attribute data.</param>
    /// <param name="metadataName">The metadata name.</param>
    /// <returns>True when the attribute matches.</returns>
    private static bool IsAttribute(AttributeData attribute, string metadataName) =>
        attribute.AttributeClass?.ToDisplayString() == metadataName;

    /// <summary>Checks whether a type declaration is partial.</summary>
    /// <param name="declaration">The declaration.</param>
    /// <returns>True when the declaration is partial.</returns>
    private static bool IsPartial(TypeDeclarationSyntax declaration) =>
        declaration.Modifiers.Any(SyntaxKind.PartialKeyword);

    /// <summary>Creates the diagnostic for non-partial PLC models.</summary>
    /// <param name="declaration">The declaration.</param>
    /// <param name="typeSymbol">The type symbol.</param>
    /// <returns>The diagnostic instance.</returns>
    private static Diagnostic CreatePartialRequiredDiagnostic(TypeDeclarationSyntax declaration, INamedTypeSymbol typeSymbol) =>
        Diagnostic.Create(PartialRequiredDescriptor, declaration.Identifier.GetLocation(), typeSymbol.Name);

    /// <summary>Gets the underlying type for nullable value types.</summary>
    /// <param name="type">The type symbol.</param>
    /// <returns>The nullable underlying type, or null.</returns>
    private static ITypeSymbol? GetNullableUnderlyingType(ITypeSymbol type) =>
        type is INamedTypeSymbol namedType &&
        namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T
            ? namedType.TypeArguments[0]
            : null;

    /// <summary>Checks whether a type represents a Boolean value.</summary>
    /// <param name="type">The type symbol.</param>
    /// <returns>True when the type is Boolean.</returns>
    private static bool IsBoolean(ITypeSymbol type) =>
        (GetNullableUnderlyingType(type) ?? type).SpecialType == SpecialType.System_Boolean;

    /// <summary>Gets the PLC registration type for a tag.</summary>
    /// <param name="type">The observed type.</param>
    /// <param name="bit">The configured bit index.</param>
    /// <returns>The registration type name.</returns>
    private static string GetRegisterType(ITypeSymbol type, int bit) =>
        IsBoolean(type) && bit >= 0 ? "short" : GetObserveType(type);

    /// <summary>Gets the generated observable value type.</summary>
    /// <param name="type">The source type.</param>
    /// <returns>The generated type name.</returns>
    private static string GetObserveType(ITypeSymbol type) =>
        (GetNullableUnderlyingType(type) ?? type).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    /// <summary>Gets a safe generated hint name for the target type.</summary>
    /// <param name="typeSymbol">The target type symbol.</param>
    /// <returns>The generated hint name.</returns>
    private static string GetSafeHintName(INamedTypeSymbol typeSymbol)
    {
        var name = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var builder = new StringBuilder(name.Length);
        foreach (var character in name)
        {
            _ = builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.ToString();
    }

    /// <summary>Sanitizes an attribute value into a C# identifier.</summary>
    /// <param name="value">The original value.</param>
    /// <returns>The sanitized identifier.</returns>
    private static string SanitizeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Value";
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            _ = builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
        }

        if (builder.Length == 0 || !IsIdentifierStart(builder[0]))
        {
            _ = builder.Insert(0, '_');
        }

        return builder.ToString();
    }

    /// <summary>Checks whether a character can start a generated identifier.</summary>
    /// <param name="value">The character to check.</param>
    /// <returns>True when the character can start an identifier.</returns>
    private static bool IsIdentifierStart(char value) =>
        char.IsLetter(value) || value == '_';

    /// <summary>Converts an identifier to camel case.</summary>
    /// <param name="value">The source identifier.</param>
    /// <returns>The camel-cased identifier.</returns>
    private static string ToCamelCase(string value) =>
        string.IsNullOrWhiteSpace(value) ? "value" : char.ToLowerInvariant(value[0]) + value.Remove(0, 1);

    /// <summary>Converts a string to a generated verbatim string literal.</summary>
    /// <param name="value">The source value.</param>
    /// <returns>The generated literal.</returns>
    private static string ToLiteral(string value) =>
        "@\"" + value.Replace("\"", "\"\"") + "\"";

    /// <summary>Stores one incremental generator output.</summary>
    private readonly struct GenerationResult
    {
        /// <summary>Initializes a new instance of the <see cref="GenerationResult"/> struct.</summary>
        /// <param name="hintName">The generated hint name.</param>
        /// <param name="source">The generated source.</param>
        /// <param name="diagnostic">The diagnostic to report.</param>
        private GenerationResult(string? hintName, string? source, Diagnostic? diagnostic)
        {
            HintName = hintName;
            Source = source;
            Diagnostic = diagnostic;
        }

        /// <summary>Gets the generated source hint name.</summary>
        public string? HintName { get; }

        /// <summary>Gets the generated source text.</summary>
        public string? Source { get; }

        /// <summary>Gets the diagnostic to report.</summary>
        public Diagnostic? Diagnostic { get; }

        /// <summary>Creates a source generation result.</summary>
        /// <param name="hintName">The generated hint name.</param>
        /// <param name="source">The generated source.</param>
        /// <returns>The source result.</returns>
        public static GenerationResult FromSource(string hintName, string source) => new(hintName, source, diagnostic: null);

        /// <summary>Creates a diagnostic generation result.</summary>
        /// <param name="diagnostic">The diagnostic to report.</param>
        /// <returns>The diagnostic result.</returns>
        public static GenerationResult FromDiagnostic(Diagnostic diagnostic) => new(hintName: null, source: null, diagnostic);
    }

    /// <summary>Describes one generated PLC tag stream.</summary>
    private readonly struct TagModel
    {
        /// <summary>Initializes a new instance of the <see cref="TagModel"/> struct.</summary>
        /// <param name="propertyName">The generated property name.</param>
        /// <param name="variable">The PLC variable key.</param>
        /// <param name="tagName">The PLC tag name.</param>
        /// <param name="group">The PLC tag group.</param>
        /// <param name="observeType">The observed value type.</param>
        /// <param name="propertyType">The generated property type.</param>
        /// <param name="registerType">The PLC registration type.</param>
        /// <param name="bit">The configured bit index.</param>
        /// <param name="registerTag">A value indicating whether to register the tag.</param>
        /// <param name="generateProperty">A value indicating whether to generate a property.</param>
        /// <param name="requiresValueGetOrDefault">A value indicating whether nullable values need default conversion.</param>
        public TagModel(
            string propertyName,
            string variable,
            string tagName,
            string group,
            string observeType,
            string propertyType,
            string registerType,
            int bit,
            bool registerTag,
            bool generateProperty,
            bool requiresValueGetOrDefault)
        {
            PropertyName = SanitizeIdentifier(propertyName);
            Variable = variable;
            TagName = tagName;
            Group = group;
            ObserveType = observeType;
            PropertyType = propertyType;
            RegisterType = registerType;
            Bit = bit;
            RegisterTag = registerTag;
            GenerateProperty = generateProperty;
            RequiresValueGetOrDefault = requiresValueGetOrDefault;
        }

        /// <summary>Gets the generated property name.</summary>
        public string PropertyName { get; }

        /// <summary>Gets the PLC variable key.</summary>
        public string Variable { get; }

        /// <summary>Gets the PLC tag name.</summary>
        public string TagName { get; }

        /// <summary>Gets the PLC tag group.</summary>
        public string Group { get; }

        /// <summary>Gets the observed value type.</summary>
        public string ObserveType { get; }

        /// <summary>Gets the generated property type.</summary>
        public string PropertyType { get; }

        /// <summary>Gets the PLC registration type.</summary>
        public string RegisterType { get; }

        /// <summary>Gets the configured bit index.</summary>
        public int Bit { get; }

        /// <summary>Gets a value indicating whether the tag should be registered.</summary>
        public bool RegisterTag { get; }

        /// <summary>Gets a value indicating whether a backing property should be generated.</summary>
        public bool GenerateProperty { get; }

        /// <summary>Gets a value indicating whether nullable values need default conversion.</summary>
        public bool RequiresValueGetOrDefault { get; }
    }

    /// <summary>Stores optional tag settings read from attribute arguments.</summary>
    private readonly struct TagSettings
    {
        /// <summary>Initializes a new instance of the <see cref="TagSettings"/> struct.</summary>
        /// <param name="variable">The PLC variable key.</param>
        /// <param name="group">The PLC tag group.</param>
        /// <param name="bit">The configured bit index.</param>
        /// <param name="registerTag">A value indicating whether to register the tag.</param>
        public TagSettings(string variable, string group, int bit, bool registerTag)
        {
            Variable = variable;
            Group = group;
            Bit = bit;
            RegisterTag = registerTag;
        }

        /// <summary>Gets the PLC variable key.</summary>
        public string Variable { get; }

        /// <summary>Gets the PLC tag group.</summary>
        public string Group { get; }

        /// <summary>Gets the configured bit index.</summary>
        public int Bit { get; }

        /// <summary>Gets a value indicating whether the tag should be registered.</summary>
        public bool RegisterTag { get; }
    }
}
