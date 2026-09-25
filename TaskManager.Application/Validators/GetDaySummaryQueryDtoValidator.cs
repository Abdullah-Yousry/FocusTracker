using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Application.DTOs;

namespace TaskManager.Application.Validators
{
    public class GetDaySummaryQueryDtoValidator : AbstractValidator<GetDaySummaryQueryDto>
    {
        public GetDaySummaryQueryDtoValidator()
        {
            RuleFor(x => x.Date)
                .NotEmpty()
                    .WithMessage("Date is required.")
                .LessThanOrEqualTo(DateTime.UtcNow.Date.AddDays(1))
                    .WithMessage("Date cannot be in the future");
        }
    }
}
