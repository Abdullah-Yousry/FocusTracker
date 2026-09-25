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
                .GreaterThan(0)
                .LessThanOrEqualTo(20)
                .WithMessage("PageSize cannot exceed 20 items per page.");
        }
    }
}
