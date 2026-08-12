import { describe, expect, it } from "vitest";
import {
  directionActionLabel,
  directionLabel,
  nextDirection,
  normalizeDirection,
} from "@shared/direction";

describe("direction foundation", () => {
  it("keeps English-first LTR as the safe default", () => {
    expect(normalizeDirection(undefined)).toBe("ltr");
    expect(normalizeDirection(null)).toBe("ltr");
    expect(normalizeDirection("en")).toBe("ltr");
  });

  it("normalizes and toggles the supported directions", () => {
    expect(normalizeDirection("rtl")).toBe("rtl");
    expect(nextDirection("ltr")).toBe("rtl");
    expect(nextDirection("rtl")).toBe("ltr");
  });

  it("provides stable UI labels for the toggle", () => {
    expect(directionLabel("ltr")).toBe("LTR");
    expect(directionLabel("rtl")).toBe("RTL");
    expect(directionActionLabel("ltr")).toBe("Switch to RTL layout");
    expect(directionActionLabel("rtl")).toBe("Switch to LTR layout");
  });
});
