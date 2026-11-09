const test = require("node:test");
const assert = require("node:assert/strict");
const { start, apply } = require("../../wwwroot/js/payment-demo-flow.js");
const future = () => new Date(Date.now() + 86400000).toISOString().slice(0, 16);
function approved() {
  let s = apply(start(), "AGREE", { accepted: true });
  s = apply(s, "PAY", { approved: true });
  return apply(s, "CONTINUE");
}
test("payment refusal does not advance or reserve", () => {
  let s = apply(apply(start(), "AGREE", { accepted: true }), "PAY", { approved: false });
  assert.equal(s.step, 2);
  assert.throws(() => apply(s, "CONTINUE"));
  s = apply(s, "RETRY");
  assert.equal(s.step, 1);
  assert.equal(s.approved, false);
});
test("cannot start checkout without accepting the demo disclosure", () => {
  assert.throws(() => apply(start(), "AGREE", { accepted: false }));
  assert.throws(() => apply(start(), "PAY", { approved: true }));
});
test("delivery needs a future date and explicit buyer approval", () => {
  let s = approved();
  assert.throws(() => apply(s, "CONFIRM_DELIVERY"));
  assert.throws(() => apply(s, "PROPOSE_DELIVERY", { kind: "em_maos", place: "Praça", at: "2000-01-01T10:00" }));
  s = apply(s, "PROPOSE_DELIVERY", { kind: "em_maos", place: "Local público", at: future() });
  assert.equal(s.step, 3);
  s = apply(s, "CONFIRM_DELIVERY");
  assert.equal(s.step, 4);
  assert.equal(s.deliveryAgreed, true);
});
test("both parties confirm handover and return; no financial operation performed", () => {
  let s = approved();
  s = apply(s, "PROPOSE_DELIVERY", { kind: "ponto_recolha", place: "Loja parceira", at: future() });
  s = apply(s, "CONFIRM_DELIVERY");
  s = apply(s, "HANDOVER", { actor: "owner" });
  assert.equal(s.step, 4);
  assert.throws(() => apply(s, "HANDOVER", { actor: "owner" }));
  s = apply(s, "HANDOVER", { actor: "renter" });
  assert.equal(s.step, 5);
  s = apply(s, "START_RETURN");
  s = apply(s, "RETURN", { actor: "renter" });
  assert.equal(s.step, 6);
  s = apply(s, "RETURN", { actor: "owner" });
  assert.equal(s.step, 7);
  s = apply(s, "RATE", { rating: 5 });
  assert.equal(s.rating, 5);
  assert.ok(s.history.some(line => line.includes("Nenhum valor transferido")));
  assert.equal(start().step, 0);
});
test("incident blocks return until resolved", () => {
  let s = approved();
  s = apply(s, "PROPOSE_DELIVERY", { kind: "transportadora", place: "Ponto de recolha", at: future() });
  s = apply(s, "CONFIRM_DELIVERY");
  s = apply(s, "HANDOVER", { actor: "owner" });
  s = apply(s, "HANDOVER", { actor: "renter" });
  s = apply(s, "REPORT_INCIDENT");
  assert.throws(() => apply(s, "START_RETURN"));
  s = apply(s, "RESOLVE_INCIDENT");
  assert.equal(apply(s, "START_RETURN").step, 6);
});
test("cancellation and refund are fictional only", () => {
  const s = apply(approved(), "CANCEL_REFUND");
  assert.equal(s.step, 8);
  assert.ok(s.history.some(line => line.includes("Não houve cobrança")));
  assert.equal(apply(s, "RESTART").step, 0);
});
test("invalid actors and data are rejected", () => {
  let s = approved();
  assert.throws(() => apply(s, "PROPOSE_DELIVERY", { kind: "outro", place: "Praça", at: future() }));
  s = apply(s, "PROPOSE_DELIVERY", { kind: "em_maos", place: "Praça central", at: future() });
  s = apply(s, "CONFIRM_DELIVERY");
  assert.throws(() => apply(s, "HANDOVER", { actor: "admin" }));
});
