

using Microsoft.Extensions.Hosting;

using var host = Host.CreateDefaultBuilder()
	.AddEventSourcing()
	.Build();
