import { Button, colors, createStyles } from "@material-ui/core";
import { Theme } from "@material-ui/core";
import { Grid, makeStyles, Typography } from "@material-ui/core";
import { Alert } from "@material-ui/lab";
import React, { useEffect, useState } from "react";
import { scoreClient } from "../../global";
import { useScorePathParameter } from "../../hooks/scores/useScorePathParameter";
import { ScoreComment } from "../../ScoreClient";

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
    },
    alartContainer: {},
    commentContainer: {},
    buttonContainer: {},
    comment: {
      userSelect: "none",
      // pointerEvents: "none",
    },
  })
);

export interface CommentListProps {}

export default function CommentList(props: CommentListProps) {
  const classes = useStyles();

  const [comments, setComments] = useState<ScoreComment[]>([]);
  const [edit, setEdit] = useState(false);
  const [
    commentLoadedErrorMessage,
    setCommentLoadedErrorMessage,
  ] = useState<string>();

  const pathParam = useScorePathParameter();
  const owner = pathParam.owner;
  const scoreName = pathParam.owner;
  const version = pathParam.version;
  const pageIndex = pathParam.pageIndex;

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
          {comments.map((comment, index) => {
            return (
              <Grid item xs={12} key={index}>
                <Typography className={classes.comment}>
                  {comment.comment}
                </Typography>
              </Grid>
            );
          })}
        </Grid>
      </div>

      <div className={classes.buttonContainer}>
        <Grid container>
          {edit ? (
            <Grid item></Grid>
          ) : (
            <Grid item>
              <Button variant="outlined">コメントを編集</Button>
            </Grid>
          )}
        </Grid>
      </div>
    </div>
  );
}
