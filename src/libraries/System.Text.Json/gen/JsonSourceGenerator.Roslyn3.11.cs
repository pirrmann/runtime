﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Generates source code to optimize serialization and deserialization with JsonSerializer.
    /// </summary>
    [Generator]
    public sealed partial class JsonSourceGenerator : ISourceGenerator
    {
        /// <summary>
        /// Registers a syntax resolver to receive compilation units.
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(GeneratorInitializationContext context)
        {
#if LAUNCH_DEBUGGER
            Diagnostics.Debugger.Launch();
#endif

            // Unfortunately, there is no cancellation token that can be passed here
            // (the one in GeneratorInitializationContext is not safe to capture).
            // In practice this should still be ok as the generator driver itself will
            // cancel after every file it processes.
            context.RegisterForSyntaxNotifications(static () => new SyntaxContextReceiver(CancellationToken.None));
        }

        /// <summary>
        /// Generates source code to optimize serialization and deserialization with JsonSerializer.
        /// </summary>
        /// <param name="executionContext"></param>
        public void Execute(GeneratorExecutionContext executionContext)
        {
            if (executionContext.SyntaxContextReceiver is not SyntaxContextReceiver receiver || receiver.ClassDeclarationSyntaxList == null)
            {
                // nothing to do yet
                return;
            }

            JsonSourceGenerationContext context = new JsonSourceGenerationContext(executionContext);
            Parser parser = new(executionContext.Compilation);
            SourceGenerationSpec? spec = parser.GetGenerationSpec(receiver.ClassDeclarationSyntaxList, executionContext.CancellationToken);

            OnSourceEmitting?.Invoke(spec);

            if (spec != null)
            {
                Emitter emitter = new(context, spec);
                emitter.Emit();
            }
        }

        private sealed class SyntaxContextReceiver : ISyntaxContextReceiver
        {
            private readonly CancellationToken _cancellationToken;

            public SyntaxContextReceiver(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
            }

            public List<ClassDeclarationSyntax>? ClassDeclarationSyntaxList { get; private set; }

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (IsSyntaxTargetForGeneration(context.Node))
                {
                    ClassDeclarationSyntax? classSyntax = GetSemanticTargetForGeneration(context, _cancellationToken);
                    if (classSyntax != null)
                    {
                        (ClassDeclarationSyntaxList ??= new List<ClassDeclarationSyntax>()).Add(classSyntax);
                    }
                }
            }

            private static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0, BaseList.Types.Count: > 0 };

            private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken cancellationToken)
            {
                var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

                foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
                {
                    foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        IMethodSymbol? attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax, cancellationToken).Symbol as IMethodSymbol;
                        if (attributeSymbol == null)
                        {
                            continue;
                        }

                        INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                        string fullName = attributeContainingTypeSymbol.ToDisplayString();

                        if (fullName == Parser.JsonSerializableAttributeFullName)
                        {
                            return classDeclarationSyntax;
                        }
                    }

                }

                return null;
            }
        }

        /// <summary>
        /// Instrumentation helper for unit tests.
        /// </summary>
        public Action<SourceGenerationSpec?>? OnSourceEmitting { get; init; }
    }

    internal readonly struct JsonSourceGenerationContext
    {
        private readonly GeneratorExecutionContext _context;

        public JsonSourceGenerationContext(GeneratorExecutionContext context)
        {
            _context = context;
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _context.ReportDiagnostic(diagnostic);
        }

        public void AddSource(string hintName, SourceText sourceText)
        {
            _context.AddSource(hintName, sourceText);
        }
    }
}
