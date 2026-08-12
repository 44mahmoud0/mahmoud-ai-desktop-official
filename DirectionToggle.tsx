import { Languages } from "lucide-react";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import {
  directionActionLabel,
  directionLabel,
  DIRECTION_STORAGE_KEY,
  nextDirection,
  normalizeDirection,
  type Direction,
} from "@shared/direction";

export default function DirectionToggle() {
  const [direction, setDirection] = useState<Direction>(() => {
    if (typeof window === "undefined") return "ltr";
    return normalizeDirection(window.localStorage.getItem(DIRECTION_STORAGE_KEY));
  });

  useEffect(() => {
    document.documentElement.dir = direction;
    document.documentElement.dataset.direction = direction;
    window.localStorage.setItem(DIRECTION_STORAGE_KEY, direction);
  }, [direction]);

  const next = nextDirection(direction);

  return (
    <Button
      type="button"
      variant="ghost"
      size="sm"
      className="direction-toggle hidden min-h-9 gap-2 px-2 text-[0.68rem] font-bold uppercase tracking-[0.12em] text-[var(--ink-soft)] md:inline-flex"
      aria-label={directionActionLabel(direction)}
      aria-pressed={direction === "rtl"}
      title={directionActionLabel(direction)}
      onClick={() => setDirection(next)}
    >
      <Languages size={15} aria-hidden="true" />
      <span>{directionLabel(direction)}</span>
    </Button>
  );
}
