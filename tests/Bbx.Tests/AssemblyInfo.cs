// Several tests redirect Console.Out / Console.Error, or read and write
// process-wide state such as Environment.ExitCode. xUnit serialises tests
// within a collection but still runs collections in parallel, so a test in
// another collection could write into a capture that a Console test was in the
// middle of asserting on. The suite runs in well under a second, so serialising
// it outright is cheaper than reasoning about which globals are safe.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
