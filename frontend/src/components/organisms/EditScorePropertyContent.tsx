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
import { ScoreProperty, ScoreSummarySet } from "../../ScoreClient";
import { PathCreator } from "../pages/HomePage";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    textField: {
      width: "100%",
    },
  })
);

export interface EditScorePropertyContentProps {
  owner: string;
  scoreName: string;
  pathCreator: PathCreator;
}

const EditScorePropertyContent = (props: EditScorePropertyContentProps) => {
  const classes = useStyles();

  const _owner = props.owner;
  const _scoreName = props.scoreName;
  const _pathCreator = props.pathCreator;

  const [property, setProperty] = useState<ScoreProperty>({});
  const [newProperty, setNewProperty] = useState<ScoreProperty>({});
  const [errorMessage, setErrorMessage] = useState<string>();
  const [loadScoreDataError, setLoadScoreDataError] = useState<string>();

  const history = useHistory();

  const loadScoreData = async (owner: string, scoreName: string) => {
    try {
      const scoreData = await scoreClient.getScore(owner, scoreName);
      setProperty(scoreData.scoreSummary.property);
      setNewProperty({ ...scoreData.scoreSummary.property });
      setLoadScoreDataError(undefined);
    } catch (err) {
      setLoadScoreDataError(`楽譜の情報取得に失敗しました`);
      console.log(err);
    }
  };

  useEffect(() => {
    loadScoreData(_owner, _scoreName);
  }, [_owner, _scoreName]);

  const handleOnChangeTitle = (event: any) => {
    setNewProperty({ ...newProperty, title: event.target.value });
  };
  const handleOnChangeDescription = (event: any) => {
    setNewProperty({ ...newProperty, description: event.target.value });
  };

  const handleOnClickUpdate = async () => {
    const op = {
      ...property,
    };
    const np = {
      ...newProperty,
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
    (property.title ?? "") === (newProperty.title ?? "") &&
    (property.description ?? "") === (newProperty.description ?? "");

  return (
    <Grid container spacing={3}>
      <Grid item xs={12}>
        <TextField
          label="楽譜のタイトル"
          className={classes.textField}
          value={newProperty.title}
          onChange={handleOnChangeTitle}
        />
      </Grid>
      <Grid item xs={12}>
        <TextField
          label="楽譜の説明"
          multiline
          className={classes.textField}
          value={newProperty.description}
          onChange={handleOnChangeDescription}
        />
      </Grid>
      <Grid item xs={12}>
        {errorMessage ? <Alert severity="error">{errorMessage}</Alert> : <></>}
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
  );
};

export default EditScorePropertyContent;
