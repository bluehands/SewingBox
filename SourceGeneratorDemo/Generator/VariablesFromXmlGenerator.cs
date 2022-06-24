using System.Collections.Immutable;
using System.Diagnostics;
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

	void Generate(SourceProductionContext productionContext, (Compilation compilation, ImmutableArray<(string filename, string content)> files) compilationAndFiles)
	{
#if DEBUG
		if (!Debugger.IsAttached)
		{
			Debugger.Launch();
		}
#endif
	}
}
