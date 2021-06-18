import { Button, createStyles, makeStyles, Theme } from "@material-ui/core";
import { Add } from "@material-ui/icons";
import { useEffect, useState } from "react";
import { useHistory } from "react-router";
import { scoreClientV2 } from "../../global";
import { ScoreSummary } from "../../ScoreClientV2";
import ScoreSummaryCard from "../atoms/ScoreSummaryCard";

export interface ScoreListContentProps {}

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      height: "100%",
      width: "100%",
    },
    controlBar: {
      height: "40px",
      width: "100%",
    },
    controlButton: {
      margin: "5px",
    },
    itemContainer: {
      height: "100%",
      width: "100%",
      display: "flex",
      flexWrap: "wrap",
    },
    item: {
      height: "250px",
      width: "250px",
      margin: "5px",
    },
  })
);

export default function ScoreListContent(props: ScoreListContentProps) {
  const classes = useStyles();
  const [scoreSummaries, setScoreSummaries] =
    useState<ScoreSummary[] | undefined>(undefined);
  const history = useHistory();

  useEffect(() => {
    const f = async () => {
      if (scoreSummaries !== undefined) return;
      try {
        const scoreSummaries = await scoreClientV2.getMyScoreSummaries();
        setScoreSummaries(scoreSummaries);
      } catch (err) {
        console.log(err);
      }
    };

    f();
  }, []);

  const hanldeNewScore = () => {
    history.push("/scores/new");
  };

  return (
    <div className={classes.root}>
      <div className={classes.controlBar}>
        <Button
          variant="contained"
          color="primary"
          size="small"
          className={classes.controlButton}
          onClick={hanldeNewScore}
          startIcon={<Add />}
        >
          新しい楽譜
        </Button>
      </div>
      <div className={classes.itemContainer}>
        {scoreSummaries?.map((score, index) => (
          <div key={score.id} className={classes.item}>
            <ScoreSummaryCard scoreSummary={score} />
          </div>
        ))}
      </div>
    </div>
  );
}
