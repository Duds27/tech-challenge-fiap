using FluentValidation;
using OficinaMecanica.Application.DTOs.Pecas;

namespace OficinaMecanica.Application.Validators;

public class CreatePecaValidator : AbstractValidator<CreatePecaDto>
{
    public CreatePecaValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200);

        RuleFor(x => x.Codigo)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.Codigo));

        RuleFor(x => x.PrecoUnitario)
            .GreaterThan(0).WithMessage("Preço unitário deve ser maior que zero.");

        RuleFor(x => x.QuantidadeEstoque)
            .GreaterThanOrEqualTo(0).WithMessage("Quantidade em estoque não pode ser negativa.");

        RuleFor(x => x.QuantidadeMinima)
            .GreaterThanOrEqualTo(0).WithMessage("Quantidade mínima não pode ser negativa.");
    }
}

public class UpdatePecaValidator : AbstractValidator<UpdatePecaDto>
{
    public UpdatePecaValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200);

        RuleFor(x => x.Codigo)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.Codigo));

        RuleFor(x => x.PrecoUnitario)
            .GreaterThan(0).WithMessage("Preço unitário deve ser maior que zero.");

        RuleFor(x => x.QuantidadeEstoque)
            .GreaterThanOrEqualTo(0).WithMessage("Quantidade em estoque não pode ser negativa.");

        RuleFor(x => x.QuantidadeMinima)
            .GreaterThanOrEqualTo(0).WithMessage("Quantidade mínima não pode ser negativa.");
    }
}
