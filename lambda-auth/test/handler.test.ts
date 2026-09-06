import { describe, it, expect } from "vitest";
import { authenticate, type AuthDeps } from "../src/handler.js";
import { issueToken, verifyToken } from "../src/jwt.js";
import type { AppConfig } from "../src/config.js";
import type { ClienteRecord } from "../src/db.js";

const config: AppConfig = {
  jwt: {
    key: "chave-de-teste-hs256-com-32-bytes-min!!",
    issuer: "OficinaMecanicaBackend",
    audience: "OficinaMecanicaBackend",
    expiresInMinutes: 60,
  },
  db: { host: "", port: 3306, user: "", password: "", database: "" },
};

const cliente: ClienteRecord = {
  Id: 7,
  Nome: "Maria Silva",
  CpfCnpj: "52998224725",
  Ativo: true,
};

function deps(overrides: Partial<AuthDeps> = {}): AuthDeps {
  return {
    config,
    findCliente: async () => cliente,
    issue: issueToken,
    ...overrides,
  };
}

describe("authenticate", () => {
  it("emite JWT válido para CPF de cliente ativo", async () => {
    const res = await authenticate("529.982.247-25", deps());
    expect(res.status).toBe(200);

    const token = res.body.token as string;
    expect(token).toBeTruthy();

    const payload = verifyToken(config, token);
    expect(payload.sub).toBe("7");
    expect((payload as Record<string, unknown>).cpf).toBe("52998224725");
    expect((payload as Record<string, unknown>).role).toBe("Cliente");
  });

  it("rejeita CPF inválido com 400", async () => {
    const res = await authenticate("111.111.111-11", deps());
    expect(res.status).toBe(400);
  });

  it("retorna 401 quando o cliente não existe", async () => {
    const res = await authenticate("529.982.247-25", deps({ findCliente: async () => null }));
    expect(res.status).toBe(401);
  });

  it("retorna 403 quando o cliente está inativo", async () => {
    const res = await authenticate(
      "529.982.247-25",
      deps({ findCliente: async () => ({ ...cliente, Ativo: false }) })
    );
    expect(res.status).toBe(403);
  });
});
