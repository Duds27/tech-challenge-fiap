import { describe, it, expect } from "vitest";
import { isValidCpf, isValidCpfCnpj, normalize } from "../src/cpf.js";

describe("normalize", () => {
  it("remove pontuação e uppercaseia", () => {
    expect(normalize("529.982.247-25")).toBe("52998224725");
    expect(normalize("12.abc.345/01de-35")).toBe("12ABC34501DE35");
  });
});

describe("isValidCpf", () => {
  it("aceita CPFs válidos", () => {
    expect(isValidCpf("52998224725")).toBe(true);
    expect(isValidCpf("11144477735")).toBe(true);
  });

  it("rejeita dígitos verificadores incorretos", () => {
    expect(isValidCpf("52998224724")).toBe(false);
  });

  it("rejeita sequências repetidas", () => {
    expect(isValidCpf("11111111111")).toBe(false);
  });

  it("rejeita tamanho incorreto ou não numérico", () => {
    expect(isValidCpf("123")).toBe(false);
    expect(isValidCpf("5299822472A")).toBe(false);
  });
});

describe("isValidCpfCnpj", () => {
  it("valida CPF com pontuação", () => {
    expect(isValidCpfCnpj("529.982.247-25")).toBe(true);
  });

  it("rejeita nulo/vazio", () => {
    expect(isValidCpfCnpj(undefined)).toBe(false);
    expect(isValidCpfCnpj("")).toBe(false);
    expect(isValidCpfCnpj("   ")).toBe(false);
  });
});
