using FluentValidation;
using OficinaMecanica.Application.DTOs.Servicos;

namespace OficinaMecanica.Application.Validators;

public class CreateServicoValidator : AbstractValidator<CreateServicoDto>
{
    public CreateServicoValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200);

        RuleFor(x => x.PrecoBase)
            .GreaterThan(0).WithMessage("Preço base deve ser maior que zero.");

        RuleFor(x => x.TempoEstimadoHoras)
            .GreaterThan(0).WithMessage("Tempo estimado deve ser maior que zero.")
            .When(x => x.TempoEstimadoHoras.HasValue);
    }
}

public class UpdateServicoValidator : AbstractValidator<UpdateServicoDto>
{
    public UpdateServicoValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200);

        RuleFor(x => x.PrecoBase)
            .GreaterThan(0).WithMessage("Preço base deve ser maior que zero.");

        RuleFor(x => x.TempoEstimadoHoras)
            .GreaterThan(0).WithMessage("Tempo estimado deve ser maior que zero.")
            .When(x => x.TempoEstimadoHoras.HasValue);
    }
}
