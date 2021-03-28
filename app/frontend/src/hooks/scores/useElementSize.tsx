import { RefObject, useEffect, useState } from "react";

export function useElementSize<T>(
  targetRef: RefObject<T>
): DOMRect | undefined {
  const current = (targetRef?.current as any) as HTMLElement | undefined;

  const [rect, setRect] = useState<DOMRect>();

  useEffect(() => {
    const currentRect = current?.getBoundingClientRect();
    setRect(currentRect);

    if (!current) return;
    const observer = new ResizeObserver((mutations) => {
      const rect = current.getBoundingClientRect();
      setRect(rect);
    });
    const options: ResizeObserverOptions = {};

    observer.observe(current, options);

    return () => {
      observer.disconnect();
    };
  }, [current]);

  if (!current) return rect;
  return rect;
}
