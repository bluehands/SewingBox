using System.Collections.Immutable;
using System.Diagnostics;
using CodeGenHelpers;
using Generator.Bpmn;
using Microsoft.CodeAnalysis;

namespace Generator;

[Generator]
public class VariablesFromXmlGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var files = context.AdditionalTextsProvider
			.Where(a => a.Path.EndsWith(".bpmn"))
			.Select((a, c) => (Path.GetFileNameWithoutExtension(a.Path), a.GetText(c)!.ToString()));

		var compilationAndFiles = context.CompilationProvider.Combine(files.Collect());
				
		context.RegisterSourceOutput(compilationAndFiles, (productionContext, sourceContext) => Generate(productionContext, sourceContext));
	}

	static void Generate(SourceProductionContext productionContext, (Compilation compilation, ImmutableArray<(string filename, string content)> files) compilationAndFiles)
	{
#if DEBUG
		if (!Debugger.IsAttached)
		{
			Debugger.Launch();
		}
#endif

		foreach (var (filename, content) in compilationAndFiles.files)
		{
			var processDefinition = BpmnParser.GetProcessDefinition(content);
			var variables = processDefinition.GetFormFields().Select(f => f.FieldId);

			var source = CodeBuilder
				.Create("WorkflowInteraction")
				.AddClass("WorkflowVariables")
				.MakePublicClass()
				.MakeStaticClass();

			foreach (var variable in variables)
			{
				source = source.AddPublicConstantStringField(variable, variable);
			}

			productionContext.AddSource(Path.ChangeExtension(Path.GetFileName(filename), ".g.cs"), source.Build());
		}
	}
}

public static class CodeBuilderExtensions
{
	public static ClassBuilder AddPublicConstantStringField(this ClassBuilder classBuilder, string name, string value) =>
		classBuilder
			.AddProperty(name)
			.MakePublicProperty()
			.SetType<string>()
			.WithConstValue($"\"{value}\"");
}
