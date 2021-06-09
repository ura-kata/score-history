import {
  Button,
  createStyles,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  IconButton,
  makeStyles,
  Paper,
  styled,
  Theme,
} from "@material-ui/core";
import React, { useEffect, useLayoutEffect, useRef } from "react";
import ArrowBackIcon from "@material-ui/icons/ArrowBack";
import { useHistory } from "react-router";
import { privateScoreItemUrlGen } from "../../global";
import { ScorePage } from "../../ScoreClientV2";
import Viewer from "viewerjs";
import "viewerjs/dist/viewer.min.css";
import ReactDOM from "react-dom";
import { sleep } from "../../util";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    overflow: {
      height: "100vh",
      width: "100vw",
      position: "fixed",
      backgroundColor: "#000000FF",
    },
    content: {
      width: "100px",
      height: "100px",
      backgroundColor: "#FFFFFF",
    },
  })
);

const CustomPaper = styled(Paper)({
  backgroundColor: "#AAAAAAAA",
});

export interface PageContentProps {
  ownerId?: string;
  scoreId?: string;
  pages?: ScorePage[];
  pageId?: string;
}

export default function PageContent(props: PageContentProps) {
  const _ownerId = props.ownerId;
  const _scoreId = props.scoreId;
  const _pages = props.pages ?? [];
  const _pageId = props.pageId;
  const classes = useStyles();
  const history = useHistory();
  const handleOnClickBack = () => {
    history.push(`/scores/${_scoreId}`);
  };

  const handleOnCloseClick = () => {
    console.log("close");
  };

  const ulRef = useRef<HTMLUListElement>(null);

  const viewerRef = useRef<Viewer>();

  useEffect(() => {
    console.log(ulRef.current);
    if (!ulRef.current) return;

    const viewer = new Viewer(ulRef.current, {
      url: "data-original",
    });
    viewerRef.current = viewer;
    console.log("new Viewer");
    console.log(viewer);

    return () => {
      console.log("destroy Viewer");
      viewer.destroy();
      viewerRef.current = undefined;
    };
  });
  console.log("root2");

  const handleOnClick = () => {
    if (!viewerRef.current) return;
    viewerRef.current.view(0);
  };
  return (
    <div>
      <Button onClick={handleOnClick}>view</Button>
      <ul ref={ulRef}>
        {_ownerId && _scoreId ? (
          _pages.map((p) => {
            const customAttr = {
              "data-original": privateScoreItemUrlGen.getImageUrl(
                _ownerId,
                _scoreId,
                p
              ),
            };
            return (
              <li key={p.id}>
                <img
                  src={privateScoreItemUrlGen.getThumbnailImageUrl(
                    _ownerId,
                    _scoreId,
                    p
                  )}
                  {...customAttr}
                />
              </li>
            );
          })
        ) : (
          <></>
        )}
      </ul>
    </div>
  );
}
