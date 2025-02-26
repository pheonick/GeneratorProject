using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Text;

namespace GeneratorProject
{
    [Generator]
    public class TestGenerator : IIncrementalGenerator
    {
        private readonly bool DebugEnabled = true;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            if (DebugEnabled && !Debugger.IsAttached)
            {
                Debugger.Launch();
            }

            context.RegisterPostInitializationOutput(postInitializationContext =>
                postInitializationContext.AddSource("GeneratorProject.TestAttribute.g.cs", SourceText.From($@"
                    using System;

                    namespace GeneratorProject
                    {{
                        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                        public sealed class TestAttribute : Attribute
                        {{
                            public string NewNamespace {{ get; }}

                            public TestAttribute(string newNamespace)
                            {{
                                NewNamespace = newNamespace;
                            }}
                        }}
                    }}", Encoding.UTF8)));

            var pipeline = context.SyntaxProvider
                .ForAttributeWithMetadataName("GeneratorProject.TestAttribute",
                    (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
                    (generatorAttributeSyntaxContext, _) => GetSemanticTargetForGeneration(generatorAttributeSyntaxContext));

            context.RegisterSourceOutput(pipeline,
                (sourceProductionContext, model) => AddSource(sourceProductionContext, model));
        }

        private static (string FileName, string GeneratedCode) GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context)
        {
            var generatedCodeBuilder = new StringBuilder();

            var classSymbol = context.TargetSymbol;
            var actualNamespace = classSymbol.ContainingNamespace.ToString();

            generatedCodeBuilder.AppendLine(GetGeneratedFileUsings(actualNamespace));

            foreach (var attr in context.Attributes)
            {
                var newNamespace = (string)attr.ConstructorArguments[0].Value;

                generatedCodeBuilder.AppendLine(GetGeneratedClassCode(classSymbol.Name, actualNamespace, newNamespace, newNamespace));
            }

            return ($"{actualNamespace}.{classSymbol.Name}.g.cs", generatedCodeBuilder.ToString());
        }

        private static string GetGeneratedFileUsings(string sourceNamespace)
        {
            return $@"
                using {sourceNamespace};";
        }

        private static string GetGeneratedClassCode(string sourceClassName, string sourceNamespace, string generatedClassName, string generatedNamespace)
        {
            return $@"
                namespace {generatedNamespace}
                {{
                    public sealed class {generatedClassName} : {sourceNamespace}.{sourceClassName}
                    {{
                    }}
                }}";
        }

        private static void AddSource(SourceProductionContext sourceProductionContext, (string FileName, string GeneratedCode) model)
        {
            sourceProductionContext.AddSource(model.FileName, SourceText.From(model.GeneratedCode, Encoding.UTF8));
        }
    }
}
