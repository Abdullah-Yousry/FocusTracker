using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Application.DTOs;

namespace TaskManager.Application.Validators
{
    public class RangeSummaryResponseDtoValidator : AbstractValidator<RangeSummaryRequestDto>
    {
        public RangeSummaryResponseDtoValidator()
        {
            RuleFor(x => x.FromDate)
                .NotEmpty()
                .WithMessage("FromDate is required.");

            RuleFor(x => x.ToDate)
                .NotEmpty()
                .WithMessage("ToDate is required.");

            RuleFor(x => x)
                .Must(x => x.FromDate <= x.ToDate)
                .WithMessage("FromDate cannot be later than ToDate.")
                .When(x => x.FromDate != default && x.ToDate != default);

            RuleFor(x => x.Pagination)
                .NotNull().WithMessage("Pagination parameters are required.")
                .SetValidator(new PaginationParamsDtoValidator());
        }
    }
}
