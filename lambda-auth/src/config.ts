import { SecretsManagerClient, GetSecretValueCommand } from "@aws-sdk/client-secrets-manager";

/**
 * Configuração da função. Segredos (chave do JWT e credenciais do banco) são lidos
 * do AWS Secrets Manager e mantidos em cache entre invocações (reuso do container),
 * reduzindo latência e chamadas à API. Em ambiente local, variáveis de ambiente
 * têm precedência para facilitar testes.
 */

export interface DbCredentials {
  host: string;
  port: number;
  user: string;
  password: string;
  database: string;
}

export interface AppConfig {
  jwt: {
    key: string;
    issuer: string;
    audience: string;
    expiresInMinutes: number;
  };
  db: DbCredentials;
}

let cached: AppConfig | undefined;

const sm = new SecretsManagerClient({});

async function readSecretJson(secretId: string): Promise<Record<string, unknown>> {
  const out = await sm.send(new GetSecretValueCommand({ SecretId: secretId }));
  if (!out.SecretString) throw new Error(`Secret ${secretId} sem SecretString.`);
  return JSON.parse(out.SecretString);
}

export async function getConfig(): Promise<AppConfig> {
  if (cached) return cached;

  const jwtSecretId = requireEnv("JWT_SECRET_ID");
  const dbSecretId = requireEnv("DB_SECRET_ID");

  const [jwtSecret, dbSecret] = await Promise.all([
    readSecretJson(jwtSecretId),
    readSecretJson(dbSecretId),
  ]);

  cached = {
    jwt: {
      key: String(jwtSecret.key),
      issuer: process.env.JWT_ISSUER ?? "OficinaMecanicaBackend",
      audience: process.env.JWT_AUDIENCE ?? "OficinaMecanicaBackend",
      expiresInMinutes: Number(process.env.JWT_EXPIRES_MINUTES ?? "60"),
    },
    db: {
      host: String(dbSecret.host),
      port: Number(dbSecret.port ?? 3306),
      user: String(dbSecret.username),
      password: String(dbSecret.password),
      database: String(dbSecret.dbname ?? process.env.DB_NAME ?? "OficinaMecanica"),
    },
  };

  return cached;
}

/** Permite injetar config nos testes (ignora o Secrets Manager). */
export function __setConfigForTests(cfg: AppConfig | undefined): void {
  cached = cfg;
}

function requireEnv(name: string): string {
  const v = process.env[name];
  if (!v) throw new Error(`Variável de ambiente obrigatória ausente: ${name}`);
  return v;
}
