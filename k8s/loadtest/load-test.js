import http from 'k6/http';
import { check, sleep } from 'k6';

// Gera carga contra a API para demonstrar a escalabilidade automática (HPA).
// Sobe gradualmente o número de usuários virtuais (VUs) para forçar o consumo
// de CPU dos pods acima do alvo do HPA (70%) e observar o scale-up.
//
// Alvo padrão: http://localhost:8080 (via `kubectl -n oficina port-forward svc/api 8080:80`).
// Sobrescreva com: k6 run -e BASE_URL=http://SEU_HOST:PORTA load-test.js

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';

export const options = {
  stages: [
    { duration: '30s', target: 20 },   // rampa inicial
    { duration: '1m',  target: 60 },   // aumenta a pressão
    { duration: '2m',  target: 100 },  // pico sustentado -> dispara o HPA
    { duration: '30s', target: 0 },    // desaquece
  ],
};

export default function () {
  // /health/ready faz um roundtrip no banco (readiness check) -> consome CPU.
  const res = http.get(`${BASE_URL}/health/ready`);
  check(res, { 'status is 200': (r) => r.status === 200 });
  sleep(0.1);
}
