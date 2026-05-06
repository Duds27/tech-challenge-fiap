using FluentValidation;
using OficinaMecanicaBackend.DTOs.OrdensServico;
using OficinaMecanicaBackend.Models.Enums;

namespace OficinaMecanicaBackend.Validators;

public class CreateOrdemServicoValidator : AbstractValidator<CreateOrdemServicoDto>
{
    public CreateOrdemServicoValidator()
    {
        RuleFor(x => x.ClienteId)
            .GreaterThan(0).WithMessage("ClienteId inválido.");

        RuleFor(x => x.VeiculoId)
            .GreaterThan(0).WithMessage("VeiculoId inválido.");

        RuleFor(x => x.DataPrevisaoTermino)
            .GreaterThan(DateTime.UtcNow).WithMessage("Data de previsão deve ser futura.")
            .When(x => x.DataPrevisaoTermino.HasValue);
    }
}

public class AddItemValidator : AbstractValidator<AddItemDto>
{
    public AddItemValidator()
    {
        RuleFor(x => x.Quantidade)
            .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero.");

        RuleFor(x => x.ServicoId)
            .NotNull().WithMessage("ServicoId é obrigatório para itens do tipo Serviço.")
            .GreaterThan(0).WithMessage("ServicoId inválido.")
            .When(x => x.Tipo == TipoItemOrdemServico.Servico);

        RuleFor(x => x.PecaId)
            .NotNull().WithMessage("PecaId é obrigatório para itens do tipo Peça.")
            .GreaterThan(0).WithMessage("PecaId inválido.")
            .When(x => x.Tipo == TipoItemOrdemServico.Peca);
    }
}
