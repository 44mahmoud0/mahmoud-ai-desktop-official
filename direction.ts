export type Direction = "ltr" | "rtl";

export const DIRECTION_STORAGE_KEY = "still-signal-direction";

export function normalizeDirection(value: string | null | undefined): Direction {
  return value === "rtl" ? "rtl" : "ltr";
}

export function nextDirection(direction: Direction): Direction {
  return direction === "ltr" ? "rtl" : "ltr";
}

export function directionLabel(direction: Direction): string {
  return direction === "ltr" ? "LTR" : "RTL";
}

export function directionActionLabel(direction: Direction): string {
  return direction === "ltr" ? "Switch to RTL layout" : "Switch to LTR layout";
}

export function directionLanguageNote(direction: Direction): string {
  return direction === "ltr"
    ? "English reading direction"
    : "Right-to-left reading direction";
}
