namespace Bbx.Tests.TestKit;

// Tests that swap Console.Out / Console.Error need to run sequentially
// or they trample each other's redirection state.
[CollectionDefinition("Console")]
public class ConsoleCollection { }
