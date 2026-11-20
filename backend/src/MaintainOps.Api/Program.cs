using MaintainOps.Api.ErrorHandling;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Every error response is ProblemDetails. Our expected failures are mapped by KnownExceptionHandler;
// anything else becomes a 500 with a plain message and no exception text.
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        if (context.ProblemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            context.ProblemDetails.Title = "Something went wrong on our side.";
            context.ProblemDetails.Extensions.TryAdd(ProblemDetailsCodes.ExtensionName, ProblemDetailsCodes.UnexpectedError);
        }
    };
});
builder.Services.AddExceptionHandler<KnownExceptionHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
