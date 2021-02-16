import classes from "*.module.css";
import {
  Button,
  createStyles,
  Grid,
  IconButton,
  makeStyles,
  Paper,
  Theme,
  Typography,
} from "@material-ui/core";
import { AddIcon } from "@material-ui/data-grid";
import { Alert } from "@material-ui/lab";
import React, { useEffect } from "react";
import { useHistory } from "react-router-dom";
import { scoreClient } from "../../global";
import { PageOperation, ScorePage, ScoreSummarySet } from "../../ScoreClient";
import PageItem, { UploadedItem } from "../molecules/PageItem";
import { PathCreator } from "../pages/HomePage";

interface AfterPage {
  page?: ScorePage;
  ope?: {
    operation: PageOperation;
    index: number;
  };
}

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    pageItem: {
      // TODO PageItem が 300x300 px になっているので後で外部空変更できるようにする
      width: "300px",
      height: "300px",
    },
  })
);

// --------------------------------------------------------

export interface UpdatePageContentProps {
  owner: string;
  scoreName: string;
  pathCreator: PathCreator;
}

const UpdatePageContent = (props: UpdatePageContentProps) => {
  const _owner = props.owner;
  const _scoreName = props.scoreName;
  const _pathCreator = props.pathCreator;
  const [pages, setPages] = React.useState([] as ScorePage[]);
  const [operations, setOperations] = React.useState([] as PageOperation[]);
  const [updateErrorMessage, setUpdateErrorMessage] = React.useState<string>();
  const [loadScoreDataError, setLoadScoreDataError] = React.useState<string>();

  const history = useHistory();

  const classes = useStyles();

  const loadScoreData = async (owner: string, scoreName: string) => {
    try {
      const scoreData = await scoreClient.getScore(owner, scoreName);
      setPages(scoreData.pages);
      setLoadScoreDataError(undefined);
    } catch (err) {
      setLoadScoreDataError(`楽譜の情報取得に失敗しました`);
      console.log(err);
    }
  };

  useEffect(() => {
    loadScoreData(_owner, _scoreName);
  }, [_owner, _scoreName]);

  const handlerOnClickPage = () => {
    const newOperations = [...operations];

    const addOperaiont: PageOperation = {
      type: "add",
    };
    newOperations.push(addOperaiont);
    setOperations(newOperations);
  };

  const handleUpdateClick = async () => {
    // ここで更新
    try {
      await scoreClient.updatePages(_owner, _scoreName, operations);

      setUpdateErrorMessage(undefined);
      history.replace(_pathCreator.getDetailPath(_owner, _scoreName));
    } catch (err) {
      setUpdateErrorMessage(`ページの更新に失敗しました`);
    }
  };

  const afterPages = pages.map((page) => ({ page: page } as AfterPage));
  operations.forEach((ope, index) => {
    switch (ope.type) {
      case "add": {
        const newItem: AfterPage = {
          ope: {
            index: index,
            operation: ope,
          },
        };
        afterPages.push(newItem);
        break;
      }
      case "remove": {
        const opeIndex = ope.index;
        if (opeIndex === undefined) break;
        afterPages.splice(index, 1);
        break;
      }
      case "insert": {
        const opeIndex = ope.index;
        if (opeIndex === undefined) break;
        const newItem: AfterPage = {
          ope: {
            index: index,
            operation: ope,
          },
        };
        afterPages.splice(opeIndex, 0, newItem);
        break;
      }
    }
  });

  const disableUpdateButton = !(
    0 < operations.length &&
    operations.reduce((elm, ope, index) => {
      if (ope.type === "add") {
        return elm && !!ope.image;
      } else if (ope.type === "insert") {
        return elm && !!ope.image;
      } else if (ope.type === "update") {
        return elm && !!ope.image;
      }
      return elm && true;
    }, true)
  );
  return (
    <Grid container>
      <Grid item xs={12}>
        {updateErrorMessage ? (
          <Alert severity="error">{updateErrorMessage}</Alert>
        ) : (
          <></>
        )}
      </Grid>
      <Grid item xs={12}>
        <Grid container spacing={2}>
          {afterPages.map((page, index) => {
            if (page.page) {
              return (
                <Grid item key={index}>
                  <Paper className={classes.pageItem}>
                    <Grid container justify="center">
                      <Grid item xs={12} style={{ textAlign: "center" }}>
                        <img
                          src={page.page.thumbnail ?? page.page.image}
                          height={"200px"}
                          alt={page.page.number}
                          style={{ userSelect: "none" }}
                        />
                      </Grid>
                      <Grid item xs={12}>
                        <Typography align="center">
                          p. {page.page.number}
                        </Typography>
                      </Grid>
                    </Grid>
                  </Paper>
                </Grid>
              );
            } else {
              const operation = page.ope;
              if (!operation) {
                return <></>;
              }
              const operationListIndex = operation.index;
              const sourceOperations = operations;
              const handleOnUploaded = (
                lodedItem: UploadedItem | undefined
              ) => {
                if (lodedItem) {
                  const newOpe = {
                    ...sourceOperations[operationListIndex],
                  };
                  newOpe.image = lodedItem.imageHref;
                  newOpe.thumbnail = lodedItem.thumbnailHref;

                  const newOperations = [...sourceOperations];
                  newOperations[operationListIndex] = newOpe;
                  setOperations(newOperations);
                } else {
                  const newOpe = {
                    ...sourceOperations[operationListIndex],
                  };
                  newOpe.image = undefined;
                  newOpe.thumbnail = undefined;

                  const newOperations = [...sourceOperations];
                  newOperations[operationListIndex] = newOpe;
                  setOperations(newOperations);
                }
              };
              return (
                <Grid item key={index}>
                  <PageItem
                    className={classes.pageItem}
                    owner={_owner}
                    scoreName={_scoreName}
                    onUploaded={handleOnUploaded}
                  />
                </Grid>
              );
            }
          })}
          <Grid item>
            <IconButton onClick={handlerOnClickPage}>
              <AddIcon />
            </IconButton>
          </Grid>
        </Grid>
      </Grid>
      <Grid item xs={12}>
        <Button
          variant="outlined"
          disabled={disableUpdateButton}
          onClick={handleUpdateClick}
        >
          更新
        </Button>
      </Grid>
    </Grid>
  );
};

export default UpdatePageContent;
