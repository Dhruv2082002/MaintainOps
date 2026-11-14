import { expect, it } from "vitest";
import App from "./App.tsx";

// Placeholder so the test runner is wired up; real component tests arrive with the frontend shell.
it("exports the App component", () => {
  expect(typeof App).toBe("function");
});
