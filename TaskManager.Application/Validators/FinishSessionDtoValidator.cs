using FluentValidation;
using FluentValidation.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Application.DTOs;

namespace TaskManager.Application.Validators
{
    public class FinishSessionDtoValidator : AbstractValidator<FinishSessionDto>
    {
        public FinishSessionDtoValidator() 
        {
            RuleFor(x => x.Summary)
                .MaximumLength(500)
                .WithMessage("Summary cannot exceed 500 characters.");
        }
    }
}
