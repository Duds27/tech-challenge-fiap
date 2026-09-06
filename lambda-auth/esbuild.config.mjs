import { build } from "esbuild";

/**
 * Empacota cada handler num bundle único (CommonJS) para o runtime Node da Lambda.
 * O AWS SDK v3 já é fornecido pelo runtime, então o mantemos como externo para
 * reduzir o tamanho do pacote.
 */
await build({
  entryPoints: {
    handler: "src/handler.ts",
    authorizer: "src/authorizer.ts",
  },
  bundle: true,
  platform: "node",
  target: "node20",
  format: "cjs",
  outdir: "dist",
  minify: true,
  sourcemap: true,
  external: ["@aws-sdk/*"],
});

console.log("Build concluído em dist/ (handler.js, authorizer.js).");
