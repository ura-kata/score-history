import {
  Button,
  createStyles,
  IconButton,
  makeStyles,
  Theme,
} from "@material-ui/core";
import { ArrowBack } from "@material-ui/icons";
import React, { useEffect, useState } from "react";
import { useHistory, useParams } from "react-router";
import { AppContext } from "../../AppContext";
import useMeyScoreDetail from "../../hooks/scores/useMeyScoreDetail";
import DetailEditableDescription from "../atoms/DetailEditableDescription";
import DetailEditableTitle from "../atoms/DetailEditableTitle";
import PageContent from "../atoms/PageContent";
import { ThumbnailListContent } from "../atoms/ThumbnailListContent";
import EditIcon from "@material-ui/icons/Edit";

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
    },
  })
);

export interface ScoreDetailContentProps {}

/** 楽譜の詳細を表示するコンポーネント */
export default function ScoreDetailContent(props: ScoreDetailContentProps) {
  const classes = useStyles();

  const { scoreId, pageId } =
    useParams<{ scoreId?: string; pageId?: string }>();
  const history = useHistory();

  const detail = useMeyScoreDetail({ scoreId, retryCount: 3 });

  const [title, setTitle] = useState<string>("");
  const [description, setDescription] = useState<string>("");

  const appContext = React.useContext(AppContext);

  const _userData = appContext.userData;

  useEffect(() => {
    setTitle(detail?.data.title ?? "");

    const desc = detail?.hashSet
      ? detail.hashSet[detail.data.descriptionHash]
      : "";
    setDescription(desc);
  }, [detail]);

  const handleBack = () => {
    history.push("/");
  };

  const handleOnChangeTitle = (newTitle: string) => {
    setTitle(newTitle);
  };

  const handleOnChangeDescription = (newDescription: string) => {
    setDescription(newDescription);
  };
  const handleOnPageEditClick = () => {
    history.push(`/scores/${scoreId}/edit-page`);
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
            <DetailEditableDescription
              id={scoreId}
              description={description}
              onChangeDescription={handleOnChangeDescription}
            />
          </div>
        </div>
        <div className={classes.thumbnailRoot}>
          <div className={classes.thumbnailControlBar}>
            <IconButton onClick={handleOnPageEditClick}>
              <EditIcon />
            </IconButton>
          </div>
          <div className={classes.thumbnailContainer}>
            {/* <ThumbnailListContent
            ownerId={_userData?.id}
            scoreId={scoreId}
            pages={detail?.data.pages}
          /> */}
            <PageContent
              ownerId={_userData?.id}
              scoreId={scoreId}
              pages={detail?.data.pages}
              pageId={pageId}
            />
          </div>
        </div>
      </div>

      {/* {scoreId && pageId ? <PageContent scoreId={scoreId} /> : <></>} */}
    </div>
  );
}
