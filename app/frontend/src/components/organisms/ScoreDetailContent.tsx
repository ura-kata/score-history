import { Button, createStyles, makeStyles, Theme } from "@material-ui/core";
import { ArrowBack } from "@material-ui/icons";
import React, { useEffect, useState } from "react";
import { useHistory, useParams } from "react-router";
import { scoreClientV2 } from "../../global";
import { ScoreDetail } from "../../ScoreClientV2";

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
    },
  })
);

export interface ScoreDetailContentProps {}

/** 楽譜の詳細を表示するコンポーネント */
export default function ScoreDetailContent(props: ScoreDetailContentProps) {
  const classes = useStyles();

  const { scoreId } = useParams<{ scoreId: string }>();
  const history = useHistory();
  const [scoreDetail, setScoreDetail] = useState<ScoreDetail | undefined>();

  useEffect(() => {
    if (scoreDetail !== undefined) {
      return;
    }

    const f = async () => {
      try {
        var detail = await scoreClientV2.getDetail(scoreId);
        setScoreDetail(detail);
      } catch (err) {
        console.log(err);
      }
    };
    f();
  }, [scoreDetail, scoreId]);

  console.log(scoreId);
  console.log(scoreDetail);
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
      <div className={classes.contentRoot}></div>
    </div>
  );
}
