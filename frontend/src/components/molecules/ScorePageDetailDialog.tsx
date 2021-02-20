import React, { useRef, useState } from "react";
import {
  Button,
  colors,
  createStyles,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  IconButton,
  makeStyles,
  Popper,
  Theme,
  Typography,
} from "@material-ui/core";
import ChevronLeftIcon from "@material-ui/icons/ChevronLeft";
import ChevronRightIcon from "@material-ui/icons/ChevronRight";
import { ScorePage } from "../../ScoreClient";
import CommentList from "./CommentList";

// -----------------------------

function DialogPaperComponent(props: PaperProps) {
  return (
    <Paper
      {...props}
      // コメントをスクロールするために高さを指定する
      style={{ height: "100%" }}
    />
  );
}

// -----------------------------
export interface ScorePageDetailDialogProps {
  page?: ScorePage;
  open: boolean;
  onClose?: () => void;
  onPrev?: () => void;
  onNext?: () => void;
}
const ScorePageDetailDialog = (props: ScorePageDetailDialogProps) => {
  const _page = props.page;
  const _open = props.open;
  const _onClose = props.onClose;
  const _onPrev = props.onPrev;
  const _onNext = props.onNext;

  // Todo マウスホイールで拡大エリアの大きさ変更に使う
  const [lenseRealSize, setLenseRealSize] = useState<{
    height: number;
    width: number;
  }>({
    height: 400,
    width: 400,
  });

  const [lenseSize, setLenseSize] = useState({
    width: 100,
    height: 100,
  });
  const zoomAreaSize = {
    width: 400,
    height: 400,
  };
  const [zoomImageSize, setZoomImageSize] = useState<{
    width: number | undefined;
    height: number | undefined;
  }>({
    width: undefined,
    height: undefined,
  });

  const [visibleLense, setVisibleLense] = useState(false);

  const classes = makeStyles((theme: Theme) =>
    createStyles({
      dialogContent: {},
      lensContainer: {
        minHeight: "70vh",
        display: "inline-block",
        position: "relative",
        border: "1px solid #ccc",
      },
      targetImg: {
        height: "70vh",
        userSelect: "none",
        pointerEvents: "none",
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
      images: {
        position: "relative",
        height: zoomAreaSize.height + "px",
        width: zoomAreaSize.width + "px",
      },
      zoomArea: {
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
        width: zoomImageSize.width ? zoomImageSize.width + "px" : undefined,
        height: zoomImageSize.height ? zoomImageSize.height + "px" : undefined,
        marginTop: "-30px",
        marginLeft: "-30px",
      },
      slidesContainer: {
        width: "344px",
        overflow: "hidden",
      },
      dialogContentRoot: { display: "flex", height: "100%", width: "100%" },
      imageArea: {
        width: "60%",
        display: "flex",
        justifyContent: "center",
      },
      commentArea: { width: "40%" },
    })
  )();

  const lensContainerRef = useRef<HTMLDivElement>(null);
  const lensRef = useRef<HTMLDivElement>(null);
  const zoomImageRef = useRef<HTMLImageElement>(null);
  const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);

  const onPrev = () => {
    if (_onPrev) _onPrev();
  };
  const onNext = () => {
    if (_onNext) _onNext();
  };

  const showLenseArea = () => {
    const container = lensContainerRef?.current;
    setAnchorEl(container);
  };

  const hideLenseArea = () => {
    setAnchorEl(null);
  };

  const showLense = () => {
    setVisibleLense(true);
  };

  const hideLense = () => {
    setVisibleLense(false);
  };

  const handleImageOnMouseEnter = (
    event: React.MouseEvent<HTMLDivElement, MouseEvent>
  ) => {
    showLense();
    showLenseArea();
  };
  const handleImageOnMouseLeave = (
    event: React.MouseEvent<HTMLDivElement, MouseEvent>
  ) => {
    hideLense();
    hideLenseArea();
  };

  const handleImageOnMouseMove = (
    event: React.MouseEvent<HTMLDivElement, MouseEvent>
  ) => {
    const container = lensContainerRef?.current;
    const lense = lensRef?.current;
    const zoomImage = zoomImageRef?.current;

    if (lense === null) return;
    if (container === null) return;
    if (zoomImage === null) return;

    const containerRect = container.getBoundingClientRect();
    const lenseRect = lense.getBoundingClientRect();
    const zoomImageRect = zoomImage.getBoundingClientRect();

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

    const marginTop = -((top * zoomImageRect.height) / containerRectHeight);
    const marginLeft = -((left * zoomImageRect.width) / containerRectWidth);
    zoomImage.style.marginTop = marginTop + "px";
    zoomImage.style.marginLeft = marginLeft + "px";
  };

  const imageOnLoaded = (
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

  const zoomImageOnLoaded = (
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

  const openLenseArea = Boolean(anchorEl);

  return (
    <>
      <Dialog
        onClose={_onClose}
        open={_open}
        fullWidth={true}
        maxWidth={"md"}
        PaperComponent={DialogPaperComponent}
      >
        <DialogTitle>
          <Typography align="center">{_page?.number}</Typography>
        </DialogTitle>
        <DialogContent dividers className={classes.dialogContent}>
          <div className={classes.dialogContentRoot}>
            <div className={classes.imageArea}>
                  <div
                    id="lense-container"
                    ref={lensContainerRef}
                    className={classes.lensContainer}
                    onMouseEnter={handleImageOnMouseEnter}
                    onMouseLeave={handleImageOnMouseLeave}
                    onMouseMove={handleImageOnMouseMove}
                  >
                    <img
                      src={_page?.image}
                      alt={_page?.number}
                      className={classes.targetImg}
                      onLoad={imageOnLoaded}
                    />
                    <div id="lense" ref={lensRef} className={classes.lens} />
                  </div>
            </div>
            <div className={classes.commentArea}>
              <CommentList />
            </div>
          </div>
          <Popper
            open={openLenseArea}
            anchorEl={anchorEl}
            style={{ zIndex: 99999 }}
            placement="right-start"
          >
            <div className={classes.images}>
              <div className={classes.zoomArea}>
                <img
                  src={_page?.image}
                  alt={_page?.number}
                  className={classes.zoomImage}
                  onLoad={zoomImageOnLoaded}
                  ref={zoomImageRef}
                />
              </div>
              <div className={classes.slidesContainer}></div>
            </div>
          </Popper>
        </DialogContent>
        <DialogActions>
          <Grid container>
            <Grid item xs={8}>
              <Grid container>
                <Grid item xs style={{ textAlign: "center" }}>
                  <IconButton
                    onClick={onPrev}
                    color="primary"
                    disabled={_onPrev === undefined}
                  >
                    <ChevronLeftIcon />
                  </IconButton>
                </Grid>
                <Grid item xs style={{ textAlign: "center" }}>
                  <IconButton
                    onClick={onNext}
                    color="primary"
                    disabled={_onNext === undefined}
                  >
                    <ChevronRightIcon />
                  </IconButton>
                </Grid>
              </Grid>
            </Grid>
            <Grid item xs={4}>
              <Grid container justify="flex-end">
                <Grid item xs style={{ textAlign: "right" }}>
                  <Button onClick={_onClose} color="primary">
                    Close
                  </Button>
                </Grid>
              </Grid>
            </Grid>
          </Grid>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default ScorePageDetailDialog;
