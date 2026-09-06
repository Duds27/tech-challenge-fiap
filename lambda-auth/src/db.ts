import mysql from "mysql2/promise";
import type { DbCredentials } from "./config.js";

/**
 * Acesso ao RDS MySQL. O pool é criado uma vez por container e reutilizado entre
 * invocações (connection reuse), mitigando o custo de conexão em cold starts.
 */

export interface ClienteRecord {
  Id: number;
  Nome: string;
  CpfCnpj: string;
  Ativo: boolean;
}

let pool: mysql.Pool | undefined;

function getPool(db: DbCredentials): mysql.Pool {
  if (!pool) {
    pool = mysql.createPool({
      host: db.host,
      port: db.port,
      user: db.user,
      password: db.password,
      database: db.database,
      waitForConnections: true,
      connectionLimit: 2,
      maxIdle: 2,
      enableKeepAlive: true,
    });
  }
  return pool;
}

/** Busca o cliente pelo CPF/CNPJ já normalizado (sem pontuação). */
export async function findClienteByCpf(
  db: DbCredentials,
  cpfNormalized: string
): Promise<ClienteRecord | null> {
  const [rows] = await getPool(db).query<mysql.RowDataPacket[]>(
    "SELECT Id, Nome, CpfCnpj, Ativo FROM Clientes WHERE CpfCnpj = ? LIMIT 1",
    [cpfNormalized]
  );
  const row = rows[0];
  if (!row) return null;
  return {
    Id: Number(row.Id),
    Nome: String(row.Nome),
    CpfCnpj: String(row.CpfCnpj),
    Ativo: Boolean(row.Ativo),
  };
}

/** Fecha o pool (usado em testes/encerramento gracioso). */
export async function closePool(): Promise<void> {
  if (pool) {
    await pool.end();
    pool = undefined;
  }
}
