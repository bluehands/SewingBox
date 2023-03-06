using EventSourcing;
using Example.Host;
using Example.Host.GraphQl;

var builder = WebApplication.CreateBuilder(args);

//var persistenceOption = Persistence.MsSqlStreamStore(StreamStoreDemoOptions.LocalSqlExpress);
var persistenceOption = Persistence.SQLite(@"DataSource=c:\temp\es.db");

builder.Services
	.AddExampleApp(persistenceOption)
	.AddGraphQLServer()
	.AddQueryType<Query>()
	.AddMutationType<Mutation>()
	.AddSubscriptionType<Subscription>()
	.AddInMemorySubscriptions();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseDeveloperExceptionPage();
}

app
	.UseRouting()
	.UseWebSockets()
	.UseEndpoints(endpoints =>
	{
		endpoints.MapGraphQL();
	});

app.Services.UseEventSourcing();

app.Run();