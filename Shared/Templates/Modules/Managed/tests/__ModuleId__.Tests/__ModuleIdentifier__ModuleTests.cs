namespace {{ModuleId}}.Tests;

public class {{ModuleIdentifier}}ModuleTests
{
    [Fact]
    public void TheAssemblyIsTheModule() =>
        typeof({{ModuleIdentifier}}Module).Assembly.GetName().Name.Should().Be({{ModuleIdentifier}}Module.Id);
}
