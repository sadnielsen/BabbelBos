using BuildingBlocks.Domain;

namespace BuildingBlocks.Domain.Tests;

public class ResultTests
{
    private static readonly DomainError SomeError = new("Test.DomainError", "Something went wrong");

    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.Error.ShouldBe(DomainError.None);
    }

    [Fact]
    public void Failure_carries_its_error()
    {
        var result = Result.Failure(SomeError);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SomeError);
    }

    [Fact]
    public void Success_with_value_exposes_the_value()
    {
        var result = Result.Success(42);

        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Failure_with_value_throws_when_reading_the_value()
    {
        var result = Result.Failure<int>(SomeError);

        Should.Throw<InvalidOperationException>(() => result.Value);
    }
}
