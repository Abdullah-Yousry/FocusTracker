using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Application.DTOs;

namespace TaskManager.Application.Validators
{
    public class CreateSessionDtoValidator : AbstractValidator<CreateSessionDto>
    {
        public CreateSessionDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MinimumLength(3).WithMessage("Title must be at least 3 characters.")
                .MaximumLength(100).WithMessage("Title cannot exceed 100 characters.");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("A valid CategoryId is required.");

            RuleFor(x => x.MaxAllowedPauseMinutes)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Max allowed pause duration cannot be negative.")
                .LessThanOrEqualTo(120)
                .WithMessage("Max allowed pause duration cannot exceed 120 minutes.");
        }
    }
}