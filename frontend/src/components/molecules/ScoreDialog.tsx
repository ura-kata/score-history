import {
  colors,
  createStyles,
  Dialog,
  DialogContent,
  DialogContentText,
  DialogTitle,
  makeStyles,
  Theme
} from "@material-ui/core";
import React from "react";
import { Score } from "../../PracticeManagerApiClient";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    dialogContent:{
      minWidth: "500px",
      margin: theme.spacing(1)
    },
  })
);

interface ScoreDialogProps{
  open: boolean;
  onClose?: (value: string) => void;
  score?: Score;
}

const ScoreDialog = (props: ScoreDialogProps) => {

  const classes = useStyles();

  const _open = props.open;
  const _onClose = props.onClose;
  const _score = props.score;

  return (
    <Dialog onClose={_onClose} open={_open}>
      <DialogTitle>{_score?.title}</DialogTitle>
      <DialogContent className={classes.dialogContent}>
        <DialogContentText>
          {_score?.description}
        </DialogContentText>
      </DialogContent>

    </Dialog>
  );
}

export default ScoreDialog;
