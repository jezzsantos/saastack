using ArchitectureTesting.Common;
using Xunit;

namespace ApiHost1.ArchitectureTests;

[CollectionDefinition("Architecture", DisableParallelization = true)]
public class AllArchitectureSpecs : ICollectionFixture<ArchitectureSpecSetup<Program>>;