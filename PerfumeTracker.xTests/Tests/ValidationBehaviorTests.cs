using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using PerfumeTracker.Server.Behaviors;
using PerfumeTracker.Server.Features.CQRS;

namespace PerfumeTracker.xTests.Tests;

public record TestCommand(string Name) : ICommand<string>;

public class ValidationBehaviorTests {

	private static Task<string> SuccessHandler() => Task.FromResult("success");

	[Fact]
	public async Task Handle_NoValidationErrors_CallsNext() {
		var validators = new List<IValidator<TestCommand>> {
			CreatePassingValidator()
		};
		var behavior = new ValidationBehavior<TestCommand, string>(validators);
		var mock = new Mock<RequestHandlerDelegate<string>>();
		mock.Setup(d => d.Invoke()).ReturnsAsync("success");
		var result = await behavior.Handle(new TestCommand("test"), mock.Object, CancellationToken.None);
		Assert.Equal("success", result);
	}

	[Fact]
	public async Task Handle_NoValidators_CallsNext() {
		var validators = Enumerable.Empty<IValidator<TestCommand>>();
		var behavior = new ValidationBehavior<TestCommand, string>(validators);
		var mock = new Mock<RequestHandlerDelegate<string>>();
		mock.Setup(d => d.Invoke()).ReturnsAsync("success");
		var result = await behavior.Handle(new TestCommand("test"), mock.Object, CancellationToken.None);
		Assert.Equal("success", result);
	}

	[Fact]
	public async Task Handle_ValidationErrors_ThrowsValidationException() {
		var validators = new List<IValidator<TestCommand>> {
			CreateFailingValidator("Name", "Name is required")
		};
		var behavior = new ValidationBehavior<TestCommand, string>(validators);
		var mock = new Mock<RequestHandlerDelegate<string>>();
		mock.Setup(d => d.Invoke()).ReturnsAsync("should not reach");
		await Assert.ThrowsAsync<ValidationException>(async () =>
			await behavior.Handle(new TestCommand(""), mock.Object, CancellationToken.None));
	}

	private static IValidator<TestCommand> CreatePassingValidator() {
		var mock = new Mock<IValidator<TestCommand>>();
		mock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ValidationResult());
		return mock.Object;
	}

	private static IValidator<TestCommand> CreateFailingValidator(string property, string message) {
		var mock = new Mock<IValidator<TestCommand>>();
		mock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(property, message) }));
		return mock.Object;
	}
}
