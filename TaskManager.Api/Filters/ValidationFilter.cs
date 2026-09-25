using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TaskManager.Api.Filters
{
    public class ValidationFilter : IAsyncActionFilter
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidationFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null) continue;

                var argumentType = argument.GetType(); 
                var validatorType = typeof(IValidator<>).MakeGenericType(argumentType); // if argumentType is CreateSessionDto --> var = Ivalidator<CreateSessionDto>

                if (_serviceProvider.GetService(validatorType) is IValidator validator)
                {
                    var contextObj = new ValidationContext<object>(argument);
                    var result = await validator.ValidateAsync(contextObj);

                    if (!result.IsValid)
                        throw new ValidationException(result.Errors);
                }
            }

            await next();
        }
    }
}
