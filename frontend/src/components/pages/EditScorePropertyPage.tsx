import {
  Button,
  createStyles,
  Grid,
  makeStyles,
  TextField,
  Theme,
} from "@material-ui/core";
import { Alert } from "@material-ui/lab";
import React, { useEffect, useState } from "react";
import { useHistory } from "react-router-dom";
import { scoreClient } from "../../global";
import { useScorePathParameter } from "../../hooks/scores/useScorePathParameter";
import PathCreator from "../../PathCreator";
import { ScoreProperty } from "../../ScoreClient";
import HomeTemplate from "../templates/HomeTemplate";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    textField: {
      width: "100%",
    },
  })
);

export default function EditScorePropertyPage() {
  const classes = useStyles();

  const pathParam = useScorePathParameter();

  const _owner = pathParam.owner;
  const _scoreName = pathParam.scoreName;
  const _pathCreator = new PathCreator();

  const [property, setProperty] = useState<ScoreProperty>({});
  const [newTitle, setNewTitle] = useState<string>("");
  const [newDescription, setNewDescription] = useState<string>("");
  const [errorMessage, setErrorMessage] = useState<string>();
  const [loadScoreDataError, setLoadScoreDataError] = useState<string>();

  const history = useHistory();

  const loadScoreData = async (owner: string, scoreName: string) => {
    try {
      const scoreData = await scoreClient.getScore(owner, scoreName);
      setProperty(scoreData.scoreSummary.property);
      setNewTitle(scoreData.scoreSummary.property?.title ?? "");
      setNewDescription(scoreData.scoreSummary.property?.description ?? "");
      setLoadScoreDataError(undefined);
    } catch (err) {
      setLoadScoreDataError(`楽譜の情報取得に失敗しました`);
      console.log(err);
    }
  };

  useEffect(() => {
    if (!_owner) return;
    if (!_scoreName) return;
    loadScoreData(_owner, _scoreName);
  }, [_owner, _scoreName]);

  const handleOnChangeTitle = (event: any) => {
    setNewTitle(event.target.value);
  };
  const handleOnChangeDescription = (event: any) => {
    setNewDescription(event.target.value);
  };

  const handleOnClickUpdate = async () => {
    if (!_owner) return;
    if (!_scoreName) return;
    const op = {
      ...property,
    };
    const np = {
      title: newTitle,
      description: newDescription,
    };
    try {
      await scoreClient.updateProperty(_owner, _scoreName, op, np);

      setErrorMessage(undefined);

      history.replace(_pathCreator.getDetailPath(_owner, _scoreName));
    } catch (err) {
      console.log(err);
      setErrorMessage("変更に失敗しました");
    }
  };

  const disableUpdateButton =
    (property.title ?? "") === (newTitle ?? "") &&
    (property.description ?? "") === (newDescription ?? "");

  return (
    <HomeTemplate>
      <Grid container spacing={3}>
        <Grid item xs={12}>
          {loadScoreDataError ? (
            <Alert severity="error">{loadScoreDataError}</Alert>
          ) : (
            <></>
          )}
        </Grid>
        <Grid item xs={12}>
          <form>
            <Grid container spacing={3}>
              <Grid item xs={12}>
                <TextField
                  label="楽譜のタイトル"
                  className={classes.textField}
                  value={newTitle}
                  onChange={handleOnChangeTitle}
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="楽譜の説明"
                  multiline
                  className={classes.textField}
                  value={newDescription}
                  onChange={handleOnChangeDescription}
                />
              </Grid>
            </Grid>
          </form>
        </Grid>
        <Grid item xs={12}>
          {errorMessage ? (
            <Alert severity="error">{errorMessage}</Alert>
          ) : (
            <></>
          )}
        </Grid>
        <Grid item xs={12}>
          <Button
            variant="outlined"
            color="primary"
            onClick={handleOnClickUpdate}
            disabled={disableUpdateButton}
          >
            更新
          </Button>
        </Grid>
      </Grid>
    </HomeTemplate>
  );
}
