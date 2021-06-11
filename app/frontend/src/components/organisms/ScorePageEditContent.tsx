import {
  Button,
  createStyles,
  IconButton,
  makeStyles,
  Theme,
} from "@material-ui/core";
import React, { useCallback, useMemo, useState } from "react";
import { useHistory, useParams } from "react-router-dom";
import useMeyScoreDetail from "../../hooks/scores/useMeyScoreDetail";
import ArrowBackIcon from "@material-ui/icons/ArrowBack";
import { AppContext } from "../../AppContext";
import EditPageImageUploadContent from "../atoms/EditPageImageUploadContent";
import EditPageSortContent from "../atoms/EditPageSortContent";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    toolBar: {
      width: "100%",
      height: "50px",
      display: "flex",
      "& button": {
        margin: "4px",
      },
    },
    editPageContainer: {
      width: "100%",
    },
  })
);

interface ScorePageEditContentProps {}

export default function ScorePageEditContent(props: ScorePageEditContentProps) {
  const classes = useStyles();
  const { scoreId, pageId } =
    useParams<{ scoreId?: string; pageId?: string }>();
  const history = useHistory();
  const detail = useMeyScoreDetail({ scoreId, retryCount: 3 });

  const appContext = React.useContext(AppContext);

  const [editState, setEditState] = useState<"upload" | "sort">("sort");

  const _userData = appContext.userData;
  const _ownerId = _userData?.id;

  const pages = detail?.data.pages ?? [];

  const handleOnBackClick = () => {
    history.goBack();
  };

  const handleOnImageUploadCompleted = () => {
    setEditState("sort");
  };
  const handleOnImageUploadCancel = () => {
    setEditState("sort");
  };
  const handleOnUpload = () => {
    setEditState("upload");
  };

  return (
    <div style={{ width: "100%" }}>
      <div
        className={classes.toolBar}
        style={{ visibility: editState === "sort" ? undefined : "hidden" }}
      >
        <IconButton onClick={handleOnBackClick}>
          <ArrowBackIcon />
        </IconButton>
      </div>
      <div className={classes.editPageContainer}>
        <div style={{ display: editState === "upload" ? undefined : "none" }}>
          <EditPageImageUploadContent
            ownerId={_ownerId}
            scoreId={scoreId}
            pages={pages}
            onCompleted={handleOnImageUploadCompleted}
            onCancel={handleOnImageUploadCancel}
          />
        </div>
        <div style={{ display: editState === "sort" ? undefined : "none" }}>
          <EditPageSortContent
            ownerId={_ownerId}
            scoreId={scoreId}
            pages={pages}
            onUpload={handleOnUpload}
          />
        </div>
      </div>
    </div>
  );
}
