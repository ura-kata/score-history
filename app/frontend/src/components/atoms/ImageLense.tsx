import {
  colors,
  createStyles,
  makeStyles,
  Popper,
  Theme,
} from "@material-ui/core";
import React, { useRef, useState } from "react";
import { useElementSize } from "../../hooks/scores/useElementSize";

const useStyle = makeStyles((theme: Theme) =>
  createStyles({
    imageContainer: {
      height: "100%",
      width: "100%",
      display: "inline-block",
      position: "relative",
    },
    targetImg: {
      top: 0,
      position: "absolute",
      height: "auto",
      width: "auto",
      maxHeight: "100%",
      maxWidth: "100%",
      userSelect: "none",
      pointerEvents: "none",
    },
    slidesContainer: {
      width: "344px",
      overflow: "hidden",
    },
    imageLenseRoot: {
      width: "100%",
      height: "100%",
    },
  })
);

interface Size {
  width: number;
  height: number;
}

// ---------------------------------------------------------------------------

const zoomAreaSize = {
  width: 300,
  height: 300,
};

// ---------------------------------------------------------------------------

export interface ImageLenseProps {
  src?: string;
}

export default function ImageLense(props: ImageLenseProps) {
  const _src = props.src;

  const [lenseSize, setLenseSize] = useState<Size>({
    width: 100,
    height: 100,
  });
  const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);
  const [zoomImageSize, setZoomImageSize] = useState<Size>();
  const [visibleLense, setVisibleLense] = useState(false);
  // Todo マウスホイールで拡大エリアの大きさ変更に使う
  const [lenseRealSize, setLenseRealSize] = useState<Size>({
    height: 400,
    width: 400,
  });

  const imageContainerRef = useRef<HTMLDivElement>(null);
  const lensRef = useRef<HTMLDivElement>(null);
  const targetImageRef = useRef<HTMLImageElement>(null);
  const zoomImageRef = useRef<HTMLImageElement>(null);
  const lensContainerRef = useRef<HTMLDivElement>(null);

  const targetImageRect = useElementSize(targetImageRef);

  const classes = useStyle();

  const useActiveStyle = makeStyles((theme: Theme) =>
    createStyles({
      lensContainer: {
        width: targetImageRect?.width,
        height: targetImageRect?.height,
        position: "absolute",
        top: 0,
        border: "1px solid #ccc",
      },
      lens: {
        position: "absolute",
        top: "30px",
        left: "30px",
        zIndex: 2,
        backgroundColor: colors.blueGrey[400],
        opacity: 0.3,
        width: lenseSize.width + "px",
        height: lenseSize.height + "px",
        display: visibleLense ? "inline" : "none",
      },
      zoomImageRoot: {
        position: "relative",
        height: zoomAreaSize.height + "px",
        width: zoomAreaSize.width + "px",
      },
      zoomClipArea: {
        display: "block",
        position: "absolute",
        top: 0,
        left: "0px",
        border: "1px solid #ccc",
        height: zoomAreaSize.height + "px",
        width: zoomAreaSize.width + "px",
        overflow: "hidden",
      },
      zoomImage: {
        width: zoomImageSize ? zoomImageSize.width + "px" : undefined,
        height: zoomImageSize ? zoomImageSize.height + "px" : undefined,
        marginTop: "-30px",
        marginLeft: "-30px",
      },
    })
  );

  const activeClasses = useActiveStyle();

  const showLenseArea = () => {
    const container = lensContainerRef?.current;
    setAnchorEl(container);
  };
  // React.useEffect(() => {
  //   const container = lensContainerRef?.current;
  //   setAnchorEl(container);
  // }, []);

  const hideLenseArea = () => {
    setAnchorEl(null);
  };

  const showLense = () => {
    setVisibleLense(true);
  };

  const hideLense = () => {
    setVisibleLense(false);
  };

  const handleImageOnMouseEnter = () => {
    showLense();
    showLenseArea();
  };
  const handleImageOnMouseLeave = () => {
    hideLense();
    hideLenseArea();
  };

  const moveLens = (event: React.MouseEvent<HTMLDivElement, MouseEvent>) => {
    const container = lensContainerRef?.current;
    const lense = lensRef?.current;

    if (lense === null) return;
    if (container === null) return;

    const containerRect = container.getBoundingClientRect();
    const lenseRect = lense.getBoundingClientRect();

    // 若干 lense がコンテナの範囲から右と下にはみ出るので少し補正
    const containerRectHeight = Math.floor(containerRect.height) - 1;
    const containerRectWidth = Math.floor(containerRect.width) - 1;
    const top = Math.max(
      0,
      Math.min(
        event.pageY - containerRect.top - lenseRect.height * 0.5,
        containerRectHeight - lenseRect.height
      )
    );
    const left = Math.max(
      0,
      Math.min(
        event.pageX - containerRect.left - lenseRect.width * 0.5,
        containerRectWidth - lenseRect.width
      )
    );
    lense.style.top = top + "px";
    lense.style.left = left + "px";

    const zoomImage = zoomImageRef?.current;
    if (zoomImage === null) return;

    const zoomImageRect = zoomImage.getBoundingClientRect();
    const marginTop = -((top * zoomImageRect.height) / containerRectHeight);
    const marginLeft = -((left * zoomImageRect.width) / containerRectWidth);
    zoomImage.style.marginTop = marginTop + "px";
    zoomImage.style.marginLeft = marginLeft + "px";
  };
  const handleImageOnMouseMove = (
    event: React.MouseEvent<HTMLDivElement, MouseEvent>
  ) => {
    moveLens(event);
  };

  const handleOnImageLoaded = (
    event: React.SyntheticEvent<HTMLImageElement, Event>
  ) => {
    const image = event.target as HTMLImageElement;

    const imageRect = image.getBoundingClientRect();

    const lenseWidth =
      lenseRealSize.width * (imageRect.width / image.naturalWidth);
    const lenseHeight =
      lenseRealSize.height * (imageRect.height / image.naturalHeight);

    setLenseSize({
      width: lenseWidth,
      height: lenseHeight,
    });
  };
  const handleOnOnZoomImageLoaded = (
    event: React.SyntheticEvent<HTMLImageElement, Event>
  ) => {
    const zoomImage = event.target as HTMLImageElement;

    const zoomImageWidth =
      zoomImage.naturalWidth * (zoomAreaSize.width / lenseRealSize.width);
    const zoomImageHeight =
      zoomImage.naturalHeight * (zoomAreaSize.height / lenseRealSize.height);

    setZoomImageSize({
      width: zoomImageWidth,
      height: zoomImageHeight,
    });
  };

  return (
    <div className={classes.imageLenseRoot}>
      <div ref={imageContainerRef} className={classes.imageContainer}>
        <img
          className={classes.targetImg}
          src={_src}
          alt="score-page"
          onLoad={handleOnImageLoaded}
          ref={targetImageRef}
        />
        <div
          className={activeClasses.lensContainer}
          ref={lensContainerRef}
          onMouseEnter={handleImageOnMouseEnter}
          onMouseLeave={handleImageOnMouseLeave}
          onMouseMove={handleImageOnMouseMove}
        >
          <div ref={lensRef} className={activeClasses.lens}></div>
        </div>
      </div>
      <Popper
        open={Boolean(anchorEl)}
        anchorEl={anchorEl}
        style={{ zIndex: 99999 }}
        placement="right-start"
      >
        <div className={activeClasses.zoomImageRoot}>
          <div className={activeClasses.zoomClipArea}>
            <img
              src={_src}
              alt="score-zoom-page"
              className={activeClasses.zoomImage}
              onLoad={handleOnOnZoomImageLoaded}
              ref={zoomImageRef}
            />
          </div>
          <div className={classes.slidesContainer}></div>
        </div>
      </Popper>
    </div>
  );
}
