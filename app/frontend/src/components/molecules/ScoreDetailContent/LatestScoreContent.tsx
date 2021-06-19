import { createStyles, IconButton, makeStyles, Theme } from "@material-ui/core";
import DetailEditableDescription from "./LatestScoreContent/DetailEditableDescription";
import DetailEditableTitle from "./LatestScoreContent/DetailEditableTitle";
import EditIcon from "@material-ui/icons/Edit";
import PageContent from "./LatestScoreContent/PageContent";
import { useEffect, useMemo, useState } from "react";
import { useHistory, useParams } from "react-router-dom";
import React from "react";
import { AppContext } from "../../../AppContext";
import useMeyScoreDetail from "../../../hooks/scores/useMeyScoreDetail";

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
      margin: "30px 0 0 0",
    },
    thumbnailContainer: {
      width: "100%",
      margin: "30px 0 0 0",
    },
  })
);

interface PathParameters {
  scoreId?: string;
  pageId?: string;
}

export interface LatestScoreContentProps {}

export default function LatestScoreContent(props: LatestScoreContentProps) {
  const { scoreId, pageId } = useParams<PathParameters>();

  const classes = useStyles();

  const history = useHistory();

  const [title, setTitle] = useState<string>("");
  const [description, setDescription] = useState<string>("");

  const [detail, updateDetail] = useMeyScoreDetail({ scoreId, retryCount: 3 });

  const appContext = React.useContext(AppContext);

  const _userData = appContext.userData;

  useEffect(() => {
    setTitle(detail?.data.title ?? "");

    const desc = detail?.hashSet
      ? detail.hashSet[detail.data.descriptionHash]
      : "";
    setDescription(desc);
  }, [detail]);

  useEffect(() => {
    updateDetail();
  }, [scoreId]);

  const handleOnChangeTitle = (newTitle: string) => {
    setTitle(newTitle);
  };

  const handleOnChangeDescription = (newDescription: string) => {
    setDescription(newDescription);
  };
  const pages = useMemo(() => {
    return [...(detail?.data.pages ?? [])].sort((x, y) => {
      const xn = parseInt("0" + x.page);
      const yn = parseInt("0" + y.page);
      if (xn < yn) return -1;
      if (yn < xn) return 1;
      return 0;
    });
  }, [detail]);

  return (
    <div style={{ width: "100%" }}>
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
      <div className={classes.thumbnailContainer}>
        <PageContent
          ownerId={_userData?.id}
          scoreId={scoreId}
          pages={pages}
          pageId={pageId}
        />
      </div>
    </div>
  );
}
