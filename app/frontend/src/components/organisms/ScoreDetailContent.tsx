import { Button, createStyles, makeStyles, Theme } from "@material-ui/core";
import { ArrowBack } from "@material-ui/icons";
import { useHistory, useParams } from "react-router";
import LatestScoreContent from "../molecules/ScoreDetailContent/LatestScoreContent";
import ScoreSnapshotContent from "../molecules/ScoreDetailContent/ScoreSnapshotContent";
import SnapshotNameList from "../molecules/ScoreDetailContent/SnapshotNameList";

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
      margin: "10px 5px 10px 10px",
    },
    snapshotArea: {
      width: "200px",
      margin: "10px 10px 10px 5px",
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
    </div>
  );
}
