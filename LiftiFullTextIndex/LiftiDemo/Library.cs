namespace LiftiDemo;

record Talk(string Title, string AuthorFirstName, string AuthorLastName, string Synopsis);

static class Library
{
	public static IEnumerable<Talk> Talks => new Talk[]
	{
		new ("Docker", "Waldemar", "Tomme", @"
Docker makes development efficient and predictable Poppinga
Docker takes away repetitive, mundane configuration tasks and is used throughout the development lifecycle for fast, easy and portable application development – desktop and cloud. Docker’s comprehensive end to end platform includes UIs, CLIs, APIs and security that are engineered to work together across the entire application delivery lifecycle.
"),

		new ("Rx .NET", "Nico", "Englert", @"A Brief Intro
The Reactive Extensions (Rx) is a library for composing asynchronous and event-based programs using observable sequences and LINQ-style query operators. Using Rx, developers represent asynchronous data streams with Observables, query asynchronous data streams using LINQ operators, and parameterize the concurrency in the asynchronous data streams using Schedulers. Simply put, Rx = Observables + LINQ + Schedulers.
Whether you are authoring a traditional desktop or web-based application, you have to deal with asynchronous and event-based programming from time to time. Desktop applications have I/O operations and computationally expensive tasks that might take a long time to complete and potentially block other active threads. Furthermore, handling exceptions, cancellation, and synchronization is difficult and error-prone.
Using Rx, you can represent multiple asynchronous data streams (that come from diverse sources, e.g., stock quote, tweets, computer events, web service requests, etc.), and subscribe to the event stream using the IObserver<T> interface. The IObservable<T> interface notifies the subscribed IObserver<T> interface whenever an event occurs.
Because observable sequences are data streams, you can query them using standard LINQ query operators implemented by the Observable extension methods. Thus you can filter, project, aggregate, compose and perform time-based operations on multiple events easily by using these standard LINQ operators. In addition, there are a number of other reactive stream specific operators that allow powerful queries to be written. Cancellation, exceptions, and synchronization are also handled gracefully by using the extension methods provided by Rx."),
		new ("Lifti", "Alex", "Wiedemann", @"Documentation

LIFTI is a simple to use netstandard2 compatible in-memory full text indexing API.

If you are building an application that refers to objects that contain lots of text, and you:
    Don't want to store all the text in memory all the time (e.g. files or other text-based resources)
    Want to be able to search the contents of the text quickly

Then LIFTI is for you. You could use it in:
    Client applications, e.g. Blazor, UWP, Xamarin, WPF, Uno Platform
    ASP.NET applications where you need to perform a fast search against a long list of words. An in-memory index of exclusion words could easily be used to do this.
    Lots of other scenarios!
"),
		new ("Hangfire", "Simon", "Poppinga", @"Hangfire

An easy way to perform background processing in .NET and .NET Core applications. No Windows Service or separate process required.
Backed by persistent storage. Open and free for commercial use. long list of something else words 
")
	};
}