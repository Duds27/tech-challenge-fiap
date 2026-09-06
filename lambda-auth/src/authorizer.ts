import type {
  APIGatewayRequestAuthorizerEventV2,
  APIGatewaySimpleAuthorizerWithContextResult,
} from "aws-lambda";
import { getConfig } from "./config.js";
import { verifyToken } from "./jwt.js";

/**
 * Lambda Authorizer (API Gateway HTTP API, formato SIMPLE) que valida o JWT HS256
 * emitido pela função de autenticação. Protege as rotas /api/* antes de encaminhar
 * o tráfego ao app no EKS. Usamos authorizer Lambda (e não o JWT authorizer nativo)
 * porque o token é HS256 (simétrico) — o authorizer nativo exige JWKS/OIDC.
 */

interface AuthContext extends Record<string, string | number | boolean | null> {
  clienteId: string;
  cpf: string;
}

export const handler = async (
  event: APIGatewayRequestAuthorizerEventV2
): Promise<APIGatewaySimpleAuthorizerWithContextResult<AuthContext>> => {
  const denied: APIGatewaySimpleAuthorizerWithContextResult<AuthContext> = {
    isAuthorized: false,
    context: { clienteId: "", cpf: "" },
  };

  try {
    const header =
      event.headers?.authorization ?? event.headers?.Authorization ?? "";
    const token = header.toLowerCase().startsWith("bearer ")
      ? header.slice(7).trim()
      : header.trim();
    if (!token) return denied;

    const cfg = await getConfig();
    const payload = verifyToken(cfg, token);

    return {
      isAuthorized: true,
      context: {
        clienteId: String(payload.sub ?? ""),
        cpf: String((payload as Record<string, unknown>).cpf ?? ""),
      },
    };
  } catch {
    return denied;
  }
};
