// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ABPlcRx.SourceGenerators;

/// <summary>
/// Generates reactive PLC stream models from ABPlcRx source generation attributes.
/// </summary>
[Generator]
public sealed class PlcModelGenerator : ISourceGenerator
{
    private const string PlcModelAttributeName = "ABPlcRx.SourceGeneration.PlcModelAttribute";
    private const string PlcTagAttributeName = "ABPlcRx.SourceGeneration.PlcTagAttribute";

    /// <inheritdoc/>
    public void Initialize(GeneratorInitializationContext context) =>
        context.RegisterForSyntaxNotifications(static () => new Receiver());

    /// <inheritdoc/>
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not Receiver receiver)
        {
            return;
        }

        foreach (var declaration in receiver.Candidates)
        {
            var semanticModel = context.Compilation.GetSemanticModel(declaration.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(declaration, context.CancellationToken) is not INamedTypeSymbol typeSymbol)
            {
                continue;
            }

            var tags = CollectTags(typeSymbol);
            if (tags.Length == 0 && !HasAttribute(typeSymbol.GetAttributes(), PlcModelAttributeName))
            {
                continue;
            }

            if (!IsPartial(declaration))
            {
                ReportPartialRequired(context, declaration, typeSymbol);
                continue;
            }

            if (tags.Length == 0)
            {
                continue;
            }

            var source = GenerateModel(typeSymbol, tags);
            context.AddSource($"{GetSafeHintName(typeSymbol)}.ABPlcRx.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

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
        tag = new TagModel(
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
        tag = new TagModel(
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
                    variable = value;
                    break;
                case "Group" when namedArgument.Value.Value is string value && !string.IsNullOrWhiteSpace(value):
                    group = value;
                    break;
                case "Bit" when namedArgument.Value.Value is int value:
                    bit = value;
                    break;
                case "RegisterTag" when namedArgument.Value.Value is bool value:
                    registerTag = value;
                    break;
            }
        }

        return new TagSettings(variable, group, bit, registerTag);
    }

    private static string GenerateModel(INamedTypeSymbol typeSymbol, ImmutableArray<TagModel> tags)
    {
        var builder = new StringBuilder();
        var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace ? null : typeSymbol.ContainingNamespace.ToDisplayString();

        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Reactive.Linq;");
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            builder.Append("namespace ").Append(namespaceName).AppendLine(";");
            builder.AppendLine();
        }

        builder.Append("partial class ").Append(typeSymbol.Name).AppendLine();
        builder.AppendLine("{");
        builder.AppendLine("    private readonly global::System.Reactive.Disposables.CompositeDisposable _abPlcRxSubscriptions = new();");
        builder.AppendLine("    private global::ABPlcRx.IABPlcRx? _abPlcRxController;");
        builder.AppendLine();

        foreach (var tag in tags)
        {
            AppendTagMembers(builder, tag);
        }

        AppendAttachMethod(builder, tags);
        AppendDetachMethod(builder, tags);

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void AppendTagMembers(StringBuilder builder, TagModel tag)
    {
        var fieldName = "_" + ToCamelCase(SanitizeIdentifier(tag.PropertyName));
        var observableFieldName = fieldName + "Observable";

        builder.Append("    private global::System.IObservable<").Append(tag.ObserveType).Append(">? ").Append(observableFieldName).AppendLine(";");

        if (tag.GenerateProperty)
        {
            builder.Append("    private ").Append(tag.PropertyType).Append(' ').Append(fieldName).AppendLine(";");
            builder.AppendLine();
            builder.Append("    public ").Append(tag.PropertyType).Append(' ').Append(tag.PropertyName).AppendLine();
            builder.AppendLine("    {");
            builder.Append("        get => ").Append(fieldName).AppendLine(";");
            builder.Append("        private set => ").Append(fieldName).AppendLine(" = value;");
            builder.AppendLine("    }");
        }

        builder.AppendLine();
        builder.Append("    public global::System.IObservable<").Append(tag.ObserveType).Append("> ").Append(tag.PropertyName).AppendLine("Observable =>");
        builder.Append("        ").Append(observableFieldName).Append(" ?? throw new global::System.InvalidOperationException(\"Call ")
            .Append("AttachPlcStreams").AppendLine(" before reading generated PLC observables.\");");
        builder.AppendLine();
        builder.AppendLine("#if NET8_0_OR_GREATER");
        builder.Append("    public global::ReactiveUI.Extensions.Async.IObservableAsync<").Append(tag.ObserveType).Append("> ")
            .Append(tag.PropertyName).AppendLine("ObservableAsync =>");
        builder.Append("        global::ReactiveUI.Extensions.Async.ObservableBridgeExtensions.ToObservableAsync(")
            .Append(tag.PropertyName).AppendLine("Observable);");
        builder.AppendLine("#endif");
        builder.AppendLine();
    }

    private static void AppendAttachMethod(StringBuilder builder, ImmutableArray<TagModel> tags)
    {
        builder.AppendLine("    public global::System.IDisposable AttachPlcStreams(global::ABPlcRx.IABPlcRx controller)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (controller is null)");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(controller));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        DetachPlcStreams();");
        builder.AppendLine("        _abPlcRxController = controller;");
        builder.AppendLine();

        foreach (var tag in tags)
        {
            var observableFieldName = "_" + ToCamelCase(SanitizeIdentifier(tag.PropertyName)) + "Observable";
            if (tag.RegisterTag)
            {
                builder.Append("        controller.AddUpdateTagItem<").Append(tag.RegisterType).Append(">(")
                    .Append(ToLiteral(tag.Variable)).Append(", ")
                    .Append(ToLiteral(tag.TagName)).Append(", ")
                    .Append(ToLiteral(tag.Group)).AppendLine(");");
            }

            builder.Append("        ").Append(observableFieldName).Append(" = controller.Observe<").Append(tag.ObserveType).Append(">(")
                .Append(ToLiteral(tag.Variable)).Append(", ").Append(tag.Bit.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .AppendLine(").Publish().RefCount();");
            builder.Append("        _abPlcRxSubscriptions.Add(").Append(observableFieldName).Append(".Subscribe(value => ");
            if (tag.RequiresValueGetOrDefault)
            {
                builder.Append(tag.PropertyName).AppendLine(" = value.GetValueOrDefault()));");
            }
            else
            {
                builder.Append(tag.PropertyName).AppendLine(" = value));");
            }
        }

        builder.AppendLine();
        builder.AppendLine("        return global::System.Reactive.Disposables.Disposable.Create(DetachPlcStreams);");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendDetachMethod(StringBuilder builder, ImmutableArray<TagModel> tags)
    {
        builder.AppendLine("    public void DetachPlcStreams()");
        builder.AppendLine("    {");
        builder.AppendLine("        _abPlcRxSubscriptions.Clear();");
        builder.AppendLine("        _abPlcRxController = null;");

        foreach (var tag in tags)
        {
            builder.Append("        _").Append(ToCamelCase(SanitizeIdentifier(tag.PropertyName))).AppendLine("Observable = null;");
        }

        builder.AppendLine("    }");
    }

    private static bool HasAttribute(ImmutableArray<AttributeData> attributes, string metadataName) =>
        attributes.Any(attribute => IsAttribute(attribute, metadataName));

    private static bool IsAttribute(AttributeData attribute, string metadataName) =>
        attribute.AttributeClass?.ToDisplayString() == metadataName;

    private static bool IsPartial(TypeDeclarationSyntax declaration) =>
        declaration.Modifiers.Any(SyntaxKind.PartialKeyword);

    private static void ReportPartialRequired(GeneratorExecutionContext context, TypeDeclarationSyntax declaration, INamedTypeSymbol typeSymbol)
    {
        var descriptor = new DiagnosticDescriptor(
            "ABPLCRXSG001",
            "PLC stream model must be partial",
            "Type '{0}' must be partial to generate ABPlcRx stream members",
            "ABPlcRx.SourceGenerators",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        context.ReportDiagnostic(Diagnostic.Create(descriptor, declaration.Identifier.GetLocation(), typeSymbol.Name));
    }

    private static ITypeSymbol? GetNullableUnderlyingType(ITypeSymbol type) =>
        type is INamedTypeSymbol namedType &&
        namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T
            ? namedType.TypeArguments[0]
            : null;

    private static bool IsBoolean(ITypeSymbol type) =>
        (GetNullableUnderlyingType(type) ?? type).SpecialType == SpecialType.System_Boolean;

    private static string GetRegisterType(ITypeSymbol type, int bit) =>
        IsBoolean(type) && bit >= 0 ? "short" : GetObserveType(type);

    private static string GetObserveType(ITypeSymbol type) =>
        (GetNullableUnderlyingType(type) ?? type).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static string GetSafeHintName(INamedTypeSymbol typeSymbol)
    {
        var name = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var builder = new StringBuilder(name.Length);
        foreach (var character in name)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.ToString();
    }

    private static string SanitizeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Value";
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
        }

        if (builder.Length == 0 || !IsIdentifierStart(builder[0]))
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }

    private static bool IsIdentifierStart(char value) =>
        char.IsLetter(value) || value == '_';

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "value";
        }

        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }

    private static string ToLiteral(string value) =>
        "@\"" + value.Replace("\"", "\"\"") + "\"";

    private readonly struct TagModel
    {
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

        public string PropertyName { get; }

        public string Variable { get; }

        public string TagName { get; }

        public string Group { get; }

        public string ObserveType { get; }

        public string PropertyType { get; }

        public string RegisterType { get; }

        public int Bit { get; }

        public bool RegisterTag { get; }

        public bool GenerateProperty { get; }

        public bool RequiresValueGetOrDefault { get; }
    }

    private readonly struct TagSettings
    {
        public TagSettings(string variable, string group, int bit, bool registerTag)
        {
            Variable = variable;
            Group = group;
            Bit = bit;
            RegisterTag = registerTag;
        }

        public string Variable { get; }

        public string Group { get; }

        public int Bit { get; }

        public bool RegisterTag { get; }
    }

    private sealed class Receiver : ISyntaxReceiver
    {
        public List<TypeDeclarationSyntax> Candidates { get; } = [];

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is TypeDeclarationSyntax { AttributeLists.Count: > 0 } declaration)
            {
                Candidates.Add(declaration);
            }
        }
    }
}
