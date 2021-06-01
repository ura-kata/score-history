import { Button, createStyles, makeStyles, Theme } from "@material-ui/core";
import { ArrowBack } from "@material-ui/icons";
import { useEffect, useState } from "react";
import { useHistory, useParams } from "react-router";
import useMeyScoreDetail from "../../hooks/scores/useMeyScoreDetail";
import DetailEditableTitle from "../atoms/DetailEditableTitle";

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
    infoContainer: {
      width: "100%",
    },
    titleContainer: {
      width: "100%",
    },
    descContainer: {
      width: "100%",
    },
    descP: {},
    thumbnailContainer: {
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

  const detail = useMeyScoreDetail({ scoreId, retryCount: 3 });

  const [title, setTitle] = useState<string>("");

  useEffect(() => {
    setTitle(detail?.data.title ?? "");
  }, [detail]);

  const handleBack = () => {
    history.push("/");
  };

  const description = detail?.hashSet
    ? detail.hashSet[detail.data.descriptionHash]
    : "";

  const handleOnChangeTitle = (newTitle: string) => {
    setTitle(newTitle);
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
        <div className={classes.infoContainer}>
          <div className={classes.titleContainer}>
            <DetailEditableTitle
              id={scoreId}
              title={title}
              onChangeTitle={handleOnChangeTitle}
            />
          </div>
          <div className={classes.descContainer}>
            {description.split("\n").map((p, index) => (
              <p key={index} className={classes.descP}>
                {p}
              </p>
            ))}
          </div>
        </div>
        <div className={classes.thumbnailContainer}></div>
      </div>
    </div>
  );
}
