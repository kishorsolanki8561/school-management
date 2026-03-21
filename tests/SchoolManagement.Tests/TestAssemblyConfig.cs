// Disable parallel test execution across classes to prevent race conditions
// on shared static state (InitializeConfiguration).
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
