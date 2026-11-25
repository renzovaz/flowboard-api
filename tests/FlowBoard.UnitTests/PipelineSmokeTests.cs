namespace FlowBoard.UnitTests;

/// <summary>
/// Teste inicial que valida a execução dos testes no pipeline de CI.
/// Será substituído pelos testes das regras de negócio (R1–R8).
/// </summary>
public class PipelineSmokeTests
{
    [Fact]
    public void ApiAssembly_IsLoadable()
    {
        // Arrange / Act
        var assembly = typeof(Program).Assembly;

        // Assert
        Assert.Equal("FlowBoard.Api", assembly.GetName().Name);
    }
}
