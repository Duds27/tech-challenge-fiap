# tech-challenge-fiap

Repositório para o projeto do Tech Challenge para o curso de Software Architecture (15SOAT) da FIAP.

# Dicionário de Linguagem Obíquoa

- **Ordem de Serviço:** Entidade principal que agrega o cliente, o veículo, os serviços e as peças de um atendimento.

- **Cliente:** Pessoa física ou jurídica proprietária do veículo, identificada por CPF ou CNPJ.

- **Status da OS:** Estados dos ciclos de vida do serviço.
  - Recebida;
  - Em diagnóstico;
  - Aguardando aprovação;
  - Em execução;
  - Finalizada;
  - Entregue.

- **Insumo/Peça:** Material físico utilizado no reparo do veículo que possui controle de estoque e valor comercial.

- **Orçamento:** Documento financeiro gerado automaticamente com a soma de serviços e peças para aprovação do cliente.

- **Tempo de Execução:** Métrica que monitora a duração real do serviço para análise de eficiência da oficina.

- **CRUD Administrativo:** Funções de criação, leitura, atualização e exclusão de cadastros base (clientes, peças, veículos) protegidas por autenticação.
