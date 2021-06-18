import { createStyles, IconButton, makeStyles, Theme } from "@material-ui/core";
import DetailEditableDescription from "./DetailEditableDescription";
import DetailEditableTitle from "./DetailEditableTitle";
import EditIcon from "@material-ui/icons/Edit";
import PageContent from "./PageContent";
import { useEffect, useMemo, useState } from "react";
import { useHistory, useParams } from "react-router-dom";
import React from "react";
import { AppContext } from "../../AppContext";
import useMeyScoreDetail from "../../hooks/scores/useMeyScoreDetail";
import useMeyScoreSnapshotDetail from "../../hooks/scores/useMeyScoreSnapshotDetail";
import DetailDescription from "./DetailDescription";
import DetailTitle from "./DetailTitle";
import SnapshotPageContent from "./SnapshotPageContent";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    infoContainer: {
      width: "100%",
    },
    titleContainer: {
      width: "100%",
    },
    descContainer: {
      width: "100%",
    },
    thumbnailRoot: {
      width: "100%",
      display: "flex",
      flexFlow: "column",
    },
    thumbnailControlBar: {
      width: "100%",
      height: "50px",
      display: "flex",
      justifyContent: "flex-end",
    },
    thumbnailContainer: {
      width: "100%",
      display: "flex",
    },
  })
);

interface PathParameters {
  scoreId?: string;
  snapshotId?: string;
  snapshotPageId?: string;
}

export interface ScoreSnapshotContentProps {}

export default function ScoreSnapshotContent(props: ScoreSnapshotContentProps) {
  const { scoreId, snapshotId, snapshotPageId } = useParams<PathParameters>();

  const classes = useStyles();

  const [title, setTitle] = useState<string>("");
  const [description, setDescription] = useState<string>("");

  const [detail, updateDetail] = useMeyScoreSnapshotDetail({
    scoreId,
    snapshotId,
    retryCount: 3,
  });

  const appContext = React.useContext(AppContext);

  const _userData = appContext.userData;

  useEffect(() => {
    setTitle(detail?.data.title ?? "");

    const desc = detail?.hashSet
      ? detail.hashSet[detail.data.descriptionHash]
      : "";
    setDescription(desc);
  }, [detail]);

  const pages = useMemo(() => {
    return [...(detail?.data.pages ?? [])].sort((x, y) => {
      const xn = parseInt("0" + x.page);
      const yn = parseInt("0" + y.page);
      if (xn < yn) return -1;
      if (yn < xn) return 1;
      return 0;
    });
  }, [detail]);

  useEffect(() => {
    updateDetail();
  }, [snapshotId]);

  return (
    <div style={{ width: "100%", height: "100%" }}>
      <div className={classes.infoContainer}>
        <div className={classes.titleContainer}>
          <DetailTitle title={title} />
        </div>
        <div className={classes.descContainer}>
          <DetailDescription description={description} />
        </div>
      </div>
      <div className={classes.thumbnailRoot}>
        <div className={classes.thumbnailControlBar}></div>
        <div className={classes.thumbnailContainer}>
          <SnapshotPageContent
            ownerId={_userData?.id}
            scoreId={scoreId}
            snapshotId={snapshotId}
            pages={pages}
            pageId={snapshotPageId}
          />
        </div>
      </div>
    </div>
  );
}
