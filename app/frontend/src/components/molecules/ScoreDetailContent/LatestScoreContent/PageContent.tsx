import {
  Button,
  createStyles,
  Dialog,
  IconButton,
  makeStyles,
  Paper,
  styled,
  Theme,
} from "@material-ui/core";
import { useMemo } from "react";
import { useHistory } from "react-router";
import { privateScoreItemUrlGen } from "../../../../global";
import { ScorePage } from "../../../../ScoreClientV2";
import "viewerjs/dist/viewer.min.css";
import { ViewContent } from "./PageContent/ViewContent";
import CloseIcon from "@material-ui/icons/Close";
import EditIcon from "@material-ui/icons/Edit";

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
    dialogContent: {
      width: "100%",
      height: "100%",
      display: "flex",
    },
    viewerContainer: {
      width: "calc(100% - 300px)",
      minWidth: "200px",
      height: "100%",
    },
    rightContainer: {
      width: "300px",
      height: "100%",
      backgroundColor: "#FFFFFF",
      display: "flex",
      flexFlow: "column",
    },
    controlBar: {
      width: "100%",
      height: "50px",
      display: "flex",
      justifyContent: "flex-end",
    },
    annotationContainer: {
      width: "100%",
      height: "calc(100% - 50px)",
    },
    thumbnailContainer: {
      display: "flex",
      width: "100%",
      flexWrap: "wrap",
    },
    pageContainer: {
      display: "flex",
      flexFlow: "column",
      justifyContent: "flex-start",
      "& img": {
        height: "200px",
        width: "auto",
      },
      "& p": {
        margin: 0,
      },
    },
    title: {
      width: "100%",
      height: "30px",
      display: "flex",
      alignItems: "center",
      "& > p": {
        margin: 0,
      },
      "& > button": {},
    },
  })
);

const CustomPaper = styled(Paper)({
  backgroundColor: "#00000000",
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

  /** key : page id , value : index */
  const pageIndexSet = useMemo(() => {
    const indexSet: { [id: string]: number } = {};

    if (_pages)
      _pages.forEach((p, index) => {
        indexSet[p.id] = index;
      });
    return indexSet;
  }, [_pages]);

  const pageIndex = _pageId !== undefined ? pageIndexSet[_pageId] : undefined;

  const handleOnCloseClick = () => {
    history.push(`/scores/${_scoreId}`);
  };

  const handleOnPageEditClick = () => {
    history.push(`/scores/${_scoreId}/edit-page`);
  };

  return (
    <div style={{ width: "100%" }}>
      <div className={classes.title}>
        <p>ページ</p>
        <IconButton onClick={handleOnPageEditClick} size="small">
          <EditIcon />
        </IconButton>
      </div>
      <div className={classes.thumbnailContainer}>
        {_ownerId && _scoreId ? (
          _pages.map((p) => {
            const thumbnailImgSrc = privateScoreItemUrlGen.getThumbnailImageUrl(
              _ownerId,
              _scoreId,
              p
            );
            const handleOnThumbnailClick = () => {
              history.push(`/scores/${_scoreId}/page/${p.id}`);
            };
            return (
              <div key={p.id}>
                <Button onClick={handleOnThumbnailClick}>
                  <Paper>
                    <div className={classes.pageContainer}>
                      <img src={thumbnailImgSrc} />
                      <p>{p.page}</p>
                    </div>
                  </Paper>
                </Button>
              </div>
            );
          })
        ) : (
          <></>
        )}
      </div>
      <Dialog
        open={_pageId ? true : false}
        PaperComponent={CustomPaper}
        fullScreen={true}
      >
        <div className={classes.dialogContent}>
          <div className={classes.viewerContainer}>
            <ViewContent
              ownerId={_ownerId}
              scoreId={_scoreId}
              pageIndex={pageIndex}
              pages={_pages}
            />
          </div>
          <div className={classes.rightContainer}>
            <div className={classes.controlBar}>
              <IconButton onClick={handleOnCloseClick} size="medium">
                <CloseIcon />
              </IconButton>
            </div>
            <div className={classes.annotationContainer}></div>
          </div>
        </div>
      </Dialog>
    </div>
  );
}
