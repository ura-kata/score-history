import React, { useEffect, useMemo, useRef } from "react";
import { privateScoreItemUrlGen } from "../../global";
import Viewer from "viewerjs";
import { ScorePage } from "../../ScoreClientV2";

export interface ViewContentProps {
  ownerId?: string;
  scoreId?: string;
  pages?: ScorePage[];
  pageIndex?: number;
}

export function ViewContent(props: ViewContentProps) {
  const _ownerId = props.ownerId;
  const _scoreId = props.scoreId;
  const _pages = props.pages ?? [];
  const _pageIndex = props.pageIndex;
  const ulRef = useRef<HTMLUListElement>(null);

  const viewerRef = useRef<Viewer>();
  const viewerContainerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!ulRef.current) return;
    if (!viewerContainerRef.current) return;
    const viewer = new Viewer(ulRef.current, {
      url: "data-original",
      loop: false,
      container: viewerContainerRef.current,
      inline: true,
      title: false,
      rotatable: false,
      toolbar: {
        zoomIn: 2,
        zoomOut: 2,
        oneToOne: 2,
        reset: 2,
        play: false,
        prev: 2,
        next: 2,
        rotateLeft: false,
        rotateRight: false,
        flipHorizontal: false,
        flipVertical: false,
      },
    });
    viewerRef.current = viewer;

    return () => {
      viewer.destroy();
      viewerRef.current = undefined;
    };
  }, [_ownerId, _scoreId, _pages]);

  useEffect(() => {
    if (!viewerRef.current) return;
    if (_pageIndex === undefined) return;
    viewerRef.current.view(_pageIndex);
  }, [_pageIndex]);

  const div = useMemo(
    () => (
      <div
        ref={viewerContainerRef}
        style={{ width: "100%", height: "calc(100% - 1px)" }}
      >
        <ul ref={ulRef} style={{ display: "none" }}>
          {_ownerId && _scoreId ? (
            _pages.map((p) => {
              const customAttr = {
                "data-original": privateScoreItemUrlGen.getImageUrl(
                  _ownerId,
                  _scoreId,
                  p
                ),
              };
              return (
                <li key={p.id}>
                  <img
                    src={privateScoreItemUrlGen.getThumbnailImageUrl(
                      _ownerId,
                      _scoreId,
                      p
                    )}
                    {...customAttr}
                  />
                </li>
              );
            })
          ) : (
            <></>
          )}
        </ul>
      </div>
    ),
    [_ownerId, _scoreId, _pages]
  );

  return div;
}
