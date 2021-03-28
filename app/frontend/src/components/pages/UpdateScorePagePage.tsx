import {
  Button,
  colors,
  createStyles,
  Grid,
  IconButton,
  makeStyles,
  Paper,
  Theme,
  Typography,
} from "@material-ui/core";
import { Alert } from "@material-ui/lab";
import React, { useEffect, useState } from "react";
import { useHistory } from "react-router-dom";
import { scoreClient } from "../../global";
import { PageOperation, ScorePage } from "../../ScoreClient";
import DeleteIcon from "@material-ui/icons/Delete";
import AddIcon from "@material-ui/icons/Add";
import PageItem, { UploadedItem } from "../molecules/PageItem";
import { useScorePathParameter } from "../../hooks/scores/useScorePathParameter";
import PathCreator from "../../PathCreator";
import HomeTemplate from "../templates/HomeTemplate";

interface AfterPage {
  page?: ScorePage;
  ope?: {
    operation: PageOperation;
    index: number;
  };
  key: string;
}

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    cardRoot: {
      width: "250px",
      height: "250px",
    },
    addPageItem: {
      display: "flex",
      justifyContent: "center",
      alignItems: "center",
      backgroundColor: colors.grey[100],
    },
    addPageItemButton: {
      height: "50px",
      width: "50px",
    },
    sourceImg: {
      height: "200px",
      userSelect: "none",
    },
    pageItem: { height: "200px" },
  })
);

export default function UpdateScorePagePage() {
  const pathParam = useScorePathParameter();
  const _owner = pathParam.owner;
  const _scoreName = pathParam.scoreName;
  const _pathCreator = new PathCreator();

  const [pages, setPages] = useState([] as ScorePage[]);
  const [operations, setOperations] = useState([] as PageOperation[]);
  const [updateErrorMessage, setUpdateErrorMessage] = useState<string>();
  const [
    loadScoreDataErrorMessage,
    setLoadScoreDataErrorMessage,
  ] = useState<string>();

  const history = useHistory();

  const classes = useStyles();

  const loadScoreData = async (owner: string, scoreName: string) => {
    try {
      const scoreData = await scoreClient.getScore(owner, scoreName);
      setPages(scoreData.pages);
      setLoadScoreDataErrorMessage(undefined);
    } catch (err) {
      setLoadScoreDataErrorMessage(`楽譜の情報取得に失敗しました`);
      console.log(err);
    }
  };

  useEffect(() => {
    if (!_owner) return;
    if (!_scoreName) return;
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
    if (!_owner) return;
    if (!_scoreName) return;
    // ここで更新
    try {
      await scoreClient.updatePages(_owner, _scoreName, operations);

      setUpdateErrorMessage(undefined);
      history.replace(_pathCreator.getDetailPath(_owner, _scoreName));
    } catch (err) {
      console.log(err);
      setUpdateErrorMessage(`ページの更新に失敗しました`);
    }
  };

  const afterPages = pages.map(
    (page, index) => ({ page: page, key: `src_${index}` } as AfterPage)
  );
  operations.forEach((ope, index) => {
    switch (ope.type) {
      case "add": {
        const newItem: AfterPage = {
          ope: {
            index: index,
            operation: ope,
          },
          key: `ope_add_${index}`,
        };
        afterPages.push(newItem);
        break;
      }
      case "remove": {
        const opeIndex = ope.index;
        if (opeIndex === undefined) break;
        afterPages.splice(opeIndex, 1);
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
          key: `ope_insert_${index}`,
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
    <HomeTemplate>
      <Grid container spacing={2}>
        <Grid item xs={12}>
          {updateErrorMessage ? (
            <Alert severity="error">{updateErrorMessage}</Alert>
          ) : (
            <></>
          )}
        </Grid>
        <Grid item xs={12}>
          {loadScoreDataErrorMessage ? (
            <Alert severity="error">{loadScoreDataErrorMessage}</Alert>
          ) : (
            <></>
          )}
        </Grid>
        <Grid item xs={12}>
          <Grid container spacing={2}>
            {afterPages.map((page, index) => {
              if (!_owner) return <></>;
              if (!_scoreName) return <></>;
              if (page.page) {
                const handleOnDeleteClick = () => {
                  const removeOpe: PageOperation = {
                    type: "remove",
                    index: index,
                  };
                  const newOperations = [...operations];
                  newOperations.push(removeOpe);
                  setOperations(newOperations);
                };

                return (
                  <Grid item key={page.key}>
                    <Paper className={classes.cardRoot}>
                      <Grid container justify="center">
                        <Grid item xs={12} style={{ textAlign: "center" }}>
                          <img
                            src={page.page.thumbnail ?? page.page.image}
                            alt={page.page.number}
                            className={classes.sourceImg}
                          />
                        </Grid>
                        <Grid item xs={12}>
                          <Typography align="center">
                            {page.page.number}
                          </Typography>
                        </Grid>
                        <Grid item xs={12}>
                          <Grid container justify="flex-end">
                            <Grid item>
                              <IconButton onClick={handleOnDeleteClick}>
                                <DeleteIcon />
                              </IconButton>
                            </Grid>
                          </Grid>
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
                const handleOnDeleteClick = () => {
                  const removeOpe: PageOperation = {
                    type: "remove",
                    index: index,
                  };
                  const newOperations = [...operations];
                  newOperations.push(removeOpe);
                  setOperations(newOperations);
                };
                return (
                  <Grid item key={page.key}>
                    <Paper className={classes.cardRoot}>
                      <Grid container justify="center">
                        <Grid item xs={12} style={{ textAlign: "center" }}>
                          <PageItem
                            className={classes.pageItem}
                            owner={_owner}
                            scoreName={_scoreName}
                            onUploaded={handleOnUploaded}
                          />
                        </Grid>
                        <Grid item xs={12}>
                          <Grid container justify="flex-end">
                            <Grid item>
                              <IconButton onClick={handleOnDeleteClick}>
                                <DeleteIcon />
                              </IconButton>
                            </Grid>
                          </Grid>
                        </Grid>
                      </Grid>
                    </Paper>
                  </Grid>
                );
              }
            })}
            <Grid item>
              <Paper
                variant="outlined"
                className={classes.cardRoot + " " + classes.addPageItem}
              >
                <IconButton
                  onClick={handlerOnClickPage}
                  className={classes.addPageItemButton}
                >
                  <AddIcon fontSize="large" />
                </IconButton>
              </Paper>
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
    </HomeTemplate>
  );
}
