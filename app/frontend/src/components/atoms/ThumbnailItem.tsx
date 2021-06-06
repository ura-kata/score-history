import { Button, createStyles, makeStyles, Theme } from "@material-ui/core";
import React from "react";
import { useHistory } from "react-router";
import { privateScoreItemUrlGen } from "../../global";
import { ScorePage } from "../../ScoreClientV2";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      height: "100%",
    },
  })
);

export interface ThumbnailItemProps {
  ownerId: string;
  scoreId: string;
  page: ScorePage;
}

export function ThumbnailItem(props: ThumbnailItemProps) {
  const _ownerId = props.ownerId;
  const _scoreId = props.scoreId;
  const _page = props.page;
  const classes = useStyles();
  const history = useHistory();

  const thumbnailSrc = privateScoreItemUrlGen.getThumbnailImageUrl(
    _ownerId,
    _scoreId,
    _page
  );

  const handleOnClick = () => {
    history.push(`/scores/${_scoreId}/page/${_page.id}`);
  };

  return (
    <div className={classes.root}>
      <Button onClick={handleOnClick}>
        <img src={thumbnailSrc} />
      </Button>
    </div>
  );
}
