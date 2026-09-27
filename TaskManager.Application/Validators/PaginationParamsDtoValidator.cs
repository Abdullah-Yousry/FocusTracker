using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Application.DTOs;

namespace TaskManager.Application.Validators
{
    public class PaginationParamsDtoValidator : AbstractValidator<PaginationParamsDto>
    {
        public PaginationParamsDtoValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("PageNumber must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 20)
                .WithMessage("PageSize must be between 1 and 100.");
        }
    }
}
