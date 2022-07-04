Event sourcing implementation as abstraction over the actual persistence layer basically offering an event stream as IObservable<Event>. Requirements to persistence are minimal (reading events since version x, reading events by stream, writing events). Implementation of domain objects is completely up to the consumer. No one tells you which interface to implement or base class to derive from on your 'aggregates'. Example persistence layer added for [SQLStreamStore](https://github.com/SQLStreamStore/SQLStreamStore) (supports InMemory, SqlServer, PostGres, MySql). There is alreay a simpile in memory persistence implementation that takes no dependencies and does not require mapping persistence to domain events, which is usful for unit testing or to start with. Other event stores can be intergrated easily, as long as they offer the minimal functionality mentioned before.

Event serialization is interchangable (depending on the persistence layer). The consumer is encouraged to have one set of serializable event payloads, which are mapped at one explicit place to and from domain events (EventPayloadMappers). This frees domain events from persistence requirements, they can be freely renamed and restructured. On the persistence side there is the one place where you have to be very careful not to break already serialized events and were one can handle event versioning / rewriting. 

CommandProcessors are designed in a functional way. They take the command and basically produce a sequence of events. There is no shortcut that updates in memory objects from events before they are persisted or similar. There is a mechanism to wait until events produced due to a specific command are applied to a projection of interest (a way to have a 'synchronous' response at api level if desired). 

For the projection / read model side there are helpers for caching a projection that is updated from the event stream as well as a possibility to listen to events starting at a specific version (useful for persistent projections). 

There is an example application with a console and a web host. Web host uses graphql to expose an api to clients. It's nice to see, how subscriptions are used to easily notify clients about changes on read models. A unit test project shows end to end testing from command to projections as well as replay testing of certain event sequences to assert that the projection is working correctly.

Feel free to use the code as starting point for your own es based solution. Similar versions are in productive use in various of our projects with events persisted in Redis, MS SqlServer, Oracle, PostGres and custom solutions. Goal is to publish parts of it as nuget package.

TODOs:
 - example angular client app
 - example for handling concurrency at the projection side as alternative for concurrency handling at persisence side
 - configurable backoff polling with wakeup
 - IAsyncEnumerable
 - read page size configurable for sql stream store
 - efficient logging with [LoggerMessage] generator
 - nuget package