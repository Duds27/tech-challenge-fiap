using FluentValidation;
using OficinaMecanicaBackend.DTOs.Clientes;

namespace OficinaMecanicaBackend.Validators;

public class CreateClienteValidator : AbstractValidator<CreateClienteDto>
{
    public CreateClienteValidator()
    {
        RuleFor(x => x.CpfCnpj)
            .NotEmpty().WithMessage("CPF/CNPJ é obrigatório.")
            .Must(CpfCnpjValidator.IsValid).WithMessage("CPF ou CNPJ inválido.");

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("E-mail inválido.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Telefone)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Telefone));
    }
}

public class UpdateClienteValidator : AbstractValidator<UpdateClienteDto>
{
    public UpdateClienteValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("E-mail inválido.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Telefone)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Telefone));
    }
}
