using System.Collections.Immutable;
using Lifti;
using Lifti.Tokenization.TextExtraction;

namespace LiftiDemo;

[TestClass]
public class Demo
{
	[TestMethod]
	public async Task TryOutLifti()
	{
		var index = new FullTextIndexBuilder<string>()
			.WithTextExtractor(new XmlTextExtractor())
			//.WithObjectTokenization<Talk>(talkOptions => 
			//	talkOptions
			//		.WithKey(t => t.Title)
			//		.WithField(nameof(Talk.Title), t => t.Title)
			//		.WithField(nameof(Talk.Synopsis), t => t.Synopsis)
			//		.WithField("Author", t => $"{t.AuthorFirstName} {t.AuthorLastName}")
			//	)
			.Build();

		await index.AddAsync("test", @"<html lang=""en"" xml:lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">
<title>Test</title>
</head>
<body>
 <p>Lifti</p>
</body>
</html>");
		
		var searchResult = index.Search("lifti");

	}
}