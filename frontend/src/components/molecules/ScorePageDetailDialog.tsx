import React from "react";
import {
  Button,
  createStyles,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  IconButton,
  makeStyles,
  Paper,
  PaperProps,
  Theme,
  Typography,
} from "@material-ui/core";
import ChevronLeftIcon from "@material-ui/icons/ChevronLeft";
import ChevronRightIcon from "@material-ui/icons/ChevronRight";
import { ScorePage } from "../../ScoreClient";
import CommentList from "./CommentList";
import ImageLense from "../atoms/ImageLense";

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

const useStyle = makeStyles((theme: Theme) =>
  createStyles({
    dialogContent: {},
    dialogContentRoot: { display: "flex", height: "100%", width: "100%" },
    imageArea: {
      width: "60%",
      height: "100%",
      display: "flex",
      justifyContent: "center",
    },
    commentArea: { width: "40%" },
  })
);

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

  const classes = useStyle();

  const onPrev = () => {
    if (_onPrev) _onPrev();
  };
  const onNext = () => {
    if (_onNext) _onNext();
  };

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
              <ImageLense src={_page?.image} />
            </div>
            <div className={classes.commentArea}>
              <CommentList />
            </div>
          </div>
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
