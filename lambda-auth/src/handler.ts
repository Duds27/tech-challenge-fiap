import type { APIGatewayProxyEventV2, APIGatewayProxyResultV2 } from "aws-lambda";
import { getConfig, type AppConfig } from "./config.js";
import { isValidCpfCnpj, normalize } from "./cpf.js";
import { findClienteByCpf, type ClienteRecord } from "./db.js";
import { issueToken, type IssuedToken } from "./jwt.js";

export interface AuthResult {
  status: number;
  body: Record<string, unknown>;
}

export interface AuthDeps {
  config: AppConfig;
  findCliente: (cpfNormalized: string) => Promise<ClienteRecord | null>;
  issue: (cfg: AppConfig, claims: { clienteId: number; cpf: string; nome: string }) => IssuedToken;
}

/**
 * Núcleo da autenticação por CPF (puro/testável, sem dependência da AWS):
 *  1. valida o CPF (formato + dígitos verificadores);
 *  2. consulta a existência do cliente na base;
 *  3. verifica o status (ativo);
 *  4. emite e devolve um JWT válido.
 */
export async function authenticate(cpfRaw: string | undefined, deps: AuthDeps): Promise<AuthResult> {
  if (!isValidCpfCnpj(cpfRaw)) {
    return { status: 400, body: { error: "CPF inválido." } };
  }

  const cpf = normalize(cpfRaw!);
  const cliente = await deps.findCliente(cpf);

  if (!cliente) {
    return { status: 401, body: { error: "Cliente não encontrado." } };
  }
  if (!cliente.Ativo) {
    return { status: 403, body: { error: "Cliente inativo." } };
  }

  const token = deps.issue(deps.config, {
    clienteId: cliente.Id,
    cpf: cliente.CpfCnpj,
    nome: cliente.Nome,
  });

  return { status: 200, body: { token: token.token, expiresAt: token.expiresAt } };
}

/** Adaptador AWS Lambda (API Gateway HTTP API v2). */
export const handler = async (
  event: APIGatewayProxyEventV2
): Promise<APIGatewayProxyResultV2> => {
  const config = await getConfig();

  let cpf: string | undefined;
  try {
    const parsed = event.body ? JSON.parse(event.body) : {};
    cpf = parsed.cpf;
  } catch {
    return json(400, { error: "Corpo da requisição inválido (JSON esperado)." });
  }

  const result = await authenticate(cpf, {
    config,
    findCliente: (c) => findClienteByCpf(config.db, c),
    issue: issueToken,
  });

  return json(result.status, result.body);
};

function json(status: number, body: unknown): APIGatewayProxyResultV2 {
  return {
    statusCode: status,
    headers: { "content-type": "application/json" },
    body: JSON.stringify(body),
  };
}
