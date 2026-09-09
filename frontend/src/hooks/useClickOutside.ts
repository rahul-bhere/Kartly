import { useEffect, type RefObject } from "react";

export function useClickOutside<T extends HTMLElement>(ref: RefObject<T | null>, onOutsideClick: () => void) {
  useEffect(() => {
    function handle(event: MouseEvent | TouchEvent) {
      if (ref.current && !ref.current.contains(event.target as Node)) onOutsideClick();
    }
    document.addEventListener("mousedown", handle);
    document.addEventListener("touchstart", handle);
    return () => {
      document.removeEventListener("mousedown", handle);
      document.removeEventListener("touchstart", handle);
    };
  }, [ref, onOutsideClick]);
}
