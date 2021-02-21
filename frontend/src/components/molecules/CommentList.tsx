import {
  Button,
  colors,
  createStyles,
  IconButton,
  Paper,
  TextField,
} from "@material-ui/core";
import { Theme } from "@material-ui/core";
import { Grid, makeStyles, Typography } from "@material-ui/core";
import { Alert } from "@material-ui/lab";
import React, { useEffect, useState } from "react";
import { scoreClient } from "../../global";
import { useScorePathParameter } from "../../hooks/scores/useScorePathParameter";
import { ScoreComment } from "../../ScoreClient";
import EditIcon from "@material-ui/icons/Edit";
import SaveIcon from "@material-ui/icons/Save";
import CancelIcon from "@material-ui/icons/Cancel";
import AddIcon from "@material-ui/icons/Add";
import DeleteIcon from "@material-ui/icons/Delete";
import CheckIcon from "@material-ui/icons/Check";
import ClearIcon from "@material-ui/icons/Clear";

const loadComments = async (
  owner: string,
  scoreName: string,
  version: string,
  pageIndex: number
): Promise<ScoreComment[]> => {
  try {
    console.log("loadComments");
    return await scoreClient.getComments(owner, scoreName, version, pageIndex);
  } catch (err) {
    console.log(err);
    throw new Error(`コメントの取得に失敗しました`);
  }
};

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      display: "flex",
      flexDirection: "column",
      height: "100%",
    },
    mainContainer: {
      flexGrow: 1,
      overflowY: "auto",
      paddingRight: "10px",
      paddingLeft: "10px",
    },
    alartContainer: {},
    commentContainer: {},
    buttonContainer: {},
    commentAddField: {
      width: "100%",
    },
  })
);

const useCommentCardStyles = makeStyles((theme: Theme) =>
  createStyles({
    comment: {
      userSelect: "none",
      // pointerEvents: "none",
    },
    commentArea: {
      padding: "5px",
    },
  })
);

interface CommentCardProps {
  comment: ScoreComment;
  enableRemoveButton?: boolean;
  onRemoveClick?: (comment: ScoreComment) => void;
}
function CommentCard(props: CommentCardProps) {
  const classes = useCommentCardStyles();

  const _comment = props.comment;
  const _enableRemove = Boolean(props.enableRemoveButton);
  const _onRemoveClick = props.onRemoveClick;
  const handleOnClick = () => {
    if (!_onRemoveClick) return;
    _onRemoveClick(_comment);
  };
  return (
    <Paper elevation={3}>
      <Grid container>
        <Grid item xs={12} className={classes.commentArea}>
          {_comment.comment?.split("\n").map((t, index) => (
            <Typography key={index}>{t}</Typography>
          ))}
        </Grid>
        {_enableRemove ? (
          <Grid item xs={12}>
            <Grid container justify="flex-end">
              <Grid item>
                <IconButton onClick={handleOnClick}>
                  <DeleteIcon />
                </IconButton>
              </Grid>
            </Grid>
          </Grid>
        ) : (
          <></>
        )}
      </Grid>
    </Paper>
  );
}

// -------------------------------------------------------------------

export interface CommentListProps {
  edit?: boolean;
  onEditChange?: (edit: boolean) => void;
  onUpdated?: () => void;
}

export default function CommentList(props: CommentListProps) {
  const _edit = props.edit;
  const _onEditChange = props.onEditChange;
  const _onUpdated = props.onUpdated;

  const classes = useStyles();

  const [comments, setComments] = useState<ScoreComment[]>([]);
  const [adding, setAdding] = useState(false);
  const [
    commentLoadedErrorMessage,
    setCommentLoadedErrorMessage,
  ] = useState<string>();

  const pathParam = useScorePathParameter();
  const owner = pathParam.owner;
  const scoreName = pathParam.owner;
  const version = pathParam.version;
  const pageIndex = pathParam.pageIndex;

  const edit = Boolean(_edit);

  useEffect(() => {
    if (!owner) return;
    if (!scoreName) return;
    if (!version) return;
    if (pageIndex === undefined) return;
    const f = async () => {
      try {
        const c = await loadComments(owner, scoreName, version, pageIndex);
        setComments(c);
        setCommentLoadedErrorMessage(undefined);
      } catch (err) {
        setCommentLoadedErrorMessage(err.message);
      }
    };

    f();
  }, [owner, pageIndex, scoreName, version]);

  const handleOnEditClick = () => {
    if (!_onEditChange) return;
    _onEditChange(true);
  };
  const handleOnSaveClick = () => {
    try {
      // TODO 更新
    } catch (err) {
      console.log(err);
      return;
    }
    if (_onUpdated) {
      _onUpdated();
    }
    if (_onEditChange) {
      _onEditChange(false);
    }
  };
  const handleOnCancelClick = () => {
    if (!_onEditChange) return;
    _onEditChange(false);
  };

  const handleOnAddClick = () => {
    setAdding(true);
  };

  const handleCommentAddOk = () => {
    setAdding(false);
  };

  const handleCommentAddCancel = () => {
    setAdding(false);
  };

  return (
    <div className={classes.root}>
      <div className={classes.mainContainer}>
        <Grid container className={classes.alartContainer} spacing={2}>
          <Grid item xs={12}>
            {commentLoadedErrorMessage ? (
              <Alert severity="error">{commentLoadedErrorMessage}</Alert>
            ) : (
              <></>
            )}
          </Grid>
        </Grid>
        <Grid container className={classes.commentContainer} spacing={2}>
          {edit && adding ? (
            <Grid item xs={12}>
              <Grid container>
                <Grid item xs={12}>
                  <form>
                    <TextField
                      label="新しいコメント"
                      multiline
                      variant="outlined"
                      className={classes.commentAddField}
                    />
                  </form>
                </Grid>
                <Grid item xs={12}>
                  <Grid container justify="flex-end">
                    <Grid item>
                      <IconButton onClick={handleCommentAddOk}>
                        <CheckIcon />
                      </IconButton>
                    </Grid>
                    <Grid item>
                      <IconButton onClick={handleCommentAddCancel}>
                        <ClearIcon />
                      </IconButton>
                    </Grid>
                  </Grid>
                </Grid>
              </Grid>
            </Grid>
          ) : (
            comments.map((comment, index) => {
              const handleOnRemoveClick = () => {};
              return (
                <Grid item xs={12} key={index}>
                  <CommentCard
                    comment={comment}
                    enableRemoveButton={edit}
                    onRemoveClick={handleOnRemoveClick}
                  />
                </Grid>
              );
            })
          )}
        </Grid>
      </div>

      <div className={classes.buttonContainer}>
        <Grid container justify="flex-end">
          {edit ? (
            <>
              <Grid item>
                <IconButton onClick={handleOnAddClick}>
                  <AddIcon />
                </IconButton>
              </Grid>
              <Grid item>
                <IconButton onClick={handleOnSaveClick}>
                  <SaveIcon />
                </IconButton>
              </Grid>
              <Grid item>
                <IconButton onClick={handleOnCancelClick}>
                  <CancelIcon />
                </IconButton>
              </Grid>
            </>
          ) : (
            <Grid item>
              <IconButton onClick={handleOnEditClick}>
                <EditIcon />
              </IconButton>
            </Grid>
          )}
        </Grid>
      </div>
    </div>
  );
}
