import {
  Button,
  colors,
  createStyles,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Fab,
  Grid,
  IconButton,
  makeStyles,
  Theme
} from "@material-ui/core";
import { AddIcon } from "@material-ui/data-grid";
import React from "react";
import { Score } from "../../PracticeManagerApiClient";
import ArrowForwardIcon from '@material-ui/icons/ArrowForward';
import ArrowBackIcon from '@material-ui/icons/ArrowBack';

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
  onPrev?: ()=> void;
  onNext?: ()=> void;
  prevDisabled?: boolean;
  nextDisabled?: boolean;
}

const ScoreDialog = (props: ScoreDialogProps) => {

  const classes = useStyles();

  const _open = props.open;
  const _onClose = props.onClose;
  const _score = props.score;
  const _onPrev = props.onPrev;
  const _onNext = props.onNext;
  const _prevDisabled = props.prevDisabled;
  const _nextDisabled = props.nextDisabled;

  const handleUpgrade = ()=>{

  };

  const handleDelete = ()=>{

  };

  return (
    <Dialog onClose={_onClose} open={_open}>
      <DialogTitle>{_score?.title}</DialogTitle>
      <DialogContent className={classes.dialogContent}>
        <DialogContentText>
          {_score?.description}
        </DialogContentText>
      </DialogContent>
      <DialogActions>
        <Grid container>
          <Grid item xs>
            <IconButton onClick={_onPrev} disabled={_prevDisabled}>
              <ArrowBackIcon fontSize="small" />
            </IconButton>
            <IconButton onClick={_onNext} disabled={_nextDisabled}>
              <ArrowForwardIcon fontSize="small" />
            </IconButton>
          </Grid>
          <Grid item xs>
            <div style={{float: "right"}}>
              <Button onClick={handleUpgrade} color="primary" autoFocus>
                更新
              </Button>
              <Button onClick={handleDelete} color="secondary">
                削除
              </Button>
            </div>
          </Grid>

        </Grid>
      </DialogActions>
    </Dialog>
  );
}

export default ScoreDialog;
