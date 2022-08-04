using System.Collections.Immutable;
using Lifti;

namespace LiftiDemo;

[TestClass]
public class Vorlage
{
	[TestMethod]
	public async Task TestMethod1()
	{
		var builder = new FullTextIndexBuilder<string>()
			.WithObjectTokenization<Talk>(
				talkOptions => talkOptions
					.WithKey(talk => talk.Title)
					.WithField(nameof(Talk.Title), t => t.Title)
					.WithField("Author", t => $"{t.AuthorFirstName} {t.AuthorLastName}")
					.WithField(nameof(Talk.Synopsis), t => t.Synopsis)
			);

		var index = builder.Build();

		await index.AddRangeAsync(Library.Talks);

		IReadOnlyCollection<SearchResult<string>> Search(string query) => index.Search(query).ToImmutableArray();

		var searchResult = Search("efficient");
		var fuzzyResult = Search("?effcient");
		var simonsTalks = Search("Author=?Popinga");
		var rxOrFullTextTalks = Search("Title=Rx | Lift*");
		var searchForCloseWords = Search("web ~3 application");
		var inSequenceSearch = Search("\"long list of words\"");
	}
}