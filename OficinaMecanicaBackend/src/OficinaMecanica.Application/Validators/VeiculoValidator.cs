using FluentValidation;
using OficinaMecanica.Application.DTOs.Veiculos;

namespace OficinaMecanica.Application.Validators;

public class CreateVeiculoValidator : AbstractValidator<CreateVeiculoDto>
{
    public CreateVeiculoValidator()
    {
        RuleFor(x => x.Placa)
            .NotEmpty().WithMessage("Placa é obrigatória.")
            .Must(PlacaValidator.IsValid)
            .WithMessage("Placa inválida. Use o formato antigo (ABC-1234) ou Mercosul (ABC1D23).");

        RuleFor(x => x.Marca)
            .NotEmpty().WithMessage("Marca é obrigatória.")
            .MaximumLength(100);

        RuleFor(x => x.Modelo)
            .NotEmpty().WithMessage("Modelo é obrigatório.")
            .MaximumLength(100);

        RuleFor(x => x.Ano)
            .InclusiveBetween(1900, DateTime.UtcNow.Year + 1)
            .WithMessage($"Ano deve estar entre 1900 e {DateTime.UtcNow.Year + 1}.");

        RuleFor(x => x.ClienteId)
            .GreaterThan(0).WithMessage("ClienteId inválido.");
    }
}

public class UpdateVeiculoValidator : AbstractValidator<UpdateVeiculoDto>
{
    public UpdateVeiculoValidator()
    {
        RuleFor(x => x.Marca)
            .NotEmpty().WithMessage("Marca é obrigatória.")
            .MaximumLength(100);

        RuleFor(x => x.Modelo)
            .NotEmpty().WithMessage("Modelo é obrigatório.")
            .MaximumLength(100);

        RuleFor(x => x.Ano)
            .InclusiveBetween(1900, DateTime.UtcNow.Year + 1)
            .WithMessage($"Ano deve estar entre 1900 e {DateTime.UtcNow.Year + 1}.");
    }
}
