import {
  Button,
  createStyles,
  IconButton,
  makeStyles,
  Theme,
} from "@material-ui/core";
import { ArrowBack } from "@material-ui/icons";
import React, { useEffect, useMemo, useState } from "react";
import { useHistory, useParams } from "react-router";
import { AppContext } from "../../AppContext";
import useMeyScoreDetail from "../../hooks/scores/useMeyScoreDetail";
import DetailEditableDescription from "../atoms/DetailEditableDescription";
import DetailEditableTitle from "../atoms/DetailEditableTitle";
import LatestScoreContent from "../atoms/LatestScoreContent";
import PageContent from "../atoms/PageContent";
import ScoreSnapshotContent from "../atoms/ScoreSnapshotContent";
import SnapshotNameList from "../atoms/SnapshotNameList";
import { ThumbnailListContent } from "../atoms/ThumbnailListContent";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      width: "100%",
    },
    controlBar: {
      width: "100%",
      height: "40px",
    },
    controlButton: {
      margin: "5px",
    },
    contentRoot: {
      width: "100%",
      display: "flex",
    },
    scoreDataArea: {
      width: "calc(100% - 200px)",
    },
    snapshotArea: {
      width: "200px",
    },
  })
);

interface PathParameters {
  scoreId?: string;
  pageId?: string;
  snapshotId?: string;
}

export interface ScoreDetailContentProps {}

/** 楽譜の詳細を表示するコンポーネント */
export default function ScoreDetailContent(props: ScoreDetailContentProps) {
  const classes = useStyles();

  const { scoreId, pageId, snapshotId } = useParams<PathParameters>();

  const history = useHistory();

  const handleBack = () => {
    history.push("/");
  };

  const handleLatestOnClick = () => {
    history.push(`/scores/${scoreId}`);
  };

  return (
    <div className={classes.root}>
      <div className={classes.controlBar}>
        <Button
          variant="contained"
          color="inherit"
          size="small"
          className={classes.controlButton}
          startIcon={<ArrowBack />}
          onClick={handleBack}
        >
          戻る
        </Button>
        <Button onClick={handleLatestOnClick}>最新</Button>
      </div>
      <div className={classes.contentRoot}>
        <div
          className={classes.scoreDataArea}
          style={{ display: snapshotId ? "none" : undefined }}
        >
          <LatestScoreContent />
        </div>
        <div
          className={classes.scoreDataArea}
          style={{ display: snapshotId ? undefined : "none" }}
        >
          <ScoreSnapshotContent />
        </div>
        <div className={classes.snapshotArea}>
          <SnapshotNameList />
        </div>
      </div>

      {/* {scoreId && pageId ? <PageContent scoreId={scoreId} /> : <></>} */}
    </div>
  );
}
