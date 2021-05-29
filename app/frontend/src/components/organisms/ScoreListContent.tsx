import {
  Button,
  colors,
  createStyles,
  makeStyles,
  Theme,
} from "@material-ui/core";
import { Add, AddBox } from "@material-ui/icons";
import React, { useEffect, useState } from "react";
import { useHistory } from "react-router";
import { scoreClientV2 } from "../../global";
import { ScoreSummary } from "../../ScoreClientV2";

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
      height: "100px",
      width: "100px",
      backgroundColor: colors.green[200],
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
  });

  const hanldeNewScore = () => {
    history.push("/score/new");
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
            <p>{score.title}</p>
            <p>{score.description}</p>
          </div>
        ))}
      </div>
    </div>
  );
}
